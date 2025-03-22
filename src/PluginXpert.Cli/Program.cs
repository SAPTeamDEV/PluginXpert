using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System;
using SAPTeam.PluginXpert;
using System.CommandLine;
using EasySign;
using Spectre.Console;
using Color = Spectre.Console.Color;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace PluginXpert.Cli
{
    internal class Program
    {
        public static PluginPackage Package { get; set; }

        static RootCommand GetCommands()
        {
            var root = new RootCommand("Easy Digital Signing Tool");

            #region Shared Options
            var pckArg = new Argument<string>("package", "Plugin Package path");
            #endregion

            var plgArg = new Argument<string>("plugin", "Plugin directory");

            var plgCfgOpt = new Option<string>("--config", "Plugin's config path");
            plgCfgOpt.AddAlias("-c");
            plgCfgOpt.IsRequired = true;

            var addCmd = new Command("add", "Add new plugin to the package")
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

            var idArg = new Argument<string>("id", "Package Identifier");

            var nameOpt = new Option<string>("--name", "Package display name");
            nameOpt.AddAlias("-n");
            nameOpt.IsRequired = true;

            var createCmd = new Command("create", "Create new plugin package")
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

            var cfgDestArg = new Argument<string>("file", "Destination path");
            cfgDestArg.SetDefaultValue("plugin.json");

            var generateCmd = new Command("generate", "Generate plugin config file")
            {
                pckArg,
                cfgDestArg,
            };

            generateCmd.SetHandler((packagePath, pluginConfigDestinationPath) =>
            {
                GeneratePluginConfig(pluginConfigDestinationPath);
            }, pckArg, cfgDestArg);
            root.AddCommand(generateCmd);

            var pfxOpt = new Option<string>("--pfx", "PFX File contains certificate and private key");
            var pfxPassOpt = new Option<string>("--pfx-password", "PFX File password");
            var pfxNoPassOpt = new Option<bool>("--no-password", "Ignore PFX File password prompt");

            var signCmd = new Command("sign", "Sign bundle with certificate")
            {
                pckArg,
                pfxOpt,
                pfxPassOpt,
                pfxNoPassOpt,
            };

            signCmd.SetHandler((packagePath, pfxFilePath, pfxFilePassword, pfxNoPasswordPrompt) =>
            {
                InitBundle(packagePath);

                X509Certificate2Collection collection = new();

                if (!string.IsNullOrEmpty(pfxFilePath))
                {
                    string pfpass = !string.IsNullOrEmpty(pfxFilePassword) ? pfxFilePassword : !pfxNoPasswordPrompt ? SecurePrompt("Enter PFX File password (if needed): ") : "";

                    var tempCollection = new X509Certificate2Collection();
                    tempCollection.Import(pfxFilePath, pfpass, X509KeyStorageFlags.EphemeralKeySet);

                    var cond = tempCollection.Where(x => x.HasPrivateKey);
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

                    var mapping = new Dictionary<string, X509Certificate2>();
                    foreach (var cert in store.Certificates)
                    {
                        mapping[$"{cert.GetNameInfo(X509NameType.SimpleName, false)},{cert.GetNameInfo(X509NameType.SimpleName, true)},{cert.Thumbprint}"] = cert;
                    }

                    var selection = AnsiConsole.Prompt(
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

            var verifyCmd = new Command("verify", "Verify bundle")
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

        static int Main(string[] args)
        {
            var root = GetCommands();
            return root.Invoke(args);
        }

        static void InitBundle(string packagePath)
        {
            Package = new(packagePath);
        }

        static void Create(string id, string name)
        {
            Package.Manifest.BundleFiles = true;

            Package.PackageInfo.Id = id;
            Package.PackageInfo.Name = name;

            Package.Update();

            Console.WriteLine($"Plugin package with id: {id} created successfully");
        }

        static void GeneratePluginConfig(string pluginConfigDestinationPath)
        {
            var blankPlugin = new PluginEntry();

            var jsonData = JsonSerializer.Serialize(pluginConfigDestinationPath);
            var buffer = Encoding.UTF8.GetBytes(jsonData);

            File.WriteAllBytes(pluginConfigDestinationPath, buffer);
        }

        static void Add(string pluginPath,string pluginConfigPath)
        {
            AnsiConsole.Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Default)
                .Start("[yellow]Reading Package[/]", ctx =>
                {
                    Package.Load(false);

                    ctx.Status("[yellow]Loading Plugin Config[/]");

                    var configFile = File.OpenRead(pluginConfigPath);
                    var config = JsonSerializer.Deserialize<PluginEntry>(configFile);

                    ctx.Status("[yellow]Validating Plugin[/]");

                    if (config == null
                        || string.IsNullOrEmpty(config.Id)
                        || config.Version == null
                        || string.IsNullOrEmpty(config.Interface)
                        || config.InterfaceVersion == null
                        || string.IsNullOrEmpty(config.Assembly)
                        || string.IsNullOrEmpty(config.Class))
                    {
                        Console.WriteLine("Invalid config");
                        return;
                    }

                    var assemblyPath = Path.Combine(pluginConfigPath, config.Assembly);
                    if (!File.Exists(assemblyPath))
                    {
                        Console.WriteLine("File not found: " + assemblyPath);
                    }

                    var assembly = Assembly.LoadFrom(assemblyPath);
                    var attribute = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
                    if (attribute != null)
                    {
                        var frameworkVer = ParseFrameworkVersion(attribute.FrameworkName);
                        if (frameworkVer != null)
                        {
                            config.TargetFrameworkVersion = frameworkVer;
                        }
                        else
                        {
                            throw new FormatException("Cannot parse the framework string");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Cannot determine plugin's target framework");
                        return;
                    }

                    using var sha256 = SHA256.Create();
                    using var stream = File.OpenRead(assemblyPath);
                    var hash = sha256.ComputeHash(stream);
                    config.BuildRef = string.Concat(hash[^5..].Select(b => b.ToString("x2")));

                    ctx.Status("[yellow]Adding Plugin[/]");

                    var pluginDest = $"{config.Id}-{config.BuildRef}";
                    Parallel.ForEach(SafeEnumerateFiles(pluginPath, "*"), file =>
                    {
                        if (file == Package.BundlePath) return;
                        Package.AddEntry(file, pluginDest, pluginPath);
                        AnsiConsole.MarkupLine($"[blue]Added:[/] {Path.GetRelativePath(pluginPath, file)}");
                    });

                    Package.PackageInfo.Plugins.Add(config);

                    ctx.Status("[yellow]Saving Package[/]");
                    Package.Update();
                });
        }

        static void Sign(X509Certificate2Collection certificates)
        {
            AnsiConsole.Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Default)
                .Start("[yellow]Signing[/]", ctx =>
                {
                    Package.Load(false);

                    int divider = 0;
                    foreach (var cert in certificates)
                    {
                        if (divider++ > 0) AnsiConsole.WriteLine();

                        var grid = new Grid();
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

                        var prvKey = cert.GetRSAPrivateKey();
                        if (prvKey == null)
                        {
                            AnsiConsole.MarkupLine($"[{Color.Green}] Failed to Acquire RSA Private Key[/]");
                            continue;
                        }

                        Package.SignBundle(cert, prvKey);
                        AnsiConsole.MarkupLine($"[green] Signing Completed Successfully[/]");
                    }

                    ctx.Status("[yellow]Updating Bundle[/]");
                    Package.Update();
                });
        }

        private static bool VerifyCertificate(X509Certificate2 certificate)
        {
            List<bool> verifyResults = new();

            var defaultVerification = Package.VerifyCertificate(certificate, out X509ChainStatus[] statuses);
            verifyResults.Add(defaultVerification);

            AnsiConsole.MarkupLine($"[{(defaultVerification ? Color.Green : Color.Red)}] Certificate Verification {(defaultVerification ? "Successful" : "Failed")}[/]");

            if (!defaultVerification)
            {
                bool timeIssue = statuses.Any(x => x.Status.HasFlag(X509ChainStatusFlags.NotTimeValid));

                EnumerateStatuses(statuses);

                if (timeIssue)
                {
                    var policy = new X509ChainPolicy();
                    policy.VerificationFlags |= X509VerificationFlags.IgnoreNotTimeValid;

                    var noTimeVerification = Package.VerifyCertificate(certificate, out X509ChainStatus[] noTimeStatuses, policy: policy);
                    verifyResults.Add(noTimeVerification);

                    AnsiConsole.MarkupLine($"[{(noTimeVerification ? Color.Green : Color.Red)}] Certificate Verification without time checking {(noTimeVerification ? "Successful" : "Failed")}[/]");
                    EnumerateStatuses(noTimeStatuses);
                }
            }

            return verifyResults.Any(x => x);
        }

        private static void EnumerateStatuses(X509ChainStatus[] statuses)
        {
            foreach (var status in statuses)
            {
                AnsiConsole.MarkupLine($"[{Color.IndianRed}]   {status.StatusInformation}[/]");
            }
        }

        static void Verify()
        {
            var colorDict = new Dictionary<string, Color>()
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
                    Package.Load();

                    int verifiedCerts = 0;
                    int divider = 0;

                    foreach (var cert in Package.Signatures.Entries.Keys)
                    {
                        if (divider++ > 0) AnsiConsole.WriteLine();

                        var certificate = Package.GetCertificate(cert);
                        AnsiConsole.MarkupLine($"Verifying Certificate [{Color.Teal}]{certificate.GetNameInfo(X509NameType.SimpleName, false)}[/] Issued by [{Color.Aqua}]{certificate.GetNameInfo(X509NameType.SimpleName, true)}[/]");

                        var verifyCert = VerifyCertificate(certificate);
                        if (!verifyCert) continue;

                        var verifySign = Package.VerifySignature(cert);
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
                        var verifyFile = false;

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

        static string SecurePrompt(string prompt)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>(prompt)
                    .PromptStyle("red")
                    .AllowEmpty()
                    .Secret(null));
        }

        public static Version ParseFrameworkVersion(string frameworkString)
        {
            if (string.IsNullOrWhiteSpace(frameworkString))
                throw new ArgumentException("Input framework string cannot be null or empty.", nameof(frameworkString));

            // Look for "Version=" (case-insensitive) in the string.
            const string versionKeyword = "Version=";
            int versionIndex = frameworkString.IndexOf(versionKeyword, StringComparison.OrdinalIgnoreCase);
            if (versionIndex == -1)
            {
                throw new FormatException("The framework string does not contain a version specification.");
            }

            // Extract the substring starting right after "Version="
            string versionSubString = frameworkString.Substring(versionIndex + versionKeyword.Length).Trim();

            // If the version starts with a 'v', remove it.
            if (versionSubString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                versionSubString = versionSubString.Substring(1);
            }

            // Try to parse the extracted substring into a Version object.
            if (Version.TryParse(versionSubString, out Version parsedVersion))
            {
                return parsedVersion;
            }
            else
            {
                throw new FormatException($"Unable to parse version from '{versionSubString}'.");
            }
        }
    }
}
