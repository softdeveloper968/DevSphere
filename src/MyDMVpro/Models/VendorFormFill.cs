using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class VendorFormFill
    {
        public int Id { get; set; }
        public Guid VendorId { get; set; }
        public string FormCode { get; set; }
        public string JData { get; set; }
        public string AppType { get; set; }
        public string AppTypeState { get; set; }

        public Vendors Vendor { get; set; }
    }
}
