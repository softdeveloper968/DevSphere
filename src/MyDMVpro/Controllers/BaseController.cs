using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    public class BaseController : SharedBaseController
    {
        protected readonly BaseMaggardDMVContext _context;

        protected BaseController(MaggardDMVContext context, IConfiguration configuration, ILogger logger) : base(configuration, logger, context)
        {
            _context = context;
        }

        protected Users LinkCurrentUserToSID(ClaimValues cv)
        {
            string sid = GetUserSID();

            List<string> emails = ClaimsHelper.EmailsFromClaims(this.User);
            Users user = _context.Users.SingleOrDefault(u => emails.Contains(u.UserPrincipalName) && u.NameIdentifierClaim == null);
            // Update with the sid
            try
            {
                if (user == null)
                {
                    // get info from claims
                    string userPrincipalName = GetUser_Email();
                    string displayName = GetUser_Name();
                    // create new user
                    user = new Users()
                    {
                        UserId = Guid.NewGuid(),
                        Active = true,
                        DisplayName = displayName,
                        UserPrincipalName = userPrincipalName,
                        NameIdentifierClaim = sid,
                        ClaimIdp = cv["idp"],
                        ClaimIss = cv["iss"],
                        ClaimSub = cv["sub"],
                        ClaimOid = cv["oid"]
                    };
                    _context.Users.Add(user);
                }
                else
                {
                    user.NameIdentifierClaim = sid;
                    user.ClaimIdp = cv["idp"];
                    user.ClaimIss = cv["iss"];
                    user.ClaimSub = cv["sub"];
                    user.ClaimOid = cv["oid"];
                }
                if (user != null)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
            }
            return user;
        }
        protected bool IsSysAdmin(Guid? userId)
        {
            if (userId == null) return false;
            SysAdmins admin = _context.SysAdmins.AsNoTracking().Where(sa => sa.UserId == userId).FirstOrDefault();
            return (admin != null);
        }
        protected bool IsSysAdmin()
        {
            UserInfo ui = GetCurrentUser();
            return IsSysAdmin(ui.UserId);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        internal void AddBreadcrumb(string displayName, string urlPath)
        {
            List<Message> messages;

            if (ViewBag.Breadcrumb == null)
            {
                messages = new List<Message>();
            }
            else
            {
                messages = ViewBag.Breadcrumb as List<Message>;
            }

            messages.Add(new Message { DisplayName = displayName, URLPath = urlPath });
            ViewBag.Breadcrumb = messages;
        }

        internal void AddPageHeader(string pageHeader = "", string pageDescription = "")
        {
            ViewBag.PageHeader = Tuple.Create(pageHeader, pageDescription);
        }

        internal enum PageAlertType
        {
            Error,
            Info,
            Warning,
            Success
        }

        internal void AddPageAlerts(PageAlertType pageAlertType, string description)
        {
            List<Message> messages;

            if (ViewBag.PageAlerts == null)
            {
                messages = new List<Message>();
            }
            else
            {
                messages = ViewBag.PageAlerts as List<Message>;
            }

            messages.Add(new Message { Type = pageAlertType.ToString().ToLower(), ShortDesc = description });
            ViewBag.PageAlerts = messages;
        }

        protected bool UserIsMemberOfGroupOrVendor(Users user, Guid? groupId, Guid? vendorId)
        {
            if (user.Active)
            {
                if (user.UserGroups != null)
                {
                    if (user.UserGroups.Any(ug => ug.GroupId == groupId))
                    {
                        return true;
                    }
                }
                if (user.VendorAgent != null)
                {
                    if (user.VendorAgent.Any(va => va.VendorId == vendorId))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected async Task<bool> UserIsMemberOfGroupOrVendorAsync(Users user, Guid? groupId, Guid? vendorId)
        {
            if (user.Active)
            {
                if (user.UserGroups != null)
                {
                    if (user.UserGroups.Any(ug => ug.GroupId == groupId))
                    {
                        return true;
                    }
                }
                if (user.VendorAgent != null)
                {
                    if (user.VendorAgent.Any(va => va.VendorId == vendorId))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected bool UserHasRequestPermission(UserInfo user, Guid? requestId)
        {
            if (requestId == null)
                return false;
            // TODO: check group and vendor guid for requestid
            var request = _context.Requests.FirstOrDefaultAsync(r => r.RequestId == requestId).Result;
            if (request == null)
                return false;
            return UserIsMemberOfGroupOrVendor(user, request.GroupId, request.VendorId);
        }

        protected async Task<bool> UserHasRequestPermissionAsync(UserInfo user, Guid? requestId)
        {
            if (requestId == null)
                return false;
            // TODO: check group and vendor guid for requestid
            var request = await _context.Requests.FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (request == null)
                return false;
            return UserIsMemberOfGroupOrVendor(user, request.GroupId, request.VendorId);
        }

        //protected bool CurrentUserHasPermission(Guid? requestId)
        //{
        //    if (requestId == null)
        //        return false;
        //    UserInfo user = GetCurrentUser();
        //    return UserHasRequestPermission(user, requestId.Value);
        //}
        protected async Task<bool> CurrentUserHasPermissionAsync(Guid? requestId)
        {
            if (requestId == null)
                return false;
            UserInfo user = await GetCurrentUserAsync();
            return await UserHasRequestPermissionAsync(user, requestId.Value);
        }

        protected bool IsVendorAdmin(Guid? vendorId, Guid? agentId)
        {
            if (vendorId == null || agentId == null) return false;
            return _context.VendorAgent.Any(va => va.VendorId == vendorId && va.AgentId == agentId && va.IsVendorAdmin == true);
        }

        public async Task<ViewDefinitionCollection> GetVendorViewDefinitions(string viewname)
        {
            return await GetVendorViewDefinitions(new string[] { viewname });
        }

        public async Task<ViewDefinitionCollection> GetVendorViewDefinitions(string[] viewnames)
        {
            UserInfo ui = GetCurrentUser(false);
            ViewDefinitionCollection viewdefs = new ViewDefinitionCollection(_context);
            foreach (string viewname in viewnames)
            {
                viewdefs.Add(viewname, await GetViewDefinition(viewname, ui));
            }
            return viewdefs;
        }

        public async Task<ViewDefinition> GetViewDefinition(string viewname, UserInfo ui)
        {
            ViewDefinition viewdef = new ViewDefinition();
            viewdef.Columns = await GetViewColumns(viewname, ui);
            viewdef.Buttons = new DatatableButtonCollection();
            return viewdef;
        }

        public async Task<DatatableColumnCollection> GetViewColumns(string viewname, UserInfo ui)
        {
            Guid? vendorId = null;
            if (ui != null)
            {
                vendorId = ui.VendorId;
            }
            DatatableColumnCollection dcc = new DatatableColumnCollection();
            var v = await _context.Uiviews.Include(vc => vc.UiviewColumns)
                                            .Where(x => x.ViewName == viewname)
                                            .Where(x => x.VendorId == null || x.VendorId == vendorId)
                                            .FirstOrDefaultAsync();
            if (v != null)
            {
                var vcs = await _context.UiviewColumns.Include(x => x.Column)
                                                        .Where(x => x.ViewId == v.ViewId)
                                                        .Where(x => x.Order.HasValue)
                                                        .OrderBy(x => x.Order)
                                                        .ToListAsync();
                foreach (var vc in vcs)
                {
                    dcc.Add(vc.Column.ColumnTitle, vc.Column.Code, hoverText: null, filterField: null, filterClass: vc.Column.FilterClassName);
                }
            }
            return dcc;
        }

        private string AppendHelperClasses(string colref)
        {
            // TODO: move config to database
            colref = colref.ToLower();
            switch (colref)
            {
                case "reqnotes": return FilterType.ASearch;
                case "reqtype": return FilterType.DropdownFilter;
                case "reqstate": return FilterType.DropdownFilter;
                case "reqgroup": return FilterType.DropdownFilter;
                case "reqlh": return FilterType.DropdownFilter;
                case "reqsubmitted by": return FilterType.DropdownFilter;
                case "reqmake": return FilterType.DropdownFilter;
                case "reqyear": return FilterType.DropdownFilter;
                case "reqeta": return FilterType.DateFilter;
                case "reqcode": return FilterType.ASearch;
                case "reqauctioneer": return FilterType.DropdownFilter;
                case "reqvin": return "";
                case "reqdatetodmv": return FilterType.DateFilter;
                case "reqtitleissued": return FilterType.DateFilter;
                case "reqodometer": return FilterType.ASearch;
                case "reqatt": return "";
                case "reqautoims": return "";
                case "reqchat": return "";
                case "reqchkbx": return "";
                case "reqclientref": return FilterType.DropdownFilter;
                case "reqdatefromdmv": return FilterType.DateFilter;
                case "reqdatetovendor": return FilterType.DateFilter;
                case "reqdmvtracking": return FilterType.ASearch;
                case "reqdupecheck": return "";
                case "reqlinks": return "";
                case "reqsubmittedby": return FilterType.DropdownFilter;
                case "reqtovendorcourier": return FilterType.DropdownFilter;
                case "reqtovendortracking": return FilterType.ASearch;
                case "shpauction": return FilterType.DropdownFilter;
                case "shpdateshipped": return "drfilter";
                case "shpgroup": return FilterType.DropdownFilter;
                case "shplinks": return FilterType.DropdownFilter;
                case "shpnumbershipped": return "";
                case "shppdf": return FilterType.DropdownFilter;
                case "shpshipmentid": return "";
                case "shptracking": return FilterType.ASearch;
                case "reqreg_lh_name": return FilterType.ASearch;
                case "reqreg_reg_name": return FilterType.ASearch;
                default:
                    return "";
            }
        }
        public async Task<MyDMVpro.Models.AppFormModels.NewAppTypeViewModel> GetModelForNewForm()
        {
            UserInfo user = await GetCurrentUserAsync();

            MyDMVpro.Models.AppFormModels.NewAppTypeViewModel model = new MyDMVpro.Models.AppFormModels.NewAppTypeViewModel();
            if (user.IsVendorAgent)
            {
                model.VendorId = user.VendorId;
                model.Groups = await _context.Groups.Where(g => g.Active == true).OrderBy(g => g.GroupName).ToListAsync();
                model.IsOnBehalfOf = true;
            }
            List<string> supportedStates = null;
            List<ApplicationTypes> supportedAppTypes = null;

            supportedAppTypes = await DataHelpers.GetApplicationTypes(user.VendorId, user.GroupId);
            supportedStates = await DataHelpers.GetVendorStates(user.VendorId, user.GroupId);

            List<USState> states = USState.GetAllStates()
                .Where(s => supportedStates.Contains(s.Abbrev))
                .OrderBy(s => s.FullName)
                .ToList();
            model.SupportedAppTypes = supportedAppTypes.OrderBy(o => o.Description).ToList();

            model.SupportedStates = states.OrderBy(s => s.Abbrev).ToList();

            return model;
        }
        public async Task<List<VinInfo>> GetVinPartialDetails(List<string> vins)
        {
            List<VinInfo> list = new List<VinInfo>();

            var l = new List<string>();
            foreach (var pv in vins)
            {
                string s = pv.Substring(0, 8) + pv.Substring(9, 2);

                var vpdList = await _context.VinPartialDetail.AsNoTracking()
                                                        .Where(v => v.VinPattern == s)
                                                        .Select(v => new { Year = v.Year, Make = v.Make })
                                                        .Distinct()
                                                        .ToListAsync();
                foreach (var vpd in vpdList)
                {
                    VinInfo vi = new VinInfo();
                    vi.Vin = pv;
                    vi.Desc = $"{pv} - {vpd.Year} {vpd.Make}";
                    list.Add(vi);
                }
            }
            return list;
        }

        protected IActionResult ReturnFileWithInlineDisposition(string filename, byte[] data)
        {
            string contentType = FileHelpers.GetContentType(filename);
            System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
            {
                FileName = filename,
                Inline = true
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(data, contentType);
        }
        protected async Task AddChat(Guid requestId, UserInfo user, string chatMessage, bool saveChanges = true)
        {
            await AddChat(requestId, user.UserId.Value, user.IsVendorAgent, chatMessage, saveChanges);
        }
        protected async Task AddChat(Guid requestId, Guid userId, bool isVendorAgent, string chatMessage, bool saveChanges = true)
        {
            Chats chat = new()
            {
                RequestId = requestId,
                UserId = userId,
                Message = chatMessage,
                IsVendor = isVendorAgent
            };
            await _context.AddAsync(chat);
            if (saveChanges)
            {
                await _context.SaveChangesAsync(userId);
            }
        }
        protected async Task AddRequestCode(Guid requestId, string tagName, string note, Guid? userId = null, bool saveChanges = false)
        {
            var tag = await _context.Tag.Where(t => t.TagName == tagName).FirstOrDefaultAsync();
            if (tag == null)
            {
                throw new Exception($"Tag '{tagName}' not found.");
            }
            RequestCode rc = new()
            {
                RequestId = requestId,
                TagId = tag.TagId,
                Note = note
            };
            await _context.AddAsync(rc);
            if (saveChanges)
            {
                if (userId == null)
                {
                    throw new ApplicationException("User ID is required to save changes.");
                }
                await _context.SaveChangesAsync(userId);
            }
        }
        protected async Task AddNoteAndRemark(Guid requestId, UserInfo user, string note, string remark)
        {
            await AddNoteAndRemark(requestId, user.UserId, note, remark);
        }

        protected async Task AddNoteAndRemark(Guid requestId, Guid? userId, string note, string remark)
        {
            string trimmedNote = (note ?? "").Trim();
            string trimmedRemark = (remark ?? "").Trim();

            if (trimmedNote.Length == 0) trimmedNote = null;
            if (trimmedRemark.Length == 0) trimmedRemark = null;

            if (!string.IsNullOrWhiteSpace(trimmedNote) || !string.IsNullOrWhiteSpace(trimmedRemark))
            {
                await _context.RequestNotes.AddAsync(
                    new RequestNotes()
                    {
                        RequestId = requestId,
                        ModifiedBy = userId,
                        Note = trimmedNote,
                        Remark = trimmedRemark,
                        LastUpdated = System.DateTime.UtcNow
                    });
            }
        }

        protected async Task ThrowIfNoAccess(string featureKey, UserInfo? user = null)
        {
            if (user == null)
            {
                user = await GetCurrentUserAsync();
            }
            var hasAccess = await HasFeatureAccess(featureKey, user.UserId, FeaturePermissionTypes.Read);
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You do not have access to this feature.");
            }
        }

        protected async Task<bool> HasFeatureAccess(string featureKey, Guid? userId, FeaturePermissionTypes minimumFeaturePermissions)
        {
            if (minimumFeaturePermissions.HasFlag(FeaturePermissionTypes.Deny))
            {
                throw new InvalidOperationException("FeaturePermissions.Deny is not a valid minimum permission.");
            }
            FeaturePermissionTypes perms = await DataHelpers.GetFeaturePermission(featureKey, userId);
            return perms.HasFlag(minimumFeaturePermissions);
        }

        protected async Task<List<string>> QueueTypes_LH(string lhQueueName)
        {
            List<string> abstractTypes = null;

            abstractTypes = await _context.MdpAppTypes
                                        .Where(at => at.QueueName == lhQueueName)
                                        .Select(a => a.AppType).ToListAsync();

            return abstractTypes;
        }

        protected async Task<List<string>> QueueTypes_Vendor(string vendorQueueName)
        {
            List<string> queueTypes = null;

            queueTypes = await _context.MdpAppTypes
                                        .Where(at => at.VendorQueueName == vendorQueueName)
                                        .Select(a => a.AppType).ToListAsync();

            return queueTypes;
        }
    }

    public class FilterType
    {
        public const string DropdownFilter = "afilter";
        public const string ASearch = "asearch";
        public const string DateFilter = "dfilter";
    }
}