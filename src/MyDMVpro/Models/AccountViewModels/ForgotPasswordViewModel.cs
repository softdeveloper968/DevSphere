using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
