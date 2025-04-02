using MyDMVpro.Controllers;
using MyDMVpro.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MyDMVpro.Common
{
    public partial class RequestHistory : BaseHistory
    {
        public RequestHistory(RequestTracking rt)
        {
            this.RequestId = rt.RequestId;
            this.StatusId = rt.StatusId;
            this.ProcessStageId = rt.ProcessStageId;
            this.AssignedToName = rt.AssignedToName;
            this.ModifiedBy = rt.ChangedBy;
            this.SortDate = rt.ChangeDate;

            if (string.IsNullOrEmpty(rt.JRequestDiff))
            {
                this.RequestDiff = JsonDiff.GetDiff(rt.JRequest_Before, rt.JRequest_After);
            }
            else
            {
                this.RequestDiff = JsonConvert.DeserializeObject<DiffDictionary>(rt.JRequestDiff);
            }
            if (string.IsNullOrEmpty(rt.JProcessDiff))
            {
                // must do the diff if not already calculated
                this.ProcessDiff = JsonDiff.GetDiff(rt.JProcess_Before, rt.JProcess_After);
            }
            else
            {
                this.ProcessDiff = JsonConvert.DeserializeObject<DiffDictionary>(rt.JProcessDiff);
            }
        }
        public Guid RequestId { get; set; }
        
        // public DateTime ChangeDate { get; set; }
        public int? StatusId { get; set; }
        public int? ProcessStageId { get; set; }
        public DiffDictionary RequestDiff { get; set; }
        public DiffDictionary ProcessDiff { get; set; }
        public string AssignedToName { get; set; }
        public string StatusChangeMsg { get; set; }
        public string StageChangeMsg { get; set; }
        public string AssignedToChangeMsg { get; set; }
    }

    public class AttachmentHistory : BaseHistory
    {
        public string FileName { get; set; }
        public string Description { get; set; }
    }
    public class ChatHistory : BaseHistory
    {
        public string Message { get; set; }
    }
    public class BaseHistory
    {
        public DateTime? SortDate { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string DisplayModifiedDate { get; set; }
        public string DisplayCreatedDate { get; set; }
        public string DisplayCreatedTime { get; set; }
        public string DisplayType { get; set; }
        public string DisplayModifiedBy { get; set; }
        public string DisplayCreatedBy { get; set; }
        public virtual bool DateOnlyFormat { get { return false; } }
        public Guid? ReadBy { get; set; }
        public string DisplayReadBy { get; set; }
        public DateTime? ReadOn { get; set; }
    }
    public class NoteHistory : BaseHistory
    {
        public string Note { get; set; }
        public string Remark { get; set; }
    }
    public class ProcessHistory : BaseHistory
    {
        public string Action { get; set; }
        public override bool DateOnlyFormat => true;
    }
    public static class ProcessHistory_Extensions
    {
        public static void AddAction(this List<ProcessHistory> list, string action, DateTime? date)
        {
            if (date != null)
            {
                list.Add(new ProcessHistory()
                {
                    SortDate = date,
                    Action = action
                });
            }
        }
    }
    public class FollowUpHistory : BaseHistory
    {
        public string Code { get; set; }
        public string Title { get; set; }
        public string Note { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool? Deleted { get; set; }

        public RequestFollowUps FollowUp { get; set; }
        public bool MissedDueDate { get; set; }
        public override bool DateOnlyFormat => true;
        public DiffDictionary Diff { get; set; }
    }
    public class RequestHistoryModel
    {
        public List<BaseHistory> History { get; set; }
        public RequestStatus Request { get; set; }
    }
    public class InvoiceHistory : BaseHistory
    {
        public string Action { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime? DatePaid { get; set; }
        public Decimal? DmvFee { get; set; }
        public Decimal? MailingFee { get; set; }
        public Decimal? TotalDue { get; set; }
        public Decimal? ServiceFee { get; set; }
        public Decimal? OtherFee { get; set; }
    }
   public class PaymentAndDisbursementHistory : BaseHistory
    {
        public int PaymentID { get; set; }
        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public string Vin { get; set; }
        public int PaymentTypeID { get; set; }
        public decimal Amount { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal? ProcessingFee { get; set; }
        public decimal TotalCharge { get; set; }
        public bool IsCredit { get; set; }

        public string ChangeType { get; set; }  

    }
}
