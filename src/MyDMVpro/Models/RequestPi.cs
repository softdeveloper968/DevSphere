using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestPi
    {
        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public string JSecure { get; set; }
    }
}
