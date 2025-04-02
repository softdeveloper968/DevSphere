using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class FormTemplates
    {
        public int Id { get; set; }
        public string AppType { get; set; }
        public Guid? VendorId { get; set; }
        public string State { get; set; }
        public string FormCode { get; set; }
        public string Description { get; set; }
        public byte[] PdfImage { get; set; }

        public Vendors Vendor { get; set; }
    }
}
