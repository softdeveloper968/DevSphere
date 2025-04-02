using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public partial class MasterFields
    {
        public MasterFields()
        {
            MdpAppSectionFields = new HashSet<MdpAppSectionFields>();
        }

        public Guid FieldId { get; set; }
        public Guid VendorId { get; set; }
        public string InternalName { get; set; }
        public string ExcelName { get; set; }
        public bool? ExcludeOnExport { get; set; }
        public string JsonValue { get; set; }
        public string AltExportSource { get; set; }
        public string FormDefaultLabel { get; set; }
        public string FormDefaultDesc { get; set; }
        public int? FormDefaultFieldTypeId { get; set; }
        public string FormDefaultHelpLink { get; set; }
        public string FormDefaultPopupText { get; set; }
        public string FormDefaultRegExValidator { get; set; }
        public string FormDefaultRegExValMsg { get; set; }
        public bool? FormDefaultVendorOnlyEdit { get; set; }
        public bool? FormDefaultVendorOnlyVisible { get; set; }
        public bool? FormDefaultIsPii { get; set; }
        public bool? FormDefaultVendorIsRequired { get; set; }
        public bool? FormDefaultIsRequired { get; set; }
        public bool? FormDefaultAllowDefault { get; set; }
        public bool? FormDefaultIsVisible { get; set; }
        public bool? FormDefaultVisibleOnEdit { get; set; }
        public string DisplayName { get; set; }

        public Guid? GroupProfileCategoryId { get; set; }
        public Guid? GroupProfileSourceFieldId { get; set; }

        [ForeignKey(nameof(GroupProfileSourceFieldId))]
        public MasterFields GroupProfileSourceField { get; set; }

        public MdpFieldTypes FormDefaultFieldType { get; set; }
        public Vendors Vendor { get; set; }
        public ICollection<MdpAppSectionFields> MdpAppSectionFields { get; set; }
    }
}
