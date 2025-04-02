using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PermissionPolicyUserLoginInfo
    {
        public int Id { get; set; }
        public string LoginProviderName { get; set; }
        public string ProviderUserKey { get; set; }
        public int UserForeignKey { get; set; }

        public PermissionPolicyUser UserForeignKeyNavigation { get; set; }
    }
}
