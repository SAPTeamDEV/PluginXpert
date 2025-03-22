using SAPTeam.PluginXpert;
using SAPTeam.PluginXpert.Types;

namespace PluginXpert.ConsoleTest
{
    class Program
    {
        static string pluginPath = "..\\..\\..\\..\\..\\samples\\TestPlugin\\bin\\sample.plx";

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "/d")
                {
                    Console.WriteLine("Waiting for any key...");
                    Console.ReadLine();
                }

                PermissionManager.RegisterPermission(new("plugin", "test", "test"));
                var package = new PluginPackage(pluginPath);
                package.Load();

                var pm = new PluginManager<IPlugin<IGateway>, IGateway>(package, throwOnFail: true);

                var commands = pm.ValidPlugins;

                if (args.Length == 0)
                {
                    Console.WriteLine("Commands: ");
                    foreach (var command in commands)
                    {
                        Console.WriteLine($"{command.Instance.Name}");
                    }
                }
                else
                {
                    foreach (string commandName in args)
                    {
                        Console.WriteLine($"-- {commandName} --");

                        var command = commands.FirstOrDefault(c => c.Instance.Name == commandName);
                        if (command == null)
                        {
                            Console.WriteLine("No such command is known.");
                            return;
                        }

                        command.Instance.Run();

                        Console.WriteLine();
                    }
                }

                pm.Cleanup();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}