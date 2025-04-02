using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyDMVpro.Models.SharedViewModels;
using MyDMVpro.Common.ViewHelpers;


namespace MyDMVpro.Models.FormsViewModels;

public class RequestTagViewModel : BaseListViewModel
{
    public string ListId;
    public VendorViewType ViewType;
    public string DataUrl;
    public string TableVarName;
    public bool EnablePDF;
    public bool AllowMove;
    public bool AllowDirectToBilling = true;
    public bool AllowStatusEdit;
    public bool AllowCancel = false;
    public bool AddFilterList = false;
    public bool EnableKeyNavigation = false;
    public bool EnableKeyShortcuts = false;
    private string _keyNavigation = "";
    public string KeyNavigation { get { InitKeys(); return _keyNavigation; } set { _keyNavigation = value; } }
    public bool EnableAdvancedSearch;
    public bool EnableFilter = true;
    public bool AllowAddToInvoice;
    public bool DisableCsvExport;
    public bool BulkVinSearch;
    public int MaxNotesLength = 25;
    public bool DisplayFullNotesAsChildRow;
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
    public bool AllowExport = false;
    public bool UseButtonList = false;
    public bool AllowSort = true;
    public bool AllowGlobalSearch = true;
    public bool DeferLoading = false;
    public bool AllowMultiSelect = true;
    public bool AddLastModified = true;
    public bool AddLastModifiedBy = true;
    public bool AddRequestId = true;
    public bool EnableReportBuilder = true;
    public bool EnablePresets = true;
    public bool ForceVinSearch = false;
    public bool ShowColVisButton = true;
    public string DefaultDateFormat = "MM-DD-YY";
    public bool ColorizeStatus = false;
    public bool SaveState = false;
    public List<Tag> FollowUpCodes = new List<Tag>();

    public void InitPageLength()
    {
        LengthMenu = "[ 10, 25, 50 ]";
        Columns = new DatatableColumnCollection();
        SortColumns = new ColumnSortInfoCollection();
        switch (ViewType)
        {
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
    public RequestTagViewModel(VendorViewType vt)
    {
        this.ViewType = vt;
        InitPageLength();
    }
}
