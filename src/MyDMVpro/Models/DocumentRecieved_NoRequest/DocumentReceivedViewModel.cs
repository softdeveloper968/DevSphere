using Newtonsoft.Json;
using System;

namespace MyDMVpro.Models.DocumentsReceived_NoRequest
{
    public class DocumentReceivedViewModel
    {
        public const string DocumentReceivedViewName = "vw_DocumentsReceived_No_Request";
        public int Id { get; set; }
        public Guid DocumentReceivedID { get; set; }
        public Guid VendorId { get; set; }
        public DateTime DateReceived { get; set; }
        [JsonProperty("VIN")]
        public string Vin { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public string Notes { get; set; }
        public byte[] Document { get; set; }
        public string DocumentName { get; set; }
        public bool IsMatched { get; set; }
        public bool IsShipped { get; set; }
        public bool IsShow { get; set; }
        public DateTime? DateShipped { get; set; }
        public string ShippedNote { get; set; }
        public string TrackingNumber { get; set; }
        public Guid AttachmentTypeId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}
