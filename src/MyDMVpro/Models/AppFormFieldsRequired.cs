using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AppFormFieldsRequired
    {
        public string AppType { get; set; }
        public string AppTypeState { get; set; }
        public string ExcelName { get; set; }
        public bool? IsRequired { get; set; }
        public bool? IsVisible { get; set; }
    }
}
