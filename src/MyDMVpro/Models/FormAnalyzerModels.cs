using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class xFormAnalyzerModels
    {
        public int Id { get; set; }
        public Guid FormId { get; set; }
        public Guid ModelId { get; set; }
        public DateTime? DateCreated { get; set; }
        public string ResultsMapping { get; set; }
        public string VinKey { get; set; }
        public string ScanPages { get; set; }
        public bool? Active { get; set; }
    }
}
