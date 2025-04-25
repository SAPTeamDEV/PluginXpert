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
    private PermissionManager permissionManager;

    public string Id => PluginEntry.Id;

    public string SecurityIdentifier => SecurityDescriptor.Owner;

    public Version Version => PluginEntry.Version;

    public SecurityDescriptor SecurityDescriptor { get; private set; }

    public PluginEntry PluginEntry { get; private set; }

    public IPlugin? Instance { get; private set; }

    public bool IsLoaded { get; private set; }

    public Exception? Exception { get; private set; }

    public IGateway? Gateway { get; private set; }

    private PluginContext()
    {
        
    }

    public static PluginContext? Create(PermissionManager permissionManager, PluginImplementation impl, IPlugin? instance, PluginEntry entry, bool throwOnFail = true)
    {
        if (instance == null)
        {
            return null;
        }

        Ensure.Any.IsNotNull(entry);

        PluginContext? context = new();

        try
        {
            context.permissionManager = permissionManager;
            context.Instance = instance;
            context.PluginEntry = entry;
            context.SecurityDescriptor = permissionManager.RegisterPlugin(impl, instance, entry);
            context.Gateway = impl.CreateGateway(instance, context.SecurityDescriptor, entry);
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

        permissionManager.RevokeSecurityDescriptor(SecurityDescriptor.Owner);
        permissionManager = null!;
    }
}
