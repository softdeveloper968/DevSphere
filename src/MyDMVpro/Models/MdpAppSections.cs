using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class MdpAppSections
    {
        public MdpAppSections()
        {
            MdpAppSectionFields = new HashSet<MdpAppSectionFields>();
        }

        public Guid SectionId { get; set; }
        public Guid AppTypeStateId { get; set; }
        public string SectionTitle { get; set; }
        public string Description { get; set; }
        public string HelpText { get; set; }
        public int? SectionSortOrder { get; set; }

        public MdpAppTypeStates AppTypeState { get; set; }
        public ICollection<MdpAppSectionFields> MdpAppSectionFields { get; set; }
    }
}
