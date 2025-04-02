using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models
{
    public class ProposedUpdates
    {
        [Key]
        public Guid ProposedUpdateId { get; set; } = Guid.NewGuid(); // Primary key as GUID

        [ForeignKey("Request")]
        public Guid RequestId { get; set; } // Foreign key to Requests table (GUID type)
        public Guid? RequestCodeId { get; set; } // Foreign key to Requests table (GUID type)
        public Guid? FieldId { get; set; } // Foreign key to Requests table (GUID type)
        public DateTime CreatedDate { get; set; } // Date and time the proposed update was created
        public Guid CreatedBy { get; set; } // ID of the user who created the proposed update
        public string jRequest_Before { get; set; } // JSON string of values before the update
        public string jRequest_Proposed { get; set; } // JSON string of proposed values
        public bool? ApprovalStatus { get; set; } // Boolean approval status (null = pending, false = rejected, true = accepted)
        public DateTime? ApprovalDate { get; set; } // Date and time of approval/rejection
        public Guid? ApprovalBy { get; set; } // ID of the user who approved/rejected
        public string? ApprovalNotes { get; set; } // Notes for approval/rejection process
        public string? CreatedByName { get; set; } // Notes for approval/rejection process
        public virtual Requests Request { get; set; }
    }

}
