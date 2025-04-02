using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class ProcessStages
    {
        public int ProcessStageId { get; set; }
        public string ProcessStageName { get; set; }
        public int? ProcessOrder { get; set; }
        public bool? IsUserStage { get; set; }
        public bool? DefaultVisible { get; set; }
        public string ProcessStageCode { get; set; }
        public bool? PiiEnabled { get; set; }
        public bool? PiiEnabledVendor { get; set; }
    }
}
