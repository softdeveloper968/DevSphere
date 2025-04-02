using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PermissionPolicyActionPermissionObject
    {
        public int Id { get; set; }
        public int? RoleId { get; set; }
        public string ActionId { get; set; }

        public PermissionPolicyRoleBase Role { get; set; }
    }
}
