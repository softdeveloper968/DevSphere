using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public class CalculationTables
    {
        public int Id { get; set; }
        
        [Column(TypeName = "char(2)")]
        public string State { get; set; }
        public string WorkflowName { get; set; }
        public string Json { get; set; }

        public virtual US_State states { get; set; }
    }
}
