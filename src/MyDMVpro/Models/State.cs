using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public class US_State
    {
        [Key]
        [Column(TypeName = "char(2)")]
        public string StateAbbreviation { get; set; } = null!;

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string StateName { get; set; } = null!;

        [Required]
        public int ProcessingDay { get; set; }

        public virtual ICollection<CalculationTables> CalculationTables { get; set; }

    }
}
