using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PendingSignBatch
    {
        public PendingSignBatch()
        {
            PendingSignRequest = new HashSet<PendingSignRequest>();
        }

        public int Id { get; set; }
        public Guid BatchId { get; set; }
        public DateTime DateRequested { get; set; }
        public byte[] PdfImage { get; set; }
        public Guid? VendorId { get; set; }
        public string FormCode { get; set; }
        public Guid? GroupId { get; set; }

        public ICollection<PendingSignRequest> PendingSignRequest { get; set; }
    }
}
