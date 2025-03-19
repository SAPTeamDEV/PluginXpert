using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using EasySign;

namespace SAPTeam.PluginXpert
{
    public class PluginContainer : Bundle
    {
        public PluginIndex PluginIndex { get; private set; } = new();

        public PluginContainer(string containerPath) : base(Path.GetFullPath(containerPath), Path.GetDirectoryName(Path.GetFullPath(containerPath)))
        {
            Updating += UpdatePluginIndex;

            Manifest.BundleFiles = true;
        }

        private void UpdatePluginIndex(ZipArchive zip)
        {
            var index = Export(PluginIndex);

            Manifest.GetConcurrentDictionary()[".plugins.ec"] = ComputeSHA512Hash(index))

            WriteEntry(zip, ".plugins.ec", index);
        }

        protected override void ReadBundle(ZipArchive zip)
        {
            base.ReadBundle(zip);

            ZipArchiveEntry entry;
            if ((entry = zip.GetEntry(".plugins.ec")) != null)
            {
                PluginIndex = JsonSerializer.Deserialize<PluginIndex>(entry.Open(), options);
            }
        }
    }
}
