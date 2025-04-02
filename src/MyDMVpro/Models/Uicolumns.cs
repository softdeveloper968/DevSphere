using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Uicolumns
    {
        public Uicolumns()
        {
            UiviewColumns = new HashSet<UiviewColumns>();
        }

        public Guid ColumnId { get; set; }
        public string ColumnName { get; set; }
        public string Code { get; set; }
        public string ClassName { get; set; }
        public string ColumnTitle { get; set; }
        public string FilterClassName { get; set; }
        public string Data { get; set; }
        public string EntityName { get; set; }

        public ICollection<UiviewColumns> UiviewColumns { get; set; }
    }
}
