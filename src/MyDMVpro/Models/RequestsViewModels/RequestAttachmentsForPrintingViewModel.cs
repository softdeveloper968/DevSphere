using System;
using System.Collections.Generic;

namespace MyDMVpro.Models.RequestsViewModels
{
    public class RequestAttachmentsForPrintingViewModel
    {
        public Guid RequestId { get; set; }
        public string State { get; set; }
        public string AppType { get; set; }
        public List<MdpAttachmentTypes> AttachmentTypes { get; set; }
    }
}
