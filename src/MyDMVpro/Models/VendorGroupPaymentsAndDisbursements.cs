using System;

namespace MyDMVpro.Models
{
    public class VendorGroupPaymentsAndDisbursements
    {
        public Guid VendorId { get; set; }
        public Guid GroupId { get; set; }
        public int PaymentID { get; set; }
        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public string Vin { get; set; }
        public string PaymentTypeName { get; set; }
        public int PaymentTypeID { get; set; }
        public decimal Amount { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal? ProcessingFee { get; set; }
        public decimal TotalCharge { get; set; }
        public bool IsCredit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid ModifiedBy { get; set; }
        public string State { get; set; }
        public string AppType { get; set; }
        public string GroupName { get; set; }

    }

}
