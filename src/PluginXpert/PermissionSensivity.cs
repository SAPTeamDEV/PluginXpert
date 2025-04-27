using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert
{
    /// <summary>
    /// Represents the sensitivity level of a permission.
    /// </summary>
    public enum PermissionSensitivity
    {
        /// <summary>
        /// The permission is not sensitive. It does not affect the security of the system. e.g. logging.
        /// </summary>
        Low,

        /// <summary>
        /// The permission is sensitive. It may affect the security of the system. e.g. limited access to files or networking.
        /// </summary>
        Medium,

        /// <summary>
        /// The permission is highly sensitive. It may grant access to critical system resources or data. e.g. full access to files or networking as the current user.
        /// </summary>
        High,
    }
}
