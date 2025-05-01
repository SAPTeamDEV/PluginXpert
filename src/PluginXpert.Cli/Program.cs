using System.Collections.Concurrent;
using System.CommandLine;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyModel;

using SAPTeam.PluginXpert;

using Spectre.Console;

using Color = Spectre.Console.Color;

namespace PluginXpert.Cli;

internal class Program
{
    public static PluginPackage Package { get; set; }

    private static RootCommand GetCommands()
    {
        RootCommand root = new RootCommand("Easy Digital Signing Tool");

        #region Shared Options
        Argument<string> pckArg = new Argument<string>("package", "Plugin Package path");
        #endregion

        Argument<string> plgArg = new Argument<string>("plugin", "Plugin directory");

        Option<string> plgCfgOpt = new Option<string>("--config", "Plugin's config path");
        plgCfgOpt.AddAlias("-c");
        plgCfgOpt.IsRequired = true;

        Command addCmd = new Command("add", "Add new plugin to the package")
        {
            pckArg,
            plgArg,
            plgCfgOpt,
        };

        addCmd.SetHandler((packagePath, pluginPath, pluginConfigPath) =>
        {
            InitBundle(packagePath);
            Add(pluginPath, pluginConfigPath);
        }, pckArg, plgArg, plgCfgOpt);
        root.AddCommand(addCmd);

        Argument<string> idArg = new Argument<string>("id", "Package Identifier");

        Option<string> nameOpt = new Option<string>("--name", "Package display name");
        nameOpt.AddAlias("-n");
        nameOpt.IsRequired = true;

        Command createCmd = new Command("create", "Create new plugin package")
        {
            pckArg,
            idArg,
            nameOpt,
        };

        createCmd.SetHandler((packagePath, id, name) =>
        {
            InitBundle(packagePath);
            Create(id, name);
        }, pckArg, idArg, nameOpt);
        root.AddCommand(createCmd);

        Argument<string> cfgDestArg = new Argument<string>("file", "Destination path");
        cfgDestArg.SetDefaultValue("plugin.json");

        Command generateCmd = new Command("generate", "Generate plugin config file")
        {
            cfgDestArg,
        };

        generateCmd.SetHandler(GeneratePluginConfig, cfgDestArg);
        root.AddCommand(generateCmd);

        Option<string> pfxOpt = new Option<string>("--pfx", "PFX File contains certificate and private key");
        Option<string> pfxPassOpt = new Option<string>("--pfx-password", "PFX File password");
        Option<bool> pfxNoPassOpt = new Option<bool>("--no-password", "Ignore PFX File password prompt");

        Command signCmd = new Command("sign", "Sign bundle with certificate")
        {
            pckArg,
            pfxOpt,
            pfxPassOpt,
            pfxNoPassOpt,
        };

        signCmd.SetHandler((packagePath, pfxFilePath, pfxFilePassword, pfxNoPasswordPrompt) =>
        {
            InitBundle(packagePath);

            X509Certificate2Collection collection = [];

            if (!string.IsNullOrEmpty(pfxFilePath))
            {
                string pfpass = !string.IsNullOrEmpty(pfxFilePassword) ? pfxFilePassword : !pfxNoPasswordPrompt ? SecurePrompt("Enter PFX File password (if needed): ") : "";

                X509Certificate2Collection tempCollection = [];
                tempCollection.Import(pfxFilePath, pfpass, X509KeyStorageFlags.EphemeralKeySet);

                IEnumerable<X509Certificate2> cond = tempCollection.Where(x => x.HasPrivateKey);
                if (cond.Any())
                {
                    collection.AddRange(cond.ToArray());
                }
                else
                {
                    collection.AddRange(tempCollection);
                }
            }
            else
            {
                X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                Dictionary<string, X509Certificate2> mapping = [];
                foreach (X509Certificate2 cert in store.Certificates)
                {
                    mapping[$"{cert.GetNameInfo(X509NameType.SimpleName, false)},{cert.GetNameInfo(X509NameType.SimpleName, true)},{cert.Thumbprint}"] = cert;
                }

                List<string> selection = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .PageSize(10)
                        .Title("Select Signing Certificates")
                        .MoreChoicesText("[grey](Move up and down to see more certificates)[/]")
                        .InstructionsText("[grey](Press [blue]<space>[/] to toggle a certificate, [green]<enter>[/] to accept)[/]")
                        .AddChoices(mapping.Keys));

                collection.AddRange(selection.Select(x => mapping[x]).ToArray());
            }

            Sign(collection);
        }, pckArg, pfxOpt, pfxPassOpt, pfxNoPassOpt);

