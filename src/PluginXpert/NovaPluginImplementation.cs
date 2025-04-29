using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DouglasDwyer.CasCore;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public class NovaPluginImplementation : PluginImplementation
{
    public override string Interface => "nova";

    public override Version Version => new(0, 1);
    
    public override Version MinimumVersion => new(0, 1);

    public NovaPluginImplementation()
    {
        
    }

    protected internal override void Initialize(SecurityContext securityContext)
    {
        
    }

    public override bool CheckPluginType(Type type)
    {
        return typeof(INovaPlugin).IsAssignableFrom(type);
    }

    public override IGateway CreateGateway(PluginLoadSession session)
    {
        return new NovaGateway(session.Token);
    }

    public override void UpdateAssemblySecurityPolicy(PluginLoadSession session, CasPolicyBuilder policy)
    {
        base.UpdateAssemblySecurityPolicy(session, policy);
        policy.Allow(new TypeBinding(typeof(NovaGateway), Accessibility.Public));
    }
}
