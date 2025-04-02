using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.VendorViewModels
{
    public class VendorFileUploadModel
    {
        public Guid GroupId;
        public string GroupName;
        public Guid UserId;
        public string UserName;
        public bool SubmitDirect;
        [Display(Name = "Sent to Vendor")]
        public DateTime? DateToVendor;

        public byte[] File;

        public List<Groups> Groups;
        public IEnumerable<SelectListItem> GetGroups()
        {
            return new SelectList(Groups, "GroupId", "GroupName");
        }
        public List<Users> Users;
        public IEnumerable<SelectListItem> GetUsers()
        {
            return new SelectList(Users, "UserId", "DisplayName");
        }
    }
}
