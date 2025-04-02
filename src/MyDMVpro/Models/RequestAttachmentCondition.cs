using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models;

[Table("RequestAttachmentCondition")]
public class RequestAttachmentCondition
{
    public RequestAttachmentCondition()
    {
    }

    [Key]
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }
    public Guid AttachmentTypeId { get; set; }
    public string AttachmentComment { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    [ForeignKey(nameof(AttachmentTypeId))]
    public virtual MdpAttachmentTypes AttachmentType { get; set; }
}