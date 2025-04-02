using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models
{
    public partial class ProcessFields
    {
        public ProcessFields()
        {
            //AppFormProcessFields = new HashSet<AppFormProcessFields>();
            MdpAppProcessFields = new HashSet<MdpAppProcessFields>();
        }

        [Key]
        public Guid ProcessFieldId { get; set; }

        [StringLength(50)]
        public string FieldName { get; set; }

        [StringLength(100)]
        public string SqlType { get; set; }

        [StringLength(100)]
        public string DotNetType { get; set; }

        [StringLength(100)]
        public string DisplayName { get; set; }

        [StringLength(50)]
        public string SqlFieldName { get; set; }

        //public ICollection<AppFormProcessFields> AppFormProcessFields { get; set; }
        public ICollection<MdpAppProcessFields> MdpAppProcessFields { get; set; }
    }
}
