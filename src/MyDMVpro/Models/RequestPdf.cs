using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestPdf
    {
        public int Id { get; set; }
        public DateTime DateCreated { get; set; }
        public Guid RequestId { get; set; }
        public bool Signed { get; set; }
        public Guid? SignedByUserId { get; set; }
        public byte[] Pdf { get; set; }
        public string TextSignature { get; set; }
        public string TitleOfSigner { get; set; }
        public Guid? VendorId { get; set; }
        public string FormCode { get; set; }

        public Requests Request { get; set; }
        public Users SignedByUser { get; set; }
    }
}
