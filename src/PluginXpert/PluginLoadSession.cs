using System.Reflection;

using DouglasDwyer.CasCore;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents a session for loading a plugin.
/// </summary>
public class PluginLoadSession
{
    /// <summary>
    /// Gets the unique identifier of this session.
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Gets the plugin manager that created this session.
    /// </summary>
    public PluginManager PluginManager { get; }

    /// <summary>
    /// Gets the package that the plugin is belonging to.
    /// </summary>
    public PluginPackage Package { get; }

    /// <summary>
    /// Gets the metadata of the plugin.
    /// </summary>
    public PluginMetadata Metadata { get; }

    /// <summary>
    /// Gets or sets the result of the plugin load session.
    /// </summary>
    public PluginLoadResult Result { get; set; } = PluginLoadResult.Unknown;

    /// <summary>
    /// Gets or sets the exception that occurred during the plugin loading.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the implementation supporting this plugin.
    /// </summary>
    public PluginImplementation? Implementation { get; set; }

    /// <summary>
    /// Gets or sets the full path to the assembly file of the plugin.
    /// </summary>
    public string? AssemblyPath { get; set; }

    /// <summary>
    /// Gets or sets the assembly loader used to load the plugin assembly.
    /// </summary>
    public IsolatedAssemblyLoader? Loader { get; set; }

    /// <summary>
    /// Gets or sets the main assembly of the plugin.
    /// </summary>
    public Assembly? Assembly { get; set; }

    /// <summary>
    /// Gets or sets the token used to identify the plugin.
    /// </summary>
    public Token? Token { get; set; }

    /// <summary>
    /// Gets or sets the plugin initialized instance.
    /// </summary>
    public IPlugin? Instance { get; set; }

    /// <summary>
    /// Gets or sets the gateway used by the plugin.
    /// </summary>
    public IGateway? Gateway { get; set; }

    internal PluginLoadSession(string sessionId,
                             PluginManager pluginManager,
                             PluginPackage package,
                             PluginMetadata metadata)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        PluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        Package = package ?? throw new ArgumentNullException(nameof(package));
        Metadata = metadata;
    }

    /// <summary>
    /// Tries to run the specified action and catches any exceptions that occur.
    /// </summary>
    /// <param name="action">
    /// The action related to the plugin loading process to run.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the action was successful; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryRun(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            Exception = ex;
            Result = PluginLoadResult.Error;
            return false;
        }
    }
}
