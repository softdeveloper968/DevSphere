using MyDMVpro.Models;
using System;

namespace MyDMVpro.Common
{
    /// <summary>
    /// This is where you customize the navigation sidebar
    /// </summary>
    public static class ModuleHelper
    {
        public enum Module
        {
            Home,
            About,
            Audit_todo,
            Contact,
            Error,
            FormAnalyzer,
            AttachmentReview,
            FormFill,
            Login,
            Logout,
            EditProfile,
            Register,
            ManageUsers,
            ManageLienholders,
            ManageGroupUsers,
            ManageVendors,
            ManageVendorUsers,
            ManageGroups,
            ManageVendorContacts,
            ManageVendorOrganizations,
            MyServices,
            ManualQueue,
            LH_SLA,
            LH_Chats,
            LH_RTDT_Pending,
            LH_RTDT_Active,
            LH_MyRequests,
            LH_MyIDR,
            LH_MyPALC,
            LH_PALC,
            LH_IDR_Active,
            LH_RTDT_Completed,
            LH_IDR_Completed,
            LH_ManageUsers,
            LH_MyUploads,
            LH_Master,
            LH_FileLibrary,
            Notifications,
            NeedToProcess,
            Communication,
            PaymentReporting,
            DocumentsReceived_NoRequest,
            #if NOT_USED
                ManageForms,
#endif
            VendorToDo_RTDT,
            VendorToDo_WVClearinghouse,
            VendorToDo_Incoming,
            VendorToDo_Print,
            VendorToDo_SendToDmv,
            VendorToDo_Receiving,
            VendorToDo_ShipToLH,
            VendorMaster,
            VendorCompleted,
            VendorActive,
            VendorHolds,
            VendorAbstracts,
            VendorToDo_LC,
            VendorToDo_LI,
            VendorToDo_TC,
            VendorToDo_Other,
            VendorToDoTree,
            VendorToDo_DeletePending,
            VendorChats,
            VendorManageUsers,
            VendorUploadRequests,
            VendorInvoicing,
            VendorDisbursements,
            VendorInvoicingNew,
            VendorBillingInfo,
            VendorUploadRequestsMNET,
            VendorSLA,
            VendorRequestLookup,
            VendorCancelled,
            VendorFollowUps,
            VendorFollowUpCodes,
            VendorRequestCodes,
            GroupProfiles,
            GroupProfileData,
            GroupAppFees,
            Vendor_Reports,
            DataReview,
            Registration_Calculator,
            TaxRules,
            Billing
        }

        public static SidebarMenu AddHeader(string name)
        {
            return new SidebarMenu
            {
                Type = SidebarMenuType.Header,
                Name = name,
            };
        }

        public static SidebarMenu AddTree(string name, string iconClassName = "fa fa-link")
        {
            return new SidebarMenu
            {
                Type = SidebarMenuType.Tree,
                IsActive = false,
                Name = name,
                IconClassName = iconClassName,
                URLPath = "#",
            };
        }

