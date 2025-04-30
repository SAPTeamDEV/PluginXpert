using DouglasDwyer.CasCore;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents the context of a plugin.
/// </summary>
public sealed class PluginContext : IDisposable
{
    private bool _disposed;
    private readonly bool _valid;

    /// <summary>
    /// Gets a value indicating whether the plugin context has been disposed.
    /// </summary>
    public bool Disposed => _disposed;

    /// <summary>
    /// Gets the identifier of the session that loaded this plugin.
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Gets the plugin identifier.
    /// </summary>
    public string Id => Metadata.Id;

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    public Version Version => Metadata.Version;

    /// <summary>
    /// Gets the unique identifier of the plugin's security token.
    /// </summary>
    public string TokenId => Token.TokenId;

    /// <summary>
    /// Gets the plugin security token.
    /// </summary>
    public Token Token { get; private set; }

    /// <summary>
    /// Gets the plugin metadata.
    /// </summary>
    public PluginMetadata Metadata { get; private set; }

    /// <summary>
    /// Gets the plugin initialized instance.
    /// </summary>
    public IPlugin? Instance { get; private set; }

    /// <summary>
    /// Gets the exception that occurred during the plugin loading process.
    /// </summary>
    public Exception? Exception { get; private set; }

    /// <summary>
    /// Gets the plugin communication interface.
    /// </summary>
    public IGateway? Gateway { get; private set; }

    /// <summary>
    /// Gets the plugin assembly loader.
    /// </summary>
    public IsolatedAssemblyLoader? Loader { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the plugin is successfully loaded and initialized.
    /// </summary>
    public bool Valid => !Disposed && _valid;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginContext"/> class.
    /// </summary>
    /// <param name="session">
    /// The plugin load session that contains the plugin's metadata and other information.
    /// </param>
    internal PluginContext(PluginLoadSession session)
    {
        SessionId = session.SessionId;

        Instance = session.Instance;
        Metadata = session.Metadata;
        Token = session.Token!;
        Gateway = session.Gateway;
        Loader = session.Loader;

        Exception = session.Exception;
        _valid = session.Result == PluginLoadResult.Success;
    }

    /// <summary>
    /// Disposes the plugin context and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Instance?.Dispose();
            Instance = null;

            Gateway?.Dispose();
            Gateway = null;

            Token.Dispose();
            Token = null!;

            Loader?.Unload();
            Loader = null;

            _disposed = true;
        }
    }
}
