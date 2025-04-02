using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyDMVpro.Models.SharedViewModels;
using MyDMVpro.Common.ViewHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.VendorViewModels
{
    public class BusinessInfo
    {
        [Display(Name = "Business Name")]
        [StringLength(100, MinimumLength = 5)]
        public string BusinessName { get; set; }

        [Display(Name = "Attention")]
        [StringLength(100)]
        public string ATTN { get; set; }

        [Display(Name="Address")]
        [StringLength(100)]
        public string AddrLine1 { get; set; }

        [Display(Name = "")]
        [StringLength(100)]
        public string AddrLine2 { get; set; }

        [StringLength(70)]
        public string City { get; set; }

        [StringLength(2)]
        public string State { get; set; }

        [Display(Name = "Zip Code")]
        [StringLength(10, MinimumLength = 5)]
        [RegularExpression(@"\d{5}(-\d{4})?", ErrorMessage = "Invalid Zip Code")]
        public string Zip { get; set; }

        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }
    public class AdministrationViewModel
    {
        public List<Groups> Groups { get; set; }
        public BusinessInfo Business { get; set; }
        public string Notes { get; set; }
        public IEnumerable<SelectListItem> GetGroups()
        {
            return new SelectList(Groups, "GroupId", "GroupName");
        }
        public AdministrationViewModel()
        {
        }
    }
}
