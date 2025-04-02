using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AppFormProcessFields
    {
        public string AppFormType { get; set; }
        public string AppTypeState { get; set; }
        public Guid ProcessFieldId { get; set; }

        public ProcessFields ProcessField { get; set; }
    }
}
