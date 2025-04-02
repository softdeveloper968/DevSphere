using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AuditData
    {
        public int Id { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string OperationType { get; set; }
        public string PropertyName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Description { get; set; }
        public int? AuditedObjectId { get; set; }
        public int? OldObjectId { get; set; }
        public int? NewObjectId { get; set; }
        public int? UserObjectId { get; set; }

        public AuditEfcoreWeakReference AuditedObject { get; set; }
        public AuditEfcoreWeakReference NewObject { get; set; }
        public AuditEfcoreWeakReference OldObject { get; set; }
        public AuditEfcoreWeakReference UserObject { get; set; }
    }
}
