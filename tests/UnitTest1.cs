using SAPTeam.PluginXpert;
using SAPTeam.PluginXpert.Types;

namespace PluginXpert.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var pm = new PluginManager("..\\..\\..\\..\\TestPlugin\\bin\\Debug\\net6.0", "TestPlugin.dll");
        }
    }
}