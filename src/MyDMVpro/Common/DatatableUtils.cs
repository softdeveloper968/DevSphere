using System;
using System.Collections.Generic;

namespace MyDMVpro.Common.ViewHelpers
{
    public class DatatableSort
    {
        public string columnName { get; set; }
        public int column { get; set; }
        public bool descending { get; set; }
    }
    public class DatatableColumn
    {
        public string name { get; set; }
        public string data { get; set; }
        public bool searchable { get; set; }
        public bool orderable { get; set; }
        public string searchValue { get; set; }
        public bool searchRegex { get; set; }
    }

    public class DatatableFormData
    {
        public DatatableFormData()
        {
            sort = new();
            columns = new();
            FilterList = new();
        }
        public List<DatatableSort> sort { get; set; }
        public List<DatatableColumn> columns { get; set; }
        public int draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public int skip { get; set; }
        public int pageSize { get; set; }
        public string globalSearch { get; set; }
        public bool globalSearchRegex { get; set; }
        public object columnFilters { get; set; }
        public bool downloadCSV { get; set; }
        public string downloadFileName { get; set; }
        public List<string> FilterList { get; set; }
        public bool includeArchived { get; set; }
        public bool includeCleared { get; set; }
        public int? notesLength { get; set; }
        public int? paymentType { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string jsonFilter { get; set; }
        /// <summary>
        /// columns to be returned when downloading as CSV
        /// </summary>
        public string jsonColumns { get; set; }
        public List<string>? stateType { get; set; }
        public List<string>? appType { get; set; }
        public bool? isCredit { get; set; }
        public bool allGroupChats { get; set; }
    }
}