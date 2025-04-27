using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnsureThat;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public class SecurityToken : SecurityObject
{
    /// <inheritdoc/>
    public override string UniqueIdentifier
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append($"token$");
            sb.Append($"{Owner}@{Domain}");
            if (Digest != null)
            {
                sb.Append($"-{Digest}");
            }
            return sb.ToString();
        }
    }
    
    /// <inheritdoc/>
    public override string CurrentState
    {
        get
        {
            Dictionary<string, string> properties = [];

            if (Permissions.Length > 0)
            {
                var permissions = string.Join(", ", Permissions.Select(p => p.PermissionId));
                properties = new Dictionary<string, string>
                {
                    ["permissions"] = permissions
                };
            }

            return CreateStateString(properties);
        }
    }

    public string TokenId
    {
        get
        {
            var sb = new StringBuilder();

            sb.Append($"{Owner}@{Domain}");
            if (Digest != null)
            {
                sb.Append($"-{Digest}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets the domain that this token belongs to.
    /// </summary>
    public string Domain { get; }

    /// <summary>
    /// Gets the owner entity name of this token.
    /// </summary>
    public string Owner { get; }

    /// <summary>
    /// Gets the digest of this token's owner.
    /// </summary>
    public string? Digest { get; }

    /// <summary>
    /// Gets the permissions granted to this token.
    /// </summary>
    public ImmutableArray<Permission> Permissions { get; }

    public SecurityToken(string domain,
                         string owner,
                         string? digest,
                         Permission[]? permissions)
    {
        Ensure.String.IsNotNullOrEmpty(domain, nameof(domain));
        Ensure.String.IsNotNullOrEmpty(owner, nameof(owner));

        Domain = domain.Trim().ToLowerInvariant();
        Owner = owner.Trim().ToLowerInvariant();
        Digest = digest?.Trim().ToLowerInvariant();
        Permissions = permissions?.ToImmutableArray() ?? [];
    }

    public override bool IsValid()
    {
        return base.IsValid()
            && Parent!.ValidateToken(this);
    }

    public bool Revoke()
    {
        if (Disposed)
        {
            return false;
        }
        if (Parent == null)
        {
            return false;
        }

        return Parent.RevokeToken(this);
    }

    public override string ToString()
    {
        return TokenId;
    }
}
