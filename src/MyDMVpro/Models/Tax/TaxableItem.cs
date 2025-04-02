using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.Tax
{
    public class TaxableItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ItemID { get; set; }

        public string ItemName { get; set; }

        public string? Definition { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public Guid? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool? IsDeleted { get; set; }
    }
}
