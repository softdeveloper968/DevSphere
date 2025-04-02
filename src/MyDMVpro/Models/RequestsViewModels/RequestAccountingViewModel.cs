using System;
using System.Collections.Generic;

namespace MyDMVpro.Models.RequestsViewModels;

public class RequestAccountingViewModel
{
    public Requests Request { get; set; }
    public List<RequestDisbursement> Disbursements { get; set; }

    public List<InvoiceDetail> InvoiceDetails { get; set; }
}
