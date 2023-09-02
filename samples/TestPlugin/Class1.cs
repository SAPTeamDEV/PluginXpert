using SAPTeam.PluginXpert.Types;

namespace TestPlugin
{
    public class Class1 : Plugin
    {
        public override string Name => "TestPlugin";
        public override string[] Permissions { get; } = new string[]
        {
            "plugin:test",
            "plugin:test2"
        };

        public override void OnLoad()
        {
            PermissionManager.RequestPermission(PermissionManager.GetPermission(Permissions[0]));
        }

        public override void Run()
        {
            Console.WriteLine("HIII");
        }
    }
}