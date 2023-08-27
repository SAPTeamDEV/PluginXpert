namespace SAPTeam.PluginXpert.Types
{
    /// <summary>
    /// Represents methods to interact with PermissionManager.
    /// </summary>
    public interface IPermissionManager
    {
        /// <summary>
        /// Requsts for an already decleared permission.
        /// </summary>
        /// <param name="permission">The name of requsted permission.</param>
        /// <returns></returns>
        bool RequestPermission(string permission);
    }
}