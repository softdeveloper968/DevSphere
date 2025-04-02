using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AppFormStateSections
    {
        public string AppFormType { get; set; }
        public string AppTypeState { get; set; }
        public int SectionId { get; set; }
        public string SectionTitle { get; set; }
        public int? SectionSortOrder { get; set; }

        public AppTypeStates App { get; set; }
    }
}
