using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert.Types
{
    /// <summary>
    /// Provides standard base class to implement managed plugin.
    /// </summary>
    public abstract class Plugin : IPlugin
    {
        /// <inheritdoc/>
        public virtual string Name { get; }

        /// <inheritdoc/>
        public virtual string[] Permissions { get; }

        /// <inheritdoc/>
        public IPermissionManager PermissionManager { get; set; }

        /// <inheritdoc/>
        public bool IsLoaded { get; set; }

        /// <inheritdoc/>
        public Exception Exception { get; set; }

        /// <inheritdoc/>
        public abstract void OnLoad();

        /// <inheritdoc/>
        public abstract void Run();
    }
}
