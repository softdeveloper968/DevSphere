using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models;

[Table("FeaturePermissions")]
public class FeaturePermission
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    public Guid FeatureId { get; set; }

    public Guid? UserId { get; set; }
    public Guid? RoleId { get; set; }
    public bool DenyState { get; set; }
    public bool ReadState { get; set; }
    public bool WriteState { get; set; }
    public bool CreateState { get; set; }
    public bool DeleteState { get; set; }
    public bool ExecuteState { get; set; }
    public bool NavigateState { get; set; }

    [ForeignKey(nameof(FeatureId))]
    public virtual Feature Feature { get; set; }
}
