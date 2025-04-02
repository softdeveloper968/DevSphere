using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyDMVpro.Controllers;
using MyDMVpro.Common;

namespace MyDMVpro.Models.AppFormModels
{
    public class NewAppTypeViewModel
    {
        public NewAppTypeViewModel()
        {
            AppType = "";// force selection in dialog
            AppTypeState = "";// force selection in dialog
            VendorCode = "MAG";
        }
        public string AppTypeState { get; set; }
        public string AppType { get; set; }
        public string VendorCode { get; set; }
        public Guid? VendorId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? UserId { get; set; }
        public bool IsOnBehalfOf { get; set; }
        public bool ForceVendorMode { get; set; }

        public List<USState> SupportedStates = new List<USState>();
        public List<ApplicationTypes> SupportedAppTypes = new List<ApplicationTypes>();

        public IEnumerable<SelectListItem> GetUSStates(string selected)
        {
            return new SelectList(SupportedStates.OrderBy(s => s.FullName), "Abbrev", "FullName", selected);
        }
        public IEnumerable<SelectListItem> GetAppTypes()
        {
            return new SelectList(SupportedAppTypes, "AppType", "Title");
        }
        public List<Groups> Groups;
        public IEnumerable<SelectListItem> GetGroups()
        {
            return new SelectList(Groups, "GroupId", "GroupName");
        }
    }
}
