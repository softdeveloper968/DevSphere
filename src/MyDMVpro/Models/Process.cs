using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Process
    {
        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public DateTime? DateReceived { get; set; }
        public DateTime? Eta { get; set; }
        public bool? DirectToVendor { get; set; }
        public DateTime? DateSigned { get; set; }
        public DateTime? DatePrinted { get; set; }
        public DateTime? DateToDmv { get; set; }
        public DateTime? DateFromDmv { get; set; }
        public DateTime? DateShipped { get; set; }
        public int? CourierId { get; set; }
        public string Courier { get; set; }
        public string TrackingNumber { get; set; }
        public int? DmvCourierId { get; set; }
        public string DmvCourier { get; set; }
        public string DmvTrackingNumber { get; set; }
        public string ToDmvTrackingNumber { get; set; }
        public int? ProcessStageId { get; set; }
        public short? StatusId { get; set; }
    }
}
