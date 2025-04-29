using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents a gateway that provides access to the plugin's implementation public API.
/// </summary>
public class Gateway : IGateway
{
    private bool _disposed;

    /// <inheritdoc/>
    public bool Disposed => _disposed;

    /// <summary>
    /// Gets the token that this gateway is associated with.
    /// </summary>
    protected Token Token { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Gateway"/> class.
    /// </summary>
    /// <param name="token">
    /// The token that this gateway is associated with.
    /// </param>
    public Gateway(Token token)
    {
        Token = token;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetGrantedPermissions()
    {
        CheckToken();

        return Token.Permissions.Select(p => p.PermissionId);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetRegisteredPermissions()
    {
        CheckToken();

        return Token.Parent!.GetSecurityObjects<Permission>().Select(p => p.PermissionId) ?? [];
    }

    /// <inheritdoc/>
    public bool HasPermission(string permissionId)
    {
        CheckToken();

        return Token.Parent!.HasPermission(Token, permissionId);
    }

    /// <inheritdoc/>
    public bool RequestPermission(string permissionId)
    {
        CheckToken();

        if (HasPermission(permissionId))
        {
            return true;
        }

        var newToken = Token.Parent!.RequestPermission(Token, permissionId);

        if (newToken == null)
        {
            return false;
        }

        Token = newToken;
        return true;
    }

    /// <summary>
    /// Checks if the token is usable.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The token is not set, has been revoked, or does not bound to a security context.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The token has been disposed.
    /// </exception>
    protected void CheckToken()
    {
        if (Token == null)
        {
            throw new InvalidOperationException("The token is not set.");
        }

        if (Token.Disposed)
        {
            throw new ObjectDisposedException(nameof(Token), "The token has been disposed.");
        }

        if (Token.Revoked)
        {
            throw new InvalidOperationException("The token has been revoked.");
        }

        if (Token.Parent == null)
        {
            throw new InvalidOperationException("The token does not bound to a security context.");
        }
    }

    /// <summary>
    /// Disposes the gateway and releases its resources.
    /// </summary>
    /// <param name="disposing">
    /// A value indicating whether the method is called from the Dispose method.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            Token = null!;

            _disposed = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
