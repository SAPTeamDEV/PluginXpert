using SAPTeam.PluginXpert.Types;

namespace TestPlugin
{
    public class Class1 : Plugin
    {
        public override string Name => "TestPlugin";
        public override string[] Permissions { get; } = new string[]
        {
            "test",
            "test2"
        };

        public override void OnLoad()
        {
            PermissionManager.RequastPermission("test");
        }

        public override void Run()
        {
            Console.WriteLine("HIII");
        }
    }
}