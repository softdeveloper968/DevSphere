using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class UserViews
    {
        public Guid ViewId { get; set; }
        public string ViewName { get; set; }
        public string ViewDefinition { get; set; }
        public string DataSource { get; set; }
    }
}
