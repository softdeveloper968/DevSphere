using System.Collections.Generic;
using System;

namespace MyDMVpro.Models.SharedViews
{
    public class UploadAttachmentsViewModel
    {
        public Guid RequestId { get; set; }
        public int RequestNo { get; set; }
        public List<RequestAttachments> Attachments { get; set; }
        public List<InternalAttachment> InternalAttachments { get; set; } = new();
        public List<MdpAppTypeAttachmentTypes> AppTypeAttachmentTypes { get; set; }
        public List<MdpAttachmentTypes> AttachmentTypes { get; set; }
        public List<MdpAttachmentTypes> MiscAttachmentTypes { get; set; }
        public Requests? Request { get; set; }
        public string AttachmentNotes { get; set; }
        public string? UploadLinkToken { get; set; }

        public UploadAttachmentsViewModel() { }
    }
}
