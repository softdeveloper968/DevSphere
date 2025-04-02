using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestNotes
    {
        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public string Note { get; set; }
        public string Remark { get; set; }
        public string ClientRemarks { get; set; }
        public DateTime? LastUpdated { get; set; }
        public byte[] Timestamp { get; set; }
        public Guid? ModifiedBy { get; set; }
        public Users ModifiedByNavigation { get; set; }
        public Requests Request { get; set; }
    }
}
