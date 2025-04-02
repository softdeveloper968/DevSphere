using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.Tax
{
    // use in future
    //public class TaxCalculationRecord
    //{
    //    [Key]
    //    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    //    public Guid Id { get; set; }

    //    [ForeignKey("TaxableItem")]
    //    public int ItemID { get; set; }

    //    public TaxableItem TaxableItem { get; set; }

    //    [ForeignKey("State")]
    //    public int StateID { get; set; }

    //    public State State { get; set; }

    //    [ForeignKey("Jurisdiction")]
    //    public int? JurisdictionID { get; set; }

    //    public Jurisdiction Jurisdiction { get; set; }

    //    [Column(TypeName = "decimal(18, 8)")]
    //    public decimal? TaxDue { get; set; }

    //    public Guid RequestId { get; set; }

    //    public Guid? CreatedBy { get; set; }

    //    public DateTime? CreatedDate { get; set; }

    //    public Guid? ModifiedBy { get; set; }

    //    public DateTime? ModifiedDate { get; set; }
    //}
}
