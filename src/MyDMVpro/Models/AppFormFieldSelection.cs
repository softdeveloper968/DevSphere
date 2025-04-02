using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AppFormFieldSelection
    {
        public string AppFormType { get; set; }
        public int FieldId { get; set; }
        public bool? Exclude { get; set; }
        public int? FieldOrder { get; set; }
        public int? SectionId { get; set; }

        public AppFormSectionSelection AppFormSectionSelection { get; set; }
        public ApplicationTypes AppFormTypeNavigation { get; set; }
        public AppFormFields Field { get; set; }
    }
}
