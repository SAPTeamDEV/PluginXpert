using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert
{
    public class PluginContext<TPlugin, TGateway>
        where TPlugin : IPlugin<TGateway>
        where TGateway : IGateway
    {
        public string Id {  get; private set; }

        public TPlugin Instance { get; }

        public bool IsLoaded { get; private set; }

        public Exception Exception { get; private set; }

        public TGateway Gateway { get; private set; }

        public PluginManager<TPlugin, TGateway> PluginManager { get; }

        public PermissionManager PermissionManager { get; }

        public PluginContext(TPlugin instance, PluginManager<TPlugin, TGateway> pluginManager, PermissionManager permissionManager)
        {
            Instance = instance;
            PluginManager = pluginManager;
            PermissionManager = permissionManager;

            IsLoaded = false;
        }

        public virtual void LoadPlugin(bool throwOnFail = true)
        {
            try
            {
                Id = PermissionManager.RegisterPlugin(Instance, PermissionManager.GetPermissions(Instance.Permissions));
                Instance.OnLoad(CreateGateway());
                IsLoaded = true;
            }
            catch (Exception e)
            {
                IsLoaded = false;
                Exception = e;

                if (throwOnFail)
                {
                    throw;
                }
            }
        }

        protected virtual TGateway CreateGateway()
        {
            return (TGateway)(IGateway)(new Gateway(Id, PermissionManager));
        }
    }
}
