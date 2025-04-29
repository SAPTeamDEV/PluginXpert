using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert
{
    public class NovaGateway : Gateway, INovaGateway
    {
        public NovaGateway(Token token) : base(token)
        {
        }

        public void Print(string text)
        {
            Console.WriteLine(text);
        }
    }
}
