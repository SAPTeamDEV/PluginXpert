using System;
using System.Collections.Generic;
using System.Text;

namespace SAPTeam.PluginXpert.Types
{
    /// <summary>
    /// Provides value-type to store permissions.
    /// </summary>
    public readonly struct Permission
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
        /// Initializes a new instance of the <see cref="Permission"/> class.
        /// </summary>
        /// <param name="scope">The scope of the permission.</param>
        /// <param name="name">The name of the permission.</param>
        public Permission(string scope, string name)
        {
            Scope = scope;
            Name = name;
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
    }
}
