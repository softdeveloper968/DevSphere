using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public partial class ApplicationTypes
    {
        public ApplicationTypes()
        {
            AppFormProcessFields = new HashSet<AppFormProcessFields>();
            AppFormSections = new HashSet<AppFormSections>();
        }

        public string AppType { get; set; }
        [NotMapped]
        public List<string> AppStates { get; set; }
        [NotMapped]
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public string QueueName { get; set; }
        public bool? AutoIMSEnabled { get; set; }
        public string AliasForAppType { get; set; }
        public ICollection<AppFormProcessFields> AppFormProcessFields { get; set; }
        public ICollection<AppFormSections> AppFormSections { get; set; }
    }
}
