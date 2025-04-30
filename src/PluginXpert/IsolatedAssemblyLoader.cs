using System.Reflection;
using System.Runtime.Loader;

using DouglasDwyer.CasCore;

using Mono.Cecil;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents a plugin assembly loader that uses the CAS (Code Access Security) policy.
/// </summary>
public class IsolatedAssemblyLoader : CasAssemblyLoader
{
    private readonly PluginMetadata _pluginMetadata;
    private readonly AssemblyDependencyResolver _assemblyDependencyResolver;

    private bool _entryPointProcessed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsolatedAssemblyLoader"/> class.
    /// </summary>
    /// <param name="session">
    /// The plugin load session.
    /// </param>
    /// <param name="policy">
    /// The policy that will apply to any assemblies created with this loader.
    /// </param>
    /// <param name="isCollectible">
    /// Whether this context should be able to unload.
    /// </param>
    public IsolatedAssemblyLoader(PluginLoadSession session, CasPolicy policy, bool isCollectible) : base(policy, isCollectible)
    {
        _pluginMetadata = session.Metadata;

        _assemblyDependencyResolver = new AssemblyDependencyResolver(session.AssemblyPath!);
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        Assembly? assembly = base.Load(assemblyName);

        if (assembly == null)
        {
            string? resolvedPath = _assemblyDependencyResolver.ResolveAssemblyToPath(assemblyName);

            if (resolvedPath != null)
            {
                return LoadFromAssemblyPath(resolvedPath);
            }
        }

        return null;
    }

    /// <inheritdoc/>
    protected override void InstrumentAssembly(AssemblyDefinition assembly)
    {
        if (!_entryPointProcessed)
        {
            foreach (TypeDefinition? type in assembly.MainModule.Types)
            {
                if (type.Name == "<Module>") continue;

                type.Namespace = $"{_pluginMetadata.Interface}:{_pluginMetadata.Id}";
            }

            _entryPointProcessed = true;
        }

        base.InstrumentAssembly(assembly);
    }
}
