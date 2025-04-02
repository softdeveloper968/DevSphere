using System.Collections.Generic;

namespace MyDMVpro.Models.RegistrationFeeCalculatorViewModels
{
    public class RemoveRulesRequest
    {
        public List<string> RuleNamesToRemove { get; set; }
        public string StateCode { get; set; }
        public int WorkflowId { get; set; }
    }
}
