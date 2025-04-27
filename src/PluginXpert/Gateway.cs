using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public class Gateway : IGateway
{
    private bool _disposed;

    public bool Disposed => _disposed;

    public SecurityToken Token { get; private set; }

    public Gateway(SecurityToken securityToken)
    {
        Token = securityToken;
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

            Token = null!;

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
