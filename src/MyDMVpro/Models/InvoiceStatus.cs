using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyDMVpro.Models
{
    public partial class InvoiceStatus
    {
        public InvoiceStatus()
        {
        }

        public Guid InvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? InvoiceDatePaid { get; set; }
        public Decimal? InvoiceAmount { get; set; }
        public Decimal? ServiceFees { get; set; }
        public Decimal? DmvFees { get; set; }
        public Decimal? OtherFees { get; set; }
        public string InvoiceNote { get; set; }

        public Guid? GroupId { get; set; }
        public string GroupName { get; set; }
        public string BillToName { get; set; }
        public string CreatedBy { get; set; }
        public Guid VendorId { get; set; }
    }
}
