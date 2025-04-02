using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models;

[Table("Features")]
public class Feature
{
    [Key]
    public Guid FeatureId { get; set; }

    [StringLength(100)]
    public string FeatureName { get; set; }

    [StringLength(20)]
    public string FeatureKey { get; set; }

    public bool VendorOnly { get; set; }

    public bool PermissionsRequired { get; set; }

    public string Description { get; set; }

}
