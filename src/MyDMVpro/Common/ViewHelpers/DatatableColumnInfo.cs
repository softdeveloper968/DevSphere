using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Common.ViewHelpers
{
    public class DatatableColumnInfo
    {
        public string Title { get; set; }
        public string Class { get; set; }
        public string HoverText { get; set; }
        public string FilterField { get; set; }
        public string FilterClass { get; set; }
        public string DefaultFilterValue { get; set; }
        public string DataField { get; set; }
        public bool Hidden { get; set; }
        public bool ReadOnly { get; set; }
        public Type FieldType { get; set; }
        public bool Exportable { get; set; } = true;
        public string HeaderTemplate { get; set; }
        public bool IsCheckbox { get; set; }
        public bool IsCombobox { get; set; }
        public bool IsInputBox { get; set; }

        public string GetHoverTextAttribute()
        {
            if (!string.IsNullOrWhiteSpace(HoverText))
            {
                string encodeText = HoverText.Replace("\"", "&quot;");
                return $@"title=""{encodeText}""";
            }
            return "";
        }
        public DatatableColumnInfo SetTitle(string title)
        {
            Title = title;
            return this;
        }
        public DatatableColumnInfo SetClass(string @class)
        {
            Class = @class;
            return this;
        }
        public DatatableColumnInfo AddClass(string @class)
        {
            string[] classes = (Class ?? "").Split(' ');
            // add @class from the Class string
            @class = string.Join(" ", classes.Union((@class ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)));
            Class = @class;
            return this;
        }
        public DatatableColumnInfo RemoveClass(string @class)
        {
            string[] classes = (Class ?? "").Split(' ');
            // remove @class from the Class string
            @class = string.Join(" ", classes.Except((@class ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)));
            Class = @class;
            return this;
        }
        public DatatableColumnInfo SetFilterClass(string filterClass)
        {
            FilterClass = filterClass;
            return this;
        }
        public DatatableColumnInfo SetFilterField(string filterField)
        {
            FilterField = filterField;
            return this;
        }
        public DatatableColumnInfo SetDataField(string dataField)
        {
            DataField = dataField;
            return this;
        }

        public DatatableColumnInfo SetIsCombobox(bool isCombobox)
        {
            IsCombobox = isCombobox;
            return this;
        }

        public DatatableColumnInfo SetIsInputBox(bool isInputBox)
        {
            IsInputBox = isInputBox;
            return this;
        }

        public DatatableColumnInfo SetExportable(bool exportable = true)
        {
            Exportable = exportable;
            return this;
        }
        public DatatableColumnInfo SetReadOnly(bool readOnly = true)
        {
            ReadOnly = readOnly;
            return this;
        }
        public DatatableColumnInfo SetHidden(bool hidden = true)
        {
            Hidden = hidden;
            return this;
        }
        public DatatableColumnInfo SetFieldType(Type fieldType)
        {
            FieldType = fieldType;
            return this;
        }
        public DatatableColumnInfo SetDefaultFilterValue(string defaultFilterValue)
        {
            DefaultFilterValue = defaultFilterValue;
            return this;
        }
        public DatatableColumnInfo SetHoverText(string hoverText)
        {
            HoverText = hoverText;
            return this;
        }
    }
    public class DatatableButtonInfo
    {
        public string id { get; set; }
    }
}
