using SAPTeam.PluginXpert.Types;

namespace TestPlugin;

public class Class1 : IPlugin
{
    public void Dispose()
    {
        // Dispose of any resources if needed
    }

    public void OnLoad(IGateway gateway)
    {
        
    }

    public void Run()
    {
        Console.WriteLine("HIII");
        throw new ApplicationException("Test exception");
    }
}