using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AppTypeStates
    {
        public string AppType { get; set; }
        public string AppTypeState { get; set; }
        public string VendorCode { get; set; }
        public bool Active { get; set; }
    }
}
