using System;
namespace MyDMVpro.Models
{
    public class GroupProfileTypeUsage
    {
        public Guid GroupProfileTypeUsageID { get; set; }
        public Guid GroupProfileTypeID { get; set; }
        public string AppType { get; set; }
        public string AppTypeState { get; set; }
        public bool Include { get; set; }
        public bool Exclude { get; set; }
        public virtual GroupProfileType GroupProfileType { get; set; }
    }
}

