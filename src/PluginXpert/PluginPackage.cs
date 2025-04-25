using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using SAPTeam.EasySign;

namespace SAPTeam.PluginXpert
{
    public class PluginPackage : Bundle
    {
        public const string PackageInfoFileName = ".plugin-package.ec";

        public PackageInfo PackageInfo { get; private set; } = new();

        public PluginPackage(string containerPath) : base(containerPath)
        {
            Updating += UpdatePackageInfo;

            Manifest.StoreOriginalFiles = true;
        }

        private void UpdatePackageInfo(ZipArchive zip)
        {
            var index = Export(PackageInfo);

            Manifest.GetEntries()[PackageInfoFileName] = ComputeSHA512Hash(index);

            WriteEntry(zip, PackageInfoFileName, index);
        }

        public bool VerifyPackageInfo()
        {
            return VerifyFile(PackageInfoFileName);
        }

        protected override void Parse(ZipArchive zip)
        {
            base.Parse(zip);

            ZipArchiveEntry entry;
            if ((entry = zip.GetEntry(PackageInfoFileName)) != null)
            {
                PackageInfo = JsonSerializer.Deserialize<PackageInfo>(entry.Open(), SerializerOptions);
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
