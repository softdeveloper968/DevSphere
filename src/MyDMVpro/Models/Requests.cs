using NuGet.Protocol;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyDMVpro.Models
{
    public partial class Requests
    {
        public Requests()
        {
            Chats = new HashSet<Chats>();
            NotifyClient = new HashSet<NotifyClient>();
            PendingSignRequest = new HashSet<PendingSignRequest>();
            RequestAttachments = new HashSet<RequestAttachments>();
            RequestLinks = new HashSet<RequestLinks>();
            RequestNotes = new HashSet<RequestNotes>();
            RequestSigning = new HashSet<RequestSigning>();
            ShipmentDetails = new HashSet<ShipmentDetails>();
            RequestCodes = new HashSet<RequestCode>();
        }

        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public string AppType { get; set; }
        public string JRequest { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? FileUploadId { get; set; }
        public string Vin { get; set; }
        public string Last6Vin { get; set; }
        public string State { get; set; }
        public string Code { get; set; }
        public bool? SpecialBilling { get; set; }
        public Guid? VendorId { get; set; }
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
        public string ToVendorCourier { get; set; }
        public string ToVendorTracking { get; set; }
        public DateTime? DateTitleIssued { get; set; }
        public Guid? UploadByAgent { get; set; }
        public string VehicleMake { get; set; }
        public string VehicleYear { get; set; }
        public string Auction { get; set; }
        public string LienholderName { get; set; }
        public string Odometer { get; set; }
        public bool? AutoImsUpdated { get; set; }
        public bool? ContentValidated { get; set; }
        public DateTime? DateReceived { get; set; }
        public DateTime? DateToVendor { get; set; }
        public DateTime? RejectionDate { get; set; }
        public DateTime? NotReadyToAccept { get; set; }
        public DateTime? AcceptedDate { get; set; }
        public DateTime? NotReadyToProcess { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public int? CheckNumber { get; set; }
        public DateTime? LI_DateToDmv { get; set; }
        public DateTime? LI_DateFromDmv { get; set; }
        public int? LI_CheckNumber { get; set; }
        public string LI_ToDmvCourier { get; set; }
        public string LI_ToDmvTracking { get; set; }
        public DateTime? DirectToBilling { get; set; }
        public Guid? DirectToBillingBy { get; set; }
        public string jDataTags { get; set; }
        public string jFeesData { get; set; }
        public bool? IsMatched {  get; set; }   
        public Groups Group { get; set; }
        public Status Status { get; set; }
        public Users User { get; set; }
        public Vendors Vendor { get; set; }
        public RequestCancellation RequestCancellation { get; set; }
        public RequestPdf RequestPdf { get; set; }
        public ICollection<Chats> Chats { get; set; }
        public virtual ICollection<NotifyClient> NotifyClient { get; set; }
        public ICollection<PendingSignRequest> PendingSignRequest { get; set; }
        public ICollection<RequestAttachments> RequestAttachments { get; set; }
        public ICollection<RequestNotes> RequestNotes { get; set; }
        public ICollection<RequestSigning> RequestSigning { get; set; }
        public ICollection<RequestLinks> RequestLinks { get; set; }
        public ICollection<ShipmentDetails> ShipmentDetails { get; set; }
        public ICollection<RequestCode> RequestCodes { get; set; }

        public DateTime? LH_ETA
        {
            get
            {
                // TBD: this does not take in effect holidays, and will need to be added
                // to the database and be set when the ETA for the vendor is set
                DateTime? dt = this.Eta;
                if (dt == null) return null;

                dt = dt.Value.AddDays(1);
                while (dt.Value.DayOfWeek == DayOfWeek.Saturday || dt.Value.DayOfWeek == DayOfWeek.Sunday)
                {
                    dt = dt.Value.AddDays(1);
                }
                return dt;
            }
        }
    }
}
