using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DouglasDwyer.CasCore;

using Mono.Cecil;

namespace SAPTeam.PluginXpert
{
    public class CasLoader : CasAssemblyLoader
    {
        PluginEntry _pluginEntry;

        public CasLoader(PluginEntry entry, CasPolicy policy, bool isCollectible) : base(policy, isCollectible)
        {
            _pluginEntry = entry;
        }

        /// <inheritdoc/>
        protected override void InstrumentAssembly(AssemblyDefinition assembly)
        {
            if (!_pluginEntry.KeepNamespace)
            {
                foreach (var type in assembly.MainModule.Types)
                {
                    if (type.Name == "<Module>") continue;

                    type.Namespace = $"{_pluginEntry.Interface}:{_pluginEntry.Id}";
                }
            }

            base.InstrumentAssembly(assembly);
        }
    }
}
