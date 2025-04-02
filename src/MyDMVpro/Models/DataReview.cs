using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{

    [Keyless]
    [Table("vw_DataReview_v2")]
    public partial class DataReview
    {
        public string? AppType { get; set; }
        public Guid? ApprovalBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime DateToVendor { get; set; }
        public string? ApprovalNotes { get; set; }
        public bool? ApprovalStatus { get; set; }
        public bool Cleared { get; set; }
        public Guid? ClearedBy { get; set; }
        public DateTime? ClearedDate { get; set; }
        public string? ClearedNote { get; set; }
        public string ExcelName { get; set; }
        public Guid? FieldId { get; set; }
        public string Note { get; set; }
        public int? ProcessStageId { get; set; }
        public Guid? RequestCodeId { get; set; }
        public Guid? RequestId { get; set; }
        public int? RequestNo { get; set; }
        public string Resolution { get; set; }
        public string State { get; set; }
        public int TagId { get; set; }
        public string TagName { get; set; }
        public string Vin { get; set; }
        public string ProcessStageName { get; set; }
        public Guid? ProposedUpdateRequestId { get; set; }
        public string? StatusName { get; set; }
        public Guid? ProposedUpdateId { get; set; }
        public Guid? VendorId { get; set; }
        public Guid? GroupId { get; set; }

    }
}
