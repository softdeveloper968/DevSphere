using System;

namespace MyDMVpro.Models
{
    public class NeedMissingDataModel
    {
        public Guid RequestId { get; set; }
        public string Note { get; set; }
        public string Resolution { get; set; }
        public string Field { get; set; } 
        public string State { get; set; }
        public string AppType { get; set; }
        public int ProcessStageId { get; set; }
        public DateTime? DateToVendor { get; set; }
        public string VIN { get; set; }
        public string ProcessStageName { get; set; }
        public int RequestNumber { get; set; }
        public string TagName { get; set; }
        public Guid RequestCodeId { get; set; }
        public Guid? FieldId { get; set; }
        public Guid VendorId { get; set; }
        public Guid GroupId { get; set; }
    }

}
