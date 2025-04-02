using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class FormAnalyzerForms
    {
        public int Id { get; set; }
        public Guid FormId { get; set; }
        public Guid? VendorId { get; set; }
        public string FormName { get; set; }
        public bool? ProcessAsTitle { get; set; }
    }
}
