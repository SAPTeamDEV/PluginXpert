using DouglasDwyer.CasCore;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public sealed class PluginContext : IDisposable
{
    public bool Disposed { get; private set; }

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
        if (!Disposed)
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

            Disposed = true;
        }
    }
}
