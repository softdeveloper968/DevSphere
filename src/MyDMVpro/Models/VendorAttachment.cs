using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace MyDMVpro.Models;

[Table("VendorAttachment")]
public class VendorAttachment
{
    [Key]
    public Guid VendorAttachmentId { get; set; }

    [Required]
    public Guid? VendorId { get; set; }

    [MaxLength(50)]
    [Required]
    public string DisplayName { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    // RESERVED FOR FUTURE USE
    public Guid? AttachmentTypeId { get; set; }

    public Guid? FileDataID { get; set; }

    public Guid? ModifiedBy { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime? ReviewDate { get; set; }
    public Guid? ReviewBy { get; set; }
    public bool? Approved { get; set; }
    public DateTime? NextReviewDate { get; set; }

    [ForeignKey(nameof(FileDataID))]
    public FileData FileData { get; set; }

    [ForeignKey(nameof(ModifiedBy))]
    public Users ModifiedByUser { get; set; }

    [ForeignKey(nameof(AttachmentTypeId))]
    public MdpAttachmentTypes AttachmentType { get; set; }

    [NotMapped]
    [JsonProperty(PropertyName = "attachmentTypeName")]
    [JsonPropertyName("attachmentTypeName")]
    public string AttachmentTypeName { get; set; }

    [NotMapped]
    [JsonProperty(PropertyName = "fileName")]
    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

    [NotMapped]
    [JsonProperty(PropertyName = "modifiedByName")]
    [JsonPropertyName("modifiedByName")]
    public string ModifiedByName { get; set; }
}
