using System;
using System.Collections.Generic;

namespace SAPTeam.PluginXpert.Types
{
    /// <summary>
    /// This interface defines the contract for plugin's public API.
    /// </summary>
    public interface IGateway : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the gateway has been disposed.
        /// </summary>
        bool Disposed { get; }

        /// <summary>
        /// Gets all permissions granted to the plugin.
        /// </summary>
        /// <returns>
        /// A collection of permission IDs that have been granted to the plugin.
        /// </returns>
        IEnumerable<string> GetGrantedPermissions();

        /// <summary>
        /// Gets all permissions registered in the plugin host.
        /// </summary>
        /// <returns>
        /// A collection of permission IDs that have been registered in the plugin host.
        /// </returns>
        IEnumerable<string> GetRegisteredPermissions();

        /// <summary>
        /// Checks if the plugin has a specific permission.
        /// </summary>
        /// <param name="permissionId">
        /// The ID of the permission to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the plugin has the permission; otherwise, <see langword="false"/>.
        /// </returns>
        bool HasPermission(string permissionId);

        /// <summary>
        /// Requests a specific permission from the plugin host.
        /// </summary>
        /// <param name="permissionId">
        /// The ID of the permission to request.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the permission was granted; otherwise, <see langword="false"/>.
        /// </returns>
        bool RequestPermission(string permissionId);
    }
}
