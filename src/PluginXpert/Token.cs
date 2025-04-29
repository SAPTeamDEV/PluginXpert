using System.Collections.Immutable;
using System.Text;

using EnsureThat;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents a security token that can be used to securely authenticate plugins and their permissions.
/// </summary>
public sealed class Token : SecurityObject
{
    /// <inheritdoc/>
    public override string UniqueIdentifier => $"token${TokenId}";

    /// <inheritdoc/>
    public override string CurrentState
    {
        get
        {
            Dictionary<string, string> properties = [];

            if (Permissions.Length > 0)
            {
                string permissions = string.Join(", ", Permissions.Select(p => p.PermissionId));
                properties = new Dictionary<string, string>
                {
                    ["permissions"] = permissions
                };
            }

            return CreateStateString(properties);
        }
    }

    /// <summary>
    /// Gets the unique identifier of this token.
    /// </summary>
    public string TokenId
    {
        get
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{Owner}");
            if (Digest != null)
            {
                sb.Append($"-{Digest}");
            }

            sb.Append($"@{Domain}");

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

    /// <summary>
    /// Gets a value indicating whether this token has been revoked.
    /// </summary>
    public bool Revoked { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    /// <param name="domain">
    /// The domain that this token belongs to.
    /// </param>
    /// <param name="owner">
    /// The owner entity name of this token.
    /// </param>
    /// <param name="digest">
    /// The digest of this token's owner.
    /// </param>
    /// <param name="permissions">
    /// The permissions granted to this token.
    /// </param>
    internal Token(string domain,
                   string owner,
                   string? digest,
                   IEnumerable<Permission>? permissions)
    {
        Ensure.String.IsNotNullOrEmpty(domain, nameof(domain));
        Ensure.String.IsNotNullOrEmpty(owner, nameof(owner));

        Domain = domain.Trim().ToLowerInvariant();
        Owner = owner.Trim().ToLowerInvariant();
        Digest = digest?.Trim().ToLowerInvariant();
        Permissions = permissions?.ToImmutableArray() ?? [];
    }

    /// <inheritdoc/>
    public override bool IsValid()
    {
        return base.IsValid()
               && Parent!.TryGetSecurityObject<Token>(UniqueIdentifier, out Token? registeredToken)
               && registeredToken == this;
    }

    /// <summary>
    /// Revokes this token.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the token was revoked successfully; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Revoke()
    {
        if (Revoked || Disposed)
        {
            return false;
        }

        bool revoked = Parent?.RemoveSecurityObject(this) ?? false;

        if (revoked)
        {
            Revoked = true;
        }

        return revoked;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        if (disposing)
        {
            _ = Revoke();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override string ToString() => TokenId;
}
