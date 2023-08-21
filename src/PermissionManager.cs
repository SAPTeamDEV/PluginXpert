using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert
{
    /// <summary>
    /// Represents mechanisms to manage plugins permissions.
    /// </summary>
    public class PermissionManager
    {
        Dictionary<string, Dictionary<string, bool>> permissions = new Dictionary<string, Dictionary<string, bool>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionManager"/> class.
        /// </summary>
        public PermissionManager()
        {
            
        }

        /// <summary>
        /// Registers a new plugin in plugin store.
        /// </summary>
        /// <param name="plugin">A managed plugin instance</param>
        public virtual void RegisterPlugin(IPlugin plugin)
        {
            string pluginFullname = plugin.GetType().FullName;
            permissions[pluginFullname] = new();

            foreach (var perm in plugin.Permissions)
            {
                permissions[pluginFullname][perm] = new();
            }

            plugin.PermissionManager = this;
        }

        /// <summary>
        /// Requsts for an already decleared permission. All permission requests will be accepted by default.
        /// </summary>
        /// <param name="permission">The name of requsted permission.</param>
        /// <returns></returns>
        public virtual bool RequastPermission(string permission)
        {
            System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace();
            var m = stack.GetFrame(0).GetMethod();
            return true;
        }
    }
}
