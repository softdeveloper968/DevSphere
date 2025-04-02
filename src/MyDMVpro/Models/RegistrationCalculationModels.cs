using Newtonsoft.Json;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public class RegistrationCalculationModels
    {
        public List<RuleObjectForJson> Data {  get; set; }
        public string StateCode { get; set; }
        public int WorkFlowId { get; set; }
        public string State { get; set; }
        public string WorkflowName {  get; set; }
    }
    public class Rule
    {
        public string RuleName { get; set; }
        public string Expression { get; set; }
        public string SuccessEvent { get; set; }
        public string Description { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Workflow
    {
        public string WorkflowName { get; set; }
        public List<Rule> Rules { get; set; }
    }

    public class RuleObjectForJson
    {
        public int Id { get; set; }
        public string? RuleName { get; set; } = null;

        [JsonProperty("Data Point 1")]
        public string DataPoint1 { get; set; } = null;

        [JsonProperty("Relation 1")]
        public string Relation1 { get; set; } = null;

        [JsonProperty("Submitted Value 1")]
        public string Condition1 { get; set; } = null;

        [JsonProperty("Data Point 2")]
        public string DataPoint2 { get; set; } = null;

        [JsonProperty("Relation 2")]
        public string Relation2 { get; set; } = null;

        [JsonProperty("Submitted Value 2")]
        public string Condition2 { get; set; } = null;

        [JsonProperty("Data Point 3")]
        public string DataPoint3 { get; set; } = null;

        [JsonProperty("Relation 3")]
        public string Relation3 { get; set; } = null;

        [JsonProperty("Submitted Value 3")]
        public string Condition3 { get; set; } = null;

        [JsonProperty("Data Point 4")]
        public string DataPoint4 { get; set; } = null;

        [JsonProperty("Relation 4")]
        public string Relation4 { get; set; } = null;

        [JsonProperty("Submitted Value 4")]
        public string Condition4 { get; set; } = null;

        [JsonProperty("Data Point 5")]
        public string DataPoint5 { get; set; } = null;

        [JsonProperty("Relation 5")]
        public string Relation5 { get; set; } = null;

        [JsonProperty("Submitted Value 5")]
        public string Condition5 { get; set; } = null;

        [JsonProperty("Calculated Value")]
        public string CalculatedValue { get; set; }
        public string? calculatedValueDescription { get; set; } = null;
    }

    
}
