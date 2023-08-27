using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Threading.Tasks;

using SAPTeam.PluginXpert.Types;
using System.Runtime.Versioning;

namespace SAPTeam.PluginXpert
{
    /// <summary>
    /// Represents mechanisms to manage plugins permissions.
    /// </summary>
    public class PermissionManager : IPermissionManager
    {
        Dictionary<string, Dictionary<string, bool>> permissions = new Dictionary<string, Dictionary<string, bool>>();

        static string[] GrantedNames = new string[]
        {
            Assembly.GetExecutingAssembly().Modules.First().Name.ToLower(), // PluginManager Assembly
            Assembly.GetEntryAssembly().Modules.First().Name.ToLower(), // Application name
        };

        static string[] GrantedPrefixes = new string[]
        {
            "system.",
            "microsoft."
        };

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
            var type = plugin.GetType();
            string moduleName = type.Module.Name;
            string pluginFullname = type.FullName;
            string permissionID = $"{moduleName}:{pluginFullname}";
            if (!IsLegalName(moduleName))
            {
                throw new SecurityException("Plugin identifier can't start with illegal prefixes.");
            }

            permissions[permissionID] = new();

            foreach (var perm in plugin.Permissions)
            {
                permissions[permissionID][perm] = new();
            }

            plugin.PermissionManager = this;
        }

        /// <summary>
        /// Requests for an already decleared permission. All permission requests will be accepted by default.
        /// </summary>
        /// <param name="permission">The name of requsted permission.</param>
        /// <returns></returns>
        public virtual bool RequestPermission(string permission)
        {
            string permissionID = GetCallFrame(permission);

            Console.WriteLine($"Permission {permission} requested by {permissionID}");
            return true;
        }

        protected string GetCallFrame(string permission)
        {
            StackTrace stack = new();
            var frames = stack.GetFrames();
            MethodBase client = null;

            foreach (var frame in frames)
            {
                var frameName = frame.GetMethod().Module.Name;
                if (frameName == null)
                {
                    continue;
                }
                
                if (GrantedNames.Contains(frameName.ToLower()) || !IsLegalName(frameName))
                {
                    continue;
                }

                client = frame.GetMethod();
            }

            if (client == null)
            {
                // Everything is good, maybe...
                return "internal";
            }

            string permissionID = $"{client.Module.Name}:{client.DeclaringType.FullName}";

            if (!permissions.ContainsKey(permissionID))
            {
                throw new SecurityException($"The plugin \"{permissionID}\" is not registered.");
            }

            var permissionStore = permissions[permissionID];

            if (!permissionStore.ContainsKey(permission))
            {
                throw new SecurityException($"The permission \"{permission}\" is not registered for the plugin: {permissionID}");
            }

            return permissionID;
        }

        protected bool IsLegalName(string pluginName)
        {
            foreach (var name in GrantedPrefixes)
            {
                if (pluginName.ToLower().StartsWith(name))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
