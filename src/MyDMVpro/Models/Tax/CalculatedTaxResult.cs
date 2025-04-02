using System.Collections.Generic;

namespace MyDMVpro.Models.Tax
{
    public class CalculatedTaxResult
    {
        public string TotalTaxableAmount { get; set; }
        public string TotalTaxDue { get; set; }
        public string TotalTaxPaidOtherState { get; set; }
        public string TotalLeftOverTitlingState { get; set; }
        public List<CalculatedTaxItem> CalculatedTaxItems { get; set; } = new();
        public TotalCollectionTaxDetailsDto TotalCollectionTaxDetails { get; set; }
    }
    public class CalculatedTaxItem
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal Value { get; set; }
        public decimal TaxDue { get; set; }
        public decimal TaxableAmount { get; set; }
    }
}
