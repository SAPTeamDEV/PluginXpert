using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using SAPTeam.EasySign;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents a bundle for plugins.
/// </summary>
public class PluginPackage : Bundle
{
    /// <summary>
    /// The name of the file that contains the package information.
    /// </summary>
    public const string PackageInfoFileName = ".plugin-package.ec";

    private JsonSerializerOptions _packageSerializerOptions;

    /// <summary>
    /// Gets the package information.
    /// </summary>
    public PackageInfo PackageInfo { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginPackage"/> class.
    /// </summary>
    /// <param name="packagePath">
    /// The path to the package file.
    /// </param>
    public PluginPackage(string packagePath) : base(packagePath)
    {
        Updating += UpdatePackageInfo;

        Manifest.StoreOriginalFiles = true;
        _ = ProtectedEntryNames.Add(PackageInfoFileName);

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
        _packageSerializerOptions = new JsonSerializerOptions(SerializerOptions)
        {
            TypeInfoResolver = resolver
        };
    }

    /// <inheritdoc/>
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

    /// <summary>
    /// Verifies the package information integrity.
    /// </summary>
    /// <returns></returns>
    public bool VerifyPackageInfo() => VerifyFile(PackageInfoFileName);

    /// <inheritdoc/>
    protected override void Parse(ZipArchive zip)
    {
        base.Parse(zip);

        Manifest.StoreOriginalFiles = true;

        PackageInfo = null!;

        ZipArchiveEntry? entry;
        if ((entry = zip.GetEntry(PackageInfoFileName)) != null)
        {
            PackageInfo = JsonSerializer.Deserialize<PackageInfo>(entry.Open(), _packageSerializerOptions) ?? throw new JsonException("Invalid Package Info");
        }
        else
        {
            throw new InvalidDataException($"Missing {PackageInfoFileName}");
        }
    }

    /// <summary>
    /// Extracts the plugin files from the package to the specified path.
    /// </summary>
    /// <param name="plugin">
    /// The plugin metadata.
    /// </param>
    /// <param name="path">
    /// The path to extract the plugin files to.
    /// </param>
    /// <exception cref="InvalidDataException">
    /// Thrown when the plugin file is invalid or the extraction fails.
    /// </exception>
    public void ExtractPlugin(PluginMetadata plugin, string path)
    {
        string pluginDirectory = $"{plugin.Id}-{plugin.BuildTag}/";

        using ZipArchive zip = GetZipArchive();

        var pluginEntries = zip.Entries
            .Where(entry => entry.FullName.StartsWith(pluginDirectory, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(entry.Name));

        foreach (var entry in pluginEntries)
        {
            if (!VerifyFile(entry.FullName))
            {
                throw new InvalidDataException($"Invalid File: {entry.FullName}");
            }

            string destinationPath = Path.Combine(path, entry.FullName);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            entry.ExtractToFile(destinationPath, overwrite: true);
        }

    }
}
