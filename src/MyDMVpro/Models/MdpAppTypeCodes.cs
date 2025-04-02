using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MdpAppTypeCodes
    {
        public Guid VendorId { get; set; }
        public string AppType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string QueueName { get; set; }
        public bool? AutoImsenabled { get; set; }
        public string AliasForAppType { get; set; }
        public bool? Active { get; set; }
    }
}
