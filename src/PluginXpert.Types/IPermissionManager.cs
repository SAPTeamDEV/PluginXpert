using System.Security;

namespace SAPTeam.PluginXpert.Types
{
    /// <summary>
    /// Represents methods to interact with PermissionManager.
    /// </summary>
    public interface IPermissionManager
    {
        /// <summary>
        /// Requests for an already declared permission.
        /// </summary>
        /// <param name="permission">The requested permission.</param>
        /// <returns>Result of the request.</returns>
        bool RequestPermission(Permission permission);

        /// <summary>
        /// Get the corresponding permission object.
        /// </summary>
        /// <param name="permissionName">The fully-qualified name of the permission.</param>
        /// <returns>An instance of <see cref="Permission"/> or a <see cref="SecurityException"/> when the requested permission is not declared.</returns>
        Permission GetPermission(string permissionName);
    }
}