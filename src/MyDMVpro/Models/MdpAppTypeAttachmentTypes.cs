using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    [Table("mdpAppTypeAttachmentTypes")]
    public partial class MdpAppTypeAttachmentTypes
    {
        public MdpAppTypeAttachmentTypes()
        {
            ReviewColumns = new Collection<MdpAppTypeAttachmentTypeReviewColumn>();
        }
        [Key]
        public Guid AppTypeAttachmentTypeId { get; set; }

        public Guid AppTypeStateId { get; set; }

        public Guid AttachmentTypeId { get; set; }

        [Column("Required")]
        public bool IsRequired { get; set; }

        [Column("Optional")]
        public bool IsOptional { get; set; }

        public int? SortOrder { get; set; }
        public string Description { get; set; }

        public bool NoReviewRequired { get; set; }

        [StringLength(200)]
        public string ReviewTextLine1 { get; set; }

        [StringLength(200)]
        public string ReviewTextLine2 { get; set; }

        [ForeignKey(nameof(AppTypeStateId))]
        //[InverseProperty(nameof(MdpAppTypeStates.AttachmentTypes))]
        public virtual MdpAppTypeStates AppTypeState { get; set; }

        [ForeignKey(nameof(AttachmentTypeId))]
        //[InverseProperty(nameof(MdpAttachmentTypes.AttachmentTypes))]
        public virtual MdpAttachmentTypes AttachmentType { get; set; }

        [NotMapped]
        public bool IsSpecialRequirement { get; set; }

        [NotMapped]
        public string SpecialRequirementComment { get; set; }

        public virtual ICollection<MdpAppTypeAttachmentTypeReviewColumn> ReviewColumns { get; set; }
    }
}
