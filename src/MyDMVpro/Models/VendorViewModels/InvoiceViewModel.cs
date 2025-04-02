using System.Collections.Generic;

namespace MyDMVpro.Models.VendorViewModels
{
    public class InvoiceViewModel
    {
        public Invoice Invoice { get; set; }
        public List<InvoiceDetail> Details { get; set; }
        public dynamic Settings { get; set; }
        public dynamic VendorSettings { get; set; }
    }

    public class InvoiceListViewModel
    {
        public List<InvoiceViewModel> InvoiceList { get; set; }

        public InvoiceListViewModel()
        {
            InvoiceList = new List<InvoiceViewModel>();
        }
    }
}
