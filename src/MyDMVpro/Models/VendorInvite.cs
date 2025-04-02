using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class VendorInvite
    {
        public Guid Id { get; set; }
        public Guid? VendorId { get; set; }
        public string UserEmail { get; set; }
        public DateTime? DateCreated { get; set; }
        public bool? Accepted { get; set; }
        public Guid CreatedBy { get; set; }
        public bool? Deleted { get; set; }
        public Guid? UserIdCreated { get; set; }
        public bool MakeVendorAdmin { get; set; }
    }
}
