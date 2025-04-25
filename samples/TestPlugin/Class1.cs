using SAPTeam.PluginXpert.Types;

namespace TestPlugin
{
    public class Class1 : IPlugin<IGateway>
    {
        public string Name => "TestPlugin";
        public string[] Permissions { get; } = new string[]
        {
            "plugin:test"
        };

        public void Dispose()
        {
            // Dispose of any resources if needed
        }

        public void OnLoad(IGateway gateway)
        {
            gateway.PermissionManager.RequestPermission(gateway.PermissionManager.GetPermissions(Permissions[0])[0]);
        }

        public void Run()
        {
            Console.WriteLine("HIII");
        }
    }
}