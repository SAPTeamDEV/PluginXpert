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
public class PluginManager : IReadOnlyCollection<PluginImplementation>, IDisposable
{
    private bool _disposed;
    private readonly bool _throwOnFail;

    private List<string> _temporaryDirectories = [];
    private List<CasAssemblyLoader> _loadContexts = [];
    private Dictionary<string, PluginImplementation> _implementations = [];

    /// <summary>
    /// Gets the list of all plugins.
    /// </summary>
    public Dictionary<string, PluginContext> Plugins { get; } = [];

    /// <summary>
    /// Gets a list of all properly loaded plugins.
    /// </summary>
    public PluginContext[] ValidPlugins
    {
        get
        {
            return Plugins.Values.Where(x => x.IsLoaded).ToArray();
        }
    }

    /// <summary>
    /// Gets the permission manager associated with this instance.
    /// </summary>
    public SecurityContext SecurityContext { get; }
    
    public int Count => _implementations.Count;

    /// <summary>
    /// Creates a new instance of the <see cref="PluginManager"/> class.
    /// </summary>
    /// <param name="permissionManager">The permission manager that controls the plugin's permissions.</param>
    /// <param name="throwOnFail">Determines whether this instance should throw an error when a plugin can't be loaded. For example, plugin crash or empty assemblies.</param>
    public PluginManager(SecurityContext permissionManager = null, bool throwOnFail = false)
    {
        _throwOnFail = throwOnFail;
        SecurityContext = permissionManager ?? new SecurityContext();
    }

    public void Add(PluginImplementation impl)
    {
        Ensure.Any.IsNotNull(impl);

        var implId = impl.ToString();

        if (_implementations.ContainsKey(implId))
        {
            throw new InvalidOperationException($"Plugin implementation {implId} already registered");
        }

        if (impl.PluginManager != null)
        {
            throw new InvalidOperationException($"Plugin implementation {implId} already registered in another manager");
        }

        impl.PluginManager = this;
        _implementations[implId] = impl;
    }

    public PluginContext[] LoadPlugins(PluginPackage package)
    {
        Ensure.Any.IsNotNull(package);

        if (_implementations.Count == 0)
        {
            throw new InvalidOperationException("No plugin implementations registered");
        }

        if (!package.ReadOnly) throw new ArgumentException("Cannot load a mutable package");

        if (!package.VerifyPackageInfo()) throw new InvalidDataException($"Invalid package info");

        Version runtimeVersion = GetRuntimeVersion();
        if (runtimeVersion == null) throw new ApplicationException("Cannot determine the runtime version");

        Dictionary<string, Dictionary<string, PluginEntry>> loadCondidates = new();

        foreach (var ent in package.PackageInfo.Plugins)
        {
            if (ResolveImplementation(ent) == null) continue;

            if (ent.TargetFrameworkVersion > runtimeVersion) continue;

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
                .Where(v => v.TargetFrameworkVersion <= runtimeVersion) // Only consider versions lower or equal to the runtime version
                .OrderByDescending(v => v.TargetFrameworkVersion) // Get the largest version among them
                .FirstOrDefault();

            if (chosenEnt != null) chosenEntries.Add(chosenEnt);
        }

        if (chosenEntries.Count == 0) throw new ArgumentException("Can't find any suitable plugins in this package");

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _temporaryDirectories.Add(tempPath);

        List<PluginContext> plugins = new();
        foreach (var entry in chosenEntries)
        {
            Directory.CreateDirectory(tempPath);

            package.ExtractPlugin(entry, tempPath);

            var entryPointPath = Path.Combine(tempPath, $"{entry.Id}-{entry.BuildRef}", entry.Assembly);

            var loader = SecurityContext.CreateAssemblyLoader(entry);
            var assembly = LoadAssembly(loader, entryPointPath);

            plugins.AddRange(InitializePlugins(assembly, entry.Class, entry));
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

    protected bool SafeRun(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch
        {
            if (_throwOnFail) throw;

            return false;
        }
    }

    private Assembly LoadAssembly(CasAssemblyLoader loadContext, string pluginLocation)
    {
        _loadContexts.Add(loadContext);

        return loadContext.LoadFromAssemblyPath(pluginLocation);
    }

    IEnumerable<PluginContext> InitializePlugins(Assembly assembly, string searchPattern, PluginEntry entry)
    {
        PluginImplementation? impl = ResolveImplementation(entry);
        if (impl == null)
        {
            if (_throwOnFail) throw new NotSupportedException($"The plugin implementation {entry.Interface} v{entry.InterfaceVersion} is not supported");

            yield break;
        }

        int count = 0;

        foreach (Type type in assembly.GetTypes())
        {
            if (!Regex.IsMatch(type.Name, searchPattern)) continue;

            PluginContext? context;
            if ((context = impl.LoadPlugin(type, entry, _throwOnFail)) != null)
            {
                Plugins[context.Token.UniqueIdentifier] = context;

                count++;
                yield return context;
            }
        }

        if (_throwOnFail && count == 0)
        {
            string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
            throw new ApplicationException(
                $"Can't find any type which implements {impl.Interface} v{impl.Version} in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");
        }
    }

    public static Version GetRuntimeVersion()
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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var impl in _implementations.Values)
                {
                    impl.Dispose();
                }
                _implementations.Clear();

                foreach (var loader in _loadContexts)
                {
                    loader.Unload();
                }
                _loadContexts.Clear();

                foreach (var dir in _temporaryDirectories)
                {
                    Directory.Delete(dir, true);
                }
                _temporaryDirectories.Clear();

                SecurityContext.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}