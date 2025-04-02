using System;
using System.Collections.Generic;

namespace MyDMVpro.Models.RequestsViewModels;

public class CheckMergeViewModel
{
    public string State { get; set; }
    public string AppType { get; set; }

    public decimal? DMVFee { get; set; }
    public string PayToName { get; set; }

    public List<CheckDetailViewModel> CheckDetails { get; set; }
}

public class CheckDetailViewModel
{
    public Guid RequestId { get; set; }
    public string RequestNo { get; set; }
    public string VIN { get; set; }
    public Decimal? Amount { get; set; }
    public bool Cleared { get; set; }
    public string ExistingCheckNumber { get; set; }
    public string PayToName { get; set; }
}