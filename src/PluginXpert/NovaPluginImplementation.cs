using DouglasDwyer.CasCore;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents the implementation of the Nova plugin.
/// </summary>
public class NovaPluginImplementation : PluginImplementation
{
    /// <inheritdoc/>
    public override string Name => "nova";

    /// <inheritdoc/>
    public override Version Version => new(0, 1);

    /// <inheritdoc/>
    public override Version MinimumVersion => new(0, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="NovaPluginImplementation"/> class.
    /// </summary>
    public NovaPluginImplementation()
    {

    }

    /// <inheritdoc/>
    protected internal override void RegisterPermissions(SecurityContext securityContext)
    {

    }

    /// <inheritdoc/>
    public override bool CheckPluginType(Type type) => typeof(INovaPlugin).IsAssignableFrom(type);

    /// <inheritdoc/>
    public override IGateway CreateGateway(PluginLoadSession session) => new NovaGateway(session.Token!);

    /// <inheritdoc/>
    public override void UpdateAssemblySecurityPolicy(PluginLoadSession session, CasPolicyBuilder policy)
    {
        base.UpdateAssemblySecurityPolicy(session, policy);
        policy.Allow(new TypeBinding(typeof(NovaGateway), Accessibility.Public));
    }
}
