using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MaggardNetRequests
    {
        public int Id { get; set; }
        public string Json { get; set; }
        public DateTime? DateSubmitted { get; set; }
        public Guid? UserId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? VendorId { get; set; }
        public Guid? AgentId { get; set; }
        public Guid? CreatedRequestId { get; set; }

        public Users Agent { get; set; }
        public Requests CreatedRequest { get; set; }
        public Groups Group { get; set; }
        public Users User { get; set; }
        public Vendors Vendor { get; set; }
    }
}
