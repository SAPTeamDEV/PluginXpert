using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert
{
    /// <summary>
    /// Provides standard interface to implement managed plugin.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        string Name { get; }
    }
}
