using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MaggardNetImportMapping
    {
        public string Key { get; set; }
        public string MappedToKey { get; set; }
        public string Datatype { get; set; }
    }
}
