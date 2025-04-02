using System;

namespace MyDMVpro.Models.VendorViewModels
{
    public class xBulkRequestAttachmentsPdfFilesViewModel
    {
        public int Id { get; set; }
        public string Filename { get; set; }
        public string AppType { get; set; }
        public string AppTypeState { get; set; }
        public DateTime DateRequested { get; set; }
        public DateTime? DateCompleted { get; set; }
    }
}
