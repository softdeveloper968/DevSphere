using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyDMVpro.Models;

[Table("mdpAppTypeAttachmentTypeReviewColumn")]
public class MdpAppTypeAttachmentTypeReviewColumn
{
    public MdpAppTypeAttachmentTypeReviewColumn()
    {
    }
    [Key]
    [Browsable(false)]
    public Guid Id { get; set; }

    [Browsable(false)]
    public Guid? AppTypeAttachmentTypeId { get; set; }

    [StringLength(100)]
    public string ReviewText { get; set; }

    [Browsable(false)]
    public Guid? FieldId { get; set; }

    [ForeignKey(nameof(FieldId))]
    public MasterFields MasterField { get; set; }

    [ForeignKey(nameof(AppTypeAttachmentTypeId))]
    [InverseProperty(nameof(MdpAppTypeAttachmentTypes.ReviewColumns))]
    public MdpAppTypeAttachmentTypes MdpAppTypeAttachmentType { get; set; }
}
