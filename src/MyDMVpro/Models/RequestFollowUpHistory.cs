using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestFollowUpHistory
    {
        public RequestFollowUpHistory()
        {
        }
        public int FollowUpId { get; set; }
        public Guid RequestId { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool? Deleted { get; set; }
        public Guid? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}

