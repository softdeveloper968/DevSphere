using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class UserSettings
    {
        public Guid UserId { get; set; }
        public string JSettings { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
    }
}
