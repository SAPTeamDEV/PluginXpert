using System.Reflection;
using System.Text.RegularExpressions;

using EasySign;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents methods for loading managed plugins.
/// </summary>
public class PluginManager<T, TGateway>
    where T : IPlugin<TGateway>
    where TGateway : IGateway
{
    bool throwOnFail;

    /// <summary>
    /// Gets the list of all plugins.
    /// </summary>
    public List<PluginContext<T, TGateway>> Plugins { get; } = new();

    /// <summary>
    /// Gets a list of all properly loaded plugins.
    /// </summary>
    public IEnumerable<PluginContext<T, TGateway>> ValidPlugins
    {
        get
        {
            foreach (var plugin in Plugins)
            {
                if (plugin.IsLoaded)
                {
                    yield return plugin;
                }
            }
        }
    }

    /// <summary>
    /// Gets the permission manager associated with this instance.
    /// </summary>
    public PermissionManager PermissionManager { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="PluginManager{T}"/> class.
    /// </summary>
    /// <param name="permissionManager">The permission manager that controls the plugin's permissions.</param>
    /// <param name="throwOnFail">Determines whether this instance should throw an error when a plugin can't be loaded.</param>
    public PluginManager(PermissionManager permissionManager = null, bool throwOnFail = false)
    {
        this.throwOnFail = throwOnFail;
        PermissionManager = permissionManager ?? new PermissionManager();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PluginManager{T}"/> class and loads plugins.
    /// </summary>
    /// <param name="directory">Directory of plugin assemblies.</param>
    /// <param name="namePattern">A regex pattern for selecting plugin assemblies.</param>
    /// <param name="permissionManager">The permission manager that controls the plugin's permissions.</param>
    /// <param name="throwOnFail">Determines whether this instance should throw an error when a plugin can't be loaded.</param>
    public PluginManager(string directory, string namePattern = "*.dll", PermissionManager permissionManager = null, bool throwOnFail = false) : this(permissionManager, throwOnFail)
    {
        AddPlugin(directory, namePattern);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PluginManager{T}"/> class and loads plugins.
    /// </summary>
    /// <param name="bundle">An eSign bundle with valid structure.</param>
    /// <param name="permissionManager">The permission manager that controls the plugin's permissions.</param>
    /// <param name="throwOnFail">Determines whether this instance should throw an error when a plugin can't be loaded.</param>
    public PluginManager(Bundle bundle, PermissionManager permissionManager = null, bool throwOnFail = false) : this(permissionManager, throwOnFail)
    {

    }

    /// <summary>
    /// Loads plugins and adds them to the <see cref="Plugins"/>. property.
    /// </summary>
    /// <param name="directory">Directory of plugin assemblies.</param>
    /// <param name="namePattern">A regex pattern for selecting plugin assemblies.</param>
    public List<PluginContext<T, TGateway>> AddPlugin(string directory, string namePattern = "*.dll")
    {
        var plugins = GetPlugins(directory, namePattern);
        Plugins.AddRange(plugins);
        return plugins;
    }

    /// <summary>
    /// Loads all plugins with the <see cref="IPlugin"/> type.
    /// </summary>
    /// <param name="directory">Directory of plugin assemblies.</param>
    /// <param name="namePattern">A regex pattern for selecting plugin assemblies.</param>
    /// <returns></returns>
    public List<PluginContext<T, TGateway>> GetPlugins(string directory, string namePattern = "*.dll")
    {
        List<PluginContext<T, TGateway>> plugins = Directory.EnumerateFiles(directory, namePattern).SelectMany(pluginPath =>
        {
            Assembly pluginAssembly = LoadAssembly(pluginPath);
            return InitializePlugins(pluginAssembly, ".*");
        }).ToList();

        return plugins;
    }

    private static Assembly LoadAssembly(string pluginLocation)
    {
        PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
    }

    IEnumerable<PluginContext<T, TGateway>> InitializePlugins(Assembly assembly, string searchPattern)
    {
        int count = 0;

        foreach (Type type in assembly.GetTypes())
        {
            if (!Regex.IsMatch(type.Name, searchPattern)) continue;

            if (typeof(T).IsAssignableFrom(type))
            {
                T result = (T)Activator.CreateInstance(type);
                if (result != null)
                {
                    PluginContext<T, TGateway> context = new(result, this, PermissionManager);
                    context.LoadPlugin();

                    count++;
                    yield return context;
                }
            }
        }

        if (count == 0)
        {
            string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
            throw new ApplicationException(
                $"Can't find any type which implements IPlugin in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");
        }
    }
}