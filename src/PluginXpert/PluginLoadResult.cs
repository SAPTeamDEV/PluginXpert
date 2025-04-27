using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert
{
    public enum PluginLoadResult
    {
        Unknown = 0,

        AlreadyLoaded,

        NotSupportedImplementation,

        NotSupportedRuntime,

        Error,

        Success,
    }
}
