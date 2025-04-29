using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using SAPTeam.EasySign;

namespace SAPTeam.PluginXpert;

public class PluginPackage : Bundle
{
    public const string PackageInfoFileName = ".plugin-package.ec";

    public PackageInfo PackageInfo { get; private set; } = new();

    public PluginPackage(string containerPath) : base(containerPath)
    {
        Updating += UpdatePackageInfo;

        Manifest.StoreOriginalFiles = true;

        ProtectedEntryNames.Add(PackageInfoFileName);
    }

    protected override byte[] GetManifestData()
    {
        byte[] index = Export(PackageInfo);

        Manifest.GetEntries()[PackageInfoFileName] = ComputeSHA512Hash(index);

        return base.GetManifestData();
    }

    private void UpdatePackageInfo(ZipArchive zip)
    {
        byte[] index = Export(PackageInfo);

        WriteEntry(zip, PackageInfoFileName, index);
    }

    public bool VerifyPackageInfo() => VerifyFile(PackageInfoFileName);

    protected override void Parse(ZipArchive zip)
    {
        base.Parse(zip);

        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(static ti =>
        {
            if (ti.Kind == JsonTypeInfoKind.Object && ti.Type == typeof(PluginMetadata))
            {
                foreach (var prop in ti.Properties)
                {
                    if (prop.Name == nameof(PluginMetadata.BuildTag) || prop.Name == nameof(PluginMetadata.TargetFrameworkVersion))
                    {
                        prop.IsRequired = true;
                    }
                }
            }
        });

        PackageInfo = null!;

        ZipArchiveEntry? entry;
        if ((entry = zip.GetEntry(PackageInfoFileName)) != null)
        {
            PackageInfo = JsonSerializer.Deserialize<PackageInfo>(entry.Open(), new JsonSerializerOptions(SerializerOptions)
            {
                TypeInfoResolver = resolver
            })!;
        }

        if (PackageInfo == null)
        {
            throw new InvalidDataException($"Cannot find {PackageInfoFileName} in the package.");
        }
    }

    public void ExtractPlugin(PluginMetadata plugin, string path)
    {
        string pluginDirectory = $"{plugin.Id}-{plugin.BuildTag}/";

        using (ZipArchive zip = GetZipArchive())
        {
            foreach (ZipArchiveEntry entry in zip.Entries)
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
