using MyDMVpro.Models.SharedViewModels;
using MyDMVpro.Common.ViewHelpers;

namespace MyDMVpro.Models.FormsViewModels
{
    public class DisbursementViewModel
    {
        public string ListId;
        public VendorViewType ViewType;
        public string DataUrl;
        public string TableVarName;
        public bool EnablePDF;
        public bool AllowVoiding;
        public bool AllowClearing;
        public int PageLength;
        public string LengthMenu;
        public DatatableColumnCollection Columns;
        public ColumnSortInfoCollection SortColumns;
        public bool AllowMove;
        public string KeyNavigation;
        public bool EnableKeyNavigation;
        public bool EnableAdvancedSearch;
        public string DefaultDateFormat = "MM-DD-YY";

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
            Columns = new DatatableColumnCollection();
            SortColumns = new ColumnSortInfoCollection();
            switch (ViewType)
            {
                case VendorViewType.Master:
                    PageLength = 25; // Master page use tends to be for search, returning 500 isn't beneficial
                    break;
                case VendorViewType.UploadPending:
                    PageLength = 500;
                    break;
                default:
                    PageLength = 25;
                    break;
            }
            EnableAdvancedSearch = true;
        }
        public DisbursementViewModel(VendorViewType vt)
        {
            this.ViewType = vt;
            InitPageLength();
        }
    }
}
