using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models
{
    [Table("Invoice")]
    [Index("InvoiceDate", Name = "IX_Invoice_InvoiceDate")]
    [Index("InvoiceDatePaid", Name = "IX_Invoice_InvoiceDatePaid")]
    [Index("InvoiceDatePaid", Name = "IX_Invoice_InvoiceDatePaid_withInclude")]
    [Index("InvoiceId", Name = "UK_Invoice", IsUnique = true)]
    [Index("VendorId", "InvoiceNo", Name = "UK_Invoice_VendorId_InvoiceNo", IsUnique = true)]
    public partial class Invoice
    {
        public Invoice()
        {
            InvoiceDetails = new HashSet<InvoiceDetail>();
        }

        [Key]
        [Column("id")]
        public long Id { get; set; }

        public Guid InvoiceId { get; set; }

        public Guid VendorId { get; set; }
        public Guid? CreatedBy { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNo { get; set; }

        public DateTime InvoiceDate { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? InvoiceAmount { get; set; }
        public DateTime? DateCreated { get; set; }

        [StringLength(1024)]
        public string InvoiceNote { get; set; }

        public DateTime? InvoiceDatePaid { get; set; }

        public Guid GroupId { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? ServiceFees { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? DmvFees { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? OtherFees { get; set; }

        [StringLength(50)]
        public string CustName { get; set; }

        [StringLength(100)]
        public string CustAddr1 { get; set; }

        [StringLength(100)]
        public string CustAddr2 { get; set; }

        [StringLength(50)]
        public string CustCity { get; set; }

        [StringLength(2)]
        public string CustState { get; set; }

        [StringLength(10)]
        public string CustZip { get; set; }

        [StringLength(50)]
        public string CustAttn { get; set; }

        public bool SecondaryInvoice { get; set; }

        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; }
    }
}
