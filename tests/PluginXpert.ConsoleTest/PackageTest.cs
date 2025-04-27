using SAPTeam.PluginXpert;
using SAPTeam.PluginXpert.Types;

namespace PluginXpert.ConsoleTest;

class Program
{
    static string pluginPath = "..\\..\\..\\..\\..\\samples\\TestPlugin\\bin\\sample.plx";

    static void Main(string[] args)
    {
        SecurityContext securityContext = new();
        securityContext.RegisterPermission(new("plugin", "test", "Test", "Ability to test"));

        var pm = new PluginManager(securityContext)
        {
            new DefaultPluginImplementation()
        };

        try
        {
            if (args.Length == 1 && args[0] == "/d")
            {
                Console.WriteLine("Waiting for any key...");
                Console.ReadLine();
            }

            var package = new PluginPackage(pluginPath);
            package.LoadFromFile();

            pm.LoadPlugins(package);

            var commands = pm.ValidPlugins;

            if (args.Length == 0)
            {
                Console.WriteLine("Commands: ");
                foreach (var command in commands)
                {
                    Console.WriteLine($"{command.Id}");
                }
            }
            else
            {
                foreach (string commandName in args)
                {
                    Console.WriteLine($"-- {commandName} --");

                    var command = commands.FirstOrDefault(c => c.Id == commandName);
                    if (command == null)
                    {
                        Console.WriteLine("No such command is known.");
                    }

                    command?.Instance?.Run();

                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine();
        Console.WriteLine("Cleaning up...");
        pm.Dispose();
    }
}