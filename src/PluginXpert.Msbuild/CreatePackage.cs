using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace PluginXpert.Msbuild
{
    public class CreatePackage : Task
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Output { get; set; }

        [Required]
        public string BuildOutput { get; set; }

        public string Name { get; set; }

        public override bool Execute()
        {
            return true;
        }
    }
}
