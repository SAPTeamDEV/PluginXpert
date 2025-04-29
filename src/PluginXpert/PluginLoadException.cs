namespace SAPTeam.PluginXpert;

/// <summary>
/// Exception thrown when a plugin load fails at instance initialization.
/// </summary>
public class PluginLoadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadException"/> class.
    /// </summary>
    public PluginLoadException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">
    /// The error message that explains the reason for the exception.
    /// </param>
    public PluginLoadException(string? message) : base(message)
    {
    }
}
