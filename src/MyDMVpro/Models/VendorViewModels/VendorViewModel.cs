using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System;
using System.Collections.Generic;

namespace MyDMVpro.Models.VendorViewModels
{
    public class VendorViewModel
    {
        public string VIN { get; set; }
        public string AppType { get; set; }
        public string State { get; set; }
        public int ReferenceNumber { get; set; }
        public DateTime? DateToVendor { get; set; }
        public bool HasIssues { get; set; }
        public string Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public Guid? VendorId { get; set; }
        public int? ProcessStageID { get; set; }
        public string? ProcessStageName { get; set; }
        public string? ProcessStageSummary { get; set; }
        public string? ProcessStageSummaryLabel { get; set; }
        public DateTime? LI_DateToDmv { get; set; }
        public DateTime? LI_DateFromDmv { get; set; }
        public DateTime? DateReceived { get; set; }
        public DateTime? RejectionDate { get; set; }
        public DateTime? NotReadyToAccept { get; set; }
        public DateTime? DateTitleIssued { get; set; }
        public DateTime? NotReadyToProcess { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public short? StatusId { get; set; }
        public DateTime? Eta { get; set; }
        public bool? DirectToVendor { get; set; }
        public DateTime? DateSigned { get; set; }
        public DateTime? DatePrinted { get; set; }
        public DateTime? DateToDmv { get; set; }
        public DateTime? DateFromDmv { get; set; }
        public DateTime? DateShipped { get; set; }
        public string TrackingNumber { get; set; }

        public ICollection<RequestNotes> RequestNotes { get; set; }




    }
}
