using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models
{
    public partial class Vendors
    {
        public Vendors()
        {
            FormTemplates = new HashSet<FormTemplates>();
            GroupVendors = new HashSet<GroupVendors>();
            MasterFields = new HashSet<MasterFields>();
            Requests = new HashSet<Requests>();
            Shipments = new HashSet<Shipments>();
            VendorAgent = new HashSet<VendorAgent>();
            VendorFormFill = new HashSet<VendorFormFill>();
        }

        public Guid VendorId { get; set; }

        [Display(Name = "Vendor Name")]
        public string VendorName { get; set; }

        [Display(Name = "Vendor Code")]
        public string VendorCode { get; set; }

        public bool Active { get; set; }

        [Display(Name = "Next Invoice #")]
        public int? NextInvoiceNum { get; set; }

        [Display(Name = "Next Check #")]
        public int? NextCheckNum { get; set; }

        [Display(Name = "Checks per page")]
        public int? ChecksPerPage { get; set; }

        [Display(Name = "Check Template")]
        [Browsable(false)]
        public Guid? CheckTemplateID{ get; set; }

        [Display(Name = "Next Batch #")]
        public int? NextBatchNum { get; set; }

        public ICollection<FormTemplates> FormTemplates { get; set; }
        public ICollection<GroupVendors> GroupVendors { get; set; }
        public ICollection<MasterFields> MasterFields { get; set; }
        public ICollection<Requests> Requests { get; set; }
        public ICollection<Shipments> Shipments { get; set; }
        public ICollection<VendorAgent> VendorAgent { get; set; }
        public ICollection<VendorFormFill> VendorFormFill { get; set; }
    }
}
