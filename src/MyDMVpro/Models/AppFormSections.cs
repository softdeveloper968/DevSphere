using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    /// <summary>
    /// This class was for backward compatibility
    /// And no longer maps to an actual table
    /// The new tables use to store application definitions
    /// are prefixed with mdp (mdpAppType, mdpAppSections, mdpAppTypeState, etc...)
    /// </summary>
    public partial class AppFormSections
    {
        public AppFormSections()
        {
            AppFormSectionFields = new HashSet<AppFormSectionFields>();
        }
        // SectionIdGuid is for adding the SectionId from mdpAppSections
        public Guid? SectionIdGuid { get; set; }
        // SectionId is no longer used, and will be zero
        public int SectionId { get; set; }
        public string SectionTitle { get; set; }
        public string AppFormType { get; set; }
        public int? SectionSortOrder { get; set; }
        public bool? IsPII { get; set; }
        [NotMapped] // Part of new field defs
        public string Description { get; set; }
        [NotMapped] // Part of new field defs
        public string HelpText { get; set; }
        public Guid? GroupProfileCategoryID { get; set; }
        public string GroupProfileCategoryName { get; set; }
        public ApplicationTypes AppFormTypeNavigation { get; set; }
        public ICollection<AppFormSectionFields> AppFormSectionFields { get; set; }
    }
}
