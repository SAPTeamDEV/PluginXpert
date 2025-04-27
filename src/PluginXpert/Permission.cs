using System;
using System.Collections.Generic;
using System.Text;

using EnsureThat;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Provides value-type to store permissions.
/// </summary>
public class Permission : SecurityObject
{
    /// <inheritdoc/>
    public override string UniqueIdentifier => $"{GetType().Name.ToLowerInvariant()}${PermissionId}";

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
    /// Gets the fully-qualified name of this permission, which is a combination of the scope and name.
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

    public Permission(string scope,
                      string name,
                      string? friendlyName,
                      string? description,
                      PermissionSensitivity sensitivity = PermissionSensitivity.Low,
                      bool runtimePermission = false)
    {
        Ensure.String.IsNotNullOrEmpty(scope, nameof(scope));
        Ensure.String.IsNotNullOrEmpty(name, nameof(name));

        Scope = scope.Trim().ToLowerInvariant();
        Name = name.Trim().ToLowerInvariant();
        FriendlyName = friendlyName?.Trim();
        Description = description?.Trim();
        Sensitivity = sensitivity;
        RuntimePermission = runtimePermission;
    }

    public override bool IsValid()
    {
        return base.IsValid()
            && Parent!.ValidatePermission(this);
    }

    /// <inheritdoc/>
    public override string ToString() => PermissionId;

    /// <summary>
    /// Returns the fully-qualified name of this instance.
    /// </summary>
    /// <param name="perm">
    /// The <see cref="Permission"/> instance to convert.
    /// </param>
    public static implicit operator string(Permission perm)
    {
        return perm.ToString();
    }
}
