using DocumentFormat.OpenXml.Spreadsheet;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace MyDMVpro.Models
{
    public class StripePaymentModel
    {
            [Key]
            public Guid PaymentId { get; set; }

            [Required]
            public Guid UserId { get; set; }

            [Required]
            public Guid VendorId { get; set; }

            [Required]
            [StringLength(255)]
            public Guid RequestId { get; set; }

            [Required]
            public string VIN { get; set; }

            [Required]
            public string Amount { get; set; }
        
            public string TotalAmount { get; set; }
        
            public string ServiceCharge { get; set; }

            [Required]
            [StringLength(10)]
            public string Currency { get; set; } = "USD";

            [StringLength(255)]
            public string PaymentIntentId { get; set; }

            [StringLength(255)]
            public string StripeTransactionId { get; set; }
            public string PaymentType { get; set; }
            public string PaymentMethod { get; set; }

            [Required]
            [StringLength(50)]
            public string Status { get; set; } = "Pending";

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
}
