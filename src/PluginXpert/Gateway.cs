using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert
{
    public class Gateway : IGateway
    {
        public string Id { get; }

        public IPermissionManager PermissionManager { get; }

        public Gateway(string id, IPermissionManager permissionManager)
        {
            Id = id;
            PermissionManager = permissionManager;
        }

        public void EraseSettings() => throw new NotImplementedException();

        public T GetSettings<T>() => throw new NotImplementedException();

        public void SaveSettings<T>(T settings) => throw new NotImplementedException();
    }
}
