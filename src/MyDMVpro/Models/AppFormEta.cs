using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AppFormEta
    {
        public Guid Id { get; set; }
        public string State { get; set; }
        public Guid? VendorId { get; set; }
        public string AppType { get; set; }
        public short? Days { get; set; }
    }
}
