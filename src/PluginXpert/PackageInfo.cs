using System.Text.Json.Serialization;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents a plugin package metadata.
/// </summary>
public class PackageInfo
{
    /// <summary>
    /// Gets or sets the package identifier.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    [JsonRequired]
    public string Id { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    /// <summary>
    /// Gets or sets the package's friendly name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the list of plugins.
    /// </summary>
    public List<PluginMetadata> Plugins { get; set; } = [];
}
