using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MdpFieldTypes
    {
        public MdpFieldTypes()
        {
            MasterFields = new HashSet<MasterFields>();
            MdpAppSectionFields = new HashSet<MdpAppSectionFields>();
        }

        public int FieldTypeId { get; set; }
        public string TypeDesc { get; set; }
        public string RegExValidator { get; set; }

        public ICollection<MasterFields> MasterFields { get; set; }
        public ICollection<MdpAppSectionFields> MdpAppSectionFields { get; set; }
    }
}
