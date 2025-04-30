using System.Reflection;

using DouglasDwyer.CasCore;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public class PluginLoadSession
{
    public string SessionId { get; }

    public PluginManager PluginManager { get; }

    public PluginPackage Package { get; }

    public PluginMetadata Metadata { get; }

    public PluginLoadResult Result { get; set; } = PluginLoadResult.Unknown;

    public Exception? Exception { get; set; }

    public PluginImplementation? Implementation { get; set; }

    public string? AssemblyPath { get; set; }

    public IsolatedAssemblyLoader? Loader { get; set; }

    public Assembly? Assembly { get; set; }

    public Token? Token { get; set; }

    public IPlugin? Instance { get; set; }

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
