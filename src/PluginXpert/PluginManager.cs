using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

using DouglasDwyer.CasCore;

using EnsureThat;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

/// <summary>
/// Represents class for loading and managing plugins.
/// </summary>
public sealed class PluginManager : IReadOnlyCollection<PluginImplementation>, IDisposable
{
    /// <summary>
    /// Gets the version of the current .NET runtime.
    /// </summary>
    public static Version? RuntimeVersion { get; } = GetRuntimeVersion();

    private bool _disposed;
    private readonly Type _defaultPluginType = typeof(IPlugin);
    private readonly Dictionary<string, PluginImplementation> _implementations = [];

    /// <summary>
    /// Gets a value indicating whether the <see cref="PluginManager"/> has been disposed.
    /// </summary>
    public bool Disposed => _disposed;

    /// <summary>
    /// Gets the list of plugin load sessions.
    /// </summary>
    /// <remarks>
    /// This list primarily serves for debugging purposes.
    /// It contains all plugin load sessions, including those that failed to load at earlier stages.
    /// </remarks>
    public Dictionary<string, PluginLoadSession> LoadSessions { get; } = [];

    /// <summary>
    /// Gets the list of plugins.
    /// </summary>
    public IEnumerable<PluginContext> Plugins => _implementations.Values.Where(x => !x.Disposed).SelectMany(x => x.Plugins);

    /// <summary>
    /// Gets the list of valid plugins.
    /// </summary>
    public IEnumerable<PluginContext> ValidPlugins => Plugins.Where(x => !x.Disposed && x.Valid);

    /// <summary>
    /// Gets the security context used by the <see cref="PluginManager"/> to load plugins.
    /// </summary>
    public SecurityContext SecurityContext { get; }

    /// <summary>
    /// Gets the number of registered plugin implementations.
    /// </summary>
    public int Count => _implementations.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginManager"/> class.
    /// </summary>
    /// <param name="securityContext">
    /// The security context used to load plugins.
    /// </param>
    /// <exception cref="ApplicationException">
    /// Thrown when the runtime version cannot be determined.
    /// </exception>
    public PluginManager(SecurityContext? securityContext = null)
    {
        if (RuntimeVersion == null)
        {
            throw new ApplicationException("Cannot determine the runtime version");
        }

        SecurityContext = securityContext ?? new();
    }

    /// <summary>
    /// Registers a plugin implementation with the <see cref="PluginManager"/>.
    /// </summary>
    /// <param name="implementation">
    /// The plugin implementation to register.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the plugin implementation is already registered.
    /// </exception>
    public void Add(PluginImplementation implementation)
    {
        Ensure.Any.IsNotNull(implementation);

        string implId = implementation.ToString();

        if (_implementations.ContainsKey(implId))
        {
            throw new InvalidOperationException($"Plugin implementation {implId} already registered");
        }

        implementation.RegisterPermissions(SecurityContext);

        _implementations[implId] = implementation;
    }