        public static SidebarMenu AddModule(Module module, Tuple<int, int, int> counter = null)
        {
            if (counter == null)
                counter = Tuple.Create(0, 0, 0);

            switch (module)
            {
                case Module.Home:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Home",
                        IconClassName = "fa fa-link",
                        URLPath = "/",
                        LinkCounter = counter,
                    };
                case Module.Login:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Login",
                        IconClassName = "fa fa-sign-in",
                        URLPath = "/MicrosoftIdentity/Account/Signin",
                        LinkCounter = counter,
                    };
                case Module.Logout:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Logout",
                        IconClassName = "fa fa-sign-in",
                        URLPath = "/MicrosoftIdentity/Account/SignOut",
                        LinkCounter = counter,
                    };
                case Module.EditProfile:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Edit Profile",
                        IconClassName = "fa fa-sign-in",
                        URLPath = "/MicrosoftIdentity/Account/EditProfile",
                        LinkCounter = counter,
                    };
                case Module.Register:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Register",
                        IconClassName = "fa fa-user-plus",
                        URLPath = "/MicrosoftIdentity/Account/Register",
                        LinkCounter = counter,
                    };
                case Module.About:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "About",
                        IconClassName = "fa fa-group",
                        URLPath = "/Home/About",
                        LinkCounter = counter,
                    };
                case Module.Contact:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Contact",
                        IconClassName = "fa fa-phone",
                        URLPath = "/Home/Contact",
                        LinkCounter = counter,
                    };
                case Module.Error:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Error",
                        IconClassName = "fa fa-warning",
                        URLPath = "/Home/Error",
                        LinkCounter = counter,
                    };
                case Module.ManageVendorContacts:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manage Contacts",
                        IconClassName = "fa fa-link",
                        URLPath = "/Contacts",
                        LinkCounter = counter,
                    };
                case Module.ManageVendorOrganizations:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manage Organizations",
                        IconClassName = "fa fa-link",
                        URLPath = "/Organizations",
                        LinkCounter = counter,
                    };
                case Module.ManageUsers:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manage Users",
                        IconClassName = "fa fa-link",
                        URLPath = "/Users",
                        LinkCounter = counter,
                    };
                case Module.ManageGroupUsers:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manage Users",
                        IconClassName = "fa fa-link",
                        URLPath = "/Groups/ManageUsers",
                        LinkCounter = counter,
                    };
                case Module.ManageVendorUsers:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manage Users",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendors/ManageUsers",
                        LinkCounter = counter,
                    };
                case Module.ManageLienholders:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manage Groups",
                        IconClassName = "fa fa-link",
                        URLPath = "/Groups",
                        LinkCounter = counter,
                    };
                case Module.ManageVendors:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manage Vendors",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendors",
                        LinkCounter = counter,
                    };
                case Module.MyServices:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "My Services",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices",
                        LinkCounter = counter,
                    };

                case Module.LH_Chats:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Active Chats",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/Chats",
                        LinkCounter = counter,
                    };
                case Module.LH_RTDT_Pending:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Unsent Requests",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/Pending",
                        LinkCounter = counter,
                    };
                case Module.LH_RTDT_Active:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Active Requests",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/Active",
                        LinkCounter = counter,
                    };
                case Module.LH_MyRequests:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "My Requests",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/MyRequests",
                        LinkCounter = counter,
                    };

                case Module.Communication:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Communication",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/Communication",
                        LinkCounter = counter,
                    };

                case Module.LH_MyIDR:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "My Inquiries, Docs, Redemptions",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/MyIDR",
                        LinkCounter = counter,
                    };
                case Module.LH_PALC:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "PA Continuations",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/PALC",
                        LinkCounter = counter,
                    };
                case Module.LH_IDR_Active:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Active Inquiries, Docs, Redemptions",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/IDRActive",
                        LinkCounter = counter,
                    };
                case Module.LH_Master:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Master List",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/Master",
                        LinkCounter = counter,
                    };
                case Module.LH_RTDT_Completed:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Completed Requests",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/Completed",
                        LinkCounter = counter,
                    };
                case Module.LH_IDR_Completed:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Completed Inquiries, Docs, Redemptions",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/IDRCompleted",
                        LinkCounter = counter,
                    };
                case Module.FormFill:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Form Entry",
                        IconClassName = "fa fa-link",
                        URLPath = "/AppForm",
                        LinkCounter = counter,
                    };

                case Module.LH_MyUploads:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "My Uploads",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/MyUploads",
                        LinkCounter = counter,
                    };
                case Module.VendorUploadRequests:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Upload Requests",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/UploadRequests",
                        LinkCounter = counter,
                    };
                case Module.VendorUploadRequestsMNET:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Upload maggard.net Requests",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/UploadRequestsMNET",
                        LinkCounter = counter,
                    };
                case Module.VendorInvoicing:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Invoicing",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Accounting",
                        LinkCounter = counter,
                    };
                case Module.VendorDisbursements:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Disbursements",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Disbursements",
                        LinkCounter = counter,
                    };
                case Module.LH_SLA:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "SLA",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/SLA",
                        LinkCounter = counter,
                    };
                case Module.LH_FileLibrary:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "File Library",
                        IconClassName = "fa fa-link",
                        URLPath = "/FileLibrary",
                        LinkCounter = counter,
                    };
                case Module.VendorSLA:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "SLA",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/SLA",
                        LinkCounter = counter,
                    };
                case Module.VendorBillingInfo:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Billing Info",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/BillingInfo",
                        LinkCounter = counter,
                    };
                case Module.VendorRequestLookup:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Request Lookup",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/RequestLookup",
                        LinkCounter = counter,
                    };
                case Module.ManageGroups:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manage Groups",
                        IconClassName = "fa fa-link",
                        URLPath = "/Admin/ManageGroups",
                        LinkCounter = counter,
                    };
                case Module.Audit_todo:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Audits",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Audits",
                        LinkCounter = counter,
                    };
                case Module.ManualQueue:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manual Queue",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ManualQueue",
                        LinkCounter = counter,
                    };

                case Module.NeedToProcess:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Need To Process",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/NeedToProcess",
                        LinkCounter = counter,
                    };

#if NOT_USED
                case Module.ManageForms:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Manage Forms",
                        IconClassName = "fa fa-link",
                        URLPath = "/FormTemplates",
                        LinkCounter = counter,
                    };
