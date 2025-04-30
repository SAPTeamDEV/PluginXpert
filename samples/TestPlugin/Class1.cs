using Newtonsoft.Json;

using Pastel;

using SAPTeam.PluginXpert.Types;

namespace TestPlugin;

public class Class1 : INovaPlugin
{
    private IGateway? _gateway;
    private INovaGateway? _novaGateway;

    public Class1()
    {
        // Only parameterless constructor allowed.
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
        foreach (string? permission in _gateway!.GetRegisteredPermissions())
        {
            Console.WriteLine($" - {permission}");
        }

        Console.WriteLine("Permissions granted to this plugin:");
        foreach (string? permission in _gateway!.GetGrantedPermissions())
        {
            Console.WriteLine($" - {permission}");
        }

        // Pastel has P/Invoke dependencies, so it will be blocked by the assembly loader.
        //Console.WriteLine("Test Pastel");
        //Console.WriteLine("Red".Pastel(ConsoleColor.Red));

        Console.WriteLine("Test CommonTK");
        Console.WriteLine(SAPTeam.CommonTK.Context.ApplicationDirectory);

        throw new ApplicationException("Test exception");
    }

    public void Dispose()
    {
        // Dispose of any resources if needed

        _gateway = null;
        _novaGateway = null;
    }
}