using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnsureThat;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public class PluginContext : IDisposable
{
    private SecurityContext securityContext;

    public string Id => PluginEntry.Id;

    public Version Version => PluginEntry.Version;

    public SecurityToken Token { get; private set; }

    public PluginEntry PluginEntry { get; private set; }

    public IPlugin? Instance { get; private set; }

    public bool IsLoaded { get; private set; }

    public Exception? Exception { get; private set; }

    public IGateway? Gateway { get; private set; }

    private PluginContext()
    {
        
    }

    public static PluginContext? Create(SecurityContext securityContext, PluginImplementation impl, IPlugin? instance, PluginEntry entry, bool throwOnFail = true)
    {
        if (instance == null)
        {
            return null;
        }

        Ensure.Any.IsNotNull(entry);

        PluginContext? context = new();

        try
        {
            context.securityContext = securityContext;
            context.Instance = instance;
            context.PluginEntry = entry;
            context.Token = securityContext.RegisterPlugin(impl, instance, entry);
            context.Gateway = impl.CreateGateway(instance, context.Token, entry);
            instance.OnLoad(context.Gateway);
            context.IsLoaded = true;
        }
        catch (Exception e)
        {
            if (throwOnFail)
            {
                throw;
            }

            context.IsLoaded = false;
            context.Exception = e;
        }

        impl.Add(context);
        return context;
    }

    public void Dispose()
    {
        Instance?.Dispose();
        Instance = null;

        Gateway?.Dispose();
        Gateway = null;

        securityContext.RevokeToken(Token);
        securityContext = null!;
    }
}
