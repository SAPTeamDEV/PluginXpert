using SAPTeam.PluginXpert;

using TestPlugin;

namespace PluginXpert.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var perm = new PermissionManager();
            var p = new PluginManager<Class1>(".", "TestPlugin.dll", perm);
        }
    }
}