    /// <summary>
    /// Loads plugins from the specified package.
    /// </summary>
    /// <param name="package">
    /// The plugin package to load plugins from.
    /// </param>
    /// <returns>
    /// An enumerable collection of <see cref="PluginContext"/> instances representing the loaded plugins.
    /// </returns>
    /// <exception cref="ApplicationException">
    /// Thrown when no plugin implementations are registered.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the package is invalid.
    /// </exception>
    public IEnumerable<PluginContext> LoadPlugins(PluginPackage package)
    {
        Ensure.Any.IsNotNull(package);

        if (_implementations.Count == 0)
        {
            throw new ApplicationException("No plugin implementations registered");
        }

        VerifyPackage(package);

        Dictionary<string, Dictionary<string, PluginLoadSession>> loadCondidates = [];

        foreach (PluginMetadata ent in package.PackageInfo.Plugins)
        {
            PluginLoadSession session = CreateSession(package, ent);
            
            if (!VerifyPlugin(session))
            {
                continue;
            }

            if (!loadCondidates.TryGetValue(ent.Id, out Dictionary<string, PluginLoadSession>? value))
            {
                value = [];
                loadCondidates[ent.Id] = value;
            }

            if (value.ContainsKey(ent.BuildTag))
            {
                session.Result = PluginLoadResult.AlreadyLoaded;
                continue;
            }

            value[ent.BuildTag] = session;
        }

        var chosenEntries = PickPlugins(loadCondidates);

        if (!chosenEntries.Any())
        {
            throw new ArgumentException("Can't find any suitable plugins in this package");
        }

        List<PluginContext> contexts = new();
        foreach (PluginLoadSession session in chosenEntries)
        {
            _ = session.TryRun(() =>
            {
                string tempPath = Path.Combine(session.Implementation!.TempPath, session.Package.PackageInfo.Id);
                Directory.CreateDirectory(tempPath);

                package.ExtractPlugin(session.Metadata, tempPath);

                session.AssemblyPath = Path.Combine(tempPath, $"{session.Metadata.Id}-{session.Metadata.BuildTag}", session.Metadata.Assembly);
                session.Token = SecurityContext.RegisterPlugin(session);

                CasPolicyBuilder policyBuilder = new CasPolicyBuilder();
                session.Implementation.UpdateAssemblySecurityPolicy(session, policyBuilder);

                session.Loader = SecurityContext.CreateAssemblyLoader(session, policyBuilder);
                session.Assembly = session.Loader.LoadFromAssemblyPath(session.AssemblyPath);

                InitializePlugin(session);
            });

            PluginContext context = new PluginContext(session);
            session.Implementation!.Add(context);

            contexts.Add(context);
        }

        return contexts;
    }

