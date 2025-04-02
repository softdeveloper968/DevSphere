using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyDMVpro.Common;
using MyDMVpro.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyDMVpro.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        public SidebarViewComponent()
        {
        }
        protected string GetUserSID()
        {
            string sid = this.UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value;
            return sid;
        }
        public async Task<IViewComponentResult> InvokeAsync(string filter)
        {
            bool userIsSysAdmin = false;
            bool userIsAuthenticated = (HttpContext.User.Identity.IsAuthenticated);
            bool userIsGroupAdmin = false;
            bool userIsGroupAgent = false;
            bool userIsVendorAdmin = false;
            bool userIsVendorAgent = false;
            UserInfo user = null;

            if (userIsAuthenticated)
            {
                user = UserInfo.GetCurrentUser(this, true);
                userIsSysAdmin = user.IsSysAdmin;
                userIsGroupAdmin = user.IsGroupAdmin;
                userIsGroupAgent = user.GroupId != null;
                userIsVendorAdmin = user.IsVendorAdmin;
                userIsVendorAgent = user.IsVendorAgent;
            }
            //you can do the access rights checking here by using session, user, and/or filter parameter
            var sidebars = new List<SidebarMenu>();

            //sidebars.Add(ModuleHelper.AddHeader("MAIN NAVIGATION"));
            sidebars.Add(ModuleHelper.AddModule(ModuleHelper.Module.Home));
            if (userIsGroupAgent)
            {
                sidebars.Add(ModuleHelper.AddTree("My Services"));
                List<SidebarMenu> myServiceslist = new List<SidebarMenu>()
                {
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_MyRequests),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_MyIDR),
                    //ModuleHelper.AddModule(ModuleHelper.Module.LHMyPALC),
                    //ModuleHelper.AddModule(ModuleHelper.Module.LHMyChats), // should we have a "My Active Chats" ?
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_MyUploads),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_RTDT_Active),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_IDR_Active),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_PALC),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_RTDT_Pending),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_Chats),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_RTDT_Completed),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_IDR_Completed),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_Master),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_SLA),
                    ModuleHelper.AddModule(ModuleHelper.Module.FormFill),
                    ModuleHelper.AddModule(ModuleHelper.Module.LH_FileLibrary),
                    ModuleHelper.AddModule(ModuleHelper.Module.Communication),
                };
                if (user.IsGroupAdmin || await DataHelpers.CheckFeaturePermission_Read(FeatureKey.CLIENTBILLING, user.UserId, throwIfNoPerm: false))
                {
                    myServiceslist.Add(ModuleHelper.AddModule(ModuleHelper.Module.Billing));
                }
                sidebars.Last().TreeChild = myServiceslist;
            }

            if (userIsVendorAgent)
            {
                sidebars.Add(ModuleHelper.AddTree("To Do"));
                {
                    List<SidebarMenu> list = new List<SidebarMenu>()
                    {
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorToDo_RTDT),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorToDo_WVClearinghouse),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorToDo_LC),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorToDo_LI),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorToDo_TC),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorToDo_Other),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorHolds),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorCancelled),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorFollowUps),
                        ModuleHelper.AddModule(ModuleHelper.Module.FormAnalyzer),
                        ModuleHelper.AddModule(ModuleHelper.Module.AttachmentReview),
                        ModuleHelper.AddModule(ModuleHelper.Module.DataReview),
                        ModuleHelper.AddModule(ModuleHelper.Module.Audit_todo),
                        ModuleHelper.AddModule(ModuleHelper.Module.ManualQueue),
                        ModuleHelper.AddModule(ModuleHelper.Module.NeedToProcess),
                        ModuleHelper.AddModule(ModuleHelper.Module.PaymentReporting),
                        ModuleHelper.AddModule(ModuleHelper.Module.DocumentsReceived_NoRequest),

                    };
                    sidebars.Last().TreeChild = list;
                }

                sidebars.Add(ModuleHelper.AddTree("Vendor"));
                {
                    List<SidebarMenu> list = new List<SidebarMenu>()
                    {
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorChats),
                        //ModuleHelper.AddModule(ModuleHelper.Module.VendorAbstracts),
                        ModuleHelper.AddModule(ModuleHelper.Module.Vendor_Reports),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorMaster),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorCompleted),
                        //ModuleHelper.AddModule(ModuleHelper.Module.ManageForms),
                        ModuleHelper.AddModule(ModuleHelper.Module.VendorUploadRequests),
                        ModuleHelper.AddModule(ModuleHelper.Module.FormFill),
                        //TBD ModuleHelper.AddModule(ModuleHelper.Module.VendorUploadRequestsMNET),
                    };
                    if (userIsVendorAdmin)
                    {
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.VendorSLA));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.VendorInvoicing));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.VendorDisbursements));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.VendorBillingInfo));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.VendorFollowUpCodes));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.VendorRequestCodes));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.ManageVendorUsers));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.GroupProfiles));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.GroupProfileData));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.ManageVendorContacts));
                        list.Add(ModuleHelper.AddModule(ModuleHelper.Module.ManageVendorOrganizations));
                        //list.Add(ModuleHelper.AddModule(ModuleHelper.Module.GroupAppFees));
                    }
                    sidebars.Last().TreeChild = list;
                }
            }


            if(userIsVendorAdmin)
            {
                sidebars.Add(ModuleHelper.AddTree("Tools", "fa fa-calculator"));
                {
                    List<SidebarMenu> list = new List<SidebarMenu>()
                    {
                        ModuleHelper.AddModule(ModuleHelper.Module.Registration_Calculator),
                        ModuleHelper.AddModule(ModuleHelper.Module.TaxRules),
                    };
                    sidebars.Last().TreeChild = list;
                }
            }
          
            sidebars.Add(ModuleHelper.AddModule(ModuleHelper.Module.About));
            sidebars.Add(ModuleHelper.AddModule(ModuleHelper.Module.Contact));

            sidebars.Add(ModuleHelper.AddTree("Account"));
            if (userIsAuthenticated)
            {
                List<SidebarMenu> list = new List<SidebarMenu>();
                if (userIsGroupAdmin)
                {
                    list.Add(ModuleHelper.AddModule(ModuleHelper.Module.ManageGroupUsers));
                }
                if (userIsVendorAdmin || userIsSysAdmin)
                {
                    list.Add(ModuleHelper.AddModule(ModuleHelper.Module.ManageLienholders));
                }
                if (userIsSysAdmin)
                {
                    list.Add(ModuleHelper.AddModule(ModuleHelper.Module.ManageVendors));
                }
                list.Add(ModuleHelper.AddModule(ModuleHelper.Module.Logout));
                sidebars.Last().TreeChild = list;
            }
            else
            {
                List<SidebarMenu> list = new List<SidebarMenu>();
                list = new List<SidebarMenu>()
                {
                    ModuleHelper.AddModule(ModuleHelper.Module.Login),
                    //ModuleHelper.AddModule(ModuleHelper.Module.Register, Tuple.Create(1, 1, 1)),
                };
                sidebars.Last().TreeChild = list;
            }
            return View(sidebars);
        }
    }
}
