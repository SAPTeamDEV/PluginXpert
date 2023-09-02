using SAPTeam.PluginXpert;
using SAPTeam.PluginXpert.Types;

namespace PluginXpert.ConsoleTest
{
    class Program
    {
#if DEBUG
        static string pluginPath = "..\\..\\..\\..\\..\\samples\\TestPlugin\\bin\\Debug\\net6.0";
#else
        static string pluginPath = "..\\..\\..\\..\\..\\samples\\TestPlugin\\bin\\Release\\net6.0";
#endif

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

                var pm = new PluginManager(pluginPath.Replace('\\', Path.DirectorySeparatorChar), "TestPlugin.dll", throwOnFail: true);

                var commands = pm.ValidPlugins;

                if (args.Length == 0)
                {
                    Console.WriteLine("Commands: ");
                    foreach (IPlugin command in commands)
                    {
                        Console.WriteLine($"{command.Name}");
                    }
                }
                else
                {
                    foreach (string commandName in args)
                    {
                        Console.WriteLine($"-- {commandName} --");

                        IPlugin? command = commands.FirstOrDefault(c => c.Name == commandName);
                        if (command == null)
                        {
                            Console.WriteLine("No such command is known.");
                            return;
                        }

                        command.Run();

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