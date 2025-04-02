using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models.VendorViewModels
{
    public class BillToInfoViewModel
    {
        public Guid? VendorId { get; set; }
        public Guid? GroupId { get; set; }
        public dynamic Settings { get; set; }
    }
}
