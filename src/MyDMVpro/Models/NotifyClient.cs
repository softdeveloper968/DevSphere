using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public class NotifyClient
    {
        public int Id { get; set; }

        public Guid RequestId { get; set; }

        public Guid NotifiedUserId { get; set; }

        public string Vin { get; set; }

        public string Note { get; set; }
        public string SubmittedUserName { get; set; }
        public string NotifiedUserName { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int RequestNumber { get; set; }

        public Guid SubmittedUserId { get; set; }
        
        public Requests Request { get; set; }


    }

    public class NotifyClientThread
    {
        public List<NotifyClient> NotifyClient { get; set; }
        public bool IsVendorAgent { get; set; }
    }
}
