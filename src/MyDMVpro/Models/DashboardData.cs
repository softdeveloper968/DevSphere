using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class DashboardData
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Title { get; set; }
        public bool SynchronizeTitle { get; set; }
    }
}
