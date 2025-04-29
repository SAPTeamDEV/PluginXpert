using System;

namespace SAPTeam.PluginXpert.Types
{
    /// <summary>
    /// This interface defines the contract for a plugin.
    /// </summary>
    public interface IPlugin : IDisposable
    {
        /// <summary>
        /// This method is called when the plugin is loaded.
        /// </summary>
        /// <param name="gateway">
        /// The gateway instance that provides access to the plugin's implementation public API.
        /// </param>
        void OnLoad(IGateway gateway);

        /// <summary>
        /// This method is called by the plugin host to run the plugin.
        /// </summary>
        void Run();
    }
}
