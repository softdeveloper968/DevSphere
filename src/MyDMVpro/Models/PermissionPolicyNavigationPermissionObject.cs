using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PermissionPolicyNavigationPermissionObject
    {
        public int Id { get; set; }
        public int? RoleId { get; set; }
        public string ItemPath { get; set; }
        public string TargetTypeFullName { get; set; }
        public int? NavigateState { get; set; }

        public PermissionPolicyRoleBase Role { get; set; }
    }
}
