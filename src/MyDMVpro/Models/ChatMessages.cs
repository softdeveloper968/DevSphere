using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MyDMVpro.Common;

namespace MyDMVpro.Models
{
    public partial class RequestChatThread
    {
        public Guid RequestId { get; set; }
        public string VIN { get; set; }
        public Guid RequestUserId { get; set; }
        public List<ChatMessages> ChatMessages { get; set; }
        public bool isVendor { get; set; }
    }
    public partial class ChatMessages
    {
        public ChatMessages()
        {
        }

        public Guid RequestId { get; set; }
        public Guid RequestUserId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? VendorId { get; set; }
        public string UserName { get; set; }
        public string GroupName { get; set; }
        public string VendorName { get; set; }
        public DateTime DateSent { get; set; }
        public string VIN { get; set; }
        public string Message { get; set; }
        public bool WaitingForUser { get; set; }
        public string AppType { get; set; }
        public string State { get; set; }
        public int RequestNumber { get; set; }
        public int ProcessStageID { get; set; }
        public string ProcessStageName { get; set; }
        public string URLPath
        {
            get
            {
                return $"/Chats/Open/{RequestId}";
            }
        }
        [NotMapped]
        public string TimeSpan {
            get {
                return DateTimeHelpers.FormatTimespan(DateSent);
            } 
        }
    }

    public partial class CommunicationChatMessages
    {
        public CommunicationChatMessages()
        {
        }

        public Guid RequestId { get; set; }
        public Guid RequestUserId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? VendorId { get; set; }
        public string UserName { get; set; }
        public string GroupName { get; set; }
        public string VendorName { get; set; }
        public DateTime DateSent { get; set; }
        public string VIN { get; set; }
        public string Message { get; set; }
        public bool WaitingForUser { get; set; }
        public string AppType { get; set; }
        public string State { get; set; }
        public int RequestNumber { get; set; }
        public int ProcessStageID { get; set; }
        public string ProcessStageName { get; set; }
        public string URLPath
        {
            get
            {
                return $"/Chats/Open/{RequestId}";
            }
        }
        [NotMapped]
        public string TimeSpan
        {
            get
            {
                return DateTimeHelpers.FormatTimespan(DateSent);
            }
        }
    }
}

