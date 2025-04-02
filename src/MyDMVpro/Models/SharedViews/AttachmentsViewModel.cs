using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace MyDMVpro.Models.SharedViews
{
    public class AttachmentsViewModel
    {
        public bool IsVendorAgent { get; set; }
        public Guid RequestId { get; set; }
        public int RequestNo { get; set; }
        public List<RequestAttachments> Attachments { get; set; }
        public List<InternalAttachment> InternalAttachments { get; set; } = new();

        public List<MdpAppTypeAttachmentTypes> AppTypeAttachmentTypes { get; set; }
        public List<MdpAttachmentTypes> AttachmentTypes { get; set; }
        public List<MdpAttachmentTypes> MiscAttachmentTypes { get; set; }
        public Requests? Request { get; set; }
        public string AttachmentNotes { get; set; }
        public RequestAttachments InstructionPacket { get; set; }
        public AttachmentsViewModel() { }

        public AttachmentsViewModel(
            bool IsVendorAgent,
            List<RequestAttachments> requestAttachments,
            List<MdpAppTypeAttachmentTypes> appTypeAttachmentTypes,
            List<MdpAttachmentTypes> attachmentTypes,
            string AttachmentNotes)
        {
            this.IsVendorAgent = IsVendorAgent;
            Attachments = requestAttachments;
            AppTypeAttachmentTypes = appTypeAttachmentTypes;
            AttachmentTypes = attachmentTypes;
            this.AttachmentNotes = AttachmentNotes;
        }


        public string GetRequestAttachmentTypesForUpload()
        {
            string attachmentsTypesForUpload = "";

            var otherOption = AttachmentTypes.FirstOrDefault(x => (x?.Name ??"").Equals("Extra"));

            var availableRequestAttachmentTypes = AttachmentTypes.Where(x => !Attachments
                                                                                    .Where(y => y.AttachmentTypeId != null)
                                                                                    .Select(y => y.AttachmentTypeId)
                                                                                    .Contains(x.AttachmentTypeId)
                                                                        )
                                                                    .ToList();

            foreach (var item in availableRequestAttachmentTypes)
            {
                if (otherOption == null || item.AttachmentTypeId != otherOption.AttachmentTypeId)
                {
                    attachmentsTypesForUpload += $"<option value='{item.AttachmentTypeId}'>{WebUtility.HtmlEncode(item.Name)}</option>";
                }
            }

            if (otherOption != null)
            {
                attachmentsTypesForUpload += $"<option value='{otherOption.AttachmentTypeId}' selected='true'>{WebUtility.HtmlEncode(otherOption.Name)}</option>";
            }
            return HttpUtility.HtmlDecode(attachmentsTypesForUpload);
        }
    }
}
