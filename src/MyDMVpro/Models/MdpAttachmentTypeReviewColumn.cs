using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyDMVpro.Models;

[Table("mdpAttachmentTypeReviewColumn")]
public class MdpAttachmentTypeReviewColumn
{
    public MdpAttachmentTypeReviewColumn()
    {
    }
    [Key]
    [Browsable(false)]
    public virtual Guid Id { get; set; }

    [Browsable(false)]
    public Guid? AttachmentTypeId { get; set; }

    [StringLength(100)]
    public string ReviewText { get; set; }

    [Browsable(false)]
    public Guid? FieldId { get; set; }

    [ForeignKey(nameof(FieldId))]
    public MasterFields MasterField { get; set; }

    [ForeignKey(nameof(AttachmentTypeId))]
    [InverseProperty(nameof(MdpAttachmentTypes.ReviewColumns))]
    public MdpAttachmentTypes MdpAttachmentType { get; set; }
}
