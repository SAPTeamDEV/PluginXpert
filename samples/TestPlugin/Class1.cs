using SAPTeam.PluginXpert.Types;

namespace TestPlugin;

public class Class1 : INovaPlugin
{
    IGateway _gateway;
    INovaGateway _novaGateway;

    public void Dispose()
    {
        // Dispose of any resources if needed
    }

    public void OnLoad(IGateway gateway)
    {
        _gateway = gateway;

        if (gateway is INovaGateway novaGateway)
        {
            _novaGateway = novaGateway;
        }
    }

    public void Run()
    {
        Console.WriteLine("Hello World");

        Console.WriteLine("Registered Permissions:");
        foreach (var permission in _gateway.GetRegisteredPermissions())
        {
            Console.WriteLine($" - {permission}");
        }

        Console.WriteLine("Permissions granted to this plugin:");
        foreach (var permission in _gateway.GetGrantedPermissions())
        {
            Console.WriteLine($" - {permission}");
        }

        throw new ApplicationException("Test exception");
    }
}