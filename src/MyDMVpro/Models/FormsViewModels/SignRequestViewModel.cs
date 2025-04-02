using System;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.FormsViewModels
{
    public class SignRequestViewModel
    {
        public Guid RequestId { get; set; }
        [Display(Name = "Print and manually sign")]
        public bool ManualSignature { get; set; }
        public string Signature { get; set; }
        [Display(Name = "Printed name, title")]
        public string PrintedNameAndTitle { get; set; }
    }
}
