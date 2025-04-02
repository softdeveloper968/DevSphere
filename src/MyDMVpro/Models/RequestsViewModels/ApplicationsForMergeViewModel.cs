using System;
using System.Collections.Generic;

namespace MyDMVpro.Models.RequestsViewModels
{
    public class ApplicationsForMergeViewModel
    {
        public string State { get; set; }
        public string AppType { get; set; }
        public bool IncludeCheckMerge { get; set; } 
        public List<PdfTemplate> PdfTemplates { get; set; }
    }
}
