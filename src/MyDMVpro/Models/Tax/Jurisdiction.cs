using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace MyDMVpro.Models.Tax
{
    public class Jurisdiction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JurisdictionID { get; set; }

        [Column(TypeName = "char(2)")]
        [Required]
        public string StateAbbreviation { get; set; }

        [ForeignKey(nameof(StateAbbreviation))]
        public US_State State { get; set; }

        public string JurisdictionName { get; set; }
    }
}