    /// <summary>
    /// Verifies the plugin metadata and checks if the plugin can be loaded.
    /// </summary>
    /// <param name="session">
    /// The plugin load session containing the metadata to verify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the plugin can be loaded; otherwise, <see langword="false"/>.
    /// </returns>
    private bool VerifyPlugin(PluginLoadSession session)
    {
        PluginImplementation? impl;
        if ((impl = ResolveImplementation(session.Metadata)) == null)
        {
            session.Result = PluginLoadResult.NotSupportedImplementation;
            return false;
        }

        session.Implementation = impl;

        if (session.Implementation.Where(x => x.Id == session.Metadata.Id).Any())
        {
            session.Result = PluginLoadResult.AlreadyLoaded;
            return false;
        }

        if (session.Metadata.TargetFrameworkVersion > RuntimeVersion)
        {
            session.Result = PluginLoadResult.NotSupportedRuntime;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Verifies the plugin package.
    /// </summary>
    /// <param name="package">
    /// The plugin package to verify.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the package is not loaded, mutable, or has invalid package info.
    /// </exception>
    private static void VerifyPackage(PluginPackage package)
    {
        if (!package.Loaded)
        {
            throw new ArgumentException("Package is not loaded");
        }

        if (!package.ReadOnly)
        {
            throw new ArgumentException("Cannot use a mutable package");
        }

        if (!package.VerifyPackageInfo())
        {
            throw new ArgumentException($"Invalid package info");
        }
    }

    /// <summary>
    /// Creates a new plugin load session for the specified plugin metadata.
    /// </summary>
    /// <param name="package">
    /// The plugin package containing the metadata.
    /// </param>
    /// <param name="metadata">
    /// The plugin metadata to create a session for.
    /// </param>
    /// <returns></returns>
    private PluginLoadSession CreateSession(PluginPackage package,
                                            PluginMetadata metadata)
    {
        var buffer = new byte[5];
        string sessionId;

        do
        {
            Random.Shared.NextBytes(buffer);
            sessionId = BitConverter.ToString(buffer).Replace("-", string.Empty).ToLowerInvariant();
        } while (LoadSessions.ContainsKey(sessionId));

        var session = new PluginLoadSession(sessionId, this, package, metadata);
        LoadSessions.Add(sessionId, session);

        return session;
    }

    /// <summary>
    /// Picks the best plugins from the load candidates.
    /// </summary>
    /// <param name="loadCondidates">
    /// The dictionary of load candidates, where the key is the plugin ID and the value is a dictionary of plugin metadata.
    /// </param>
    /// <returns>
    /// A list of chosen plugin metadata.
    /// </returns>
    private static IEnumerable<PluginLoadSession> PickPlugins(Dictionary<string, Dictionary<string, PluginLoadSession>> loadCondidates)
    {
        foreach (Dictionary<string, PluginLoadSession> plg in loadCondidates.Values)
        {
            if (plg.Count == 1)
            {
                yield return plg.Values.First();
                continue;
            }

            var sorted = plg.Values
                .Where(v => v.Metadata.TargetFrameworkVersion <= RuntimeVersion) // Only consider versions lower or equal to the runtime version
                .OrderByDescending(v => v.Metadata.TargetFrameworkVersion); // Get the largest version among them

            foreach (var entry in sorted.Skip(1))
            {
                entry.Result = PluginLoadResult.Skipped;
            }

            var chosen = sorted.FirstOrDefault();
            if (chosen != null)
            {
                yield return chosen;
            }
        }
    }

    /// <summary>
    /// Resolves the plugin implementation for the specified metadata.
    /// </summary>
    /// <param name="metadata">
    /// The plugin metadata to resolve the implementation for.
    /// </param>
    /// <returns>
    /// The resolved plugin implementation, or <see langword="null"/> if no suitable implementation is found.
    /// </returns>
    public PluginImplementation? ResolveImplementation(PluginMetadata metadata)
    {
        return this.Where(x => x.Name == metadata.Interface)
                        .Where(x => metadata.InterfaceVersion >= x.MinimumVersion && metadata.InterfaceVersion <= x.Version)
                        .OrderByDescending(x => x.Version)
                        .FirstOrDefault();
    }

    /// <summary>
    /// Initializes the plugin by loading its assembly and creating an instance of the plugin type.
    /// </summary>
    /// <param name="session">
    /// The plugin load session containing the assembly and metadata.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the session is invalid or the assembly is not found.
    /// </exception>
    /// <exception cref="PluginLoadException">
    /// Thrown when the plugin cannot be created.
    /// </exception>
    private void InitializePlugin(PluginLoadSession session)
    {
        if (session.Implementation == null)
        {
            throw new ArgumentException("Invalid session");
        }

        Assembly assembly = session.Assembly ?? throw new ArgumentException("Invalid session");
        Type[] assemblyTypes = assembly.GetTypes();
        if (assemblyTypes.Length == 0)
        {
            throw new PluginLoadException("No types found in assembly");
        }

        Type[] compatibleTypes = assemblyTypes.Where(x => x.IsClass && !x.IsAbstract && _defaultPluginType.IsAssignableFrom(x)).ToArray();
        if (compatibleTypes.Length == 0)
        {
            throw new PluginLoadException("No compatible types found in assembly");
        }

        Type? targetType = compatibleTypes.SingleOrDefault(x => x.Name == session.Metadata.Class);
        if (targetType == null)
        {
            throw new PluginLoadException("Cannot find target type");
        }

        bool implCheck = session.Implementation.CheckPluginType(targetType);
        if (!implCheck)
        {
            throw new PluginLoadException("Plugin type is not compatible with the implementation");
        }

        IPlugin? instance = (IPlugin?)Activator.CreateInstance(targetType);
        if (instance == null)
        {
            throw new PluginLoadException("Cannot create plugin instance");
        }

        session.Instance = instance;
        session.Gateway = session.Implementation.CreateGateway(session);
        session.Instance.OnLoad(session.Gateway);

        session.Result = PluginLoadResult.Success;
    }

    /// <summary>
    /// Gets the version of the current .NET runtime.
    /// </summary>
    /// <returns>
    /// The version of the current .NET runtime, or <see langword="null"/> if it cannot be determined.
    /// </returns>
    private static Version? GetRuntimeVersion()
    {
        string frameworkDescription = RuntimeInformation.FrameworkDescription;

        if (frameworkDescription.StartsWith(".NET"))
        {
            string[] parts = frameworkDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return Version.TryParse(parts[1], out Version? version) ? version : null;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public IEnumerator<PluginImplementation> GetEnumerator() => _implementations.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _implementations.Values.GetEnumerator();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            LoadSessions.Clear();

            foreach (PluginImplementation impl in _implementations.Values)
            {
                impl.Dispose();
            }

            _implementations.Clear();

            SecurityContext.Dispose();

            _disposed = true;
        }
    }
}