using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PoliceDept
    {
        public int PoliceDeptId { get; set; }
        public int? CityId { get; set; }
        public string PoliceDeptName { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string County { get; set; }
        public string TypeofDepartment { get; set; }
    }
}
