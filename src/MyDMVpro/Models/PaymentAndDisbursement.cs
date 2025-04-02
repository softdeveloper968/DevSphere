using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace MyDMVpro.Models
{
    public class PaymentsAndDisbursement
    {
        [Key]
        public int PaymentID { get; set; } 

        [Required]
        public Guid RequestId { get; set; } 

        [StringLength(255)]
        public string Vin { get; set; } 

        [Required]
        public int PaymentTypeID { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } 

        [StringLength(255)]
        public string ReferenceNumber { get; set; } 

        public DateTime? PaymentDate { get; set; } = DateTime.Now; 

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ProcessingFee { get; set; } 

        [NotMapped]
        public decimal TotalCharge => Amount + (ProcessingFee ?? 0);

        public bool IsCredit { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now; 

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public Guid CreatedBy { get; set; }
        public Guid ModifiedBy { get; set; } 

        [ForeignKey("PaymentTypeID")]
        public virtual PaymentTypes? PaymentTypes { get; set; }
    }
}
