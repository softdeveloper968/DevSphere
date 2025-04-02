using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PermissionPolicyUser
    {
        public PermissionPolicyUser()
        {
            PermissionPolicyRolePermissionPolicyUser = new HashSet<PermissionPolicyRolePermissionPolicyUser>();
            PermissionPolicyUserLoginInfo = new HashSet<PermissionPolicyUserLoginInfo>();
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public bool IsActive { get; set; }
        public bool ChangePasswordOnFirstLogon { get; set; }
        public string StoredPassword { get; set; }
        public string Discriminator { get; set; }

        public ICollection<PermissionPolicyRolePermissionPolicyUser> PermissionPolicyRolePermissionPolicyUser { get; set; }
        public ICollection<PermissionPolicyUserLoginInfo> PermissionPolicyUserLoginInfo { get; set; }
    }
}
