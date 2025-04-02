using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models
{
    public class PaymentTypes
    {
        [Key]
        public int PaymentTypeID { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentTypeName { get; set; } = string.Empty;
        public bool IsCredit { get; set; }

        public virtual ICollection<PaymentsAndDisbursement> PaymentsAndDisbursement { get; set; }
    }
}
