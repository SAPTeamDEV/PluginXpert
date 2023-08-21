﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert
{
    /// <summary>
    /// Provides standard base class to implement managed plugin.
    /// </summary>
    public class Plugin : IPlugin
    {
        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public string[] Permissions { get; }

        /// <inheritdoc/>
        public PermissionManager PermissionManager { get; set; }

        /// <inheritdoc/>
        public bool IsLoaded { get; set; }

        /// <inheritdoc/>
        public Exception Exception { get; set; }

        /// <inheritdoc/>
        public void OnLoad()
        {
            throw new NotImplementedException();
        }

    /// <inheritdoc/>
        public void Run()
        {
            throw new NotImplementedException();
        }
    }
}
