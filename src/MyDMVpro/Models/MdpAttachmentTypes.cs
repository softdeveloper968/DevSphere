using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    [Table("mdpAttachmentTypes")]
    public partial class MdpAttachmentTypes
    {
        public MdpAttachmentTypes()
        {
            ReviewColumns = new Collection<MdpAttachmentTypeReviewColumn>();
        }
        [Key]
        public Guid AttachmentTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public bool HardcopyOnly { get; set; }

        public bool InternalFromVendor { get; set; }

        public bool InternalFromClient { get; set; }

        [StringLength(50)]
        public string ExcelName { get; set; }

        public bool NoReviewRequired { get; set; }

        [StringLength(200)]
        public string ReviewTextLine1 { get; set; }

        [StringLength(200)]
        public string ReviewTextLine2 { get; set; }

        public bool? ExcludeFromOneOffs { get; set; }

        public ICollection<MdpAttachmentTypeReviewColumn> ReviewColumns { get; set; }
    }
}
