using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models
{
    public class LinkRequest_Create_Model
    {
        public Guid RequestId { get; set; }
        public Guid? LinkRequestId { get; set; }
        public int? RequestNo { get; set; }
        public int? LinkRequestNo { get; set; }
    }
    public class UnlinkRequest_Model
    {
        public Guid RequestId { get; set; }
        public Guid LinkRequestId { get; set; }
    }
}
