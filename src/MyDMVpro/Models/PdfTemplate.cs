using Microsoft.EntityFrameworkCore;
using MyDMVpro.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System;

namespace MyDMVpro.Models
{
    [Table("PdfTemplate")]
    [Index("AppTypeStateId", Name = "IX_PdfTemplate_AppTypeStateId")]
    public partial class PdfTemplate
    {
        public PdfTemplate()
        {
        }

        [Key]
        [Browsable(false)]
        public Guid TemplateId { get; set; }

        [Browsable(false)]
        public Guid? AppTypeStateId { get; set; }

        [StringLength(10)]
        [DisplayName("App Type")]
        public string AppType { get; set; }

        [StringLength(2)]
        [DisplayName("State")]
        [Column("AppTypeState")]
        public string AppState { get; set; }

        [StringLength(100)]
        public string DisplayName { get; set; }

        [Browsable(false)]
        public Guid? FileDataID { get; set; }

        public string Notes { get; set; }

        public bool ClientVisible { get; set; }

        public bool GenerateForClient { get; set; }

        public bool UseClientLetterhead { get; set; }

        public string LetterheadPages { get; set; }
        public int? ClientSortOrder { get; set; }

        public int? VendorSortOrder { get; set; }

        //[ForeignKey(nameof(AppTypeStateId))]
        //public virtual MdpAppTypeState AppTypeState { get; set; }
    }
}
