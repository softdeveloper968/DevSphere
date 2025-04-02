using System;

namespace MyDMVpro.Models.RequestsViewModels
{
    public class NotifyClientViewModal
    {
        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public Guid NotifiedUserId { get; set; }
        public string Vin { get; set; }
        public string Note { get; set; }
        public string SubmittedUserName { get; set; }
        public string NotifiedUserName { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public int RequestNumber { get; set; }
        public Guid SubmittedUserId { get; set; }
        public Guid GroupId { get; set; }
        public string AppType { get; set; }
        public string State { get; set; }
    }

    public class RequestDto
    {
        public int Id { get; set; }
        public string AppType { get; set; }
        public string Vin { get; set; }
        public string State { get; set; }
    }

}
