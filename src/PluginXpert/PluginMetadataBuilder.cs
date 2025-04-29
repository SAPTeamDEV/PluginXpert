using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Builder for creating <see cref="PluginMetadata"/> instances.
/// </summary>
public class PluginMetadataBuilder : ICloneable
{
    private string? _id;
    private string? _buildTag;
    private Version? _version;
    private string? _name;
    private string? _description;
    private string? _interface;
    private Version? _interfaceVersion;
    private Version? _targetFrameworkVersion;
    private string? _assembly;
    private string? _class;
    private List<string> _permissions = [];

    /// <summary>
    /// Creates a new instance of <see cref="PluginMetadataBuilder"/>.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="PluginMetadataBuilder"/>.
    /// </returns>
    public static PluginMetadataBuilder Create()
    {
        return new PluginMetadataBuilder();
    }

    /// <summary>
    /// Creates a new instance of <see cref="PluginMetadataBuilder"/> from an existing <see cref="PluginMetadata"/> instance.
    /// </summary>
    /// <param name="metadata">
    /// The existing <see cref="PluginMetadata"/> instance to copy from.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="PluginMetadataBuilder"/> initialized with the values from the provided <see cref="PluginMetadata"/>.
    /// </returns>
    public static PluginMetadataBuilder Create(PluginMetadata metadata)
    {
        return new PluginMetadataBuilder()
        {
            _id = metadata.Id,
            _buildTag = metadata.BuildTag,
            _version = metadata.Version,
            _name = metadata.Name,
            _description = metadata.Description,
            _interface = metadata.Interface,
            _interfaceVersion = metadata.InterfaceVersion,
            _targetFrameworkVersion = metadata.TargetFrameworkVersion,
            _assembly = metadata.Assembly,
            _class = metadata.Class,
            _permissions = metadata.Permissions.ToList(),
        };
    }

    /// <summary>
    /// Sets the plugin ID.
    /// </summary>
    /// <param name="id">
    /// The plugin ID.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder SetId(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the plugin version.
    /// </summary>
    /// <param name="version">
    /// The plugin version.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder SetVersion(Version version)
    {
        _version = version;
        return this;
    }

    /// <summary>
    /// Sets the plugin implementation's interface ID and version.
    /// </summary>
    /// <param name="interfaceId">
    /// The plugin implementation's interface ID.
    /// </param>
    /// <param name="interfaceVersion">
    /// The supported plugin implementation's interface version.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder SetImplementation(string interfaceId, Version interfaceVersion)
    {
        _interface = interfaceId;
        _interfaceVersion = interfaceVersion;
        return this;
    }

    /// <summary>
    /// Sets the plugin entry point.
    /// </summary>
    /// <param name="assembly">
    /// The plugin main assembly's file name.
    /// </param>
    /// <param name="className">
    /// The plugin's class name.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder SetEntryPoint(string assembly, string className)
    {
        _assembly = assembly;
        _class = className;
        return this;
    }

    /// <summary>
    /// Computes the build tag based on the provided stream.
    /// </summary>
    /// <param name="stream">
    /// The stream to compute the build tag from.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder ComputeBuildTag(Stream stream)
    {
        byte[] hash = SHA256.HashData(stream);
        _buildTag = string.Concat(hash[^5..].Select(b => b.ToString("x2")));
        return this;
    }

    /// <summary>
    /// Sets the build tag.
    /// </summary>
    /// <param name="buildTag">
    /// The build tag.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder SetBuildTag(string buildTag)
    {
        _buildTag = buildTag;
        return this;
    }

