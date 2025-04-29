using DouglasDwyer.CasCore;

using Mono.Cecil;

namespace SAPTeam.PluginXpert;

public class CasLoader : CasAssemblyLoader
{
    private readonly PluginMetadata _pluginMetadata;

    public CasLoader(PluginMetadata metadata, CasPolicy policy, bool isCollectible) : base(policy, isCollectible) => _pluginMetadata = metadata;

    /// <inheritdoc/>
    protected override void InstrumentAssembly(AssemblyDefinition assembly)
    {
        foreach (TypeDefinition? type in assembly.MainModule.Types)
        {
            if (type.Name == "<Module>") continue;

            type.Namespace = $"{_pluginMetadata.Interface}:{_pluginMetadata.Id}";
        }

        base.InstrumentAssembly(assembly);
    }
}
