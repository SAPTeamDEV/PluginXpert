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
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the package's friendly name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the list of plugins.
    /// </summary>
    public List<PluginMetadata> Plugins { get; set; } = [];
}

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false)]
[JsonSerializable(typeof(PackageInfo))]
internal partial class PackageInfoContext : JsonSerializerContext
{
}
