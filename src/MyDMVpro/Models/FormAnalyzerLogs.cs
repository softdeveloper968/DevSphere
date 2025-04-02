using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class FormAnalyzerLogs
    {
        public int Id { get; set; }
        public DateTime? Executed { get; set; }
        public Guid? ModelId { get; set; }
        public Guid? FileUploadId { get; set; }
        public int? StatusCode { get; set; }
        public string Results { get; set; }
        public Guid? RequestedBy { get; set; }
        public int? PageCount { get; set; }
    }
}
