using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestNotesHistory
    {
        public long HistoryId { get; set; }
        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public string Note { get; set; }
        public string Remark { get; set; }
        public DateTime? LastUpdated { get; set; }
        public byte[] Timestamp { get; set; }
        public Guid? ModifiedBy { get; set; }
    }
}
