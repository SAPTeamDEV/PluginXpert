using SAPTeam.PluginXpert;


namespace PluginXpert.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var perm = new PermissionManager();
            var p = new PluginManager<Plugin>(".", "TestPlugin.dll", perm);
        }
    }
}