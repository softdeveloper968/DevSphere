using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.FormsViewModels
{
    public class ViewSignedRequestViewModel
    {
        public Guid RequestId { get; set; }
        public string Signature { get; set; }
        [Display(Name = "Printed name, title")]
        public string PrintedNameAndTitle { get; set; }
        [Display(Name = "Date Signed")]
        public DateTime DateSigned { get; set; }
        [Display(Name = "Signed By")]
        public string SignedBy { get; set; }
    }
    public class ViewSignedBatchViewModel
    {
        public Guid BatchId { get; set; }
        public string Signature { get; set; }
        public bool ManualSignature { get; set; }
        [Display(Name = "Printed name, title")]
        public string PrintedNameAndTitle { get; set; }
        [Display(Name = "Date Signed")]
        public DateTime DateSigned { get; set; }
        [Display(Name = "Signed By")]
        public string SignedBy { get; set; }
    }
}
