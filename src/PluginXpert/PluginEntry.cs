using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert;

public class PluginEntry
{
    public string Id { get; set; }

    public string BuildRef { get; set; }

    public Version Version { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Interface {  get; set; }

    public Version InterfaceVersion { get; set; }

    public Version TargetFrameworkVersion { get; set; }

    public string Assembly {  get; set; }

    public string Class { get; set; }

    public string[] Permissions { get; set; } = [];
}
