using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public partial class MdpAppTypes
    {
        public MdpAppTypes()
        {
            AppTypeStates = new HashSet<MdpAppTypeStates>();
            VendorId = new Guid("C9B2E999-6CBA-4D3F-8F38-39EE7FEE6DB9");
        }

        public Guid AppTypeId { get; set; }

        public Guid VendorId { get; set; }

        public string AppType { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string QueueName { get; set; }

        public string VendorQueueName { get; set; }

        public bool? AutoIMSEnabled { get; set; }

        public string AliasForAppType { get; set; }

        public bool Active { get; set; }

        [StringLength(100)]
        public string QueuePageName { get; set; }

        public string QueuePageUrl { get; set; }

        [ForeignKey(nameof(AppTypeId))]
        public MdpAppTypes AppTypeNavigation { get; set; }

        public virtual ICollection<MdpAppTypeStates> AppTypeStates { get; set; }
    }

    // MdpAppTypesSimple is used for the RequestStatusDbContext
    public partial class MdpAppTypesSimple
    {
        public MdpAppTypesSimple()
        {
            VendorId = new Guid("C9B2E999-6CBA-4D3F-8F38-39EE7FEE6DB9");
        }

        public Guid AppTypeId { get; set; }

        public Guid VendorId { get; set; }

        public string AppType { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string QueueName { get; set; }

        public string VendorQueueName { get; set; }

        public bool? AutoIMSEnabled { get; set; }

        public string AliasForAppType { get; set; }

        public bool Active { get; set; }
    }
}
