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

        /// <summary>
        /// Gets an array of the plugin's permissions.
        /// </summary>
        string[] Permission { get; }

        /// <summary>
        /// Gets or sets the load status of this plugin.
        /// </summary>
        bool IsLoaded { get; set; }

        /// <summary>
        /// Gets or sets the exception of this plugin.
        /// </summary>
        Exception Exception { get; set; }

        /// <summary>
        /// Executed right after loading the plugin.
        /// </summary>
        void OnLoad();

        /// <summary>
        /// The executive codes of plugin placed in this method. This method called manually by the host application request.
        /// </summary>
        void Run();
    }
}
