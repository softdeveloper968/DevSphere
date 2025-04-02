using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AuditEfcoreWeakReference
    {
        public AuditEfcoreWeakReference()
        {
            AuditDataAuditedObject = new HashSet<AuditData>();
            AuditDataNewObject = new HashSet<AuditData>();
            AuditDataOldObject = new HashSet<AuditData>();
            AuditDataUserObject = new HashSet<AuditData>();
        }

        public int Id { get; set; }
        public string TypeName { get; set; }
        public string Key { get; set; }
        public string DefaultString { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public ICollection<AuditData> AuditDataAuditedObject { get; set; }
        public ICollection<AuditData> AuditDataNewObject { get; set; }
        public ICollection<AuditData> AuditDataOldObject { get; set; }
        public ICollection<AuditData> AuditDataUserObject { get; set; }
    }
}
