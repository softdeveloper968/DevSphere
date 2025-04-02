using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MyDMVpro.Models
{
    public partial class RequestFollowUpStatus
    {
        public int FollowUpId { get; set; }
        public Guid RequestId { get; set; }
        public int RequestNo { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Code { get; set; }
        public string Tags { get; set; }
        public string TagIds { get; set; }
        public string ContactAction { get; set; }
        public string Contacts { get; set; }
        public string ContactIds { get; set; }

        [NotMapped]
        public List<Guid> ContactIdList
        {
            get
            {
                if (string.IsNullOrEmpty(ContactIds))
                {
                    return new List<Guid>();
                }
                return ContactIds.Split(',').Select(Guid.Parse).ToList();
            }
        }

        public string Title { get; set; }
        public string Notes { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool? Deleted { get; set; }
        public Guid VendorId { get; set; }
        public string AppType { get; set; }
        public string State { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public string VIN { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime? LastModified { get; set; }
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
