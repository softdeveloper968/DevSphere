using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyDMVpro.Models.SharedViewModels;
using MyDMVpro.Common.ViewHelpers;

namespace MyDMVpro.Models.VendorViewModels
{
    public class GroupsViewModel
    {
        public string ListId;
        public string DataUrl;
        public string TableVarName;
        public bool EnablePDF;
        public int PageLength;
        public string LengthMenu;
        public DatatableColumnCollection Columns;
        public ColumnSortInfoCollection SortColumns;
        public bool AllowMove;
        public bool AllowStatusEdit;
        public bool EnableKeyNavigation = false;
        public bool EnableKeyShortcuts = false;
        private string _keyNavigation = "";
        public string KeyNavigation { get { InitKeys(); return _keyNavigation; } set { _keyNavigation = value; } }
        public bool EnableAdvancedSearch;
        public bool AllowAddToInvoice;
        public bool DisableCsvExport;
        public bool BulkVinSearch;
        public int MaxNotesLength = 25;
        public bool DisplayFullNotesAsChildRow;
        public string ExcelExportSelector = ":visible";
        public string PdfExportSelector = ":visible";
        public string CopyExportSelector = ":visible";
        public bool UserIsVendorAdmin = false;
        public bool AllowDelete = false;
        public bool AllowMultilineVin = true;
        public bool UseNoWrap = true;
        public bool AllowMultiSelect = false;
        public bool AllowAutoIMSRefresh = false;
        public bool AllowRefresh = true;
        public bool AllowReassign = false;
        public bool UseButtonList = false;
        public bool AllowSort = true;
        public bool AllowGlobalSearch = true;
        public bool DeferLoading = false;
        public bool AddLastModified = true;
        public bool AddLastModifiedBy = true;
        public bool ForceVinSearch = false;
        public bool ShowColVisButton = true;
        public string DefaultDateFormat = "MM-DD-YY";
        public bool ColorizeStatus = false;
        public bool EnableFilter = true;
        public bool EnableReportBuilder = true;
        public bool RefreshDropdownFiltersOnDraw = true;

        public void InitPageLength()
        {
            LengthMenu = "[ 5, 10, 25, 50 ]";
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
            KeyNavigation = "keys: { keys: [33,34,35,36,38,40 ] },";
            Columns = new DatatableColumnCollection();
            SortColumns = new ColumnSortInfoCollection();
            PageLength = 25; // Master page use tends to be for search, returning 500 isn't beneficial
            EnableAdvancedSearch = true;
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
        public GroupsViewModel()
        {
            InitPageLength();
        }
    }
}
