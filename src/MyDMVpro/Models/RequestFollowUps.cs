using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestFollowUps
    {
        public RequestFollowUps()
        {
            FollowUpTags = new HashSet<FollowUpTags>();
            FollowUpContacts = new HashSet<FollowUpContacts>();
        }

        public int FollowUpId { get; set; }
        public Guid RequestId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public string ContactAction { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool? Deleted { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public ICollection<FollowUpTags> FollowUpTags { get; set; }
        public ICollection<FollowUpContacts> FollowUpContacts { get; set; }
    }
}
