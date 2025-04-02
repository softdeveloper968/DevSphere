using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class VendorSettings
    {
        public Guid VendorId { get; set; }
        public string JSettings { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public Guid? ModifiedBy { get; set; }
    }
}
