using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MdpAppProcessFields
    {
        public Guid AppTypeId { get; set; }
        public Guid ProcessFieldId { get; set; }

        public ProcessFields ProcessField { get; set; }
    }
}
