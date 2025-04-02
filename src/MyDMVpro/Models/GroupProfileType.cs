using System;
using System.Collections.Generic;
namespace MyDMVpro.Models
{
    public class GroupProfileType
    {
        public GroupProfileType()
        {
            GroupProfiles = new HashSet<GroupProfile>();
            GroupProfileTypeUsages = new HashSet<GroupProfileTypeUsage>();
        }
        public Guid GroupProfileTypeID { get; set; }
        public string GroupProfileTypeName { get; set; }
        // no need for navigation properties on this type
        public virtual ICollection<GroupProfile> GroupProfiles { get; set; }
        public virtual ICollection<GroupProfileTypeUsage> GroupProfileTypeUsages { get; set; }
    }
}

