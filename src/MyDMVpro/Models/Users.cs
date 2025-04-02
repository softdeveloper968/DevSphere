using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Users
    {
        public Users()
        {
            Chats = new HashSet<Chats>();
            FileUploads = new HashSet<FileUploads>();
            RequestAttachments = new HashSet<RequestAttachments>();
            RequestNotes = new HashSet<RequestNotes>();
            RequestPdf = new HashSet<RequestPdf>();
            Requests = new HashSet<Requests>();
            UserGroups = new HashSet<UserGroups>();
            VendorAgent = new HashSet<VendorAgent>();
        }

        public Guid UserId { get; set; }
        public string UserPrincipalName { get; set; }
        public bool Active { get; set; }
        public string DisplayName { get; set; }
        public bool? IsVendorAgent { get; set; }
        public string NameIdentifierClaim { get; set; }
        public string ClaimSub { get; set; }
        public string ClaimIss { get; set; }
        public string ClaimIdp { get; set; }
        public string ClaimOid { get; set; }
        public string Email { get; set; }
        public string Claims { get; set; }

        public ICollection<Chats> Chats { get; set; }
        public ICollection<FileUploads> FileUploads { get; set; }
        public ICollection<RequestAttachments> RequestAttachments { get; set; }
        public ICollection<RequestNotes> RequestNotes { get; set; }
        public ICollection<RequestPdf> RequestPdf { get; set; }
        public ICollection<Requests> Requests { get; set; }
        public ICollection<UserGroups> UserGroups { get; set; }
        public ICollection<VendorAgent> VendorAgent { get; set; }
    }
}