        root.AddCommand(signCmd);

        Command verifyCmd = new Command("verify", "Verify bundle")
        {
            pckArg,
        };

        verifyCmd.SetHandler((packagePath) =>
        {
            InitBundle(packagePath);
            Verify();
        }, pckArg);

        root.AddCommand(verifyCmd);

        return root;
    }

    private static int Main(string[] args)
    {
        RootCommand root = GetCommands();
        return root.Invoke(args);
    }

    private static void InitBundle(string packagePath) => Package = new(packagePath);

    private static void Create(string id, string name)
    {
        Package.Manifest.StoreOriginalFiles = true;

        Package.PackageInfo.Id = id;
        Package.PackageInfo.Name = name;

        Package.Update();

        Console.WriteLine($"Plugin package with id: {id} created successfully");
    }

    private static void GeneratePluginConfig(string pluginConfigDestinationPath)
    {
        PluginMetadata blankPlugin = new()
        {
            Id = "PluginId",
            Version = new Version(1, 0, 0),
            Interface = "PluginInterface",
            InterfaceVersion = new Version(1, 0, 0),
            Assembly = "PluginAssembly.dll",
            Class = "PluginClass",
            Name = "PluginName",
            Description = "PluginDescription",
            Permissions = new List<string>(),
        };

        string jsonData = JsonSerializer.Serialize(blankPlugin, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        byte[] buffer = Encoding.UTF8.GetBytes(jsonData);

        File.WriteAllBytes(pluginConfigDestinationPath, buffer);
    }

    private static void Add(string pluginPath, string pluginConfigPath)
    {
        AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .Start("[yellow]Reading Package[/]", ctx =>
            {
                Package.LoadFromFile(false);

                ctx.Status("[yellow]Loading Plugin Config[/]");

                FileStream configFile = File.OpenRead(pluginConfigPath);
                PluginMetadata config = JsonSerializer.Deserialize<PluginMetadata>(configFile);

                ctx.Status("[yellow]Validating Plugin[/]");

                if (string.IsNullOrEmpty(config.Id)
                    || config.Version == null
                    || string.IsNullOrEmpty(config.Interface)
                    || config.InterfaceVersion == null
                    || string.IsNullOrEmpty(config.Assembly)
                    || string.IsNullOrEmpty(config.Class))
                {
                    Console.WriteLine("Invalid config");
                    return;
                }

                PluginMetadataBuilder configBuilder = PluginMetadataBuilder.Create(config);

                string assemblyPath = Path.Combine(pluginPath, config.Assembly);
                if (!File.Exists(assemblyPath))
                {
                    Console.WriteLine("File not found: " + assemblyPath);
                }

                string depsPath = Path.Combine(pluginPath, Path.GetFileNameWithoutExtension(config.Assembly) + ".deps.json");

                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                configBuilder.DetectTargetFramework(assembly);

                var deps = new DependencyContextJsonReader().Read(File.OpenRead(depsPath));
                var resolver = new AssemblyDependencyResolver(assemblyPath);

                if (deps != null)
                {
                    foreach (RuntimeLibrary lib in deps.RuntimeLibraries)
                    {
                        var asmName = new AssemblyName($"{lib.Name}, Version={lib.Version}, Culture=neutral, PublicKeyToken=null");

                        Console.WriteLine($"Library: {lib.Name}");
                        Console.WriteLine($"Version: {lib.Version}");
                        Console.WriteLine($"Type: {lib.Type}");
                        Console.WriteLine($"Hash: {lib.Hash}");
                        Console.WriteLine($"Path: {resolver.ResolveAssemblyToPath(asmName)}");
                        Console.WriteLine();
                    }
                }

                FileStream stream = File.OpenRead(assemblyPath);
                configBuilder.ComputeBuildTag(stream);
                stream.Close();

                ctx.Status("[yellow]Adding Plugin[/]");

                var finalConfig = configBuilder.Build();
                string pluginDest = $"{finalConfig.Id}-{finalConfig.BuildTag}";
                Parallel.ForEach(SafeEnumerateFiles(pluginPath, "*"), file =>
                {
                    if (file == Package.BundlePath) return;
                    Package.AddEntry(file, pluginDest, pluginPath);
                    AnsiConsole.MarkupLine($"[blue]Added:[/] {Path.GetRelativePath(pluginPath, file)}");
                });

                Package.PackageInfo.Plugins.Add(finalConfig);

                ctx.Status("[yellow]Saving Package[/]");
                Package.Update();
            });
    }

    private static void Sign(X509Certificate2Collection certificates)
    {
        AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .Start("[yellow]Signing[/]", ctx =>
            {
                Package.LoadFromFile(false);

                int divider = 0;
                foreach (X509Certificate2 cert in certificates)
                {
                    if (divider++ > 0) AnsiConsole.WriteLine();

                    Grid grid = new Grid();
                    grid.AddColumn(new GridColumn().NoWrap());
                    grid.AddColumn(new GridColumn().PadLeft(2));
                    grid.AddRow("Certificate Info:");
                    grid.AddRow("  Common Name", cert.GetNameInfo(X509NameType.SimpleName, false));
                    grid.AddRow("  Issuer Name", cert.GetNameInfo(X509NameType.SimpleName, true));
                    grid.AddRow("  Holder Email", cert.GetNameInfo(X509NameType.EmailName, false));
                    grid.AddRow("  Valid From", cert.GetEffectiveDateString());
                    grid.AddRow("  Valid To", cert.GetExpirationDateString());
                    grid.AddRow("  Thumbprint", cert.Thumbprint);

                    AnsiConsole.Write(grid);
                    AnsiConsole.WriteLine();
                    bool verifyCert = VerifyCertificate(cert);
                    if (!verifyCert) continue;

                    RSA prvKey = cert.GetRSAPrivateKey();
                    if (prvKey == null)
                    {
                        AnsiConsole.MarkupLine($"[{Color.Green}] Failed to Acquire RSA Private Key[/]");
                        continue;
                    }

                    Package.Sign(cert, prvKey);
                    AnsiConsole.MarkupLine($"[green] Signing Completed Successfully[/]");
                }

                ctx.Status("[yellow]Updating Bundle[/]");
                Package.Update();
            });
    }

    private static bool VerifyCertificate(X509Certificate2 certificate)
    {
        List<bool> verifyResults = [];

        bool defaultVerification = Package.VerifyCertificate(certificate, out X509ChainStatus[] statuses);
        verifyResults.Add(defaultVerification);

        AnsiConsole.MarkupLine($"[{(defaultVerification ? Color.Green : Color.Red)}] Certificate Verification {(defaultVerification ? "Successful" : "Failed")}[/]");

        if (!defaultVerification)
        {
            bool timeIssue = statuses.Any(x => x.Status.HasFlag(X509ChainStatusFlags.NotTimeValid));

            EnumerateStatuses(statuses);

            if (timeIssue)
            {
                X509ChainPolicy policy = new X509ChainPolicy();
                policy.VerificationFlags |= X509VerificationFlags.IgnoreNotTimeValid;

                bool noTimeVerification = Package.VerifyCertificate(certificate, out X509ChainStatus[] noTimeStatuses, policy: policy);
                verifyResults.Add(noTimeVerification);

                AnsiConsole.MarkupLine($"[{(noTimeVerification ? Color.Green : Color.Red)}] Certificate Verification without time checking {(noTimeVerification ? "Successful" : "Failed")}[/]");
                EnumerateStatuses(noTimeStatuses);
            }
        }

        return verifyResults.Any(x => x);
    }

    private static void EnumerateStatuses(X509ChainStatus[] statuses)
    {
        foreach (X509ChainStatus status in statuses)
        {
            AnsiConsole.MarkupLine($"[{Color.IndianRed}]   {status.StatusInformation}[/]");
        }
    }

    private static void Verify()
    {
        Dictionary<string, Color> colorDict = new Dictionary<string, Color>()
        {
            ["file_verified"] = Color.MediumSpringGreen,
            ["file_failed"] = Color.OrangeRed1,
            ["file_missing"] = Color.Grey70,
            ["file_error"] = Color.Red3_1,
        };

        AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .Start("[yellow]Verifying Signature[/]", ctx =>
            {
                Package.LoadFromFile();

                int verifiedCerts = 0;
                int divider = 0;

                foreach (string cert in Package.Signatures.Entries.Keys)
                {
                    if (divider++ > 0) AnsiConsole.WriteLine();

                    X509Certificate2 certificate = Package.GetCertificate(cert);
                    AnsiConsole.MarkupLine($"Verifying Certificate [{Color.Teal}]{certificate.GetNameInfo(X509NameType.SimpleName, false)}[/] Issued by [{Color.Aqua}]{certificate.GetNameInfo(X509NameType.SimpleName, true)}[/]");

                    bool verifyCert = VerifyCertificate(certificate);
                    if (!verifyCert) continue;

                    bool verifySign = Package.VerifySignature(cert);
                    AnsiConsole.MarkupLine($"[{(verifySign ? Color.Green : Color.Red)}] Signature Verification {(verifySign ? "Successful" : "Failed")}[/]");
                    if (!verifySign) continue;

                    verifiedCerts++;
                }

                AnsiConsole.WriteLine();

                if (verifiedCerts == 0)
                {
                    if (Package.Signatures.Entries.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]This bundle is not signed[/]");
                    }

                    AnsiConsole.MarkupLine($"[red]Verification failed[/]");
                    return;
                }

                if (verifiedCerts == Package.Signatures.Entries.Count)
                {
                    AnsiConsole.MarkupLine($"[{Color.Green3}]All Certificates were verified[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[{Color.Yellow}]{verifiedCerts} out of {Package.Signatures.Entries.Count} Certificates were verified[/]");
                }

                AnsiConsole.WriteLine();

                ctx.Status("[yellow]Verifying Files[/]");

                bool p2Verified = true;

                int fv = 0;
                int ff = 0;
                int fm = 0;
                int fe = 0;

                Parallel.ForEach(Package.Manifest.Entries, (entry) =>
                {
                    bool verifyFile = false;

                    try
                    {
                        verifyFile = Package.VerifyFile(entry.Key);

                        if (verifyFile)
                        {
                            Interlocked.Increment(ref fv);
                        }
                        else
                        {
                            Interlocked.Increment(ref ff);
                        }

                        AnsiConsole.MarkupLine($"[{(verifyFile ? colorDict["file_verified"] : colorDict["file_failed"])}]{entry.Key}[/]");
                    }
                    catch (FileNotFoundException)
                    {
                        Interlocked.Increment(ref fm);
                        AnsiConsole.MarkupLine($"[{colorDict["file_missing"]}]{entry.Key}[/]");
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref fe);
                        AnsiConsole.MarkupLine($"[{colorDict["file_error"]}]{entry.Key} - {ex.GetType().Name}: {ex.Message}[/]");
                    }

                    if (!verifyFile)
                    {
                        p2Verified = false;
                    }
                });

                AnsiConsole.WriteLine();

                if (Package.Manifest.Entries.Count != fv)
                {
                    AnsiConsole.MarkupLine("File Verification Summary");
                    AnsiConsole.MarkupLine($"[{colorDict["file_verified"]}] {fv} Files verified[/]");
                    if (ff > 0) AnsiConsole.MarkupLine($"[{colorDict["file_failed"]}] {ff} Files tampered with[/]");
                    if (fm > 0) AnsiConsole.MarkupLine($"[{colorDict["file_missing"]}] {fm} Files not found[/]");
                    if (fe > 0) AnsiConsole.MarkupLine($"[{colorDict["file_error"]}] {fe} Files encountered with errors[/]");

                    AnsiConsole.WriteLine();
                }

                if (!p2Verified)
                {
                    AnsiConsole.MarkupLine($"[red]File Verification Failed[/]");
                    return;
                }

                AnsiConsole.MarkupLine("[green]Bundle Verification Completed Successfully[/]");
            });
    }

    public static IEnumerable<string> SafeEnumerateFiles(string path, string searchPattern)
    {
        ConcurrentQueue<string> folders = new();
        folders.Enqueue(path);

        while (!folders.IsEmpty)
        {
            folders.TryDequeue(out string currentDir);
            string[] subDirs;
            string[] files = null;

            try
            {
                files = Directory.GetFiles(currentDir, searchPattern);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }

            if (files != null)
            {
                foreach (string file in files)
                {
                    yield return file;
                }
            }

            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            catch (UnauthorizedAccessException) { continue; }
            catch (DirectoryNotFoundException) { continue; }

            foreach (string str in subDirs)
            {
                folders.Enqueue(str);
            }
        }
    }

    private static string SecurePrompt(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .PromptStyle("red")
                .AllowEmpty()
                .Secret(null));
    }
}
