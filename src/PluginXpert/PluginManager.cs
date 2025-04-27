using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

using DouglasDwyer.CasCore;

using EnsureThat;

using SAPTeam.EasySign;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents methods for loading managed plugins.
/// </summary>
public sealed class PluginManager : IReadOnlyCollection<PluginImplementation>, IDisposable
{
    public static Version? RuntimeVersion { get; } = GetRuntimeVersion();

    private bool _disposed;
    private Type _defaultPluginType = typeof(IPlugin);

    private Dictionary<string, PluginImplementation> _implementations = [];

    public bool Disposed => _disposed;

    public List<PluginLoadSession> LoadSessions { get; } = [];

    /// <summary>
    /// Gets the list of all plugins.
    /// </summary>
    public IEnumerable<PluginContext> Plugins => _implementations.Values.Where(x => !x.Disposed).SelectMany(x => x.Plugins);

    /// <summary>
    /// Gets a list of all properly loaded plugins.
    /// </summary>
    public IEnumerable<PluginContext> ValidPlugins
    {
        get
        {
            return Plugins.Where(x => !x.Disposed && x.Valid);
        }
    }

    /// <summary>
    /// Gets the permission manager associated with this instance.
    /// </summary>
    public SecurityContext SecurityContext { get; }
    
    public int Count => _implementations.Count;

    public PluginManager(SecurityContext? securityContext = null)
    {
        if (RuntimeVersion == null)
        {
            throw new ApplicationException("Cannot determine the runtime version");
        }

        SecurityContext = securityContext ?? new();
    }

    public void Add(PluginImplementation impl)
    {
        Ensure.Any.IsNotNull(impl);

        var implId = impl.ToString();

        if (_implementations.ContainsKey(implId))
        {
            throw new InvalidOperationException($"Plugin implementation {implId} already registered");
        }

        _implementations[implId] = impl;
    }

    public PluginContext[] LoadPlugins(PluginPackage package)
    {
        Ensure.Any.IsNotNull(package);

        if (_implementations.Count == 0)
        {
            throw new ApplicationException("No plugin implementations registered");
        }

        if (!package.Loaded) throw new ArgumentException("Package is not loaded");

        if (!package.ReadOnly) throw new ArgumentException("Cannot use a mutable package");

        if (!package.VerifyPackageInfo()) throw new InvalidDataException($"Invalid package info");

        Dictionary<string, Dictionary<string, PluginEntry>> loadCondidates = [];

        foreach (var ent in package.PackageInfo.Plugins)
        {
            var session = new PluginLoadSession(this, package, ent);
            LoadSessions.Add(session);

            PluginImplementation? impl;
            if ((impl = ResolveImplementation(ent)) == null)
            {
                session.Result = PluginLoadResult.NotSupportedImplementation;
                continue;
            }

            session.Implementation = impl;

            if (session.Implementation.Where(x => x.Id == ent.Id).Any())
            {
                session.Result = PluginLoadResult.AlreadyLoaded;
                continue;
            }

            if (ent.TargetFrameworkVersion > RuntimeVersion)
            {
                session.Result = PluginLoadResult.NotSupportedRuntime;
                continue;
            }

            if (!loadCondidates.TryGetValue(ent.Id, out Dictionary<string, PluginEntry>? value))
            {
                value = [];
                loadCondidates[ent.Id] = value;
            }

            if (value.ContainsKey(ent.BuildRef))
            {
                throw new InvalidOperationException("Cannot load duplicated plugins");
            }

            value[ent.BuildRef] = ent;
        }

        List<PluginEntry> chosenEntries = new();
        foreach (var plg in loadCondidates.Values)
        {
            if (plg.Count == 1)
            {
                chosenEntries.Add(plg.First().Value);
                continue;
            }

            var chosenEnt = plg.Values
                .Where(v => v.TargetFrameworkVersion <= RuntimeVersion) // Only consider versions lower or equal to the runtime version
                .OrderByDescending(v => v.TargetFrameworkVersion) // Get the largest version among them
                .FirstOrDefault();

            if (chosenEnt != null) chosenEntries.Add(chosenEnt);
        }

        if (chosenEntries.Count == 0) throw new ArgumentException("Can't find any suitable plugins in this package");

        List<PluginContext> plugins = [];
        foreach (var entry in chosenEntries)
        {
            var session = LoadSessions.Where(x => x.Entry == entry).Single();

            _ = session.TryRun(() =>
            {
                var tempPath = session.Implementation!.TempPath;
                Directory.CreateDirectory(tempPath);

                package.ExtractPlugin(entry, tempPath);

                session.AssemblyPath = Path.Combine(tempPath, $"{entry.Id}-{entry.BuildRef}", entry.Assembly);
                session.Token = SecurityContext.RegisterPlugin(session);
                session.Loader = SecurityContext.CreateAssemblyLoader(session);
                session.Assembly = session.Loader.LoadFromAssemblyPath(session.AssemblyPath);

                InitializePlugin(session);

                var context = new PluginContext(session);
                session.Implementation.Add(context);
                plugins.Add(context);
            });
        }

        return plugins.ToArray();
    }

    public PluginImplementation? ResolveImplementation(PluginEntry pluginEntry)
    {
        return this.Where(x => x.Interface == pluginEntry.Interface)
                        .Where(x => pluginEntry.InterfaceVersion >= x.MinimumVersion && pluginEntry.InterfaceVersion <= x.Version)
                        .OrderByDescending(x => x.Version)
                        .FirstOrDefault();
    }

    void InitializePlugin(PluginLoadSession session)
    {
        if (session.Implementation == null)
        {
            throw new ArgumentException("Invalid session");
        }

        Assembly assembly = session.Assembly ?? throw new ArgumentException("Invalid session");
        var assemblyTypes = assembly.GetTypes();
        if (assemblyTypes.Length == 0)
        {
            throw new PluginLoadException("No types found in assembly");
        }

        var compatibleTypes = assemblyTypes.Where(x => x.IsClass && !x.IsAbstract && _defaultPluginType.IsAssignableFrom(x)).ToArray();
        if (compatibleTypes.Length == 0)
        {
            throw new PluginLoadException("No compatible types found in assembly");
        }

        var targetType = compatibleTypes.SingleOrDefault(x => x.Name == session.Entry.Class);
        if (targetType == null)
        {
            throw new PluginLoadException("Cannot find target type");
        }

        var implCheck = session.Implementation.CheckPluginType(targetType);
        if (!implCheck)
        {
            throw new PluginLoadException("Plugin type is not compatible with the implementation");
        }

        IPlugin? instance = (IPlugin?)Activator.CreateInstance(targetType);
        if (instance == null)
        {
            throw new PluginLoadException("Cannot create plugin instance");
        }

        session.Instance = instance;
        session.Gateway = session.Implementation.CreateGateway(session);
        session.Instance.OnLoad(session.Gateway);

        session.Result = PluginLoadResult.Success;
    }

    static Version? GetRuntimeVersion()
    {
        string frameworkDescription = RuntimeInformation.FrameworkDescription;

        if (frameworkDescription.StartsWith(".NET"))
        {
            string[] parts = frameworkDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return Version.TryParse(parts[1], out Version version) ? version : null;
            }
        }
        return null;
    }

    public IEnumerator<PluginImplementation> GetEnumerator() => _implementations.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _implementations.Values.GetEnumerator();

    public void Dispose()
    {
        if (!_disposed)
        {
            LoadSessions.Clear();

            foreach (var impl in _implementations.Values)
            {
                impl.Dispose();
            }
            _implementations.Clear();

            SecurityContext.Dispose();

            _disposed = true;
        }
    }
}