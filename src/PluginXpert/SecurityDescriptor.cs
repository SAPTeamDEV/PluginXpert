using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SAPTeam.PluginXpert.Types;

namespace SAPTeam.PluginXpert;

public readonly struct SecurityDescriptor
{
    public string Owner { get; init; }

    public Permission[] Permissions { get; init; }

    public byte[] Signature { get; init; }

    public static string GetString(string owner, Permission[] permissions)
    {
        return $"{owner}${string.Join(",", permissions.Select(p => p.ToString()))}";
    }

    public override string ToString() => GetString(Owner, Permissions);
}
