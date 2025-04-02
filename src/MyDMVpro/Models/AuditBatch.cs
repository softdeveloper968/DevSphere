using System;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models
{
    public class AuditBatch
    {
        public string AuditBatchId { get; set; }

        public string AuditFinalNote { get; set; }

        public DateTime? AuditFinalTime { get; set; }

        public string AuditCompletedBy { get; set; }

        public string AuditName { get; set; }
        public string AuditType { get; set; }
        public DateTime? StartTime { get; set; }
        public TimeSpan? TotalTime { get; set; }
    }
}


