using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MdpAppFields
    {
        public Guid VendorId { get; set; }
        public Guid FieldId { get; set; }
        public int OldFieldId { get; set; }
        public string InternalName { get; set; }
        public string FormId { get; set; }
        public int? OldSectionId { get; set; }
        public int? FieldSortOrder { get; set; }
        public string FieldLabel { get; set; }
        public string FieldDesc { get; set; }
        public int? FieldTypeId { get; set; }
        public string FieldHelpLink { get; set; }
        public string FieldPopupText { get; set; }
        public string ExcelName { get; set; }
        public bool? IsRequired { get; set; }
        public bool? AllowDefault { get; set; }
        public bool? IsVisible { get; set; }
        public string RegExValidator { get; set; }
        public string RegExValMsg { get; set; }
        public bool? VendorOnlyEdit { get; set; }
        public bool? VendorOnlyVisible { get; set; }
        public bool? VisibleOnEdit { get; set; }
        public bool? IsPii { get; set; }
        public bool? VendorIsRequired { get; set; }
    }
}
