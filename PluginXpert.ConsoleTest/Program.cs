using SAPTeam.PluginXpert;
using SAPTeam.PluginXpert.Types;

var p = new PluginManager<Plugin>(".", "TestPlugin.dll");
p.Plugins[0].Run();
Console.ReadKey();