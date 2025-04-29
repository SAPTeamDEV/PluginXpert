namespace SAPTeam.PluginXpert;

public enum PluginLoadResult
{
    Unknown = 0,

    AlreadyLoaded,

    NotSupportedImplementation,

    NotSupportedRuntime,

    Error,

    Success,
}
