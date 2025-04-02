using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models
{
    public class StateConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Column(TypeName = "char(2)")]
        [Required]
        public string StateAbbreviation { get; set; }

        [ForeignKey(nameof(StateAbbreviation))]
        public US_State State { get; set; }

        public bool EnableTaxRateFetchFromApi { get; set; }
        public bool IncludeStateTax { get; set; }
        public bool IncludeCityTax { get; set; }
        public bool IncludeSpecialDistrictTax { get; set; }
        public bool IncludeCountyTax { get; set; }
    }
}
