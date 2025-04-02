using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AutoImsqueries
    {
        public long Id { get; set; }
        public DateTime RunDate { get; set; }
        public string Vin { get; set; }
        public string XmlResult { get; set; }
        public Guid? UpdatedRequestId { get; set; }
        public string StatusCode { get; set; }
        public string ErrorResult { get; set; }
        public Guid? AttachmentId { get; set; }
    }
}
