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
        /// <returns></returns>
        bool RequestPermission(Permission permission);
    }
}