using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DouglasDwyer.CasCore;

using EnsureThat;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public sealed class PluginContext : IDisposable
{
    private bool _disposed;

    public bool Disposed => _disposed;

    public string Id => PluginEntry.Id;

    public Version Version => PluginEntry.Version;

    public Token Token { get; private set; }

    public PluginEntry PluginEntry { get; private set; }

    public IPlugin? Instance { get; private set; }

    public bool Valid { get; private set; }

    public Exception? Exception { get; private set; }

    public IGateway? Gateway { get; private set; }

    public CasAssemblyLoader? Loader { get; private set; }

    public PluginContext(PluginLoadSession session)
    {
        Instance = session.Instance;
        PluginEntry = session.Entry;
        Token = session.Token!;
        Gateway = session.Gateway;
        Loader = session.Loader;
        Exception = session.Exception;
        Valid = session.Result == PluginLoadResult.Success;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Valid = false;

            Instance?.Dispose();
            Instance = null;

            Gateway?.Dispose();
            Gateway = null;

            Token.Revoke();
            Token = null!;

            Loader?.Unload();
            Loader = null;

            Exception = null;

            _disposed = true;
        }
    }
}
