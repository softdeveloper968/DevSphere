namespace MyDMVpro.Models.VendorViewModels;

public class InvoiceBuilderAddItemsModel
{
    public InvoiceItemFeesModel Primary { get; set; }
    public InvoiceItemFeesModel Secondary { get; set; }
}

public class InvoiceItemFeesModel
{
    public decimal? ServiceFee { get; set; }
    public decimal? DMVFee { get; set; }
    public decimal? OtherFee { get; set; }
    public string OtherFeeDesc { get; set; }
    public decimal? TotalFees { get; set; }
}
