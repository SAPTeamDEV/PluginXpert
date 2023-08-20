using System.Reflection;

namespace SAPTeam.PluginXpert;

public class PluginManager
{
    public static List<T> GetPlugins<T>(string directory, string namePattern = "*.dll")
        where T : new()
    {
        IEnumerable<T> commands = Directory.EnumerateFiles(directory, namePattern).SelectMany(pluginPath =>
        {
            Assembly pluginAssembly = LoadPlugin(pluginPath);
            return CreateCommands<T>(pluginAssembly);
        }).ToList();

        return (List<T>)commands;
    }

    private static Assembly LoadPlugin(string pluginLocation)
    {
        PluginLoadContext loadContext = new(pluginLocation);
        return loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pluginLocation));
    }

    private static IEnumerable<T> CreateCommands<T>(Assembly assembly)
    {
        int count = 0;

        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(T).IsAssignableFrom(type))
            {
                if (Activator.CreateInstance(type) is T result)
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
                $"Can't find any type which implements ICommand in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");
        }
    }
}