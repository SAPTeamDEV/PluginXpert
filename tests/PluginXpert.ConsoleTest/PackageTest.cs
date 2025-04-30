using SAPTeam.PluginXpert;

namespace PluginXpert.ConsoleTest;

internal class Program
{
    private static readonly string pluginPath = "..\\..\\..\\..\\..\\samples\\TestPlugin\\bin\\sample.plx";

    private static void Main(string[] args)
    {
        PluginManager pm =
        [
            new NovaPluginImplementation()
        ];

        try
        {
            if (args.Length == 1 && args[0] == "/d")
            {
                Console.WriteLine("Waiting for any key...");
                Console.ReadLine();
            }

            PluginPackage package = new PluginPackage(pluginPath);
            package.LoadFromFile();

            pm.LoadPlugins(package);

            Console.WriteLine($"Initiated {pm.LoadSessions.Count} plugin load sessions.");
            Console.WriteLine($"Loaded {pm.Plugins.Count()} plugins from {pluginPath}");

            Console.WriteLine();
            Console.WriteLine("Sessions: ");
            foreach (PluginLoadSession session in pm.LoadSessions.Values)
            {
                Console.WriteLine($"- {session.Metadata.Id}: ({session.Result})");
                if (session.Exception != null)
                {
                    Console.WriteLine($"  {session.Exception.GetType().Name}: {session.Exception.Message}");
                    Console.WriteLine(session.Exception.StackTrace);
                }
            }

            Console.WriteLine();
            Console.WriteLine();

            IEnumerable<PluginContext> commands = pm.ValidPlugins;

            if (args.Length == 0)
            {
                Console.WriteLine("Commands: ");
                foreach (PluginContext command in commands)
                {
                    Console.WriteLine($"{command.Id}");
                }
            }
            else
            {
                foreach (string commandName in args)
                {
                    Console.WriteLine($"-- {commandName} --");

                    PluginContext? command = commands.FirstOrDefault(c => c.Id == commandName);
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