namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents the result of a plugin load session.
/// </summary>
public enum PluginLoadResult
{
    /// <summary>
    /// Indicates that the plugin load result is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Indicates that a plugin with the same id is already loaded.
    /// </summary>
    AlreadyLoaded,

    /// <summary>
    /// Indicates that the plugin is not supported by the registered implementations.
    /// </summary>
    NotSupportedImplementation,

    /// <summary>
    /// Indicates that the plugin is not supported by the current .NET runtime.
    /// </summary>
    NotSupportedRuntime,

    /// <summary>
    /// Indicates that an error occurred while loading the plugin.
    /// </summary>
    Error,

    /// <summary>
    /// Indicates that the plugin was loaded successfully.
    /// </summary>
    Success,
}
