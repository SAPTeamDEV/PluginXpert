using System.Reflection;
using System.Text.RegularExpressions;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents methods for loading managed plugins.
/// </summary>
public class PluginManager
{
    bool throwOnFail;

    /// <summary>
    /// Gets the list of all plugins.
    /// </summary>
    public List<IPlugin> Plugins { get; } = new();

    /// <summary>
    /// Gets a list of plugins with <see cref="IPlugin.IsLoaded"/> property.
    /// </summary>
    public IEnumerable<IPlugin> ValidPlugins
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
    /// Creates a new instance of the <see cref="PluginManager"/> class.
    /// </summary>
    /// <param name="permissionManager">The permission manager that controls the plugin's permissions.</param>
    /// <param name="throwOnFail">Determines whether this instance should throw an error when a plugin can't be loaded.</param>
    public PluginManager(PermissionManager permissionManager = null, bool throwOnFail = false)
    {
        this.throwOnFail = throwOnFail;
        PermissionManager = permissionManager ?? new PermissionManager();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PluginManager"/> class and loads plugins.
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
    /// Loads plugins and adds them to the <see cref="Plugins"/>. property.
    /// </summary>
    /// <param name="directory">Directory of plugin assemblies.</param>
    /// <param name="namePattern">A regex pattern for selecting plugin assemblies.</param>
    public List<IPlugin> AddPlugin(string directory, string namePattern = "*.dll")
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
    public List<IPlugin> GetPlugins(string directory, string namePattern = "*.dll")
    {
        List<IPlugin> plugins = Directory.EnumerateFiles(directory, namePattern).SelectMany(pluginPath =>
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

    IEnumerable<IPlugin> InitializePlugins(Assembly assembly, string searchPattern)
    {
        int count = 0;

        foreach (Type type in assembly.GetTypes())
        {
            if (!Regex.IsMatch(type.Name, searchPattern)) continue;

            if (typeof(IPlugin).IsAssignableFrom(type))
            {
                IPlugin result = Activator.CreateInstance(type) as IPlugin;
                if (result != null)
                {
                    try
                    {
                        PermissionManager.RegisterPlugin(result);
                        result.OnLoad();
                        result.IsLoaded = true;
                    }
                    catch (Exception e)
                    {
                        result.IsLoaded = false;
                        result.Exception = e;

                        if (throwOnFail)
                        {
                            throw;
                        }
                    }

                    count++;
                    yield return result;
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