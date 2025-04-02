using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class ExportDefinition
    {
        public Guid Id { get; set; }
        public Guid VendorId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FieldOrderJson { get; set; }
    }
}
