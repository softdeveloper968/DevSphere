using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class UiviewColumns
    {
        public Guid ViewId { get; set; }
        public Guid ColumnId { get; set; }
        public int? Order { get; set; }

        public Uicolumns Column { get; set; }
        public Uiviews View { get; set; }
    }
}
