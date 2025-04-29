namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents a plugin metadata.
/// </summary>
/// <param name="Id">
/// The plugin identifier.
/// </param>
/// <param name="BuildTag">
/// The build tag.
/// </param>
/// <param name="Version">
/// The plugin version.
/// </param>
/// <param name="Name">
/// The plugin's friendly name.
/// </param>
/// <param name="Description">
/// The plugin's description.
/// </param>
/// <param name="Interface">
/// The plugin implementation's interface id.
/// </param>
/// <param name="InterfaceVersion">
/// The supported plugin implementation's interface version.
/// </param>
/// <param name="TargetFrameworkVersion">
/// The minimum supported target framework version.
/// </param>
/// <param name="Assembly">
/// The plugin main assembly's file name.
/// </param>
/// <param name="Class">
/// The plugin's class name.
/// </param>
/// <param name="Permissions">
/// The plugin's declared permissions.
/// </param>
public readonly record struct PluginMetadata(string Id,
                                             string BuildTag,
                                             Version Version,
                                             string? Name,
                                             string? Description,
                                             string Interface,
                                             Version InterfaceVersion,
                                             Version TargetFrameworkVersion,
                                             string Assembly,
                                             string Class,
                                             IEnumerable<string> Permissions)
{

}
