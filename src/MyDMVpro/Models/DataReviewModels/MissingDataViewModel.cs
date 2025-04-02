using System;

namespace MyDMVpro.Models.DataReviewModels
{
    public class MissingDataViewModel
    {
        public Guid RequestId { get; set; }
        public Guid RequestCodeId { get; set; }
        public Guid FieldId { get; set; }
        public string Note { get; set; }
        public string Resolution { get; set; }
        public string Field { get; set; }
        public string TagName { get; set; }
        public int ProcessStageId { get; set; }
        public string VIN { get; set; }
        public string State { get; set; }
        public string Type { get; set; }
        public DateTime? DateToVendor { get; set; }
        public int RequestNumber { get; set; }
    }

}
