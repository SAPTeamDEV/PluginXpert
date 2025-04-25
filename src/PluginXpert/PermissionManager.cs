using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents mechanisms to manage plugins permissions.
/// </summary>
public class PermissionManager
{
    /// <summary>
    /// Gets a dictionary containing declared permissions.
    /// </summary>
    protected Dictionary<string, Permission> DeclaredPermissions { get; } = [];

    readonly Dictionary<string, SecurityDescriptor> _plugins = [];
    readonly byte[] _secretKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionManager"/> class.
    /// </summary>
    public PermissionManager()
    {
        _secretKey = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(_secretKey);
    }

    /// <summary>
    /// Registers the plugin in the permission manager and generates a security descriptor for it.
    /// </summary>
    /// <param name="plugin">A managed plugin instance</param>
    public virtual SecurityDescriptor RegisterPlugin(PluginImplementation impl, IPlugin plugin, PluginEntry entry)
    {
        var type = plugin.GetType();
        string moduleName = type.Module.Name;
        string pluginFullname = type.FullName ?? throw new ApplicationException("Plugin type does not have a full name");
        string securityIdentifier = $"{impl}{moduleName}:{pluginFullname}";

        Permission[] permissions = GetPermissions(entry.Permissions);

        var securityDescriptor = _plugins[securityIdentifier] = new()
        {
            Owner = securityIdentifier,
            Permissions = permissions,
            Signature = SignSecurityDescriptor(SecurityDescriptor.GetString(securityIdentifier, permissions))
        };

        return securityDescriptor;
    }

    public virtual void RevokeSecurityDescriptor(SecurityDescriptor securityDescriptor)
    {
        if (_plugins.ContainsValue(securityDescriptor))
        {
            string securityIdentifier = securityDescriptor.Owner;
            _plugins.Remove(securityIdentifier);
        }
    }

    public virtual bool HasPermission(SecurityDescriptor securityDescriptor, Permission permission)
    {
        if (!ValidateSecurityDescriptor(securityDescriptor))
        {
            throw new SecurityException($"The security descriptor {securityDescriptor.Owner} is invalid");
        }

        Permission resolvedPerm = GetPermissions(permission.ToString()).Single();
        return securityDescriptor.Permissions.Contains(resolvedPerm);
    }

    public bool ValidateSecurityDescriptor(SecurityDescriptor securityDescriptor)
    {
        if (securityDescriptor.Owner == null
            || securityDescriptor.Owner.Length == 0
            || !_plugins.ContainsKey(securityDescriptor.Owner)
            || securityDescriptor.Signature == null
            || securityDescriptor.Signature.Length == 0
            || !securityDescriptor.Signature.SequenceEqual(SignSecurityDescriptor(securityDescriptor.ToString())))
        {
            return false;
        }

        return true;
    }

    private byte[] SignSecurityDescriptor(string securityDescriptorString)
    {
        using var hmac = new HMACSHA256(_secretKey);
        byte[] data = Encoding.UTF8.GetBytes(securityDescriptorString);
        return hmac.ComputeHash(data);
    }

    /// <summary>
    /// Adds the given <paramref name="permission"/> to the <see cref="DeclaredPermissions"/>.
    /// </summary>
    /// <param name="permission">An instance of the <see cref="Permission"/>.</param>
    /// <returns>True if the permission was registered successfully, otherwise false.</returns>
    public bool RegisterPermission(Permission permission)
    {
        if (!permission.IsValid)
        {
            throw new ArgumentException("The permission is not valid");
        }

        if (!DeclaredPermissions.ContainsKey(permission.ToString()))
        {
            DeclaredPermissions[permission.ToString()] = permission;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the corresponding permission object.
    /// </summary>
    /// <param name="permissionNames">The fully-qualified name of the permission.</param>
    /// <returns>An instance of <see cref="Permission"/> or a <see cref="SecurityException"/> when the requested permission is not declared.</returns>
    public Permission[] GetPermissions(params string[] permissionNames)
    {
        List<Permission> permissions = [];

        foreach (var permissionName in permissionNames)
        {
            if (DeclaredPermissions.TryGetValue(permissionName, out Permission value))
            {
                permissions.Add(value);
            }
            else
            {
                throw new SecurityException($"The permission {permissionName} is not declared");
            }
        }

        return permissions.ToArray();
    }
}
