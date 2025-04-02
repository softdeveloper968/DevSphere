using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestSigning
    {
        public int Id { get; set; }
        public Guid SigningId { get; set; }
        public Guid RequestId { get; set; }

        public Requests Request { get; set; }
        public Signings Signing { get; set; }
    }
}
