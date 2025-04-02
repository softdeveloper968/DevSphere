using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PermissionPolicyTypePermissionObject
    {
        public PermissionPolicyTypePermissionObject()
        {
            PermissionPolicyMemberPermissionsObject = new HashSet<PermissionPolicyMemberPermissionsObject>();
            PermissionPolicyObjectPermissionsObject = new HashSet<PermissionPolicyObjectPermissionsObject>();
        }

        public int Id { get; set; }
        public string TargetTypeFullName { get; set; }
        public int? RoleId { get; set; }
        public int? ReadState { get; set; }
        public int? WriteState { get; set; }
        public int? CreateState { get; set; }
        public int? DeleteState { get; set; }
        public int? NavigateState { get; set; }

        public PermissionPolicyRoleBase Role { get; set; }
        public ICollection<PermissionPolicyMemberPermissionsObject> PermissionPolicyMemberPermissionsObject { get; set; }
        public ICollection<PermissionPolicyObjectPermissionsObject> PermissionPolicyObjectPermissionsObject { get; set; }
    }
}
