using EnsureThat;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Provides value-type to store permissions.
/// </summary>
public class Permission : SecurityObject
{
    /// <inheritdoc/>
    public override string UniqueIdentifier => $"permission${PermissionId}";

    /// <inheritdoc/>
    public override string CurrentState
    {
        get
        {
            Dictionary<string, string> properties = [];

            if (FriendlyName != null)
            {
                properties["friendlyName"] = FriendlyName;
            }

            if (Description != null)
            {
                properties["description"] = Description;
            }

            properties["sensitivity"] = Sensitivity.ToString();
            properties["runtimePermission"] = RuntimePermission.ToString();

            return CreateStateString(properties);
        }
    }

    /// <summary>
    /// Gets the fully-qualified name of this permission.
    /// </summary>
    public string PermissionId => $"{Scope}:{Name}";

    /// <summary>
    /// Gets the scope of this permission.
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Gets the name of this permission.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the friendly name of this permission.
    /// </summary>
    public string? FriendlyName { get; }

    /// <summary>
    /// Gets the description of this permission.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the sensitivity level of this permission.
    /// </summary>
    public PermissionSensitivity Sensitivity { get; }

    /// <summary>
    /// Gets a value indicating whether this permission can be granted at runtime.
    /// </summary>
    public bool RuntimePermission { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Permission"/> class.
    /// </summary>
    /// <param name="scope">
    /// The scope of this permission. it is highly recommended to use the implementation's interface name as the scope.
    /// </param>
    /// <param name="name">
    /// The name of this permission.
    /// </param>
    /// <param name="friendlyName">
    /// The friendly name of this permission. It is used for display purposes only.
    /// </param>
    /// <param name="description">
    /// The description of this permission. It is used for display purposes only.
    /// </param>
    /// <param name="sensitivity">
    /// The sensitivity level of this permission.
    /// </param>
    /// <param name="runtimePermission">
    /// A value indicating whether this permission can be granted at runtime.
    /// </param>
    public Permission(string scope,
                      string name,
                      string? friendlyName,
                      string? description,
                      PermissionSensitivity sensitivity = PermissionSensitivity.Low,
                      bool runtimePermission = false)
    {
        Ensure.Any.IsNotNull(scope, nameof(scope));
        Ensure.String.IsNotEmptyOrWhiteSpace(scope, nameof(scope));

        Ensure.Any.IsNotNull(name, nameof(name));
        Ensure.String.IsNotEmptyOrWhiteSpace(name, nameof(name));

        Scope = scope.Trim().ToLowerInvariant();
        Name = name.Trim().ToLowerInvariant();
        FriendlyName = friendlyName?.Trim();
        Description = description?.Trim();
        Sensitivity = sensitivity;
        RuntimePermission = runtimePermission;
    }

    /// <inheritdoc/>
    public override bool IsValid()
    {
        return base.IsValid()
               && Parent!.TryGetSecurityObject<Permission>(UniqueIdentifier, out Permission? registeredPermission)
               && registeredPermission == this;
    }

    /// <inheritdoc/>
    public override string ToString() => PermissionId;

    /// <summary>
    /// Returns the fully-qualified name of this instance.
    /// </summary>
    /// <param name="perm">
    /// The <see cref="Permission"/> instance to convert.
    /// </param>
    public static implicit operator string(Permission perm) => perm.ToString();
}
