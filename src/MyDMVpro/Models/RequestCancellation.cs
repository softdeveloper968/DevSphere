using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestCancellation
    {
        public Guid RequestId { get; set; }
        public DateTime CancelledDate { get; set; }
        public Guid? CancelledBy { get; set; }
        public string Reason { get; set; }

        public Requests Request { get; set; }
    }
}
