using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class ModulesInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AssemblyFileName { get; set; }
        public string Version { get; set; }
        public bool IsMain { get; set; }
    }
}
