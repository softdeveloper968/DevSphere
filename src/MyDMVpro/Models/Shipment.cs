using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyDMVpro.Models
{
    public partial class Shipment
    {
        public Shipment()
        {
        }

        public Guid ShipmentId { get; set; }
        public DateTime DateShipped { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string AuctionName { get; set; }
        public string GroupName { get; set; }
        public Guid? VendorId { get; set; }
        public string Courier { get; set; }
        public string TrackingNumber { get; set; }
        public int NumberShipped { get; set; }
        [NotMapped]
        public string QRSVG { get; set; }
        [NotMapped]
        public string EditUrl { get; set; }
        [NotMapped]
        public List<ShipmentDetail> ShipmentDetails { get; set; }
    }

    public partial class ShipmentDetail
    {
        public ShipmentDetail()
        {
        }

        public Guid ShipmentId { get; set; }
        public int SortOrder { get; set; }
        public Guid RequestId { get; set; }
        [NotMapped]
        public RequestStatus RequestStatus { get; set; }
    }
}
