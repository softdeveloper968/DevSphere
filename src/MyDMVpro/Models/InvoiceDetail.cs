using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    [Table("InvoiceDetail")]
    [Index("RequestId", "SecondaryInvoice", Name = "IX_InvoiceDetail_RequestId", IsUnique = true)]
    [Index("InvoiceId", "InvoiceListOrder", Name = "UK_InvoiceDetail_InvoiceIdInvoiceListOrder", IsUnique = true)]
    public partial class InvoiceDetail
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        public Guid InvoiceId { get; set; }

        public Guid RequestId { get; set; }

        public int InvoiceListOrder { get; set; }

        [Column(TypeName = "decimal(8, 2)")]
        public decimal? ServiceFee { get; set; }

        [Column("DMVDisbursement", TypeName = "decimal(8, 2)")]
        public decimal? DmvFee { get; set; }

        [Column(TypeName = "decimal(8, 2)")]
        public decimal? TotalDue { get; set; }

        [StringLength(5)]
        public string SvcCode { get; set; }

        [StringLength(2)]
        public string State { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateSubmitted { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateShipped { get; set; }

        [StringLength(50)]
        public string ClientRefNo { get; set; }

        [Column("VIN")]
        [StringLength(17)]
        public string VIN { get; set; }

        [Column(TypeName = "decimal(8, 2)")]
        public decimal? OtherFee { get; set; }

        [StringLength(50)]
        public string OtherDesc { get; set; }

        [Column(TypeName = "decimal(8, 2)")]
        public decimal? AbstractFee { get; set; }

        [Column(TypeName = "decimal(8, 2)")]
        public decimal? MailingFee { get; set; }

        public int? RequestNo { get; set; }

        [Column("GLCode")]
        [StringLength(50)]
        public string GlCode { get; set; }

        public bool SecondaryInvoice { get; set; }

        public virtual Invoice Invoice { get; set; }
    }
}
