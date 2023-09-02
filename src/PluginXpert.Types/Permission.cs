using System;
using System.Collections.Generic;
using System.Text;

namespace SAPTeam.PluginXpert.Types
{
    /// <summary>
    /// Provides value-type to store permissions.
    /// </summary>
    public class Permission
    {
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
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Permission"/> class.
        /// </summary>
        /// <param name="scope">The scope of the permission.</param>
        /// <param name="name">The name of the permission.</param>
        /// <param name="description">The friendly name of the permission.</param>
        public Permission(string scope, string name, string description)
        {
            Scope = scope;
            Name = name;
            Description = description;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Scope}:{Name}";
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Permission perm && Scope == perm.Scope && Name == perm.Name;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Scope.GetHashCode() * Name.GetHashCode();
        }

        /// <summary>
        /// Returns the fully-qualified name of this instance.
        /// </summary>
        /// <param name="perm"></param>
        public static implicit operator string(Permission perm)
        {
            return perm.ToString();
        }
    }
}
