using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using EasySign;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents methods for loading managed plugins.
/// </summary>
public class PluginManager<TPlugin, TGateway>
    where TPlugin : IPlugin<TGateway>
    where TGateway : IGateway
{
    bool throwOnFail;

    public virtual string Interface => "pluginxpert";

    public virtual Version Version => new Version(2, 0);

    /// <summary>
    /// Gets the list of all plugins.
    /// </summary>
    public Dictionary<string, PluginContext<TPlugin, TGateway>> Plugins { get; } = new();

    /// <summary>
    /// Gets a list of all properly loaded plugins.
    /// </summary>
    public PluginContext<TPlugin, TGateway>[] ValidPlugins
    {
        get
        {
            return Plugins.Values.Where(x => x.IsLoaded).ToArray();
        }
    }

    /// <summary>
    /// Gets the permission manager associated with this instance.
    /// </summary>
    public PermissionManager PermissionManager { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="PluginManager{TPlugin, TGateway}"/> class.
    /// </summary>
    /// <param name="permissionManager">The permission manager that controls the plugin's permissions.</param>
    /// <param name="throwOnFail">Determines whether this instance should throw an error when a plugin can't be loaded. For example, plugin crash or empty assemblies.</param>
    public PluginManager(PermissionManager permissionManager = null, bool throwOnFail = false)
    {
        this.throwOnFail = throwOnFail;
        PermissionManager = permissionManager ?? new PermissionManager();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PluginManager{TPlugin, TGateway}"/> class and loads plugins.
    /// </summary>
    /// <param name="directory">Directory of plugin assemblies.</param>
    /// <param name="namePattern">A regex pattern for selecting plugin assemblies.</param>
    /// <param name="permissionManager">The permission manager that controls the plugin's permissions.</param>
    /// <param name="throwOnFail">Determines whether this instance should throw an error when a plugin can't be loaded. For example, plugin crash or empty assemblies.</param>
    public PluginManager(string directory, string namePattern = "*.dll", PermissionManager permissionManager = null, bool throwOnFail = false) : this(permissionManager, throwOnFail)
    {
        LoadPlugins(directory, namePattern);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PluginManager{TPlugin, TGateway}"/> class and loads plugins.
    /// </summary>
    /// <param name="package">A valid plugin package. File verification is done through plugin loading, but signature verification is not performed.</param>
    /// <param name="permissionManager">The permission manager that controls the plugin's permissions.</param>
    /// <param name="throwOnFail">Determines whether this instance should throw an error when a plugin can't be loaded. For example, plugin crash or empty assemblies.</param>
    public PluginManager(PluginPackage package, PermissionManager permissionManager = null, bool throwOnFail = false) : this(permissionManager, throwOnFail)
    {
        LoadPlugins(package);
    }

    /// <summary>
    /// Loads all plugins with the <typeparamref name="TPlugin"/> type.
    /// </summary>
    /// <param name="directory">Directory of plugin assemblies.</param>
    /// <param name="namePattern">A regex pattern for selecting plugin assemblies.</param>
    /// <returns></returns>
    public PluginContext<TPlugin, TGateway>[] LoadPlugins(string directory, string namePattern = "*.dll")
    {
        return Directory.EnumerateFiles(directory, namePattern).SelectMany(pluginPath =>
        {
            Assembly pluginAssembly = LoadAssembly(pluginPath);
            return InitializePlugins(pluginAssembly, ".*");
        }).ToArray();
    }

    public PluginContext<TPlugin, TGateway>[] LoadPlugins(PluginPackage package)
    {
        if (package == null) throw new ArgumentNullException(nameof(package));

        if (!package.ReadOnly) throw new ArgumentException("Cannot load a mutable package");

        if (!package.VerifyPackageInfo()) throw new InvalidDataException($"Invalid package info");

        Version runtimeVersion = GetRuntimeVersion();
        if (runtimeVersion == null) throw new ApplicationException("Cannot determine the runtime version");

        Dictionary<string, Dictionary<string, PluginEntry>> loadCondidates = new();

        foreach (var ent in package.PackageInfo.Plugins)
        {
            if (ent.Interface != Interface) continue;
            if (ent.InterfaceVersion != Version) continue;

            if (ent.TargetFrameworkVersion > runtimeVersion) continue;

            if (!loadCondidates.ContainsKey(ent.Id))
            {
                loadCondidates[ent.Id] = new();
            }

            if (loadCondidates[ent.Id].ContainsKey(ent.BuildRef))
            {
                throw new InvalidOperationException("Cannot load duplicated plugins");
            }

            loadCondidates[ent.Id][ent.BuildRef] = ent;
        }

        List<PluginEntry> chosenEntries = new();
        foreach (var plg in loadCondidates.Values)
        {
            if (plg.Count == 1)
            {
                chosenEntries.Add(plg.First().Value);
            }

            var chosenEnt = plg.Values
                .Where(v => v.TargetFrameworkVersion <= runtimeVersion) // Only consider versions lower or equal to the runtime version
                .OrderByDescending(v => v.TargetFrameworkVersion) // Get the largest version among them
                .FirstOrDefault();

            if (chosenEnt != null) chosenEntries.Add(chosenEnt);
        }

        if (chosenEntries.Count == 0) throw new ArgumentException("Can't find any suitable plugins in this package");

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        List<PluginContext<TPlugin, TGateway>> plugins = new();
        foreach (var entry in chosenEntries)
        {
            var pluginPath = Path.Combine(tempPath, Guid.NewGuid().ToString());

            Directory.CreateDirectory(pluginPath);

            package.ExtractPlugin(entry, pluginPath);

            var assembly = LoadAssembly(Path.Combine(pluginPath, entry.Assembly));

            plugins.AddRange(InitializePlugins(assembly, entry.Class, package));
        }

        return plugins.ToArray();
    }

    private static Assembly LoadAssembly(string pluginLocation)
    {
        PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
    }

    IEnumerable<PluginContext<TPlugin, TGateway>> InitializePlugins(Assembly assembly, string searchPattern, PluginPackage package = null)
    {
        int count = 0;

        foreach (Type type in assembly.GetTypes())
        {
            if (!Regex.IsMatch(type.Name, searchPattern)) continue;

            if (typeof(TPlugin).IsAssignableFrom(type))
            {
                TPlugin result = (TPlugin)Activator.CreateInstance(type);
                if (result != null)
                {
                    PluginContext<TPlugin, TGateway> context = new(result, this, PermissionManager);
                    context.LoadPlugin(throwOnFail);

                    Plugins[context.Id] = context;

                    count++;
                    yield return context;
                }
            }
        }

        if (throwOnFail && count == 0)
        {
            string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
            throw new ApplicationException(
                $"Can't find any type which implements {typeof(TPlugin)} in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");
        }
    }

    static Version GetRuntimeVersion()
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
}