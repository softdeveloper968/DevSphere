using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class VendorState
    {
        public int Id { get; set; }
        public Guid VendorId { get; set; }
        public string State { get; set; }
    }
}
