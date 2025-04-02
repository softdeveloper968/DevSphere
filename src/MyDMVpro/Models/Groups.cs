using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Groups
    {
        public Groups()
        {
            GroupVendors = new HashSet<GroupVendors>();
            Requests = new HashSet<Requests>();
            UserGroups = new HashSet<UserGroups>();
            GroupProfiles = new HashSet<GroupProfile>();
        }

        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public bool Active { get; set; }
        public bool AutoIMSEnabled { get; set; }

        public ICollection<GroupVendors> GroupVendors { get; set; }
        public ICollection<Requests> Requests { get; set; }
        public ICollection<UserGroups> UserGroups { get; set; }
        public ICollection<GroupProfile> GroupProfiles { get; set; }
        public ICollection<DocumentReceived> DocumentReceived { get; set; }
    }
}
