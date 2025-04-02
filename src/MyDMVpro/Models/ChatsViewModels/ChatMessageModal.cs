using System;

namespace MyDMVpro.Models.ChatsViewModels
{
    public class ChatMessageModel
    {
        public Guid RequestId { get; set; }
        public Guid RequestUserId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? VendorId { get; set; }
        public string UserName { get; set; }
        public string RequestUserName { get; set; }
        public string GroupName { get; set; }
        public string VendorName { get; set; }
        public DateTime DateSent { get; set; }
        public string VIN { get; set; }
        public string Message { get; set; }
        public bool WaitingForUser { get; set; }
        public RequestDetailsModel RequestDetails { get; set; }
    }

    public class RequestDetailsModel
    {
        public Guid Id { get; set; }
        public string State { get; set; }
        public string AppType { get; set; }
        public string ProcessStageName { get; set; }
        public int RequestNumber { get; set; }

    }

    public class AllUnreadChatMessageModel
    {
        public int RequestId { get; set; }
        public int RequestUserId { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public int VendorId { get; set; }
        public string UserName { get; set; }
        public string GroupName { get; set; }
        public string VendorName { get; set; }
        public DateTime DateSent { get; set; }
        public string VIN { get; set; }
        public string Message { get; set; }
        public bool WaitingForUser { get; set; }
        public string RequestUserName { get; set; }
        public string State { get; set; }
        public string AppType { get; set; }
        public string ProcessStageName { get; set; }
        public int RequestNumber { get; set; }
    }
}
