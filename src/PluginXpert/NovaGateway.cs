using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public class NovaGateway : Gateway, INovaGateway
{
    public NovaGateway(Token token) : base(token)
    {
    }
}
