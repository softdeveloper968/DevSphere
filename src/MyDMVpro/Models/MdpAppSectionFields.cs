using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MdpAppSectionFields
    {
        public Guid SectionFieldId { get; set; }
        public Guid SectionId { get; set; }
        public Guid FieldId { get; set; }
        public int? SortOrder { get; set; }
        public string Label { get; set; }
        public string Desc { get; set; }
        public int? FieldTypeId { get; set; }
        public string HelpLink { get; set; }
        public string PopupText { get; set; }
        public string RegExValidator { get; set; }
        public string RegExValMsg { get; set; }
        public bool? VendorOnlyEdit { get; set; }
        public bool? VendorOnlyVisible { get; set; }
        public bool? IsPii { get; set; }
        public bool? VendorIsRequired { get; set; }
        public bool? IsRequired { get; set; }
        public bool? AllowDefault { get; set; }
        public bool? IsVisible { get; set; }
        public bool? VisibleOnEdit { get; set; }

        public MasterFields Field { get; set; }
        public MdpFieldTypes FieldType { get; set; }
        public MdpAppSections Section { get; set; }
    }
}
