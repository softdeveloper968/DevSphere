using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MaggardNetImportTypeMap
    {
        public int Id { get; set; }
        public int FormTypeId { get; set; }
        public int? FormSubTypeId { get; set; }
        public string AppType { get; set; }
        public string AppTypeState { get; set; }
    }
}
