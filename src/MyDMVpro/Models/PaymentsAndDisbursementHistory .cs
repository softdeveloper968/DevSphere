using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace MyDMVpro.Models
{
    public class PaymentsAndDisbursementHistory
    {
        public int HistoryID { get; set; } 

        public int PaymentID { get; set; }

        public Guid RequestId { get; set; }

        public string? Vin { get; set; }

        public int PaymentTypeID { get; set; }

        public decimal Amount { get; set; }

        public string? ReferenceNumber { get; set; }

        public DateTime PaymentDate { get; set; }

        public decimal? ProcessingFee { get; set; }

        public decimal TotalCharge { get; set; } 

        public bool IsCredit { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Guid? ModifiedBy { get; set; } 
        public Guid? CreatedBy { get; set; } 

        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public string ChangeType { get; set; } 
    }
}
