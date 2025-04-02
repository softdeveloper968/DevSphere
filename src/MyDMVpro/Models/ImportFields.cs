using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class ImportFields
    {
        public int Id { get; set; }
        public string InternalName { get; set; }
        public string Description { get; set; }
        public string DataType { get; set; }
    }
}
