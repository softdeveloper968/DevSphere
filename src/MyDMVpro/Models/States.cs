using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models
{
#if false
// using State.cs instead
    public class States
    {
        [Key]
        public int StateId { get; set; }
        public string StateCode { get; set; }
        public string StateName { get; set; }

        public virtual ICollection<CalculationTables> CalculationTables { get; set; }
    }
#endif
}
