using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyDMVpro.Models
{
    public partial class ChatStatus
    {
        public ChatStatus()
        {
        }

        public Guid RequestId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? VendorId { get; set; }
        public bool HasNewChat { get; set; }
        public bool HasActiveChat { get; set; }
        public bool WaitingForVendorReply { get; set; }
        public bool WaitingForUserReply { get; set; }
        public DateTime? LastChatUpdate { get; set; }
    }
}

