using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Uiviews
    {
        public Uiviews()
        {
            UiviewColumns = new HashSet<UiviewColumns>();
        }

        public Guid ViewId { get; set; }
        public string ViewName { get; set; }
        public string Title { get; set; }
        public bool? IsVendorView { get; set; }
        public Guid? VendorId { get; set; }

        public ICollection<UiviewColumns> UiviewColumns { get; set; }
    }
}
