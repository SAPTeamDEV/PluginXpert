using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public abstract class PluginImplementation : IReadOnlyCollection<PluginContext>, IDisposable
{
    private List<PluginContext> _plugins = [];
    private bool _disposed;

    public abstract string Interface { get; }

    public abstract Version Version { get; }

    public virtual Version MinimumVersion => Version;

    public IReadOnlyList<PluginContext> Plugins => _plugins;

    public PluginManager? PluginManager { get; internal set; }

    public int Count => _plugins.Count;

    protected PluginImplementation()
    {
        
    }

    internal void Add(PluginContext context)
    {
        _plugins.Add(context);
    }

    public virtual PluginContext? LoadPlugin(Type type, PluginEntry entry, bool throwOnFail = true)
    {
        PluginContext? context = null;

        if (typeof(IPlugin).IsAssignableFrom(type))
        {
            IPlugin? result = (IPlugin?)Activator.CreateInstance(type);
            context = PluginContext.Create(GetPluginManager().PermissionManager, this, result, entry);
        }

        return context;
    }

    public virtual IGateway CreateGateway(IPlugin? plugin, SecurityToken securityToken, PluginEntry entry)
    {
        return new Gateway(securityToken, GetPluginManager());
    }

    private PluginManager GetPluginManager() => PluginManager ?? throw new InvalidOperationException("Plugin implementation is not registered in a plugin manager");
    
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
            }

            PluginManager = null;

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
