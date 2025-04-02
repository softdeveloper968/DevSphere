using System;

namespace MyDMVpro.Models
{
    public class AuditFollowUpResponse
    {
        public int FollowUpId { get; set; }
        public string? Outcome { get; set; }
        public string? AuditNotes { get; set; }
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


