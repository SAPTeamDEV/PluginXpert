using System.Diagnostics.CodeAnalysis;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents a plugin metadata.
/// </summary>
public readonly struct PluginMetadata : IEquatable<PluginMetadata>
{
    /// <summary>
    /// The plugin identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The build tag.
    /// </summary>
    public string BuildTag { get; init; }

    /// <summary>
    /// The plugin version.
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// The plugin's friendly name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The plugin's description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The plugin implementation's interface id.
    /// </summary>
    public required string Interface { get; init; }

    /// <summary>
    /// The supported plugin implementation's interface version.
    /// </summary>
    public required Version InterfaceVersion { get; init; }

    /// <summary>
    /// The minimum supported target framework version.
    /// </summary>
    public Version TargetFrameworkVersion { get; init; }

    /// <summary>
    /// The plugin main assembly's file name.
    /// </summary>
    public required string Assembly { get; init; }

    /// <summary>
    /// The plugin's class name.
    /// </summary>
    public required string Class { get; init; }

    /// <summary>
    /// The plugin's declared permissions.
    /// </summary>
    public required IEnumerable<string> Permissions { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMetadata"/> struct.
    /// </summary>
    /// <param name="id">The plugin identifier.</param>
    /// <param name="buildTag">The build tag.</param>
    /// <param name="version">The plugin version.</param>
    /// <param name="name">The plugin's friendly name.</param>
    /// <param name="description">The plugin's description.</param>
    /// <param name="interfaceId">The plugin implementation's interface id.</param>
    /// <param name="interfaceVersion">The supported plugin implementation's interface version.</param>
    /// <param name="targetFrameworkVersion">The minimum supported target framework version.</param>
    /// <param name="assembly">The plugin main assembly's file name.</param>
    /// <param name="className">The plugin's class name.</param>
    /// <param name="permissions">The plugin's declared permissions.</param>
    [SetsRequiredMembers]
    public PluginMetadata(string id,
                     string buildTag,
                     Version version,
                     string? name,
                     string? description,
                     string interfaceId,
                     Version interfaceVersion,
                     Version targetFrameworkVersion,
                     string assembly,
                     string className,
                     IEnumerable<string>? permissions)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        BuildTag = buildTag ?? throw new ArgumentNullException(nameof(buildTag));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Interface = interfaceId ?? throw new ArgumentNullException(nameof(interfaceId));
        InterfaceVersion = interfaceVersion ?? throw new ArgumentNullException(nameof(interfaceVersion));
        TargetFrameworkVersion = targetFrameworkVersion ?? throw new ArgumentNullException(nameof(targetFrameworkVersion));
        Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        Class = className ?? throw new ArgumentNullException(nameof(className));
        Permissions = permissions ?? [];

        Name = name;
        Description = description;
    }

    /// <summary>
    /// Checks if the current instance is equal to another <see cref="PluginMetadata"/> instance.
    /// </summary>
    /// <param name="other">
    /// The other <see cref="PluginMetadata"/> instance to compare with.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the current instance is equal to the other instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(PluginMetadata other)
    {
        return Id == other.Id &&
               BuildTag == other.BuildTag &&
               Version == other.Version &&
               Name == other.Name &&
               Description == other.Description &&
               Interface == other.Interface &&
               InterfaceVersion == other.InterfaceVersion &&
               TargetFrameworkVersion == other.TargetFrameworkVersion &&
               Assembly == other.Assembly &&
               Class == other.Class &&
               Permissions.SequenceEqual(other.Permissions);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PluginMetadata other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.Add(BuildTag);
        hash.Add(Version);
        hash.Add(Name);
        hash.Add(Description);
        hash.Add(Interface);
        hash.Add(InterfaceVersion);
        hash.Add(TargetFrameworkVersion);
        hash.Add(Assembly);
        hash.Add(Class);

        foreach (var permission in Permissions)
        {
            hash.Add(permission);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Compares two <see cref="PluginMetadata"/> instances for equality.
    /// </summary>
    /// <param name="left">
    /// The left <see cref="PluginMetadata"/> instance.
    /// </param>
    /// <param name="right">
    /// The right <see cref="PluginMetadata"/> instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if both instances are equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(PluginMetadata left, PluginMetadata right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="PluginMetadata"/> instances for inequality.
    /// </summary>
    /// <param name="left">
    /// The left <see cref="PluginMetadata"/> instance.
    /// </param>
    /// <param name="right">
    /// The right <see cref="PluginMetadata"/> instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if both instances are not equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(PluginMetadata left, PluginMetadata right)
    {
        return !(left == right);
    }
}
