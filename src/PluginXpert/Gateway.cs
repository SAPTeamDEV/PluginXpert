using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public class Gateway : IGateway
{
    PluginManager _pluginManager;
    private bool _disposed;

    public SecurityDescriptor SecurityDescriptor { get; }

    public Gateway(SecurityDescriptor securityDescriptor, PluginManager pluginManager)
    {
        SecurityDescriptor = securityDescriptor;
        _pluginManager = pluginManager;
    }

    public void EraseSettings() => throw new NotImplementedException();

    public T GetSettings<T>() => throw new NotImplementedException();

    public void SaveSettings<T>(T settings) => throw new NotImplementedException();

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                
            }

            _pluginManager = null!;
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
