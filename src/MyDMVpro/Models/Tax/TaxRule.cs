using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.Tax
{
    public class TaxRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid RuleID { get; set; }

        [ForeignKey("TaxableItem")]
        public int ItemID { get; set; }

        public TaxableItem TaxableItem { get; set; }

        [Column(TypeName = "char(2)")]
        public string StateAbbreviation { get; set; }

        [ForeignKey(nameof(StateAbbreviation))]
        public US_State State { get; set; }

        [ForeignKey("Jurisdiction")]
        public int? JurisdictionID { get; set; }

        public Jurisdiction Jurisdiction { get; set; }
        public bool IsApplicable { get; set; }

        [Column(TypeName = "decimal(18, 8)")]
        public decimal? Taxable { get; set; }

        [Column(TypeName = "decimal(18, 8)")]
        public decimal? TaxRate { get; set; }

        [Column(TypeName = "decimal(18, 8)")]
        public decimal? MaxTaxableAmount { get; set; }

        public bool RequiresTaxRateFetch { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public Guid? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
