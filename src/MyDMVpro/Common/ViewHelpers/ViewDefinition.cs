using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Common.ViewHelpers
{
    public class ViewDefinitionCollection : Dictionary<string, ViewDefinition>
    {
        public ViewDefinitionCollection(BaseMaggardDMVContext context) : base(StringComparer.InvariantCultureIgnoreCase)
        {
            this.Context = context;
        }
        public BaseMaggardDMVContext Context { get; set; }
    }
    public class ViewDefinition
    {
        public DatatableColumnCollection Columns { get; set; }
        public DatatableButtonCollection Buttons { get; set; }
        public ColumnSortInfoCollection SortColumns { get; set; }

        public ViewDefinition()
        {
            this.Columns = new DatatableColumnCollection();
            this.Buttons = new DatatableButtonCollection();
            this.SortColumns = new ColumnSortInfoCollection();
        }
    }
}