    /// <summary>
    /// Sets the plugin's friendly name.
    /// </summary>
    /// <param name="name">
    /// The plugin's friendly name.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder SetFriendlyName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the plugin's description.
    /// </summary>
    /// <param name="description">
    /// The plugin's description.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder SetDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Detects and sets the target framework version from the provided assembly.
    /// </summary>
    /// <param name="assembly">
    /// The assembly to detect the target framework version from.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the assembly does not have a <see cref="TargetFrameworkAttribute"/>.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the framework string cannot be parsed into a version.
    /// </exception>
    public PluginMetadataBuilder DetectTargetFramework(Assembly assembly)
    {
        TargetFrameworkAttribute attribute = assembly.GetCustomAttribute<TargetFrameworkAttribute>() ?? throw new ArgumentException("Assembly does not have a TargetFrameworkAttribute");

        Version frameworkVer = ParseFrameworkVersion(attribute.FrameworkName);
        _targetFrameworkVersion = frameworkVer ?? throw new FormatException("Cannot parse the framework string");

        return this;
    }

    /// <summary>
    /// Sets the target framework version.
    /// </summary>
    /// <param name="targetFrameworkVersion">
    /// The target framework version.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder SetTargetFrameworkVersion(Version targetFrameworkVersion)
    {
        _targetFrameworkVersion = targetFrameworkVersion;
        return this;
    }

    /// <summary>
    /// Adds a permission to the plugin's declared permissions.
    /// </summary>
    /// <param name="permission">
    /// The permission to add.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder AddPermission(string permission)
    {
        _permissions.Add(permission);
        return this;
    }

    /// <summary>
    /// Adds multiple permissions to the plugin's declared permissions.
    /// </summary>
    /// <param name="permissions">
    /// The permissions to add.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="PluginMetadataBuilder"/> for method chaining.
    /// </returns>
    public PluginMetadataBuilder AddPermissions(IEnumerable<string> permissions)
    {
        _permissions.AddRange(permissions);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="PluginMetadata"/> instance.
    /// </summary>
    /// <returns>
    /// The constructed <see cref="PluginMetadata"/> instance.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required properties are not set.
    /// </exception>
    public PluginMetadata Build()
    {
        if (_id  == null) throw new InvalidOperationException("Plugin ID is required.");
        if (_version == null) throw new InvalidOperationException("Plugin version is required.");
        if (_buildTag == null) throw new InvalidOperationException("Plugin build tag is required.");
        if (_interface == null) throw new InvalidOperationException("Plugin implementation's interface ID is required.");
        if (_interfaceVersion == null) throw new InvalidOperationException("Plugin implementation's interface version is required.");
        if (_targetFrameworkVersion == null) throw new InvalidOperationException("Plugin's target framework version is required.");
        if (_assembly == null) throw new InvalidOperationException("Plugin's entrypoint is required.");
        if (_class == null) throw new InvalidOperationException("Plugin's entrypoint is required.");

        return new PluginMetadata(
            id: _id,
            buildTag: _buildTag,
            version: _version,
            name: _name,
            description: _description,
            interfaceId: _interface,
            interfaceVersion: _interfaceVersion,
            targetFrameworkVersion: _targetFrameworkVersion,
            assembly: _assembly,
            className: _class,
            permissions: _permissions
        );
    }

    static Version ParseFrameworkVersion(string frameworkString)
    {
        if (string.IsNullOrWhiteSpace(frameworkString))
            throw new ArgumentException("Input framework string cannot be null or empty.", nameof(frameworkString));

        // Look for "Version=" (case-insensitive) in the string.
        const string versionKeyword = "Version=";
        int versionIndex = frameworkString.IndexOf(versionKeyword, StringComparison.OrdinalIgnoreCase);
        if (versionIndex == -1)
        {
            throw new FormatException("The framework string does not contain a version specification.");
        }

        // Extract the substring starting right after "Version="
        string versionSubString = frameworkString.Substring(versionIndex + versionKeyword.Length).Trim();

        // If the version starts with a 'v', remove it.
        if (versionSubString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            versionSubString = versionSubString.Substring(1);
        }

        // Try to parse the extracted substring into a Version object.
        return Version.TryParse(versionSubString, out Version? parsedVersion)
            ? parsedVersion
            : throw new FormatException($"Unable to parse version from '{versionSubString}'.");
    }

    /// <inheritdoc/>
    public object Clone()
    {
        return MemberwiseClone();
    }
}
