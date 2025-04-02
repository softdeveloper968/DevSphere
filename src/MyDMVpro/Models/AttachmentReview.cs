using System;

namespace MyDMVpro.Models
{
    public class AttachmentReview
    {
        public const string AttachmentReviewViewName = "vw_AttachmentReview_v2";
        public Guid AttachmentId { get; set; }
        public string Filename { get; set; }
        public DateTime? DateAdded { get; set; }
        public Guid? UploadedBy { get; set; }
        public string Description { get; set; }
        public Guid? AttachmentTypeId { get; set; }
        public string AttachmentTypeName { get; set; }
        public Guid? ReviewBy { get; set; }
        public string ReviewByName { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string ReviewStatus { get; set; }
        public Guid VendorId { get; set; }
        public Guid? RequestId { get; set; }
        public int? RequestNo { get; set; }
        public string AppType { get; set; }
        public string State { get; set; }
        public int? ProcessStageId { get; set; }
        public string ProcessStageName { get; set; }
        public short? StatusId { get; set; }
        public string StatusName { get; set; }

        public string Vin { get; set; }
        public string GroupName { get; set; }
        public Guid GroupId { get; set; }
    }
}
