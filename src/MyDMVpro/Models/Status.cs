using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Status
    {
        public Status()
        {
            Requests = new HashSet<Requests>();
        }

        public short StatusId { get; set; }
        public string StatusName { get; set; }
        public int? SortOrder { get; set; }

        public ICollection<Requests> Requests { get; set; }
    }
}
