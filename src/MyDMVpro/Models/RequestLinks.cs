using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestLinks
    {
        public Guid RequestId { get; set; }
        public Guid LinkId { get; set; }

        public Requests Request { get; set; }
    }
}
