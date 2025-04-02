using System;

namespace MyDMVpro.Models
{
    public class NeedToProcessResponse
    {
        public int? RequestNo { get; set; }
        public string AssignedProcessor { get; set; }
        public string AssignedShipper { get; set; }
        public string NeedToProcessStageName { get; set; }
        public string Assignedby { get; set; }
    }
}
