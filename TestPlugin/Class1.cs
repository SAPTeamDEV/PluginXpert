using SAPTeam.PluginXpert;

namespace TestPlugin
{
    public class Class1 : IPlugin
    {
        public string Name => "Test {;ugin";
        public string[] Permissions { get; } = new string[]
        {
            "test",
            "test2"
        };
        public bool IsLoaded { get; set; }
        public Exception Exception { get; set; }

        public PermissionManager PermissionManager { get; set; }

        public void OnLoad()
        {
            PermissionManager.RequastPermission("test");
        }

        public void Run()
        {
            
        }
    }
}