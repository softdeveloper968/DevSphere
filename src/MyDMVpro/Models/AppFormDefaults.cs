using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class AppFormDefaults
    {
        public int Id { get; set; }
        public string AppType { get; set; }
        public string AppTypeState { get; set; }
        public string ExcelName { get; set; }
        public string DefaultValue { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? UserId { get; set; }
    }
}
