using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MyDMVpro.Models;

[Table("RequestAttachmentReviewHistory")]
public partial class RequestAttachmentReviewHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid AttachmentId { get; set; }

    public bool Approved { get; set; }

    public DateTime ReviewDate { get; set; }

    public Guid ReviewBy { get; set; }

    public string ReviewNotes { get; set; }

    [ForeignKey(nameof(AttachmentId))]
    [InverseProperty(nameof(RequestAttachments.RequestAttachmentReviewHistories))]
    public RequestAttachments Attachment { get; set; }
}
