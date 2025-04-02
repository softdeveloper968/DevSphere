using System;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.RequestsViewModels
{
    public class PurgeDataViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        public Guid id { get; set; }
    }
}
