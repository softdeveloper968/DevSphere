using System.Collections.Generic;

namespace MyDMVpro.Common.ViewHelpers
{
    public class ColumnSortInfoCollection : List<ColumnSortInfo>
    {
        public ColumnSortInfoCollection() : base()
        {

        }
        public void Add(string name, bool descending = false)
        {
            ColumnSortInfo csi = new ColumnSortInfo()
            {
                Name = name,
                Descending = descending
            };
            this.Add(csi);
        }
    }
}
