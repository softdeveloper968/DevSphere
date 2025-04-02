using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Chats
    {
        public int Id { get; set; }
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Created { get; set; }
        public string Message { get; set; }
        public Guid RequestId { get; set; }
        public bool Inactive { get; set; }
        public DateTime? DateClosed { get; set; }
        public bool ReadByVendor { get; set; }
        public bool ReadByUser { get; set; }
        public Guid? ReadBy { get; set; }
        public DateTime? ReadOn { get; set; }
        public bool IsVendor { get; set; }
        public DateTime? Modified { get; set; }
        public Requests Request { get; set; }
        public Users User { get; set; }
    }
}
