
using SAPTeam.PluginXpert;
using SAPTeam.PluginXpert.Types;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AppWithPlugin
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "/d")
                {
                    Console.WriteLine("Waiting for any key...");
                    Console.ReadLine();
                }

                var pm = new PluginManager("..\\..\\..\\..\\TestPlugin\\bin\\Debug\\net6.0", "TestPlugin.dll");

                var commands = pm.Plugins;

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

                        IPlugin command = commands.FirstOrDefault(c => c.Name == commandName);
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