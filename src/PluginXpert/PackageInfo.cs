namespace SAPTeam.PluginXpert;

public class PackageInfo
{
    public string Id { get; set; }

    public string Name { get; set; }

    public List<PluginEntry> Plugins { get; set; } = [];
}
