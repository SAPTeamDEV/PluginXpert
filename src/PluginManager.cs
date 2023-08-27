using System.Reflection;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents methods for loading managed plugins.
/// </summary>
public class PluginManager
{   public static PluginManager Global { get; set; }

    /// <summary>
    /// Gets the list of loaded plugins.
    /// </summary>
    public List<IPlugin> Plugins { get; }

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
        Plugins = GetPlugins(directory, namePattern);
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
    /// <param name="permissionManager">The permision manager that controls the plugin's permissions.
    /// This argument only applies to <see cref="IPlugin"/> managed plugins.</param>
    /// <returns></returns>
    public List<IPlugin> GetPlugins(string directory, string namePattern = "*.dll")
    {
        List<IPlugin> commands = Directory.EnumerateFiles(directory, namePattern).SelectMany(pluginPath =>
        {
            Assembly pluginAssembly = LoadPlugin(pluginPath);
            return CreateCommands(pluginAssembly);
        }).ToList();

        return commands;
    }

    static Assembly LoadPlugin(string pluginLocation)
    {
        PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
    }

    static IEnumerable<IPlugin> CreateCommands(Assembly assembly)
    {
        int count = 0;

        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(IPlugin).IsAssignableFrom(type))
            {
                IPlugin result = Activator.CreateInstance(type) as IPlugin;
                if (result != null)
                {
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