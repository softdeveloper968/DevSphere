using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class GroupSettings
    {
        public Guid GroupId { get; set; }
        public Guid VendorId { get; set; }
        public string SettingsName { get; set; }
        public string JSettings { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public Guid? ModifiedBy { get; set; }
    }
}
