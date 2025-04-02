using System.Collections.Generic;

namespace MyDMVpro.Models.RegistrationFeeCalculatorViewModels
{
    public class SaveTableDataRequest
    {
        public int TableDataId { get; set; }
        public string TableStateCode { get; set; }
        public List<RuleObjectForJson>? Rows { get; set; }
    }

}
