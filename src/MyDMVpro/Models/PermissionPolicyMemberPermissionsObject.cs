using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PermissionPolicyMemberPermissionsObject
    {
        public int Id { get; set; }
        public string Members { get; set; }
        public string Criteria { get; set; }
        public int? ReadState { get; set; }
        public int? WriteState { get; set; }
        public int? TypePermissionObjectId { get; set; }

        public PermissionPolicyTypePermissionObject TypePermissionObject { get; set; }
    }
}
