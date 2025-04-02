using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestTracking
    {
        public Guid RequestId { get; set; }
        public DateTime ChangeDate { get; set; }
        public Guid? ChangedBy { get; set; }
        public string ChangedByName { get; set; }
        public int? StatusId { get; set; }
        public int? ProcessStageId { get; set; }
        public string JRequest_Before { get; set; }
        public string JRequest_After { get; set; }
        public string JRequestDiff { get; set; }
        public string JProcess_Before { get; set; }
        public string JProcess_After { get; set; }
        public string JProcessDiff { get; set; }
        public Guid? AssignedTo { get; set; }
        public string AssignedToName { get;set; }
    }
}
