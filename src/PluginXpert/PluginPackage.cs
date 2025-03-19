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
    public class PluginPackage : Bundle
    {
        public PackageInfo PackageInfo { get; private set; } = new();

        public PluginPackage(string containerPath) : base(Path.GetFullPath(containerPath), Path.GetDirectoryName(Path.GetFullPath(containerPath)))
        {
            Updating += UpdatePackageInfo;

            Manifest.BundleFiles = true;
        }

        private void UpdatePackageInfo(ZipArchive zip)
        {
            var index = Export(PackageInfo);

            Manifest.GetConcurrentDictionary()[".plugin-package.ec"] = ComputeSHA512Hash(index);

            WriteEntry(zip, ".plugin-package.ec", index);
        }

        protected override void ReadBundle(ZipArchive zip)
        {
            base.ReadBundle(zip);

            ZipArchiveEntry entry;
            if ((entry = zip.GetEntry(".plugin-package.ec")) != null)
            {
                PackageInfo = JsonSerializer.Deserialize<PackageInfo>(entry.Open(), options);
            }
        }

        public void ExtractPlugin(PluginEntry plugin, string path)
        {
            string pluginDirectory = $"{plugin.Id}-{plugin.BuildRef}/";

            using (ZipArchive zip = GetZipArchive())
            {
                foreach (var entry in zip.Entries)
                {
                    if (entry.FullName.StartsWith(pluginDirectory, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(entry.Name))
                    {
                        if (!VerifyFile(entry.FullName))
                        {
                            throw new InvalidDataException($"Invalid File: {entry.FullName}");
                        }

                        string destinationPath = Path.Combine(path, entry.FullName);

                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }
            }
        }
    }
}
