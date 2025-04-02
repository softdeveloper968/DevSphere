using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models
{
    public class FormAnalyzerUploads
    {
        public int id { get; set; }
        public Guid FileUploadId { get; set; }
        public Guid? FormId { get; set; }
        public Guid VendorId { get; set; }
        public DateTime? DateUploaded { get; set; }
        public DateTime? DateScanned { get; set; }
        public Guid? RequestId { get; set; }
        public DateTime? DateApproved { get; set; }
        public Guid? ApprovedBy { get; set; }
        public Guid? AttachmentId { get; set; }
        public string FormName { get; set; }
        public string UploadFileName { get; set; }
        public string Vin { get; set; }
        public Guid? ModelId { get; set; }
        //public string AnalyzeResult { get; set; }
        public string ExtractedFields { get; set; }
        public string VisibleFields { get; set; }
        public string HiddenFields { get; set; }
        public int ActiveMatches { get; set; }
        public int TotalMatches { get; set; }
    }
}
