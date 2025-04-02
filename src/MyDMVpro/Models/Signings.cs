using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Signings
    {
        public Signings()
        {
            RequestSigning = new HashSet<RequestSigning>();
        }

        public int Id { get; set; }
        public Guid SigningId { get; set; }
        public Guid UserId { get; set; }
        public DateTime DateSigned { get; set; }
        public string Signature { get; set; }

        public ICollection<RequestSigning> RequestSigning { get; set; }
    }
}
