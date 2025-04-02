using System;

namespace MyDMVpro.Models.AttachmentReviewModels
{
    public class MissingAttachmentsViewModel
    {
        public MissingAttachmentsViewModel() { }

        public Guid RequestId { get; set; }
        public string AttachmentDescription { get; set; }
        public string AttachmentName { get; set; }
        public string AttachmentType { get; set; } 
        public string AttachmentTypeName { get; set; } 
        public Guid AttachmentTypeId { get; set; } 
        public int ProcessStageId { get; set; } 
        public bool HardcopyOrDigital { get; set; } 
        public string VIN { get; set; } 
        public string State { get; set; } 
        public string Type { get; set; } 
        public DateTime? DateToVendor { get; set; } 
        public int RequestNumber { get; set; } 
    }

}
