using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DouglasDwyer.CasCore;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public abstract class PluginImplementation : IReadOnlyCollection<PluginContext>, IDisposable
{
    private List<PluginContext> _plugins = [];
    private bool _disposed;

    public bool Disposed => _disposed;

    public abstract string Interface { get; }

    public abstract Version Version { get; }

    public virtual Version MinimumVersion => Version;

    public string TempPath { get; protected set; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public IReadOnlyList<PluginContext> Plugins => _plugins;

    public int Count => _plugins.Count;

    protected PluginImplementation()
    {
        
    }

    internal protected abstract void Initialize(SecurityContext securityContext);

    internal void Add(PluginContext context)
    {
        _plugins.Add(context);
    }

    public virtual bool CheckPluginType(Type type)
    {
        return true;
    }

    public virtual IGateway CreateGateway(PluginLoadSession session)
    {
        return new Gateway(session.Token);
    }

    public virtual void UpdateAssemblySecurityPolicy(PluginLoadSession session, CasPolicyBuilder policy)
    {
        policy.Allow(new TypeBinding(typeof(Gateway), Accessibility.Public));
    }

    public override string ToString() => $"{Interface}-{Version}";

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var plugin in _plugins)
                {
                    plugin.Dispose();
                }

                _plugins.Clear();
                _plugins = null!;

                if (Directory.Exists(TempPath))
                {
                    Directory.Delete(TempPath, true);
                }
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public IEnumerator<PluginContext> GetEnumerator() => _plugins.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _plugins.GetEnumerator();
}
