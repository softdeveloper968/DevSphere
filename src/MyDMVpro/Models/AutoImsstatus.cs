using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AutoImsstatus
    {
        public Guid RequestId { get; set; }
        public string AutoImsstatus1 { get; set; }
        public string AutoImsError { get; set; }

        public Requests Request { get; set; }
    }
}
