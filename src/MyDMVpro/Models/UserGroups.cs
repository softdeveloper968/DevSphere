using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class UserGroups
    {
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public bool IsGroupAdmin { get; set; }

        public Groups Group { get; set; }
        public Users User { get; set; }
    }
}
