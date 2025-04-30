using System.Collections;

using DouglasDwyer.CasCore;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents the base class for plugin implementations.
/// </summary>
public abstract class PluginImplementation : IReadOnlyCollection<PluginContext>, IDisposable
{
    private bool _disposed;
    private List<PluginContext> _plugins = [];

    /// <summary>
    /// Gets a value indicating whether the plugin implementation has been disposed.
    /// </summary>
    public bool Disposed => _disposed;

    /// <summary>
    /// Gets the name of the plugin implementation.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the version of the plugin implementation.
    /// </summary>
    public abstract Version Version { get; }

    /// <summary>
    /// Gets the minimum version supported by this plugin implementation.
    /// </summary>
    public virtual Version MinimumVersion => Version;

    /// <summary>
    /// Gets the path to the temporary directory used by the plugin manager for package extraction.
    /// </summary>
    public string TempPath { get; protected set; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    /// <summary>
    /// Gets the list of plugins.
    /// </summary>
    public IEnumerable<PluginContext> Plugins => _plugins;

    /// <summary>
    /// Gets the number of plugins.
    /// </summary>
    public int Count => _plugins.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginImplementation"/> class.
    /// </summary>
    protected PluginImplementation()
    {

    }

    /// <summary>
    /// Registers the plugin permissions with the specified security context.
    /// </summary>
    /// <param name="securityContext">
    /// The security context to register the permissions with.
    /// </param>
    protected internal abstract void RegisterPermissions(SecurityContext securityContext);

    /// <summary>
    /// Adds a plugin context to the list of plugins.
    /// </summary>
    /// <param name="context">
    /// The plugin context to add.
    /// </param>
    internal void Add(PluginContext context) => _plugins.Add(context);

    /// <summary>
    /// Checks if the <paramref name="type"/> is supported by this plugin implementation.
    /// </summary>
    /// <param name="type">
    /// The type to check.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the type is supported; otherwise, <see langword="false"/>.
    /// </returns>
    public virtual bool CheckPluginType(Type type) => true;

    /// <summary>
    /// Creates a new gateway for the specified plugin.
    /// </summary>
    /// <param name="session">
    /// The plugin load session.
    /// </param>
    /// <returns>
    /// A new instance of the <see cref="IGateway"/> interface.
    /// </returns>
    public virtual IGateway CreateGateway(PluginLoadSession session) => new Gateway(session.Token!);

    /// <summary>
    /// Updates the Code Access Security (CAS) policy for the plugin's assembly loader.
    /// </summary>
    /// <remarks>
    /// Derived classes must override this method to add their gateway type to the CAS policy.
    /// </remarks>
    /// <param name="session">
    /// The plugin load session.
    /// </param>
    /// <param name="policy">
    /// The CAS policy builder to update.
    /// </param>
    public virtual void UpdateAssemblySecurityPolicy(PluginLoadSession session, CasPolicyBuilder policy) => policy.Allow(new TypeBinding(typeof(Gateway), Accessibility.Public));

    /// <inheritdoc/>
    public override string ToString() => $"{Name}-{Version}";

    /// <summary>
    /// Disposes the plugin implementation and releases its resources.
    /// </summary>
    /// <param name="disposing">
    /// A value indicating whether the method is called from the Dispose method.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (PluginContext plugin in _plugins)
                {
                    plugin.Dispose();
                }

                _plugins.Clear();
                _plugins = null!;

                if (Directory.Exists(TempPath))
                {
                    Directory.Delete(TempPath, true);
                }
            }

            _disposed = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public IEnumerator<PluginContext> GetEnumerator() => _plugins.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _plugins.GetEnumerator();
}
