using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace MyDMVpro.Models
{
    public class PaymentLink
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }
        public Guid RequestId { get; set; }
        public Guid VendorId { get; set; }
        public string Vin { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceCharge { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; }

        [Required]
        [MaxLength(255)]
        public string PaymentLinkUrl { get; set; }

        public string PaymentMethod { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        public bool IsPaid { get; set; } = false;
    }
}
