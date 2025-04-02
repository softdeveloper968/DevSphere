using System;

namespace MyDMVpro.Models
{
    public class AuditResponse
    {
        public int RequestNo { get; set; }
        public string? Outcome { get; set; }
        public string? Notes { get; set; }
        public string? Internal { get; set; }
        public string? AssignedUser { get; set; }
        public string? AssignedBy { get; set; }
        public DateTime? AuditTime { get; set; }
        public DateTime? AuditCompleteTime { get; set; }
        public string? Auditstatus { get; set; }
        public string? AuditType { get; set; }
        public string AuditBatchId { get; set; }
    }
}
