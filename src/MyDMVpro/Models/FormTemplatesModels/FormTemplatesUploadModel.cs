using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models.FormTemplatesModels
{
    public class FormTemplatesUploadModel
    {
        public int Id { get; set; }
        public string State { get; set; }
        public string FormCode { get; set; }
        public string Description { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile PdfImage { get; set; }
        public Guid? VendorId { get; set; }

        public Vendors Vendor { get; set; }
    }
}
