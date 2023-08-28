using SAPTeam.PluginXpert.Types;

namespace TestPlugin
{
    public class Class1 : Plugin
    {
        public override string Name => "TestPlugin";
        public override Permission[] Permissions { get; } = new Permission[]
        {
            new Permission("plugin", "test"),
            new Permission("plugin", "test2")
        };

        public override void OnLoad()
        {
            PermissionManager.RequestPermission(Permissions[0]);
        }

        public override void Run()
        {
            Console.WriteLine("HIII");
        }
    }
}