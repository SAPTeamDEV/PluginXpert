using System.Reflection;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents methods for loading generic or managed plugins.
/// </summary>
/// <typeparam name="T">A class that implements <see cref="IPlugin"/> and has a parameterless constructor.</typeparam>
public class PluginManager<T>
    where T : IPlugin, new()
{
    public static PluginManager<T> Global { get; set; }

    /// <summary>
    /// Gets the list of loaded plugins.
    /// </summary>
    public List<T> Plugins { get; }

    /// <summary>
    /// Gets the permission manager assosiated with this instance.
    /// </summary>
    public PermissionManager PermissionManager { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="PluginManager{T}"/> and loads plugins.
    /// </summary>
    /// <param name="directory">Directory of plugin assemblies.</param>
    /// <param name="namePattern">A regex pattern for selecting plugin assemblies.</param>
    /// <param name="permissionManager">The permision manager that controls the plugin's permissions.</param>
    public PluginManager(string directory, string namePattern = "*.dll", PermissionManager permissionManager = null)
    {
        PermissionManager = permissionManager ?? new PermissionManager();
        Plugins = GetPlugins<T>(directory, namePattern);
    }

    /// <summary>
    /// Loads plugins and adds them to the <see cref="Plugins"/>. property.
    /// </summary>
    /// <param name="directory">Directory of plugin assemblies.</param>
    /// <param name="namePattern">A regex pattern for selecting plugin assemblies.</param>
    public List<T> AddPlugin(string directory, string namePattern = "*.dll")
    {
        var plugins = GetPlugins<T>(directory, namePattern);
        Plugins.AddRange(plugins);
        return plugins;
    }

    /// <summary>
    /// Loads all plugins with the given <typeparamref name="T"/> type.
    /// </summary>
    /// <typeparam name="TPlugin">Type of objects that would be loaded.</typeparam>
    /// <param name="directory">Directory of plugin assemblies.</param>
    /// <param name="namePattern">A regex pattern for selecting plugin assemblies.</param>
    /// <param name="permissionManager">The permision manager that controls the plugin's permissions.
    /// This argument only applies to <see cref="IPlugin"/> managed plugins.</param>
    /// <returns></returns>
    public static List<TPlugin> GetPlugins<TPlugin>(string directory, string namePattern = "*.dll", PermissionManager permissionManager = null)
        where TPlugin : new()
    {
        IEnumerable<TPlugin> commands = Directory.EnumerateFiles(directory, namePattern).SelectMany(pluginPath =>
        {
            Assembly pluginAssembly = LoadPlugin(pluginPath);
            return CreateCommands<TPlugin>(pluginAssembly, permissionManager);
        }).ToList();

        return (List<TPlugin>)commands;
    }

    private static Assembly LoadPlugin(string pluginLocation)
    {
        PluginLoadContext loadContext = new(pluginLocation);
        return loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pluginLocation));
    }

    private static IEnumerable<TPlugin> CreateCommands<TPlugin>(Assembly assembly, PermissionManager permissionManager = null)
    {
        int count = 0;

        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(TPlugin).IsAssignableFrom(type))
            {
                if (Activator.CreateInstance(type) is TPlugin result)
                {
                    if (result is IPlugin plugin)
                    {
                        try
                        {
                            permissionManager.RegisterPlugin(plugin);
                            plugin.OnLoad();
                            plugin.IsLoaded = true;
                        }
                        catch (Exception e)
                        {
                            plugin.IsLoaded = false;
                            plugin.Exception = e;
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
                $"Can't find any type which implements {typeof(T).Name} in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");
        }
    }
}