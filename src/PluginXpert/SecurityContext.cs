using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

using DouglasDwyer.CasCore;

using Mono.Cecil;

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

    public CasAssemblyLoader CreateAssemblyLoader(PluginLoadSession session)
    {
        var policy = new CasPolicyBuilder()
            .WithDefaultSandbox()
            .Build();

        CasAssemblyLoader loadContext = new CasLoader(session.Entry, policy, isCollectible: true);

        return loadContext;
    }

    public virtual SecurityToken RegisterPlugin(PluginLoadSession session)
    {
        Permission[] permissions = GetPermissions(session.Entry.Permissions);

        var token = new SecurityToken(session.Implementation?.Interface ?? throw new ArgumentException("Invalid session"),
                                      session.Entry.Id,
                                      ComputePluginDigest(session),
                                      permissions);

        AddSecurityObject(token);
        _tokens[token.UniqueIdentifier] = token;

        return token;
    }

    private static string ComputePluginDigest(PluginLoadSession session)
    {
        byte[] allBytes = session.Package.Signatures.Entries.Keys.Select(x => session.Package.GetCertificate(x).Thumbprint)
            .Concat([session.Implementation!.Interface, session.Entry.Id])
            .SelectMany(Encoding.UTF8.GetBytes)
            .ToArray();

        byte[] hash = SHA256.HashData(allBytes);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }

    public virtual bool RevokeToken(SecurityToken securityToken)
    {
        if (!securityToken.IsValid())
        {
            throw new SecurityException($"The token {securityToken.TokenId} is invalid");
        }

        _securityObjects.Remove(securityToken.UniqueIdentifier);
        return _tokens.Remove(securityToken.UniqueIdentifier);
    }

    public bool ValidateToken(SecurityToken securityToken)
    {
        return _securityObjects.ContainsKey(securityToken.UniqueIdentifier)
               && _tokens.TryGetValue(securityToken.UniqueIdentifier, out SecurityToken? registeredToken)
               && registeredToken == securityToken;
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
        if (!securityToken.IsValid())
        {
            throw new SecurityException($"The token {securityToken.TokenId} is invalid");
        }

        Permission resolvedPerm = GetPermissions(permission.ToString()).Single();
        return securityToken.Permissions.Contains(resolvedPerm);
    }

    internal bool ValidatePermission(Permission permission)
    {
        return ValidatePermissionObject(permission)
            && _securityObjects.TryGetValue(permission.UniqueIdentifier, out _)
            && _permissions.TryGetValue(permission, out Permission? registeredPermission)
            && registeredPermission == permission;
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
