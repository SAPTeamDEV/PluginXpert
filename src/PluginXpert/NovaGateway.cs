using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents the Nova plugin's public API.
/// </summary>
public class NovaGateway : Gateway, INovaGateway
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NovaGateway"/> class.
    /// </summary>
    /// <param name="token">
    /// The token that this gateway is associated with.
    /// </param>
    public NovaGateway(Token token) : base(token)
    {

    }
}
