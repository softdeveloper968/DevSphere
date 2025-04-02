using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyDMVpro.Models
{
    public partial class RequestChats
    {
        public RequestChats()
        {
        }

        public Guid RequestId { get; set; }
        public string Vin { get; set; }
        public DateTime DateSent { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string UserOrg { get; set; }
        public bool isVendor { get; set; }
        public string Message { get; set; }
        public bool ReadByRecipient { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? VendorId { get; set; }
    }
}
