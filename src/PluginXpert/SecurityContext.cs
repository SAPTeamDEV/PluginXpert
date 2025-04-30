using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Cryptography;
using System.Text;

using DouglasDwyer.CasCore;

using EnsureThat;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents a security infrastructure for plugins.
/// </summary>
public class SecurityContext : IEnumerable<SecurityObject>, IDisposable
{
    private readonly Dictionary<string, SecurityObject> _securityObjects = [];

    private byte[] _secretKey;

    /// <summary>
    /// Gets a value indicating whether this <see cref="SecurityContext"/> has been disposed.
    /// </summary>
    public bool Disposed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityContext"/> class.
    /// </summary>
    public SecurityContext()
    {
        _secretKey = new byte[64];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(_secretKey);
    }

    /// <summary>
    /// Signs and adds a <see cref="SecurityObject"/> to the security context.
    /// </summary>
    /// <param name="securityObject">
    /// The <see cref="SecurityObject"/> to add.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The <see cref="SecurityObject"/> is disposed, already registered in a security context or is already signed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A <see cref="SecurityObject"/> with the same unique identifier is already registered.
    /// </exception>
    protected void AddSecurityObject(SecurityObject securityObject)
    {
        CheckDisposed();

        Ensure.Any.IsNotNull(securityObject, nameof(securityObject));

        string uid = securityObject.UniqueIdentifier;
        if (string.IsNullOrEmpty(uid))
        {
            throw new ArgumentException("The security object does not have a unique identifier");
        }

        if (securityObject.Disposed)
        {
            throw new ArgumentException("The security object has been disposed");
        }

        if (securityObject.Parent != null)
        {
            throw new ArgumentException("The security object is already registered in a security context");
        }

        if (securityObject.Signature != null && securityObject.Signature?.Length > 0)
        {
            throw new ArgumentException("The security object is already signed");
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
        using HMACSHA256 hmac = new HMACSHA256(_secretKey);

        string state = securityObject.CurrentState;
        byte[] data = Encoding.UTF8.GetBytes(state);

        return hmac.ComputeHash(data);
    }

    /// <summary>
    /// Gets the <see cref="SecurityObject"/> with the specified unique identifier.
    /// </summary>
    /// <param name="uniqueIdentifier">
    /// The unique identifier of the <see cref="SecurityObject"/> to get.
    /// </param>
    /// <returns>
    /// The <see cref="SecurityObject"/> with the specified unique identifier.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// The <see cref="SecurityObject"/> with the specified unique identifier was not found.
    /// </exception>
    public SecurityObject GetSecurityObject(string uniqueIdentifier)
    {
        CheckDisposed();

        Ensure.String.IsNotNullOrEmpty(uniqueIdentifier, nameof(uniqueIdentifier));

        return _securityObjects.TryGetValue(uniqueIdentifier, out SecurityObject? securityObject)
            ? securityObject
            : throw new KeyNotFoundException($"The security object with the unique identifier {uniqueIdentifier} was not found");
    }

    /// <summary>
    /// Gets the <see cref="SecurityObject"/> with the specified unique identifier and casts it to the specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the <see cref="SecurityObject"/> to get.
    /// </typeparam>
    /// <param name="uniqueIdentifier">
    /// The unique identifier of the <typeparamref name="T"/> to get.
    /// </param>
    /// <returns>
    /// The <typeparamref name="T"/> with the specified unique identifier.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// The <see cref="SecurityObject"/> with the specified unique identifier was not found.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// The <see cref="SecurityObject"/> with the specified unique identifier is not of type <typeparamref name="T"/>.
    /// </exception>
    public T GetSecurityObject<T>(string uniqueIdentifier)
        where T : SecurityObject
    {
        SecurityObject securityObject = GetSecurityObject(uniqueIdentifier);

        return securityObject is T typedSecurityObject
            ? typedSecurityObject
            : throw new InvalidCastException($"The security object with the unique identifier {uniqueIdentifier} is not of type {typeof(T).Name}");
    }

    /// <summary>
    /// Tries to get the <see cref="SecurityObject"/> with the specified unique identifier.
    /// </summary>
    /// <param name="uniqueIdentifier">
    /// The unique identifier of the <see cref="SecurityObject"/> to get.
    /// </param>
    /// <param name="securityObject">
    /// The <see cref="SecurityObject"/> with the specified unique identifier.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <see cref="SecurityObject"/> was found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetSecurityObject(string uniqueIdentifier, [MaybeNullWhen(false)] out SecurityObject securityObject)
    {
        try
        {
            securityObject = GetSecurityObject(uniqueIdentifier);
            return true;
        }
        catch (KeyNotFoundException)
        {
            securityObject = null;
            return false;
        }
    }

    /// <summary>
    /// Tries to get the <see cref="SecurityObject"/> with the specified unique identifier and casts it to the specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the <see cref="SecurityObject"/> to get.
    /// </typeparam>
    /// <param name="uniqueIdentifier">
    /// The unique identifier of the <typeparamref name="T"/> to get.
    /// </param>
    /// <param name="securityObject">
    /// The <typeparamref name="T"/> with the specified unique identifier.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <typeparamref name="T"/> was found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetSecurityObject<T>(string uniqueIdentifier, [MaybeNullWhen(false)] out T securityObject)
        where T : SecurityObject
    {
        try
        {
            securityObject = GetSecurityObject<T>(uniqueIdentifier);
            return true;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (ObjectDisposedException)
        {
            throw;
        }
        catch (Exception)
        {
            securityObject = null;
            return false;
        }
    }

    /// <summary>
    /// Gets all registered security objects.
    /// </summary>
    /// <returns>
    /// An enumerable collection of all registered <see cref="SecurityObject"/> instances.
    /// </returns>
    public IEnumerable<SecurityObject> GetSecurityObjects()
    {
        CheckDisposed();

        return _securityObjects.Values;
    }

    /// <summary>
    /// Gets all registered security objects of the specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the <see cref="SecurityObject"/> to get.
    /// </typeparam>
    /// <returns>
    /// An enumerable collection of all registered <typeparamref name="T"/> instances.
    /// </returns>
    public IEnumerable<T> GetSecurityObjects<T>()
        where T : SecurityObject => GetSecurityObjects().OfType<T>();

    /// <summary>
    /// Verifies the signature of the specified <see cref="SecurityObject"/> against its current state.
    /// </summary>
    /// <param name="securityObject">
    /// The <see cref="SecurityObject"/> to verify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="SecurityException">
    /// The <see cref="SecurityObject"/> is not registered in this security context.
    /// </exception>
    public bool VerifySecurityObject(SecurityObject securityObject)
    {
        CheckDisposed();

        Ensure.Any.IsNotNull(securityObject, nameof(securityObject));

        if (securityObject.Parent != this || !_securityObjects.ContainsKey(securityObject.UniqueIdentifier))
        {
            throw new SecurityException($"The security object {securityObject.UniqueIdentifier} is not registered in this security context");
        }

        if (securityObject.Signature == null || securityObject.Signature?.Length == 0)
        {
            return false;
        }

        byte[] signature = SignSecurityObject(securityObject);
        return securityObject.Signature?.SequenceEqual(signature) ?? false;
    }

    /// <summary>
    /// Removes the specified <see cref="SecurityObject"/> from the security context.
    /// </summary>
    /// <param name="securityObject">
    /// The <see cref="SecurityObject"/> to remove.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <see cref="SecurityObject"/> was removed successfully; otherwise, <see langword="false"/>.
    /// </returns>
    protected internal bool RemoveSecurityObject(SecurityObject securityObject)
    {
        CheckDisposed();

        securityObject.Parent = null;
        securityObject.Signature = null;

        return _securityObjects.Remove(securityObject.UniqueIdentifier);
    }

    /// <summary>
    /// Creates a new assembly loader for the specified plugin.
    /// </summary>
    /// <param name="session">
    /// The plugin load session with a valid <see cref="PluginLoadSession.Token"/>.
    /// </param>
    /// <param name="policyBuilder">
    /// The policy builder to use for creating the assembly loader.
    /// </param>
    /// <returns>
    /// A highly secure assembly loader for the specified plugin, limited to the assembly permissions granted to the token.
    /// </returns>
    public IsolatedAssemblyLoader CreateAssemblyLoader(PluginLoadSession session, CasPolicyBuilder policyBuilder)
    {
        CheckDisposed();

        Ensure.Any.IsNotNull(session, nameof(session));

        CasPolicy policy = policyBuilder
            .WithDefaultSandbox()
            .Build();

        var loadContext = new IsolatedAssemblyLoader(session, policy, isCollectible: true);

        return loadContext;
    }

    /// <summary>
    /// Creates a security token for the specified plugin.
    /// </summary>
    /// <param name="session">
    /// The plugin load session with a valid <see cref="PluginLoadSession.Implementation"/>.
    /// </param>
    /// <returns>
    /// A security token containing the plugin identity and requested permissions.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The <see cref="PluginLoadSession"/> does not contain a valid implementation.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A <see cref="SecurityObject"/> with the same unique identifier is already registered.
    /// </exception>
    public virtual Token RegisterPlugin(PluginLoadSession session)
    {
        CheckDisposed();

        Ensure.Any.IsNotNull(session, nameof(session));

        IEnumerable<Permission> permissions = ResolvePermissions(session.Metadata.Permissions);

        Token token = new Token(session.Implementation?.Name ?? throw new ArgumentException("Invalid session"),
                                      session.Metadata.Id,
                                      ComputePluginDigest(session),
                                      permissions);

        AddSecurityObject(token);

        return token;
    }

    private static string ComputePluginDigest(PluginLoadSession session)
    {
        byte[] allBytes = session.Package.Signatures.Entries.Keys.Select(x => session.Package.GetCertificate(x).Thumbprint)
            .Concat([session.Implementation!.Name, session.Metadata.Id])
            .SelectMany(Encoding.UTF8.GetBytes)
            .ToArray();

        byte[] hash = SHA256.HashData(allBytes);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }

    /// <summary>
    /// Requests a permission for the specified token.
    /// </summary>
    /// <param name="token">
    /// The token to request the permission for.
    /// </param>
    /// <param name="permissionId">
    /// The ID of the permission to request.
    /// </param>
    /// <returns>
    /// The new token with the requested permission, <paramref name="token"/> if the permission is already granted or <see langword="null"/> if the permission request was denied.
    /// </returns>
    /// <exception cref="SecurityException">
    /// The token is invalid or the permission is not registered or invalid.
    /// </exception>
    public Token? RequestPermission(Token token, string permissionId)
    {
        CheckDisposed();

        Ensure.Any.IsNotNull(token, nameof(token));
        Ensure.String.IsNotNullOrEmpty(permissionId, nameof(permissionId));

        if (!token.IsValid(this))
        {
            throw new SecurityException($"The token {token.TokenId} is invalid");
        }

        Permission permission = ResolvePermission(permissionId);

        if (token.Permissions.Contains(permission))
        {
            return token;
        }

        if (!permission.RuntimePermission)
        {
            throw new SecurityException($"The permission {permission.PermissionId} cannot be requested at runtime");
        }

        if (!HandlePermissionRequest(token, permission))
        {
            return null;
        }

        Token newToken = new Token(token.Domain,
                                 token.Owner,
                                 token.Digest,
                                 token.Permissions.Add(permission));

        if (!token.Revoke())
        {
            throw new SecurityException($"The token {token.TokenId} could not be revoked");
        }

        AddSecurityObject(newToken);

        return newToken;
    }

    /// <summary>
    /// Handles the permission request for the specified token and permission.
    /// </summary>
    /// <remarks>
    /// This method can be overridden to implement custom permission request handling logic.
    /// By default, it always returns <see langword="true"/>, allowing the permission request to proceed.
    /// </remarks>
    /// <param name="token">
    /// The token requesting the permission.
    /// </param>
    /// <param name="permission">
    /// The permission being requested.
    /// </param>
    /// <returns>
    /// <see langword="true"/> to allow the permission request; <see langword="false"/> to deny it.
    /// </returns>
    protected bool HandlePermissionRequest(Token token, Permission permission) => true;

    /// <summary>
    /// Registers a new permission in the security context.
    /// </summary>
    /// <param name="permission">
    /// The permission to register.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The <see cref="SecurityObject"/> is already registered in a security context or is already signed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A <see cref="SecurityObject"/> with the same unique identifier is already registered.
    /// </exception>
    public void RegisterPermission(Permission permission) => AddSecurityObject(permission);

    /// <summary>
    /// Checks if the specified token has the specified permission.
    /// </summary>
    /// <param name="token">
    /// The token to check.
    /// </param>
    /// <param name="permissionId">
    /// The registered permission ID to check.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the token has the specified permission; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="SecurityException">
    /// The token is invalid or the permission is not registered or invalid.
    /// </exception>
    public bool HasPermission(Token token, string permissionId)
    {
        CheckDisposed();

        Ensure.Any.IsNotNull(token, nameof(token));
        Ensure.String.IsNotNullOrEmpty(permissionId, nameof(permissionId));

        Permission resolvedPermission = ResolvePermission(permissionId);
        return HasPermission(token, resolvedPermission);
    }

    /// <summary>
    /// Checks if the specified token has the specified permission.
    /// </summary>
    /// <param name="token">
    /// The token to check.
    /// </param>
    /// <param name="permission">
    /// The registered permission to check.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the token has the specified permission; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="SecurityException">
    /// The token is invalid or the permission is not registered or invalid.
    /// </exception>
    public virtual bool HasPermission(Token token, Permission permission)
    {
        CheckDisposed();

        Ensure.Any.IsNotNull(token, nameof(token));
        Ensure.Any.IsNotNull(permission, nameof(permission));

        return !token.IsValid(this)
            ? throw new SecurityException($"The token {token.TokenId} is invalid")
            : token.Permissions.Contains(permission);
    }

    /// <summary>
    /// Resolves the specified permission IDs to registered <see cref="Permission"/> instances.
    /// </summary>
    /// <param name="permissionIds">
    /// The permission IDs to resolve.
    /// </param>
    /// <returns>
    /// An enumerable collection of registered <see cref="Permission"/> instances.
    /// </returns>
    /// <exception cref="SecurityException">
    /// One of the permission IDs is not registered or the permission is invalid.
    /// </exception>
    public IEnumerable<Permission> ResolvePermissions(IEnumerable<string> permissionIds) => permissionIds.Select(ResolvePermission).ToArray();

    /// <summary>
    /// Resolves the specified permission ID to a registered <see cref="Permission"/>.
    /// </summary>
    /// <param name="permissionId">
    /// The permission ID to resolve.
    /// </param>
    /// <returns>
    /// The registered <see cref="Permission"/> with the specified ID.
    /// </returns>
    /// <exception cref="SecurityException">
    /// The permission ID is not registered or the permission is invalid.
    /// </exception>
    public Permission ResolvePermission(string permissionId)
    {
        CheckDisposed();

        Ensure.String.IsNotNullOrEmpty(permissionId, nameof(permissionId));

        Permission? permission = GetSecurityObjects<Permission>()
            .FirstOrDefault(p => p.PermissionId == permissionId);

        return permission == null
            ? throw new SecurityException($"The permission {permissionId} is not registered")
            : !permission.IsValid() ? throw new SecurityException($"The permission {permission.PermissionId} is invalid") : permission;
    }

    /// <summary>
    /// Checks if the <see cref="SecurityContext"/> has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The <see cref="SecurityContext"/> has been disposed.
    /// </exception>
    protected void CheckDisposed()
    {
        if (Disposed)
        {
            throw new ObjectDisposedException(nameof(SecurityContext));
        }
    }

    /// <summary>
    /// Disposes the <see cref="SecurityContext"/> and releases all resources.
    /// </summary>
    /// <param name="disposing">
    /// A value indicating whether the method is called from the <see cref="Dispose()"/> method.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            if (disposing)
            {
                foreach (SecurityObject securityObject in _securityObjects.Values)
                {
                    securityObject.Dispose();
                }

                _securityObjects.Clear();
            }

            _secretKey = null!;

            Disposed = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public IEnumerator<SecurityObject> GetEnumerator() => _securityObjects.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
