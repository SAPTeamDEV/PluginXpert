using System.Diagnostics;
using System.Reflection;
using System.Security;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert
{
    /// <summary>
    /// Represents mechanisms to manage plugins permissions.
    /// </summary>
    public class PermissionManager : IPermissionManager
    {
        /// <summary>
        /// Gets or sets the global permission manager.
        /// </summary>
        public PermissionManager Global { get; set; }

        /// <summary>
        /// Get a dictionary containing declared permissions.
        /// </summary>
        static Dictionary<string, Permission> DeclaredPermissions { get; } = new Dictionary<string, Permission>();

        readonly Dictionary<string, Dictionary<string, bool>> permissions = new Dictionary<string, Dictionary<string, bool>>();

        /// <summary>
        /// An array of assembly names with unlimited privileges.
        /// </summary>
        protected string[] GrantedNames { get; } = new string[]
        {
            Assembly.GetExecutingAssembly().Modules.First().Name.ToLower(), // PluginManager Assembly
            Assembly.GetEntryAssembly().Modules.First().Name.ToLower(), // Application name
        };

        /// <summary>
        /// An array of assembly prefixes with unlimited privileges.
        /// </summary>
        protected string[] GrantedPrefixes { get; } = new string[]
        {
            "system.",
            "microsoft.",
            "xunit."
        };

        const string InternalPackageName = "internal";

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionManager"/> class.
        /// </summary>
        /// <param name="grantedNames">
        /// An array of assembly names with unlimited privileges.
        /// </param>
        /// <param name="grantedPrefixes">
        /// An array of assembly prefixes with unlimited privileges.
        /// </param>
        public PermissionManager(string[] grantedNames = null, string[] grantedPrefixes = null)
        {
            if (grantedNames != null)
            {
                GrantedNames = GrantedNames.Concat(grantedNames).ToArray();
            }

            if (grantedPrefixes != null)
            {
                GrantedPrefixes = GrantedPrefixes.Concat(grantedPrefixes).ToArray();
            }
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
            string packageName = $"{moduleName}:{pluginFullname}";
            if (!IsLegalName(moduleName))
            {
                throw new SecurityException("Plugin identifier can't start with illegal prefixes.");
            }

            permissions[packageName] = new();

            foreach (var perm in plugin.Permissions)
            {
                permissions[packageName][perm] = new();
            }

            plugin.PermissionManager = this;
        }

        /// <summary>
        /// Checks the privileges of the caller for the giving <paramref name="permission"/>.
        /// </summary>
        /// <param name="permission">An instance of <see cref="Permission"/> that corresponds to the specific permission.</param>
        /// <returns></returns>
        public virtual bool HasPermission(Permission permission)
        {
            string packageName = GetPackageName(permission);
            return packageName == InternalPackageName || permissions[packageName][permission];
        }

        /// <summary>
        /// Adds the given <paramref name="permission"/> to the <see cref="DeclaredPermissions"/>.
        /// </summary>
        /// <param name="permission">An instance of the <see cref="Permission"/> with all properties.</param>
        public void RegisterPermission(Permission permission)
        {
            if (!DeclaredPermissions.ContainsKey(permission.ToString()))
            {
                DeclaredPermissions[permission.ToString()] = permission;
            }
        }

        /// <inheritdoc/>
        public Permission GetPermission(string permissionName)
        {
            if (DeclaredPermissions.ContainsKey(permissionName))
            {
                return DeclaredPermissions[permissionName];
            }
            else
            {
                throw new SecurityException($"The permission {permissionName} is not declared.");
            }
        }

        /// <inheritdoc/>
        public virtual bool RequestPermission(Permission permission)
        {
            string packageName = GetPackageName(permission);
#if DEBUG
            Console.WriteLine($"Permission {permission} requested by {packageName}");
#endif
            return true;
        }

        /// <summary>
        /// Gets the caller package name.
        /// </summary>
        /// <param name="permission">The specific permission to check security attributes of it.</param>
        /// <returns>The package name of the caller plugin. if the caller was not a plugin, it returns <see cref="InternalPackageName"/>.</returns>
        /// <exception cref="SecurityException"></exception>
        protected string GetPackageName(Permission permission)
        {
            StackTrace stack = new();
            MethodBase client = null;

            foreach (var frame in stack.GetFrames())
            {
                var frameCaller = frame.GetMethod();
                var frameName = frameCaller.Module.Name;
                if (frameName == null)
                {
                    continue;
                }

                if (GrantedNames.Contains(frameName.ToLower()) || !IsLegalName(frameName))
                {
                    continue;
                }

                client = frameCaller;
                break;
            }

            if (client == null)
            {
                // Everything is good, maybe...
                return InternalPackageName;
            }

            string packageName = $"{client.Module.Name}:{client.DeclaringType.FullName}";

            if (!permissions.ContainsKey(packageName))
            {
                throw new SecurityException($"The plugin \"{packageName}\" is not registered.");
            }

            var permissionStore = permissions[packageName];

            if (!permissionStore.ContainsKey(permission))
            {
                throw new SecurityException($"The permission \"{permission}\" is not registered for the plugin: {packageName}");
            }

            return packageName;
        }

        /// <summary>
        /// Checks the availability of the plugin name.
        /// </summary>
        /// <param name="pluginName">The name of the plugin file name.</param>
        /// <returns><see langword="true"/> if the plugin name is not starts with the denied prefixes (such as System. or Microsoft.) otherwise returns <see langword="false"/>.</returns>
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
