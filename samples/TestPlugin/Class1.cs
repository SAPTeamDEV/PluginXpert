using SAPTeam.PluginXpert.Types;

namespace TestPlugin;

public class Class1 : INovaPlugin
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
        Console.WriteLine("Hello World");
        throw new ApplicationException("Test exception");
    }
}