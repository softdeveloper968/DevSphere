using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MdpAppDefinitions
    {
        public Guid ApplicationId { get; set; }
        public Guid? AppTypeId { get; set; }
        public bool Active { get; set; }
    }
}
