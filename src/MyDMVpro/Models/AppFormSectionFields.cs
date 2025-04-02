using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public partial class AppFormSectionFields
    {
        public int SectionId { get; set; }
        public int FieldId { get; set; }
        public int? SortOrder { get; set; }
        public string FieldLabel { get; set; }
        public bool? VendorOnlyEdit { get; set; }
        public bool? VendorOnlyVisible { get; set; }
        public bool? VendorIsRequired { get; set; }
        public bool? VisibleOnEdit { get; set; }
        public bool? IsRequired { get; set; }
        public bool? AllowDefault { get; set; }
        public bool? IsPII { get; set; }
        public string Datalist { get; set; }
        [NotMapped] // Part of new field defs
        public string Description { get; set; }
        [NotMapped] // Part of new field defs
        public string HelpText { get; set; }
        [NotMapped]
        public string PopupText { get; set; }
        [NotMapped]
        public string ChoiceListValues { get; set; }
        public AppFormFields Field { get; set; }
        public AppFormSections Section { get; set; }
    }
}
