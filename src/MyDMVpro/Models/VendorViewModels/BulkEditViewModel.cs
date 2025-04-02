using MyDMVpro.Common.ViewHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MyDMVpro.Models.FormsViewModels
{
    public class BulkEditViewModel
    {
        public string QueueName;
        public string ListId;
        public string DataUrl;
        public string TableVarName;
        public int PageLength;
        public string LengthMenu;
        public DatatableButtonCollection Buttons;
        public DatatableColumnCollection Columns;
        public ColumnSortInfoCollection SortColumns;
        public bool EnableKeyNavigation = false;
        public bool EnableKeyShortcuts = false;
        private string _keyNavigation = "";
        public string KeyNavigation { get { InitKeys(); return _keyNavigation; } set { _keyNavigation = value; } }
        public bool EnableFilter = true;
        public bool EnableBulkEdit = true;
        public bool EnableBulkEditToInProcess = false;
        public bool DisplayFullNotesAsChildRow;
        public bool UserIsVendorAdmin = false;
        public bool AllowRefresh = true;
        public bool UseButtonList = false;
        public bool AllowSort = true;
        public bool AllowGlobalSearch = true;
        public bool EnableAdvancedSearch = true;
        public bool EnableSortButton = true;
        public bool DeferLoading = false;
        public bool AllowMultiSelect = true;
        public bool AddSelectDeselectButtons = true;
        public bool EnableReportBuilder = true;
        public bool EnablePresets = true;
        public bool ForceVinSearch = false;
        public bool ShowColVisButton = true;
        public string DefaultDateFormat = "MM-DD-YY";
        public bool ColorizeStatus = false;
        public bool SaveState = false;
        public bool TableLayoutFixed = false;
        public bool AddLinkIcon = true;
        /// <summary>
        /// PrimaryRequestId is only used for LinkedRequests
        /// </summary>
        public Guid? PrimaryRequestId = null;
        public string PrimaryVIN = null;
        public int? PrimaryRequestNo = null;

        public List<Dictionary<string, string>> ListData = new();
        public List<Dictionary<string, object>> EditorFields = new();
        public List<Dictionary<string, object>> ListColumns = new();

        public void InitPageLength()
        {
            LengthMenu = "[ 10, 25, 50, 75, 100 ]";
            Columns = new DatatableColumnCollection();
            SortColumns = new ColumnSortInfoCollection();

            InitKeys();
        }
        private void InitKeys()
        {
            /*            
            33 // page up (previous page)
			34 // page down (next page)
			35 // end (end of current page)
			36 // home (start of current page)
            38 // Up
            40 // Down
            32 // space
            65 // a
            67 // c
            69 // e
            */
            //KeyNavigation = "keys: { keys: [33,34,35,36,38, 40, 32, 65,67,69 ] },";
            const string c_KeyNavigation = "38,40";
            const string c_KeyShortcuts = "38,40,32,65,67,69";
            if (EnableKeyNavigation && EnableKeyShortcuts)
            {
                KeyNavigation = $"keys: {{ keys: [ {c_KeyNavigation},{c_KeyShortcuts}  ] }},";
            }
            else if (EnableKeyNavigation)
            {
                KeyNavigation = $"keys: {{ keys: [ {c_KeyNavigation} ] }},";
            }
            else if (EnableKeyShortcuts)
            {
                KeyNavigation = $"keys: {{ keys: [ {c_KeyShortcuts} ] }},";
            }
            else
            {
                KeyNavigation = "";
            }
        }

        private BaseMaggardDMVContext _context;

        static List<string> _hiddenFieldList = new List<string>();
        static List<string> _readonlyFieldList = new List<string>();
        private ICollection<InternalAttachment> _internalAttachmentFieldList = null;

        static BulkEditViewModel()
        {
            string[] fields = { "requestid", "reqno" };
            _hiddenFieldList.AddRange(fields);
            _readonlyFieldList.AddRange(fields);
        }

        public BulkEditViewModel(BaseMaggardDMVContext context)
        {
            _context = context;
            InitPageLength();
            InitializeInternalAttachmentFields();
        }

        public void AddInternalAttachmentColumns(List<Dictionary<string, object>> listColumns)
        {
            foreach (var att in _internalAttachmentFieldList)
            {
                var dict = new Dictionary<string, object>();
                dict.Add("data", att.ExcelName);
                dict.Add("title", att.DisplayName);
                dict.Add("className", "editable");
                dict.Add("defaultContent", "");
                dict.Add("orderable", false);
                dict.Add("searchable", false);
                if (att.InternalFromClient)
                {
                    dict.Add("render", $"function (data, type, row, meta) {{ return datatable_render_groupAttachment(data, type, row, meta, '{att.AttachmentTypeId}'); }}");
                }
                else
                {
                    dict.Add("render", $"function (data, type, row, meta) {{ return datatable_render_vendorAttachment(data, type, row, meta, '{att.AttachmentTypeId}'); }}");
                }

                listColumns.Add(dict);
            }
        }

        private void InitializeInternalAttachmentFields()
        {
            var fields = _context.AttachmentTypes
                                    .Where(a => a.InternalFromVendor || a.InternalFromClient)
                                    .Select(a => new InternalAttachment()
                                    {
                                        AttachmentTypeId = a.AttachmentTypeId,
                                        ExcelName = a.ExcelName,
                                        DisplayName = a.Name,
                                        InternalFromClient = a.InternalFromClient
                                    })
                                    .OrderBy(a => a.ExcelName)
                                    .ToList();

            _internalAttachmentFieldList = new ReadOnlyCollection<InternalAttachment>(fields);
        }

        public Type GetFieldType(MasterFields field)
        {
            int? fieldTypeId = field.FormDefaultFieldType?.FieldTypeId;
            if (fieldTypeId == null)
            {
                return typeof(string);
            }
            switch (fieldTypeId)
            {
                case 4://   Past Date
                case 5://   Future Date
                    return typeof(DateTime);
                    break;
                case 10://  Zip Code
                    return typeof(string);
                default:
                    break;
            }
            /*
            fieldTypeId	typeDesc
            1	States DDL
            2	Mileage Brand DDL
            3	Odometer Digits DDL
            6	Police Agency DDL
            7	DMV DDL
            8	Odometer Certification
            9	Damage Disclosure
            10	Zip Code
            11	Phone
            12	Date
            13	VIN
            14	Email
            15	Power Type DDL
            16	Oath
            17	Mileage Certification
            18	VIN(s)
            19	Auction
            20	NameAddressParse
            98	Single-file upload
            99	Multi-file Upload
             */
            return typeof(string);
        }
        public bool IsHidden(string excelName)
        {
            return _hiddenFieldList.Contains(excelName.ToLower());
        }
        public bool IsReadOnly(string excelName)
        {
            return _readonlyFieldList.Contains(excelName.ToLower());
        }
        public ICollection<InternalAttachment> InternalAttachments()
        {
            return _internalAttachmentFieldList;
        }
        public static BulkEditViewModel LoadView(string name)
        {
            /* placeholder to load view definitions from the database */
            return null;
        }
    }
}
