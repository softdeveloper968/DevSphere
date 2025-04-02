using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class ShipmentDetails
    {
        public Guid ShipmentId { get; set; }
        public int SortOrder { get; set; }
        public Guid RequestId { get; set; }

        public Requests Request { get; set; }
        public Shipments Shipment { get; set; }
    }
}
