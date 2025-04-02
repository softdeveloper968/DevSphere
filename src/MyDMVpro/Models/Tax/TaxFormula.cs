using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.Tax
{
    public class TaxFormula
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [Column(TypeName = "char(2)")]
        public string StateAbbreviation { get; set; }

        [ForeignKey(nameof(StateAbbreviation))]
        public US_State State { get; set; }

        [ForeignKey("Jurisdiction")]
        public int? JurisdictionID { get; set; }

        public Jurisdiction? Jurisdiction { get; set; }
        public string TotalTaxableAmount { get; set; }
        public string TotalTaxDue { get; set; }
        public string TotalTaxPaidOtherState { get; set; }
        public string TotalLeftOverTitlingState { get; set; }
        public Guid? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public Guid? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
