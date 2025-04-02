using System;

namespace MyDMVpro.Models.RequestsViewModels
{
    public class CalculatedFieldsSummaryViewModal
    {
        public Guid RequestId { get; set; }
        public decimal TotalDueToMaggard { get; set; } 
        public decimal TotalPaidToMaggard { get; set; }
        public decimal BalanceDue => TotalDueToMaggard - TotalPaidToMaggard; 
    }

}
