using System;
using System.Collections.Generic;

namespace MyDMVpro.Models.AttachmentReviewModels;

public class AttachmentReviewFieldsModel
{
    public Guid AttachmentTypeId { get; set; }
    public Guid RequestId { get; set; }
    public MdpAttachmentTypes AttachmentType { get; set; }
    public ICollection<MdpAttachmentTypeReviewColumn> AttachmentTypeReviewColumns { get; set; }
    public MdpAppTypeAttachmentTypes AppTypeAttachmentType { get; set; }
    public ICollection<MdpAppTypeAttachmentTypeReviewColumn> AppTypeAttachmentTypeReviewColumns { get; set; }
    public string JRequest { get; set; }
}
