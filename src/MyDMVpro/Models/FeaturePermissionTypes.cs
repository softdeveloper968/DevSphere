using System;

namespace MyDMVpro.Models;

[Flags]
public enum FeaturePermissionTypes
{
    None = 0,
    Read = 1,
    Write = 2,
    Create = 4,
    Delete = 8,
    Execute = 16,
    Navigate = 32,
    Deny = 64,
    All = Read | Write | Create | Delete | Execute | Navigate
}

