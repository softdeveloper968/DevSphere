using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public partial class FollowUpTags
    {
        public int FollowUpId { get; set; }
        public int TagId { get; set; }

        public RequestFollowUps FollowUp { get; set; }
        public Tag Tag { get; set; }
    }

    public partial class FollowUpContacts
    {
        public int FollowUpId { get; set; }
        public Guid ContactId { get; set; }

        [ForeignKey(nameof(FollowUpId))]
        public RequestFollowUps FollowUp { get; set; }

        [ForeignKey(nameof(ContactId))]
        public OrganizationContact Contact { get; set; }
    }
}
