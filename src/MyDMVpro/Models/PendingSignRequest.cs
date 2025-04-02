using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PendingSignRequest
    {
        public int Id { get; set; }
        public Guid BatchId { get; set; }
        public Guid RequestId { get; set; }

        public PendingSignBatch Batch { get; set; }
        public Requests Request { get; set; }
    }
}
