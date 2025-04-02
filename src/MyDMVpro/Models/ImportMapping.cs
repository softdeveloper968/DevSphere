using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class ImportMapping
    {
        public Guid MappingId { get; set; }
        public Guid GroupId { get; set; }
        public string JMapping { get; set; }
    }
}
