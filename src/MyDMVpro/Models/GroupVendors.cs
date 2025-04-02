using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class GroupVendors
    {
        public Guid GroupId { get; set; }
        public Guid VendorId { get; set; }
        public bool IsDefault { get; set; }

        public Groups Group { get; set; }
        public Vendors Vendor { get; set; }
    }
}