#endif
                case Module.VendorToDo_RTDT:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Repo/Duplicate Title",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDo",
                        LinkCounter = counter,
                    };

                case Module.VendorToDo_WVClearinghouse:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "WV Clearinghouse",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoWVClearing",
                        LinkCounter = counter,
                    };

                case Module.Vendor_Reports:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Reports",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Reports",
                        LinkCounter = counter,
                    };

                case Module.VendorToDo_Print:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "To Do - Print",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoPrint",
                        LinkCounter = counter,
                    };
                case Module.VendorToDo_Receiving:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "To Do - Receive",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoReceiving",
                        LinkCounter = counter,
                    };
                case Module.VendorToDo_SendToDmv:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "To Do - Send to DMV",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoSendToDmv",
                        LinkCounter = counter,
                    };
                case Module.VendorToDo_ShipToLH:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "To Do - Ship To LH",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoShipToLH",
                        LinkCounter = counter,
                    };
                case Module.VendorToDo_Incoming:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "To Do - Incoming",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoIncoming",
                        LinkCounter = counter,
                    };

                case Module.VendorMaster:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Master List",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Master",
                        LinkCounter = counter,
                    };

                case Module.VendorActive:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Active List",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Active",
                        LinkCounter = counter,
                    };

                case Module.VendorHolds:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Holds",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Holds",
                        LinkCounter = counter,
                    };

                case Module.VendorCancelled:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Cancelled",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Cancelled",
                        LinkCounter = counter,
                    };

                case Module.VendorFollowUps:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Follow-Ups",
                        IconClassName = "fa fa-link",
                        URLPath = "/FollowUp",
                        LinkCounter = counter,
                    };

                case Module.VendorFollowUpCodes:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Follow-Up Codes",
                        IconClassName = "fa fa-link",
                        URLPath = "/FollowUp/FollowUpCodes",
                        LinkCounter = counter,
                    };

                case Module.VendorRequestCodes:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Request Codes",
                        IconClassName = "fa fa-link",
                        URLPath = "/Requests/RequestCodes",
                        LinkCounter = counter,
                    };

                case Module.VendorAbstracts:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Inquiries, Docs, Redemptions",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/IDR",
                        LinkCounter = counter,
                    };

                case Module.VendorToDoTree:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Tree,
                        Name = "ToDos",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoLC",
                        LinkCounter = counter,
                    };

                case Module.VendorToDo_DeletePending:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Pending Deletes",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Deletes",
                        LinkCounter = counter,
                    };

                case Module.VendorToDo_LC:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Lien Continuation",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoLC",
                        LinkCounter = counter,
                    };

                case Module.VendorToDo_LI:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Lien Inquiry",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoLI",
                        LinkCounter = counter,
                    };

                case Module.VendorToDo_TC:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Title Correction",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoTC",
                        LinkCounter = counter,
                    };

                case Module.VendorToDo_Other:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Other",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/ToDoOther",
                        LinkCounter = counter,
                    };

                case Module.VendorChats:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Chats",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Chats",
                        LinkCounter = counter,
                    };

                case Module.VendorCompleted:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Completed List",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/Completed",
                        LinkCounter = counter,
                    };

                case Module.FormAnalyzer:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Form Analyzer",
                        IconClassName = "fa fa-link",
                        URLPath = "/FormAnalyzer",
                        LinkCounter = counter,
                    };

                case Module.AttachmentReview:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Attachment Review",
                        IconClassName = "fa fa-link",
                        URLPath = "/AttachmentReview",
                        LinkCounter = counter,
                    };

                case Module.DataReview:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Data Review",
                        IconClassName = "fa fa-link",
                        URLPath = "/DataReview",
                        LinkCounter = counter,
                    };

                case Module.Registration_Calculator:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Registration Calculator",
                        IconClassName = "fa fa-link",
                        URLPath = "/RegistrationCalculator",
                        LinkCounter = counter,
                    };

                case Module.GroupProfiles:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Group Profiles",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/GroupProfiles",
                        LinkCounter = counter,
                    };

                case Module.GroupProfileData:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Group Profile Data",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/GroupProfileData",
                        LinkCounter = counter,
                    };
                case Module.GroupAppFees:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Group Application Fees",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/GroupAppFees",
                        LinkCounter = counter,
                    };
                case Module.TaxRules:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Tax Rules",
                        IconClassName = "fa fa-calculator",
                        URLPath = "/Tax",
                        LinkCounter = counter,
                    };
                case Module.PaymentReporting:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Payment Reporting Page",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/PaymentReport",
                        LinkCounter = counter,
                    };
                case Module.DocumentsReceived_NoRequest:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Document Received - No Request",
                        IconClassName = "fa fa-link",
                        URLPath = "/Vendor/DocumentsReceived_NoRequest",
                        LinkCounter = counter,
                    };
                case Module.Billing:
                    return new SidebarMenu
                    {
                        Type = SidebarMenuType.Link,
                        Name = "Billing",
                        IconClassName = "fa fa-link",
                        URLPath = "/MyServices/Billing",
                        LinkCounter = counter,
                    };
                default:
                    break;
            }

            return null;
        }
    }
}
