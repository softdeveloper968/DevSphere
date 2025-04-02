using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyDMVpro.Models.VendorViewModels
{
    public class BillingInfoViewModel
    {
        public List<Groups> Groups { get; set; }
        public IEnumerable<SelectListItem> GetGroups()
        {
            return new SelectList(Groups, "GroupId", "GroupName");
        }
    }
}
