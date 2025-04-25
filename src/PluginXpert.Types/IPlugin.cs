using System;

namespace SAPTeam.PluginXpert.Types
{
    /// <summary>
    /// Provides standard interface to implement managed plugins.
    /// </summary>
    public interface IPlugin<T> : IDisposable
        where T : IGateway
    {
        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets an array of the plugin's permissions.
        /// </summary>
        string[] Permissions { get; }

        /// <summary>
        /// Executed right after loading the plugin.
        /// </summary>
        /// <param name="gateway">
        /// The gateway to communicate with host.
        /// </param>
        void OnLoad(T gateway);

        /// <summary>
        /// The executive codes of plugin placed in this method. This method called manually by the host application request.
        /// </summary>
        void Run();
    }
}
