using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    [Table("mdpAppTypeStates")]
    public partial class MdpAppTypeStates
    {
        public MdpAppTypeStates()
        {
            MdpAppSections = new HashSet<MdpAppSections>();
        }
        [Key]
        public Guid AppTypeStateId { get; set; }

        public Guid AppTypeId { get; set; }
        public string AppState { get; set; }
        public bool Active { get; set; }
        public string AttachmentNotes { get; set; }

        public Guid? InternalHelpID { get; set; }
        public Guid? PublicHelpID { get; set; }

        [ForeignKey(nameof(AppTypeId))]
        public MdpAppTypes AppType { get; set; }

        public ICollection<MdpAppSections> MdpAppSections { get; set; }
    }
}
