using System;
using System.Collections.Generic;
using System.Text;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Provides value-type to store permissions.
/// </summary>
public readonly struct Permission
{
    /// <summary>
    /// Gets the scope of this permission.
    /// </summary>
    public string Scope { get; init; }

    /// <summary>
    /// Gets the name of this permission.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the friendly name of this permission.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether this permission is valid.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(Scope) && !string.IsNullOrEmpty(Name);

    /// <summary>
    /// Initializes a new instance of the <see cref="Permission"/> struct.
    /// </summary>
    /// <param name="scope">The scope of the permission.</param>
    /// <param name="name">The name of the permission.</param>
    /// <param name="description">The friendly name of the permission.</param>
    public Permission(string scope, string name, string description)
    {
        Scope = scope.ToLower();
        Name = name.ToLower();
        Description = description;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Scope}:{Name}";
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Permission perm && Scope == perm.Scope && Name == perm.Name;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

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

    /// <summary>
    /// Compares two <see cref="Permission"/> instances for equality.
    /// </summary>
    /// <param name="left">
    /// The left operand of the equality operator.
    /// </param>
    /// <param name="right">
    /// The right operand of the equality operator.
    /// </param>
    /// <returns>
    /// true if the two <see cref="Permission"/> instances are equal; otherwise, false.
    /// </returns>
    public static bool operator ==(Permission left, Permission right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Permission left, Permission right)
    {
        return !(left == right);
    }
}
