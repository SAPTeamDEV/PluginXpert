using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public class SecurityContext : IDisposable
{
    readonly Dictionary<string, SecurityObject> _securityObjects = [];
    readonly Dictionary<string, Permission> _permissions = [];
    readonly Dictionary<string, SecurityToken> _tokens = [];
    
    byte[] _secretKey;
    bool _disposed;

    public bool Disposed => _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityContext"/> class.
    /// </summary>
    public SecurityContext()
    {
        _secretKey = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(_secretKey);
    }

    protected void AddSecurityObject(SecurityObject securityObject)
    {
        if (securityObject == null)
        {
            throw new ArgumentNullException(nameof(securityObject));
        }

        if (securityObject.Parent != null)
        {
            throw new InvalidOperationException("The security object is already registered in a permission manager");
        }

        if (securityObject.Signature != null)
        {
            throw new InvalidOperationException("The security object is already signed");
        }

        var uid = securityObject.UniqueIdentifier;
        if (string.IsNullOrEmpty(uid))
        {
            throw new ArgumentException("The security object does not have a unique identifier");
        }

        if (_securityObjects.ContainsKey(uid))
        {
            throw new InvalidOperationException($"A security object with the unique identifier {uid} is already registered");
        }

        securityObject.Parent = this;
        securityObject.Signature = SignSecurityObject(securityObject).ToImmutableArray();

        _securityObjects[uid] = securityObject;
    }

    private byte[] SignSecurityObject(SecurityObject securityObject)
    {
        using var hmac = new HMACSHA256(_secretKey);

        var state = securityObject.CurrentState;
        byte[] data = Encoding.UTF8.GetBytes(state);

        return hmac.ComputeHash(data);
    }

    public SecurityObject GetSecurityObject(string uniqueIdentifier)
    {
        if (string.IsNullOrEmpty(uniqueIdentifier))
        {
            throw new ArgumentNullException(nameof(uniqueIdentifier));
        }

        return _securityObjects.TryGetValue(uniqueIdentifier, out SecurityObject? securityObject)
            ? securityObject
            : throw new KeyNotFoundException($"The security object with the unique identifier {uniqueIdentifier} was not found");
    }

    public IEnumerable<SecurityObject> GetSecurityObjects()
    {
        return _securityObjects.Values;
    }

    public IEnumerable<T> GetSecurityObjects<T>()
        where T : SecurityObject
    {
        return _securityObjects.Values.OfType<T>();
    }

    public bool ValidateSecurityObject(SecurityObject securityObject)
    {
        if (securityObject == null)
        {
            throw new ArgumentNullException(nameof(securityObject));
        }

        if (securityObject.Parent != this || !_securityObjects.ContainsKey(securityObject.UniqueIdentifier))
        {
            throw new SecurityException("The security object is not registered in this permission manager");
        }

        if (securityObject.Signature == null || securityObject.Signature?.Length == 0)
        {
            return false;
        }

        var signature = SignSecurityObject(securityObject);
        return securityObject.Signature?.SequenceEqual(signature) ?? false;
    }

    /// <summary>
    /// Registers the plugin in the permission manager and generates a security descriptor for it.
    /// </summary>
    /// <param name="plugin">A managed plugin instance</param>
    public virtual SecurityToken RegisterPlugin(PluginImplementation impl, IPlugin plugin, PluginEntry entry)
    {
        

        Permission[] permissions = GetPermissions(entry.Permissions);

        var token = new SecurityToken(impl.Interface,
                                      entry.Id,
                                      "test",
                                      permissions);

        AddSecurityObject(token);
        _tokens[token.UniqueIdentifier] = token;

        return token;
    }

    public virtual bool RevokeToken(SecurityToken securityToken)
    {
        return _tokens.Remove(securityToken.UniqueIdentifier);
    }

    public bool ValidateToken(SecurityToken securityToken)
    {
        var isInvalid = securityToken.Owner == null
                        || securityToken.Owner.Length == 0
                        || !_tokens.ContainsKey(securityToken.Owner)
                        || securityToken.Signature == null
                        || securityToken.Signature?.Length == 0
                        || !securityToken.Signature.SequenceEqual(SignSecurityObject(securityToken));

        return !isInvalid;
    }

    /// <summary>
    /// Adds the given <paramref name="permission"/> to the <see cref="_permissions"/>.
    /// </summary>
    /// <param name="permission">An instance of the <see cref="Permission"/>.</param>
    /// <returns>True if the permission was registered successfully, otherwise false.</returns>
    public bool RegisterPermission(Permission permission)
    {
        if (!ValidatePermissionObject(permission))
        {
            throw new ArgumentException("The permission is not valid");
        }

        if (!_permissions.ContainsKey(permission.ToString()))
        {
            AddSecurityObject(permission);
            _permissions[permission.PermissionId] = permission;
            return true;
        }

        return false;
    }

    public virtual bool HasPermission(SecurityToken securityToken, Permission permission)
    {
        if (!ValidateToken(securityToken))
        {
            throw new SecurityException($"The security descriptor {securityToken.Owner} is invalid");
        }

        Permission resolvedPerm = GetPermissions(permission.ToString()).Single();
        return securityToken.Permissions.Contains(resolvedPerm);
    }

    internal bool ValidatePermission(Permission permission)
    {
        return ValidatePermissionObject(permission)
            && _permissions.TryGetValue(permission, out Permission? registeredPermission)
            && permission == registeredPermission;
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
            if (_permissions.TryGetValue(permissionName, out Permission? value))
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

    private static bool ValidatePermissionObject(Permission permission)
    {
        return !string.IsNullOrEmpty(permission.Scope.Trim())
            && !string.IsNullOrEmpty(permission.Name.Trim());
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _tokens.Clear();
                _permissions.Clear();
            }

            _secretKey = null!;

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
