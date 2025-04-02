using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class ReportDataV2
    {
        public int Id { get; set; }
        public string DataTypeName { get; set; }
        public bool IsInplaceReport { get; set; }
        public string PredefinedReportTypeName { get; set; }
        public byte[] Content { get; set; }
        public string DisplayName { get; set; }
        public string ParametersObjectTypeName { get; set; }
    }
}
