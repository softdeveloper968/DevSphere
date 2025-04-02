using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MaggardNetImport
    {
        public int Id { get; set; }
        public string Json { get; set; }
        public DateTime DateSubmitted { get; set; }
        public int? Deleted { get; set; }
        public int? Processed { get; set; }
    }
}
