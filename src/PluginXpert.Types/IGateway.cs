using System;
using System.Collections.Generic;
using System.Text;

namespace SAPTeam.PluginXpert.Types
{
    public interface IGateway : IDisposable
    {
        bool Disposed { get; }

        T GetSettings<T>();

        void SaveSettings<T>(T settings);

        void EraseSettings();
    }
}
