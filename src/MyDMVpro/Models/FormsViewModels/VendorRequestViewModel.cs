using Microsoft.Extensions.Configuration;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyDMVpro.Models.FormsViewModels
{
    public class VendorRequestViewModel : BaseListViewModel
    {


        public VendorViewType ViewType;
        public bool EnablePDF;
        public bool AllowMove;
        public bool AllowDirectToBilling = true;
        public bool AllowStatusEdit;
        public bool AllowCancel = false;
        public bool EnableKeyNavigation = false;
        public bool EnableKeyShortcuts = false;
        private string _keyNavigation = "";
        public string KeyNavigation { get { InitKeys(); return _keyNavigation; } set { _keyNavigation = value; } }
        public bool IncludeArchivedOnCsvDownload = false;
        public bool EnableAdvancedSearch;
        public bool EnableFilter = true;
        public bool EnableExport = true;
        public bool EnableSortButton = true;
        public bool EnableBulkEdit = true;
        public bool EnableBulkEditToInProcess = false;
        public bool EnableExportAll = false;
        public bool IncludeCancelledStatus = false;
        public bool AllowAddToInvoice;
        public bool AddExpandNotesOption = true;
        public bool AddIncludeArchivedOption = true;
        public bool DisableCsvExport;
        public bool EnableUniversalExport = true;
        public bool BulkVinSearch;
        public int MaxNotesLength = 25;
        public bool DisplayFullNotesAsChildRow;
        public bool DisplayRequiredAttachmentsAsChildRow;
        public bool RefreshDropdownFiltersOnDraw = false;
        public string ExcelExportSelector = ":visible:not(.noexport)";
        public string PdfExportSelector = ":visible:not(.noexport)";
        public string CopyExportSelector = ":visible:not(.noexport)";
        public string CsvExportSelector = ":visible:not(.noexport)";
        public string PrintExportSelector = ":visible:not(.noexport)";
        public bool IsLinkedRequestView = false;
        public bool UserIsVendorAdmin = false;
        public bool AllowDelete = false;
        public bool AllowMultilineVin = true;
        public bool AllowAutoIMSRefresh = false;
        public bool AllowRefresh = true;
        public bool AllowReassign = false;
        public bool UseButtonList = false;
        public bool AllowSort = true;
        public bool AllowGlobalSearch = true;
        public bool DeferLoading = false;
        public bool AllowMultiSelect = true;
        public bool AddSelectDeselectButtons = true;
        public bool AddLastModified = true;
        public bool AddLastModifiedBy = true;
        public bool EnableReportBuilder = true;
        public bool EnablePrintAttachments = true;
        public bool EnablePresets = true;
        public bool ForceVinSearch = false;
        public bool ShowColVisButton = true;
        public string DefaultDateFormat = "MM-DD-YY";
        public bool ColorizeStatus = false;
        public bool SaveState = false;
        public bool TableLayoutFixed = false;
        public bool AddLinkIcon = true;
        public bool UpdateAuditsButton = false;
        public bool AddOCRReviewButton = false;
        public bool UpdateNeedToProcessButton = false;
        public bool AddProcessButton = false;
        public bool AddCalculateTax = false;
        public bool AddPdfLink = false;
        public bool EnablePrintInstructionPacket = false;
        /// <summary>
        /// PrimaryRequestId is only used for LinkedRequests
        /// </summary>
        public Guid? PrimaryRequestId = null;
        public string PrimaryVIN = null;
        public int? PrimaryRequestNo = null;
        public string TableClass = "";

        public void InitPageLength()
        {
            LengthMenu = "[ 10, 25, 50, 100, 200, 500, 1000, 2500, 5000 ]";
            Columns = new DatatableColumnCollection();
            SortColumns = new ColumnSortInfoCollection();
            switch (ViewType)
            {
                case VendorViewType.Master:
                    PageLength = 25; // Master page use tends to be for search, returning 500 isn't beneficial
                    break;
                case VendorViewType.UnbilledList:
                    LengthMenu = "[ 10, 25, 37, 50, 55, 100, 200 ]";
                    PageLength = 37;
                    break;
                case VendorViewType.LI_Incoming:
                case VendorViewType.Audits:
                    LengthMenu = "[ 5 ,10, 25, 50, 100, 200, 500, 1000]";
                    PageLength = 10;
                    break;
                case VendorViewType.LI_InProcessing:
                case VendorViewType.LI_NotReadyToProcess:
                case VendorViewType.LI_ReadyToProcess:
                case VendorViewType.LI_ReadyForPacking:
                case VendorViewType.LI_Pending:


                case VendorViewType.LCLI_Pending:

                    //case VendorViewType.LC_Request:
                    //case VendorViewType.LC_NotReadyToProcess:
                    //case VendorViewType.LC_ReadyToProcess:
                    //case VendorViewType.LC_InProcessing:
                    //case VendorViewType.LC_ReadyForPacking:
                    //case VendorViewType.LC_Pending:
#if DEBUG
                    PageLength = 10;
#else
                    PageLength = 25;
#endif
                    break;
                case VendorViewType.UploadPending:
                    PageLength = 500;
                    break;
                default:
#if DEBUG
                    PageLength = 10;
#else
                    PageLength = 25;
#endif
                    break;
            }

            EnableAdvancedSearch = true;
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
        public VendorRequestViewModel(VendorViewType vt) : this(vt, null)
        {
        }
        public VendorRequestViewModel(VendorViewType vt, string pageName)
        {
            this.ViewType = vt;
            this.RefreshDropdownFiltersOnDraw = ConfigurationHelper.Configuration.GetValue<bool>("AppSettings:RefreshDropdownFiltersOnDraw", true);
            this.PageName = pageName;
            InitPageLength();
        }
        public static VendorRequestViewModel LoadView(string name)
        {
            /* placeholder to load view definitions from the database */
            return null;
        }
        public static List<string> GetDefaultDropdowns()
        {
            List<string> dropdowns = new(new string[] {
                    "processStageId",
                    "statusId",
                    "appType",
                    "state",
                    "submittedBy",
                    "RT_LH",
                    "DT_LH_Name",
                    "LienholderName",
                    "groupName"
            });
            return dropdowns;
        }
        public static string GetDefaultDropdownsDelimited()
        {
            return string.Join(',', GetDefaultDropdowns());
        }
    }
    public class CsvColumn
    {
        public string Title { get; set; }
        public string DataName { get; set; }
    }
}
