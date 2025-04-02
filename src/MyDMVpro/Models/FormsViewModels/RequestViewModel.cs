using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyDMVpro.Models.SharedViewModels;
using MyDMVpro.Common.ViewHelpers;

namespace MyDMVpro.Models.FormsViewModels
{
    public enum MyServicesViewType
    {
        Chats,
        Pending,
        Active,
        Completed,
        LCActive,
        LCCompleted,
        IDRActive,
        IDRCompleted,
        MyIDR,
        Master,
        SLAReport,
        PALC,
        FileLibrary,
        FileLibraryMatches,
        Communication,
        DataNeeded,
        DocumentNeeded,
        DocumentReceived_NoRequest,
        DocumentShipped_NoRequest,
        Missing_Data,
        Communication_Chat,
        ClientNotifications
    };
    public class RequestViewModel
    {
        public RequestViewModel(MyServicesViewType viewType, string pageName = null)
        {
            PageName = pageName;
            LienholderInfo = new LienHolderConfig()
            {
                ManuallySign = true
            };
            CourierNames = new List<string>(new string[] {
                "DHL",
                "FEDEX",
                "UPS",
                "USPS"
            });
            Columns = new DatatableColumnCollection();
            SortColumns = new ColumnSortInfoCollection();
            ViewType = viewType;
            InitPageLength();
        }
        public string PageName;
        public string ListId;
        public MyServicesViewType ViewType;
        public string DataUrl;
        public string TableVarName;
        public LienHolderConfig LienholderInfo;
        public List<string> CourierNames;
        public bool EnablePDF;
        public int PageLength;
        public string LengthMenu;
        public DatatableColumnCollection Columns;
        public ColumnSortInfoCollection SortColumns;
        public bool FilterOnCurrentUser;
        public bool AllowSort = true;
        public bool EnableFilter = false;
        public bool IncludeCancelledStatus = false;
        public bool EnableKeyNavigation = false;
        public bool EnableKeyShortcuts = false;
        public bool AllowMultilineVin = true;
        public bool UseNoWrap = true;
        public bool AllowMultiSelect = false;
        private string _keyNavigation = "";
        public bool EnableReportBuilder = true;
        public bool EnablePresets = false;

        public string KeyNavigation { get { InitKeys(); return _keyNavigation; } set { _keyNavigation = value; } }
        public bool EnableAdvancedSearch;
        public bool DisplayFullNotesAsChildRow;
        public bool DisableCsvExport;
        public string ExcelExportSelector = ":visible:not(.noexport)";
        public string PdfExportSelector = ":visible:not(.noexport)";
        public string CopyExportSelector = ":visible:not(.noexport)";
        public string CsvExportSelector = ":visible:not(.noexport)";
        public string PrintExportSelector = ":visible:not(.noexport)";
        public bool AddLastModified;
        public bool AddLastModifiedBy;
        public bool EnablePrintInstructionPacket = false;

        public void InitPageLength()
        {
            LengthMenu = "[ 10, 25, 50, 100, 200, 500, 1000 ]";

            switch (ViewType)
            {
                case MyServicesViewType.Pending:
                    PageLength = 500;
                    break;
                default:
                    PageLength = 25;
                    break;
            }
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
    }
    public class LienHolderConfig
    {
        public bool ManuallySign { get; set; } = true;
    }

    public class LCRequestViewModel
    {
        public LCRequestViewModel(MyServicesViewType vt)
        {
            this.ViewType = vt;
            LienholderInfo = new LienHolderConfig()
            {
                ManuallySign = true
            };
            CourierNames = new List<string>(new string[] {
                "DHL",
                "FEDEX",
                "UPS",
                "USPS"
            });
            InitPageLength();

            Columns = new DatatableColumnCollection();
            SortColumns = new ColumnSortInfoCollection();

            InitPageLength();
        }
        public string ListId;
        public MyServicesViewType ViewType;
        public string DataUrl;
        public string TableVarName;
        public LienHolderConfig LienholderInfo;
        public List<string> CourierNames;
        public bool EnablePDF;
        public int PageLength;
        public string LengthMenu;
        public DatatableColumnCollection Columns;
        public ColumnSortInfoCollection SortColumns;
        public bool FilterOnCurrentUser;
        public string KeyNavigation;
        public bool EnableKeyNavigation;
        public bool EnableAdvancedSearch;

        public void InitPageLength()
        {
            LengthMenu = "[ 10, 25, 50, 100, 200, 500, 1000 ]";
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
            KeyNavigation = "keys: { keys: [33,34,35,36,38, 40, 32, 65,67,69 ] },";
            switch (ViewType)
            {
                case MyServicesViewType.Pending:
                    PageLength = 500;
                    break;
                default:
                    PageLength = 25;
                    break;
            }
        }
    }

}
