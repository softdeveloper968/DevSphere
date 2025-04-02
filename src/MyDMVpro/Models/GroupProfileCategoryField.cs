using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models;

public class GroupProfileCategoryField
{
    public GroupProfileCategoryField()
    {

    }
    [Key]
    [Column("id")]
    public Guid? Id { get; set; }

    [Browsable(false)]
    [Column("GroupProfileCategoryID")]
    public Guid GroupProfileCategoryId { get; set; }

    [Browsable(false)]
    public Guid? FieldId { get; set; }

    [ForeignKey(nameof(FieldId))]
    public virtual MasterFields MasterField { get; set; }

    [ForeignKey(nameof(GroupProfileCategoryId))]
    [InverseProperty(nameof(GroupProfileCategory.Fields))]
    public GroupProfileCategory ProfileCategory { get; set; }
}

