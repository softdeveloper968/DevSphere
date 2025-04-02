using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AppFormSectionSelection
    {
        public AppFormSectionSelection()
        {
            AppFormFieldSelection = new HashSet<AppFormFieldSelection>();
        }

        public string AppFormType { get; set; }
        public int SectionId { get; set; }
        public int? SectionOrder { get; set; }

        public ApplicationTypes AppFormTypeNavigation { get; set; }
        public AppFormSections Section { get; set; }
        public ICollection<AppFormFieldSelection> AppFormFieldSelection { get; set; }
    }
}
