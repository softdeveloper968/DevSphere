using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models.FormsViewModels;
using System.Collections.Generic;

namespace MyDMVpro.Models.VendorViewModels;

public class GroupLibraryModel : BaseListViewModel
{
    public GroupLibraryModel()
    {
        Columns = new DatatableColumnCollection();
        SortColumns = new ColumnSortInfoCollection();
        InitPageLength();
    }
    public LienHolderConfig LienholderInfo;
    public List<string> CourierNames;
    public bool EnablePDF;

    public bool FilterOnCurrentUser;
    public bool EnableFilter = false;
    public bool IncludeCancelledStatus = false;
    public bool EnableKeyNavigation = false;
    public bool EnableKeyShortcuts = false;
    public bool AllowMultilineVin = false;
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

    public void InitPageLength()
    {
        LengthMenu = "[ 10, 25, 50, 100 ]";
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

public enum GroupProfileTypeEnum
{
    None = 0,
    Lienholder,
    Owner
}
