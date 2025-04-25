using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert;

public class DefaultPluginImplementation : PluginImplementation
{
    public override string Interface => "pluginxpert";

    public override Version Version => new(3, 0);
    
    public override Version MinimumVersion => new(3, 0);
}
