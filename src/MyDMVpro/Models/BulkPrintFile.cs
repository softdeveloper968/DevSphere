using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using MyDMVpro.Common;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    [Table("BulkPrintFile")]
    public partial class BulkPrintFile
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(200)]
        public string Filename { get; set; }
        public int? BatchNumber { get; set; }

        public DateTime? BatchTimestamp { get; set; }

        [MaxLength(5)]
        public string AppType { get; set; }

        [MaxLength(2)]
        public string AppTypeState { get; set; }

        public Guid? UploadedBy { get; set; }

        public DateTime DateRequested { get; set; }

        public DateTime? DateCompleted { get; set; }

        public string Metadata { get; set; }

        public int? RequestCount { get; set; }   

        public Guid BulkPrintFileId { get; set; }
        public bool? IsApplication { get; set; }
        public bool? IsCheckMerge { get; set; }

        [NotMapped]
        public string CreatedBy { get; set; }
    }
}

#region Metadata Models
public class MetadataAttachmentInfo
{
    public Guid RequestId { get; set; }
    public Guid? AttachmentId { get; set; }
    public Guid? AttachmentTypeId { get; set; }
    public string? VIN { get; set; }
    public int RequestNo { get; set; }
    public byte[]? FileBytes { get; set; }
}
public class MetadataTemplateInfo
{
    public Guid? TemplateId { get; set; }
    public Guid? AttachmentTypeId { get; set; }
    public string? DisplayName { get; set; }
    public byte[]? FileBytes { get; set; }
}
public class MetadataLetterheadInfo
{
    public Guid? groupId { get; set; }
    public string profileName { get; set; }
    public Guid? letterheadId { get; set; }
}
public class MetadataInfo
{
    public List<Guid> requestIds { get; set; } = new();
    public List<MetadataLetterheadInfo> letterheads { get; set; } = new();
    public string? batchNumber { get; set; }
    public DateTime? batchTimestamp { get; set; }
    public Guid? attachmentTypeId { get; set; }
    public Guid? templateId { get; set; }
    public bool useClientLetterhead { get; set; }
    public string letterheadPages { get; set; }
    public string? attachmentTypeName { get; set; }
    public string? templateName { get; set; }
    public string? appType { get; set; }
    public string? state { get; set; }
    public bool internalFromClient { get; set; }
    public bool internalFromVendor { get; set; }
    public bool hasProtectedPdf { get; set; }
    public List<MetadataAttachmentInfo> attachments { get; set; } = new List<MetadataAttachmentInfo>();
    public List<Dictionary<string,string>> data { get; set; } = new List<Dictionary<string, string>>();
}
public class CaseInsensitiveDictionary : Dictionary<string, object>
{
    [System.Text.Json.Serialization.JsonConstructor]
    public CaseInsensitiveDictionary() : base(StringComparer.OrdinalIgnoreCase) { }
}
#endregion
