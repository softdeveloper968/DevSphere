using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public partial class RequestStatus
    {
        // The view name should be set to to match
        public const string RequestStatusViewName = "vw_RequestStatus_v34";
        public RequestStatus()
        {
        }
        public int RequestNo { get; set; }
        public Guid RequestId { get; set; }
        public string AppType { get; set; }
        public string State { get; set; }
        public Guid? GroupId { get; set; }
        public string GroupName { get; set; }
        public Guid? UserId { get; set; }
        public string SubmittedBy { get; set; }
        public Guid? FileUploadId { get; set; }
        public Guid? FileUploadUserId { get; set; }
        public string Vin { get; set; }
        public string Last6Vin { get; set; }
        public Guid? VendorId { get; set; }
        public string VendorCode { get; set; }
        public DateTime? DateReceived { get; set; }
        public DateTime? Eta { get; set; }

        public DateTime? lh_eta { get; set; }

        public int? ETA_BusinessDays { get; set; } = 5; // Default value

        public string Code { get; set; }
        public int CodeCount { get; set; }
        public bool? SpecialBilling { get; set; }
        public bool? DirectToVendor { get; set; }
        public DateTime? DateToVendor { get; set; }
        public string ToVendorCourier { get; set; }
        public string ToVendorTracking { get; set; }
        public DateTime? DateSigned { get; set; }
        public DateTime? DatePrinted { get; set; }
        public DateTime? DateToDmv { get; set; }
        public DateTime? DateFromDmv { get; set; }
        public DateTime? DateShipped { get; set; }
        public DateTime? DateTitleIssued { get; set; }
        public int? CourierId { get; set; }
        public string Courier { get; set; }
        public string TrackingNumber { get; set; }
        public int? DmvCourierId { get; set; }
        public string DmvCourier { get; set; }
        public string DmvTrackingNumber { get; set; }
        public string ToDmvTrackingNumber { get; set; }
        public int? ProcessStageId { get; set; }
        public string ProcessStageName { get; set; }
        public short? StatusId { get; set; }
        public short ChatSort { get; set; }
        public short VendorChatSort { get; set; }
        public bool HasNewChat { get; set; }
        public bool HasActiveChat { get; set; }
        public bool WaitingForVendorReply { get; set; }
        public bool WaitingForUserReply { get; set; }
        public int AttachmentCount { get; set; }
        public int? AttachmentStatus { get; set; }
        public string VehicleMake { get; set; }
        public string VehicleYear { get; set; }
        public string ClientRef { get; set; }
        public string NotesAbbrev { get; set; }
        public string LienholderName { get; set; }
        [Column("DT-LH-Name")]
        public string DT_LH_Name { get; set; }
        [Column("RT-LH")]
        public string RT_LH { get; set; }
        public string Auction { get; set; }
        public string Odometer { get; set; }
        public string MileageBrand { get; set; }
        public string PA_TitleNo { get; set; }
        public string PA_LRBOL { get; set; }
        public DateTime? LienExpDate { get; set; }
        public string ELT { get; set; }
        public string BorrowerName { get; set; }
        public string AutoImsStatus { get; set; }
        public string AutoImsError { get; set; }
        public int DupeAppTypeForVin { get; set; }
        public int ActiveCountForVin { get; set; }
        public bool HasLink { get; set; }
#if false
        [JsonIgnore]
        public string RequestData { get; set; }

        [JsonIgnore]
        [NotMapped]
        public dynamic jRequest
        {
            get
            {
                return JsonConvert.DeserializeObject(string.IsNullOrEmpty(RequestData) ? "{}" : RequestData);
            }
            set
            {
                RequestData = value.ToString();
            }
        }
#endif
        public Guid? InvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? InvoiceDatePaid { get; set; }
        public decimal? ServiceFee { get; set; }
        public decimal? DmvFee { get; set; }
        public decimal? OtherFee { get; set; }
        public string OtherDesc { get; set; }
        public decimal? TotalDue { get; set; }
        public DateTime? RejectionDate { get; set; }
        public int? CheckNumber { get; set; }
        public DateTime? LI_DateToDmv { get; set; }
        public DateTime? LI_DateFromDmv { get; set; }
        public int? LI_CheckNumber { get; set; }
        public string LI_ToDmvCourier { get; set; }
        public string LI_ToDmvTracking { get; set; }
        public DateTime? LastModified { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime? TitleScanTime { get; set; }
        public DateTime? LastChatDate { get; set; }
        public DateTime? RepoDate { get; set; }
        public string ReasonCancelled { get; set; }
        public DateTime? CancelledDate { get; set; }
        public bool? HasInternalHelp { get; set; }
        public bool? HasPublicHelp { get; set; }
        public string? Outcome { get; set; }
        public string? Notes { get; set; }
        public string? Internal { get; set; }
        public string? AssignedUser { get; set; }
        public string? AssignedBy { get; set; }
        public DateTime? AuditTime { get; set; }
        public DateTime? AuditCompleteTime { get; set; }
        public string? Auditstatus { get; set; }
        public string? AuditType { get; set; }
        public string AuditBatchId { get; set; }
        public DateTime? NotReadyToAccept { get; set; }
        public DateTime? AcceptedDate { get; set; }
        public DateTime? NotReadyToProcess { get; set; }
        public DateTime? ReturnedDate { get; set; }
        [Column("REG-LH-Name")]
        public string REG_LH_Name { get; set; }
        [Column("REG-Reg-Name")]
        public string REG_Reg_Name { get; set; }
        public int ProcessingDay { get; set; }
        public string AssignedShipper { get; set; }
        public string AssignedProcessor { get; set; }
        public bool? IsMatched { get; set; }

    }
}
