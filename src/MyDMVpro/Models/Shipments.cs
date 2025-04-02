using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Shipments
    {
        public Shipments()
        {
            ShipmentDetails = new HashSet<ShipmentDetails>();
        }

        public Guid ShipmentId { get; set; }
        public Guid VendorId { get; set; }
        public DateTime DateShipped { get; set; }
        public string AuctionName { get; set; }
        public int NumberShipped { get; set; }
        public DateTime? DateCreated { get; set; }
        public Guid? CreatedBy { get; set; }
        public string Courier { get; set; }
        public string TrackingNumber { get; set; }
        public string GroupName { get; set; }

        public Vendors Vendor { get; set; }
        public ICollection<ShipmentDetails> ShipmentDetails { get; set; }
    }
}
