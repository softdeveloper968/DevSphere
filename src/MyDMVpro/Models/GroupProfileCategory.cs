using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models;

[Table("GroupProfileCategory")]
public class GroupProfileCategory
{
    public GroupProfileCategory()
    {
        Fields = new List<GroupProfileCategoryField>();
    }
    [Key]
    public Guid? GroupProfileCategoryID { get; set; }

    [StringLength(100)]
    [Column("CategoryName")]
    public string GroupProfileCategoryName { get; set; }

    public ICollection<GroupProfileCategoryField> Fields { get; set; }
}

