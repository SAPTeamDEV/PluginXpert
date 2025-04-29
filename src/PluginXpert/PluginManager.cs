using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

using DouglasDwyer.CasCore;

using EnsureThat;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents methods for loading managed plugins.
/// </summary>
public sealed class PluginManager : IReadOnlyCollection<PluginImplementation>, IDisposable
{
    public static Version? RuntimeVersion { get; } = GetRuntimeVersion();

    private readonly Type _defaultPluginType = typeof(IPlugin);

    private readonly Dictionary<string, PluginImplementation> _implementations = [];

    public bool Disposed { get; private set; }

    public List<PluginLoadSession> LoadSessions { get; } = [];

    /// <summary>
    /// Gets the list of all plugins.
    /// </summary>
    public IEnumerable<PluginContext> Plugins => _implementations.Values.Where(x => !x.Disposed).SelectMany(x => x.Plugins);

    /// <summary>
    /// Gets a list of all properly loaded plugins.
    /// </summary>
    public IEnumerable<PluginContext> ValidPlugins => Plugins.Where(x => !x.Disposed && x.Valid);

    /// <summary>
    /// Gets the security context associated with this instance.
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

        string implId = impl.ToString();

        if (_implementations.ContainsKey(implId))
        {
            throw new InvalidOperationException($"Plugin implementation {implId} already registered");
        }

        impl.Initialize(SecurityContext);

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

        foreach (PluginEntry ent in package.PackageInfo.Plugins)
        {
            PluginLoadSession session = new PluginLoadSession(this, package, ent);
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

        List<PluginEntry> chosenEntries = [];
        foreach (Dictionary<string, PluginEntry> plg in loadCondidates.Values)
        {
            if (plg.Count == 1)
            {
                chosenEntries.Add(plg.First().Value);
                continue;
            }

            PluginEntry? chosenEnt = plg.Values
                .Where(v => v.TargetFrameworkVersion <= RuntimeVersion) // Only consider versions lower or equal to the runtime version
                .OrderByDescending(v => v.TargetFrameworkVersion) // Get the largest version among them
                .FirstOrDefault();

            if (chosenEnt != null) chosenEntries.Add(chosenEnt);
        }

        if (chosenEntries.Count == 0) throw new ArgumentException("Can't find any suitable plugins in this package");

        List<PluginContext> plugins = [];
        foreach (PluginEntry entry in chosenEntries)
        {
            PluginLoadSession session = LoadSessions.Where(x => x.Entry == entry).Single();

            _ = session.TryRun(() =>
            {
                string tempPath = session.Implementation!.TempPath;
                Directory.CreateDirectory(tempPath);

                package.ExtractPlugin(entry, tempPath);

                session.AssemblyPath = Path.Combine(tempPath, $"{entry.Id}-{entry.BuildRef}", entry.Assembly);
                session.Token = SecurityContext.RegisterPlugin(session);

                CasPolicyBuilder policyBuilder = new CasPolicyBuilder();
                session.Implementation.UpdateAssemblySecurityPolicy(session, policyBuilder);

                session.Loader = SecurityContext.CreateAssemblyLoader(session, policyBuilder);
                session.Assembly = session.Loader.LoadFromAssemblyPath(session.AssemblyPath);

                InitializePlugin(session);

                PluginContext context = new PluginContext(session);
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

    private void InitializePlugin(PluginLoadSession session)
    {
        if (session.Implementation == null)
        {
            throw new ArgumentException("Invalid session");
        }

        Assembly assembly = session.Assembly ?? throw new ArgumentException("Invalid session");
        Type[] assemblyTypes = assembly.GetTypes();
        if (assemblyTypes.Length == 0)
        {
            throw new PluginLoadException("No types found in assembly");
        }

        Type[] compatibleTypes = assemblyTypes.Where(x => x.IsClass && !x.IsAbstract && _defaultPluginType.IsAssignableFrom(x)).ToArray();
        if (compatibleTypes.Length == 0)
        {
            throw new PluginLoadException("No compatible types found in assembly");
        }

        Type? targetType = compatibleTypes.SingleOrDefault(x => x.Name == session.Entry.Class);
        if (targetType == null)
        {
            throw new PluginLoadException("Cannot find target type");
        }

        bool implCheck = session.Implementation.CheckPluginType(targetType);
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

    private static Version? GetRuntimeVersion()
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
        if (!Disposed)
        {
            LoadSessions.Clear();

            foreach (PluginImplementation impl in _implementations.Values)
            {
                impl.Dispose();
            }

            _implementations.Clear();

            SecurityContext.Dispose();

            Disposed = true;
        }
    }
}