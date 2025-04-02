using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PermissionPolicyRolePermissionPolicyUser
    {
        public int RolesId { get; set; }
        public int UsersId { get; set; }

        public PermissionPolicyRoleBase Roles { get; set; }
        public PermissionPolicyUser Users { get; set; }
    }
}
