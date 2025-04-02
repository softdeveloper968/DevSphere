using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class VendorAgent
    {
        public Guid VendorId { get; set; }
        public Guid AgentId { get; set; }
        public bool IsVendorAdmin { get; set; }

        public Users Agent { get; set; }
        public Vendors Vendor { get; set; }
    }
}
