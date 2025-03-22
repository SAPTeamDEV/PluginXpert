using SAPTeam.PluginXpert;
using SAPTeam.PluginXpert.Types;

namespace PluginXpert.ConsoleTest
{
    class LegacyTest
    {
#if DEBUG
        static string pluginPath = "..\\..\\..\\..\\..\\samples\\TestPlugin\\bin\\Debug\\net6.0";
#else
        static string pluginPath = "..\\..\\..\\..\\..\\samples\\TestPlugin\\bin\\Release\\net6.0";
#endif

        static void MainLegacy(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "/d")
                {
                    Console.WriteLine("Waiting for any key...");
                    Console.ReadLine();
                }

                PermissionManager.RegisterPermission(new("plugin", "test", "test"));

                var pm = new PluginManager<IPlugin<IGateway>, IGateway>(pluginPath.Replace('\\', Path.DirectorySeparatorChar), "TestPlugin.dll", throwOnFail: true);

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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}