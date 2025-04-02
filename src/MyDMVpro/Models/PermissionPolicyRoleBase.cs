using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PermissionPolicyRoleBase
    {
        public PermissionPolicyRoleBase()
        {
            PermissionPolicyActionPermissionObject = new HashSet<PermissionPolicyActionPermissionObject>();
            PermissionPolicyNavigationPermissionObject = new HashSet<PermissionPolicyNavigationPermissionObject>();
            PermissionPolicyRolePermissionPolicyUser = new HashSet<PermissionPolicyRolePermissionPolicyUser>();
            PermissionPolicyTypePermissionObject = new HashSet<PermissionPolicyTypePermissionObject>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsAdministrative { get; set; }
        public bool CanEditModel { get; set; }
        public int PermissionPolicy { get; set; }
        public bool IsAllowPermissionPriority { get; set; }
        public string Discriminator { get; set; }

        public ICollection<PermissionPolicyActionPermissionObject> PermissionPolicyActionPermissionObject { get; set; }
        public ICollection<PermissionPolicyNavigationPermissionObject> PermissionPolicyNavigationPermissionObject { get; set; }
        public ICollection<PermissionPolicyRolePermissionPolicyUser> PermissionPolicyRolePermissionPolicyUser { get; set; }
        public ICollection<PermissionPolicyTypePermissionObject> PermissionPolicyTypePermissionObject { get; set; }
    }
}
