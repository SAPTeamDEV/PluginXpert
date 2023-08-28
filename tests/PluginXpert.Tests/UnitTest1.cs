using SAPTeam.PluginXpert;
using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert.Tests
{
    public class UnitTest1
    {
#if DEBUG
        static string pluginPath = "..\\..\\..\\..\\..\\samples\\TestPlugin\\bin\\Debug\\net6.0";
#else
        static string pluginPath = "..\\..\\..\\..\\..\\samples\\TestPlugin\\bin\\Release\\net6.0";
#endif

        [Fact]
        public void Test1()
        {
            var pm = new PluginManager(pluginPath.Replace('\\', Path.DirectorySeparatorChar), "TestPlugin.dll", new PermissionManager(new string[] { GetType().Module.Name.ToLower() }));
        }
    }
}