using System;
using System.Collections.Generic;

namespace MyDMVpro.Common
{
    public class BulkEditProfile
    {
        public Guid BulkEditProfileId { get; set; }
        public string BulkEditProfileName { get; set; } 
        public string AppType { get; set; }
        public string AppState { get; set; }
        public bool Active { get; set; }
        public List<BulkEditProfileField> Fields { get; set; }
    }
    public class BulkEditProfileField
    {
        public string ExcelName { get; set; }
        public string Type { get; set; }
    }
}
