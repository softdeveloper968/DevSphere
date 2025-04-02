using System;

namespace MyDMVpro.Models;

/// <summary>
/// Used for prepping internal attachments for 
/// building the UI
/// </summary>
public class InternalAttachment
{
    public Guid AttachmentTypeId { get; set; }
    public string ExcelName { get; set; }
    public string DisplayName { get; set; }
    public bool InternalFromClient { get; set; }
    public string Description { get; set; }
    public string AttachmentTypeName { get; set; }

    public Guid? AttachmentId { get; set; }
    /// <summary>
    /// Only valid if AttachmentId is not null
    /// </summary>
    public string ModifiedByName { get; set; }
    /// <summary>
    /// Only valid if AttachmentId is not null
    /// </summary>
    public DateTime? ModifiedByDate { get; set; }
}


