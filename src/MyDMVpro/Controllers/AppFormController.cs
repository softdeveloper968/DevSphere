using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Models;
using MyDMVpro.Models.AppFormModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    public class AppFormController : BaseController
    {
        public AppFormController(MaggardDMVContext context, IConfiguration configuration, ILogger<AppFormController> logger) : base(context, configuration, logger)
        {

        }

        public async Task<IActionResult> Index()
        {
            try
            {
                return View(await GetModelForNewForm());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
                return Ok("An error occurred loading the page...");
            }
        }
        public async Task<IActionResult> NewForm()
        {
            try
            {
                return PartialView("Index", await GetModelForNewForm());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
                return Ok("An error occurred loading the page...");
            }
        }
        public async Task<IActionResult> Vendor()
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                List<string> supportedStates = null;
                List<ApplicationTypes> supportedAppTypes = null;

                supportedAppTypes = await DataHelpers.GetApplicationTypes(ui.VendorId, ui.GroupId, null);
                supportedStates = await DataHelpers.GetVendorStates(ui.VendorId, ui.GroupId);

                List<USState> states = USState.GetAllStates()
                    .Where(s => supportedStates.Contains(s.Abbrev))
                    .OrderBy(s => s.FullName)
                    .ToList();

                MyDMVpro.Models.AppFormModels.NewAppTypeViewModel model = new()
                {
                    SupportedStates = states.OrderBy(s => s.FullName).ToList(),
                    SupportedAppTypes = supportedAppTypes.OrderBy(o => o.Title).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
                return Ok("An error occurred loading the page...");
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private List<ApplicationTypes> GetSorted(List<ApplicationTypes> supportedAppTypes)
        {
            return supportedAppTypes.OrderBy(o => o.Description).ToList();
        }
#if false
        public IEnumerable<SelectListItem> GetUSStates(string selected)
        {
            // Get list of states supported across all app types
            List<string> supportedStates = _context.AppTypeStates.Select(s => s.AppTypeState).Distinct().ToList();

            // get USState list for just the supported states
            List<USState> states = USState.GetAllStates().Where(s => supportedStates.Contains(s.Abbrev)).ToList();
            return new SelectList(states, "Abbrev", "FullName", selected);
        }
#endif
        public async Task<IActionResult> FormTypes(string id /* state */)
        {
            var user = GetCurrentUser(false);

            List<string> formTypes = new();
            if (id?.Length == 2)
            {
                var result = await DataHelpers.GetApplicationTypes(user.VendorId, user.GroupId, id);
                formTypes = result.Select(a => a.AppType).ToList();
            }
            return Json(formTypes);
        }
        class AddressParseConfig
        {
            public string regexFormula = null;
            public int nameIndex;
            public int streetIndex;
            public int cityIndex;
            public int stateIndex;
            public int zip5Index;
            public int zip5plus4Index;
        }
        class ParsedAddress
        {
            public string input;
            public string name;
            public string city;
            public string street;
            public string state;
            public string zip5;
            public string zip5plus4;
        }
        [HttpPost]
        public async Task<IActionResult> ParseAddress([FromForm] string address)
        {
            address ??= "";

            // replace multiple whitespace with single space
            address = Regex.Replace(address, @"\s+", " ");

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    AddressParseConfig config = new()
                    {
                        regexFormula = _configuration.GetValue<string>($"AddressParser:{i}:Regex")
                    };
                    if (config.regexFormula != null)
                    {
                        config.nameIndex = _configuration.GetValue<int>($"AddressParser:{i}:NameIndex");
                        config.streetIndex = _configuration.GetValue<int>($"AddressParser:{i}:StreetIndex");
                        config.cityIndex = _configuration.GetValue<int>($"AddressParser:{i}:CityIndex");
                        config.stateIndex = _configuration.GetValue<int>($"AddressParser:{i}:StateIndex");
                        config.zip5Index = _configuration.GetValue<int>($"AddressParser:{i}:Zip5Index");
                        config.zip5plus4Index = _configuration.GetValue<int>($"AddressParser:{i}:Zip5plus4Index");

                        ParsedAddress pa = await Parse(address, config);
                        if (pa != null)
                        {
                            object o = new
                            {
                                Name = pa.name,
                                Street = pa.street,
                                City = pa.city,
                                State = pa.state,
                                Zip5 = pa.zip5,
                                Zip5plus4 = pa.zip5plus4
                            };
                            return JsonSuccess(o);
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.WriteLine(e.ToString());
                }
            }
            return JsonError("Cannot parse address");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private AddressParseConfig GetDefaultParse()
        {
            const string c_regex1 = @"(.*) ((\d{1,6}) (.*)) (.*) ([A-Z][A-Z]) ((\d{5})(-\d{4})?)";
            AddressParseConfig config = new()
            {
                regexFormula = c_regex1,
                nameIndex = 1,
                streetIndex = 2,
                cityIndex = 5,
                stateIndex = 6,
                zip5Index = 8,
                zip5plus4Index = 7
            };
            return config;
        }
        private async Task<ParsedAddress> Parse(string address, AddressParseConfig config)
        {
            ParsedAddress pa = new();
            Regex zipRegex = new(config.regexFormula, RegexOptions.IgnoreCase);

            var matches = zipRegex.Matches(address);
            if (matches.Count == 1)
            {
                var groups = matches[0].Groups;
                if (config.nameIndex != -1) { pa.name = groups[config.nameIndex].Value; }
                if (config.streetIndex != -1) { pa.street = groups[config.streetIndex].Value; }
                if (config.cityIndex != -1) { pa.city = groups[config.cityIndex].Value; }
                if (config.stateIndex != -1) { pa.state = groups[config.stateIndex].Value; }
                if (config.zip5Index != -1) { pa.zip5 = groups[config.zip5Index].Value; }
                if (config.zip5plus4Index != -1) { pa.zip5plus4 = groups[config.zip5plus4Index].Value; }
                if (!string.IsNullOrEmpty(pa.zip5))
                {
                    // lookup zip name
                    var zip = await _context.ZipCodes.Where(z => z.ZipCode == pa.zip5).ToListAsync();
                    if (zip.Count > 0)
                    {
                        var zFound = zip.Where(z => z.City == pa.city).FirstOrDefault();
                        if (zFound != null)
                        {
                            // Found city match, so don't need to do anything
                        }
                        else
                        {
                            ZipCodes z = null;
                            if (zip.Count > 1)
                            {
                                // do special case to find most correct
                                z = zip[0];
                            }
                            else
                            {
                                z = zip[0];
                            }
                            //
                            // Uses LastIndex of to handle things like
                            // JOHN SMITH 18606 OLD TRIANGLE RD TRIANGLE VA 22172-1910
                            //
                            int cityOffset = address.LastIndexOf(z.City, StringComparison.InvariantCultureIgnoreCase);
                            if (cityOffset >= 0)
                            {
                                int streetOffset = address.IndexOf(pa.street, StringComparison.InvariantCultureIgnoreCase);
                                if (streetOffset >= 0)
                                {
                                    string substr = address[streetOffset..cityOffset];
                                    pa.street = substr.Trim();
                                }
                                pa.city = z.City;
                                // now remove part of city that was put into address line
                            }
                        }
                    }
                }
                return pa;
            }
            return null;
        }
        [HttpGet]
        public async Task<IActionResult> GetFieldsForAppType(string appType, string appTypeState)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (appType == null || appTypeState == null)
                return JsonError("AppType and AppTypeState are required");

            ApplicationTypes appForm = await GetAppFormNoTracking(appType,
                                                    appTypeState,
                                                    VendorMode: true,
                                                    EditMode: true, ShowPII: true, ui.VendorId);
            List<AppFormSectionFields> fields = new();
            foreach (var section in appForm.AppFormSections)
            {
                foreach (var field in section.AppFormSectionFields)
                {
                    fields.Add(field);
                }
            }
            return new JsonResult(fields.Select(f => new { f.IsRequired, f.VendorIsRequired, f.Field.ExcelName }).ToList());
        }
        public class FieldAndAttachment
        {
            public bool IsRequired { get; set; }
            public bool VendorIsRequired { get; set; }
            public string ExcelName { get; set; }
            public bool IsInternalClientAttachment { get; set; }
            public bool IsInternalVendorAttachment { get; set; }
            public Guid? AttachmentTypeId { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetFieldsAndAttachmentsForAppType(string appType, string appTypeState)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (appType == null || appTypeState == null)
                return JsonError("AppType and AppTypeState are required");

            ApplicationTypes appForm = await GetAppFormNoTracking(appType,
                                                    appTypeState,
                                                    VendorMode: true,
                                                    EditMode: true, ShowPII: true, ui.VendorId);
            List<AppFormSectionFields> fields = new();
            foreach (var section in appForm.AppFormSections)
            {
                foreach (var field in section.AppFormSectionFields)
                {
                    fields.Add(field);
                }
            }
            List<FieldAndAttachment> fieldAndAttachments = new();
            foreach (var field in fields)
            {
                fieldAndAttachments.Add(new FieldAndAttachment
                {
                    IsRequired = field.IsRequired ?? false,
                    VendorIsRequired = field.VendorIsRequired ?? false,
                    ExcelName = field.Field.ExcelName,
                    IsInternalClientAttachment = false,
                    IsInternalVendorAttachment = false
                });
            }
            var attTypes = await _context.MdpAppTypeAttachmentTypes
                                                .Include(x => x.AttachmentType)
                                                .Include(x => x.AppTypeState)
                                                .ThenInclude(x => x.AppType)
                                                .Where(x => x.AppTypeState.AppState == appTypeState && x.AppTypeState.AppType.AppType == appType)
                                                .Where(x => x.AttachmentType.InternalFromClient || x.AttachmentType.InternalFromVendor)
                                                .ToListAsync();

            foreach (var att in attTypes)
            {
                fieldAndAttachments.Add(new FieldAndAttachment
                {
                    AttachmentTypeId = att.AttachmentTypeId,
                    IsRequired = false,
                    VendorIsRequired = false,
                    ExcelName = att.AttachmentType.ExcelName,
                    IsInternalClientAttachment = att.AttachmentType.InternalFromClient,
                    IsInternalVendorAttachment = att.AttachmentType.InternalFromVendor
                });
            }

            return new JsonResult(fieldAndAttachments);
        }
        [HttpPost]
        public async Task<IActionResult> Create(NewAppTypeViewModel newFormRequest)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                ApplicationTypes appForm = await GetAppFormNoTracking(newFormRequest.AppType,
                                                        newFormRequest.AppTypeState,
                                                        VendorMode: (ui.IsVendorAgent && (!newFormRequest.IsOnBehalfOf || newFormRequest.ForceVendorMode)),
                                                        EditMode: true, ShowPII: true, ui.VendorId);

                if (ui.IsVendorAgent && newFormRequest.IsOnBehalfOf)
                {
                    if (newFormRequest.GroupId == null)
                        return JsonError("Group not selected");
                    if (newFormRequest.UserId == null)
                        return JsonError("User not selected");
                    if (string.IsNullOrEmpty(newFormRequest.AppType))
                        return JsonError("App Type not selected");
                    if (string.IsNullOrEmpty(newFormRequest.AppTypeState))
                        return JsonError("State not selected");
                }
                else
                {
                    if (string.IsNullOrEmpty(newFormRequest.AppType))
                        return JsonError("App Type not selected");
                    if (string.IsNullOrEmpty(newFormRequest.AppTypeState))
                        return JsonError("State not selected");
                }

                if (appForm != null)
                {
                    bool hideSaveDefaults = true;
                    foreach (var section in appForm.AppFormSections.OrderBy(x => x.SectionSortOrder))
                    {
                        if (section.AppFormSectionFields != null)
                        {
                            foreach (AppFormSectionFields afsf in section.AppFormSectionFields.OrderBy(x => x.SortOrder))
                            {
                                AppFormFields field = afsf.Field;
                                if ((field.AllowDefault ?? false) && (field.IsVisible ?? false))
                                {
                                    hideSaveDefaults = false;
                                }
                            }
                        }
                    }

                    AppFormViewModel model = new()
                    {
                        AppForm = appForm,
                        AppTypeState = newFormRequest.AppTypeState,
                        VendorCode = newFormRequest.VendorCode ?? "MAG",
                        EditMode = true,
                        HideSubmit = newFormRequest.ForceVendorMode
                    };

                    if (newFormRequest.IsOnBehalfOf && ui.IsVendorAgent)
                    {
                        model.IsOnBehalfOf = true;
                        model.GroupId = newFormRequest.GroupId;
                        model.UserId = newFormRequest.UserId;
                        Users user = await _context.Users
                                .Include(u => u.UserGroups)
                                .Where(u => u.UserId == newFormRequest.UserId)
                                .SingleAsync();

                        if (!user.UserGroups.Any(ug => ug.GroupId == newFormRequest.GroupId))
                        {
                            // userid is not member of groupid
                            throw new ApplicationException("User is not a member of specified group");
                        }
                        model.UserDisplayName = user.DisplayName;
                        model.GroupName = await _context.Groups
                                                    .Where(g => g.GroupId == newFormRequest.GroupId)
                                                    .Select(g => g.GroupName)
                                                    .SingleAsync();
                        Vendors vendor = await _context.Vendors.Where(v => v.VendorId == ui.VendorId).SingleAsync();
                        model.VendorCode = vendor.VendorCode;
                    }
                    if (model.EditMode && string.Equals(model.AppTypeState, "NY", StringComparison.InvariantCultureIgnoreCase))
                    {
                        model.Dmvoffices = await _context.Dmvoffice.OrderBy(d => d.Commissioner).ToListAsync();
                        model.PoliceAgencies = await _context.PoliceAgency.OrderBy(p => p.Agency).ToListAsync();
                    }
                    else
                    {
                        model.Dmvoffices = new List<Dmvoffice>();
                        model.PoliceAgencies = new List<PoliceAgency>();
                    }

                    // Hide save defaults if no fields allow saving a default value
                    model.HideSaveDefaults = hideSaveDefaults;

                    model.Values = await LoadFormDefaults(ui, appForm.AppType, newFormRequest.AppTypeState);
                    return PartialView("_FormFill", model);
                }
            }
            catch (ApplicationException aex)
            {
                return GetErrorResult(aex.Message);
            }
            catch (Exception ex)
            {
                return GetErrorResult("Exception occurred");
            }
            return NotFound();

        }
#if false
        private string GetDefaultVendorCode(Guid? groupId)
        {
            try
            {
                if (groupId != null)
                {
                    List<GroupVendors> list = _context.GroupVendors.Include(v => v.Vendor).Where(gv => gv.GroupId == groupId.Value).ToList();
                    if (list.Count > 0)
                    {
                        GroupVendors gv = list.Where(l => l.IsDefault).FirstOrDefault();
                        if (gv != null)
                        {
                            return gv.Vendor.VendorCode;
                        }
                        return list[0].Vendor.VendorCode;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.Write($"GetDefaultVendorCode: {ex}");
            }
            return "MAG";
        }
#endif
        [ActionName("View")]
        public async Task<IActionResult> OpenReadonly(Guid id)
        {
            return await InternalOpen(id, true, false);
        }
        public async Task<IActionResult> Open(Guid id)
        {
            return await InternalOpen(id, false, true);
        }

        //1 - Pending
        //2 - Incoming
        //3 - Signing
        //4 - Print
        private static readonly List<int?> s_piiEnabledProcessStageIDs = new(new int?[] { 1, 2, 3, 4 });
        //StatusID StatusName
        //0	Pending
        //1	Active
        //2	Completed
        //9	Hold
        private static readonly List<short?> s_piiEnabledStatusIDs = new(new short?[] { 0, 1 });
        private bool EnablePIICheck(bool enablePII, UserInfo ui, Requests request)
        {
            if (enablePII == false) return false;

            if (!ui.IsVendorAgent)
            {
                if (!ui.IsGroupAdmin && ui.UserId != request.UserId)
                {
                    // disable viewing PII by other users unless an admin
                    enablePII = false;
                }
            }
            if (!s_piiEnabledProcessStageIDs.Contains(request.ProcessStageId))
            {
                enablePII = false;
            }
            if (!s_piiEnabledStatusIDs.Contains(request.StatusId))
            {
                enablePII = false;
            }
            return enablePII;
        }

        private bool CanEditRequest(MyDMVpro.Common.UserInfo user, Requests request)
        {
            if (user == null || user.UserId == null) return false;
            if (request.ProcessStageId == null) return false;
            if (request.ProcessStageId == 1 || request.ProcessStageId == 2)
            {
                if (request.StatusId == null) return false;
                if (request.StatusId != 0 && request.StatusId != 1)
                {
                    return false;
                }
                if (user.UserId == request.UserId || user.IsGroupAdmin)
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<IActionResult> InternalOpen(Guid id, bool isReadOnly, bool enablePII)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui == null)
                return NotFound();

            Requests request = await _context.Requests
                    .Include(r => r.Vendor)
                    .FirstOrDefaultAsync(r => r.RequestId == id
                        && ((ui.IsGroupMember && ui.GroupId == r.GroupId) || (ui.IsVendorAgent && ui.VendorId == r.VendorId)));
            // For Lienholders, view is readonly unless
            // 1) in Pending or Incoming stage
            // 2) AND statusid = Pending or Active
            // 3) AND userId matches request, or user is a group admin
            //
            //if ((full.processStageId == '1' || full.processStageId == '2') // Pending or Incoming
            //    && (full.statusId == '0' || full.statusId == '1') // Pending or Active
            //    && (full.userId.toLowerCase() == userid || groupAdmin == true))

            if (request != null && UserIsMemberOfGroupOrVendor(ui, request.GroupId, request.VendorId))
            {
                if (!isReadOnly && !ui.IsVendorAgent)
                {
                    // check user can edit
                    isReadOnly = !CanEditRequest(ui, request);
                }
                string appType = request.AppType;
                string appTypeState = request.State;
                if (string.IsNullOrEmpty(appType)) appType = "RT";

                enablePII = EnablePIICheck(enablePII, ui, request);
                ApplicationTypes appForm = null;

                appForm = await GetAppFormNoTracking(appType, appTypeState, VendorMode: ui.IsVendorAgent, EditMode: !isReadOnly, ShowPII: enablePII);

                if (appForm != null)
                {
                    AppFormViewModel model = new()
                    {
                        EditMode = !isReadOnly,
                        RequestId = id,
                        AppForm = appForm,
                        AppTypeState = request.State,
                        VendorMode = false,
                        HideSaveDefaults = true,
                        HideSubmit = true, // uses parent window save button
                        VendorCode = request.Vendor.VendorCode,
                        Values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(request.JRequest)
                    };
                    if (model.EditMode && appTypeState.ToUpper() == "NY")
                    {
                        model.Dmvoffices = await _context.Dmvoffice.OrderBy(d => d.Commissioner).ToListAsync();
                        model.PoliceAgencies = await _context.PoliceAgency.OrderBy(p => p.Agency).ToListAsync();
                    }
                    else
                    {
                        model.Dmvoffices = new List<Dmvoffice>();
                        model.PoliceAgencies = new List<PoliceAgency>();
                    }
                    return PartialView("Edit", model);
                }
            }
            return NotFound();
        }
        public async Task<IActionResult> VendorOpen(Guid id)
        {
            UserInfo user = await GetCurrentUserAsync();
            // Only vendor's can access this view
            if (!user.IsVendorAgent)
                return new UnauthorizedResult();

            Requests request = await _context.Requests
                    .Include(r => r.Vendor)
                    .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request != null && UserIsMemberOfGroupOrVendor(user, request.GroupId, request.VendorId))
            {
                string appType = request.AppType;
                string appTypeState = request.State;
                if (string.IsNullOrEmpty(appType)) appType = "RT";

                bool enablePII = EnablePIICheck(true, user, request);
                ApplicationTypes appForm = await GetAppFormNoTracking(appType, appTypeState, VendorMode: true, EditMode: true, ShowPII: enablePII);

                if (appForm != null)
                {
                    AppFormViewModel model = new()
                    {
                        EditMode = true,
                        RequestId = id,
                        VendorMode = true,
                        HideSaveDefaults = true,
                        HideSubmit = true,
                        AppForm = appForm,
                        AppTypeState = request.State,
                        VendorCode = request.Vendor.VendorCode,
                        Values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(request.JRequest)
                    };
                    CalcFields(model.Values);

                    //                    model.SupportedAppTypes = _context.ApplicationTypes.Where(at => at.Active == true).ToList();

                    if (model.EditMode && appTypeState.ToUpper() == "NY")
                    {
                        model.Dmvoffices = await _context.Dmvoffice.OrderBy(d => d.Commissioner).ToListAsync();
                        model.PoliceAgencies = await _context.PoliceAgency.OrderBy(p => p.Agency).ToListAsync();
                    }
                    else
                    {
                        model.Dmvoffices = new List<Dmvoffice>();
                        model.PoliceAgencies = new List<PoliceAgency>();
                    }
                    //_context..OrderBy(p => p.Agency).ToListAsync();
                    //
                    return PartialView("Edit", model);
                }
            }
            return NotFound();
        }
        private void CalcFields(Dictionary<string, string> fields)
        {
            // Fixup Borrower Name from fname/lname
            fields["Borrower Name"] = GetFullName(fields, "Borrower Name", "Borrower FName", "Borrower LName");
            fields["CoBorrower Name"] = GetFullName(fields, "CoBorrower Name", "CoBorrower FName", "CoBorrower LName");
        }
        private string GetFullName(Dictionary<string, string> fields, string fullNameTitle, string firstTitle, string lastTitle)
        {
            string fullName = null;
            if (fields.ContainsKey(fullNameTitle))
            {
                fullName = fields[fullNameTitle];
            }
            if (!string.IsNullOrEmpty(fullName))
                return fullName;

            string firstname = "";
            string lastname = "";
            if (fields.ContainsKey(firstTitle))
            {
                firstname = fields[firstTitle] ?? "";
            }
            if (fields.ContainsKey(lastTitle))
            {
                lastname = fields[lastTitle] ?? "";
            }

            string bname = (firstname + " " + lastname).Trim();
            if (!string.IsNullOrEmpty(bname))
            {
                return bname;
            }
            return "";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private async Task<ApplicationTypes> GetApplicationType(Guid? vendorId, string appType, string appState = null)
        {
            ApplicationTypes applicationType = await DataHelpers.GetApplicationType(vendorId, appType, appState);
            return applicationType;
        }

        private async Task<List<ProcessFields>> GetProcessFieldsForType(Guid? vendorId, string appType, string appState = null)
        {
            List<ProcessFields> fields = await DataHelpers.GetProcessFieldsForType(vendorId.Value, appType, appState);
            return fields;
        }
        public async Task<IActionResult> VendorEditStatus(Guid id)
        {
            UserInfo user = await GetCurrentUserAsync();
            // Only vendor's can access this view
            if (user.IsVendorAgent != true)
                return NotFound();

            Requests request = await _context.Requests
                    .Include(r => r.Vendor)
                    .FirstOrDefaultAsync(r => r.RequestId == id && r.VendorId == user.VendorId);

            if (request != null && UserIsMemberOfGroupOrVendor(user, request.GroupId, request.VendorId))
            {
                ProcessStatus model = new()
                {
                    EnabledFields = await GetProcessFieldsForType(request.VendorId, request.AppType, request.State),
                    AppType = request.AppType,
                    AppTypeState = request.State,
                    RequestId = request.RequestId,
                    Code = request.Code,
                    Courier = request.Courier,
                    DateFromDmv = request.DateFromDmv,
                    DatePrinted = request.DatePrinted,
                    DateReceived = request.DateReceived,
                    DateShipped = request.DateShipped,
                    DateSigned = request.DateSigned,
                    DateTitleIssued = request.DateTitleIssued,
                    DateToDmv = request.DateToDmv,
                    DateToVendor = request.DateToVendor,
                    DmvCourier = request.DmvCourier,
                    DmvTrackingNumber = request.DmvTrackingNumber,
                    ToDmvTrackingNumber = request.ToDmvTrackingNumber,
                    ETA = request.Eta,
                    ToVendorCourier = request.ToVendorCourier,
                    ToVendorTracking = request.ToVendorTracking,
                    TrackingNumber = request.TrackingNumber,
                    //StatusID = request.StatusId.Value,
                    CheckNumber = request.CheckNumber,
                    RejectionDate = request.RejectionDate,
                    LI_CheckNumber = request.LI_CheckNumber,
                    LI_DateFromDmv = request.LI_DateFromDmv,
                    LI_DateToDmv = request.LI_DateToDmv,
                    LI_ToDmvCourier = request.LI_ToDmvCourier,
                    LI_ToDmvTracking = request.LI_ToDmvTracking
                };

                return PartialView("VendorEditStatus", model);
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> VendorEditStatus([FromForm] ProcessStatus frm)
        {
            UserInfo user = await GetCurrentUserAsync();
            // Only vendor's can access this view
            if (user.IsVendorAgent != true)
                return NotFound();

            Requests request = await _context.Requests
                    .Include(r => r.Vendor)
                    .FirstOrDefaultAsync(r => r.RequestId == frm.RequestId && r.VendorId == user.VendorId);

            if (request != null && UserIsMemberOfGroupOrVendor(user, request.GroupId, request.VendorId))
            {
                //model.RequestId = request.RequestId;
                // IMPORTANT!: Code is not updaetd in the edit status form
                // setting it here will result in the value being cleared
                //request.Code = frm.Code;
                if (HasField("Courier")) request.Courier = frm.Courier;
                if (HasField("DateFromDmv")) request.DateFromDmv = frm.DateFromDmv;
                if (HasField("DatePrinted")) request.DatePrinted = frm.DatePrinted;
                if (HasField("DateReceived")) request.DateReceived = frm.DateReceived;
                if (HasField("DateShipped")) request.DateShipped = frm.DateShipped;
                if (HasField("DateSigned")) request.DateSigned = frm.DateSigned;
                if (HasField("DateTitleIssued")) request.DateTitleIssued = frm.DateTitleIssued;
                if (HasField("DateToDmv")) request.DateToDmv = frm.DateToDmv;
                if (HasField("DateToVendor")) request.DateToVendor = frm.DateToVendor;
                if (HasField("DmvCourier")) request.DmvCourier = frm.DmvCourier;
                if (HasField("DmvTrackingNumber")) request.DmvTrackingNumber = frm.DmvTrackingNumber;
                if (HasField("ToDmvTrackingNumber")) request.ToDmvTrackingNumber = frm.ToDmvTrackingNumber;
                if (HasField("ETA")) request.Eta = frm.ETA;
                if (HasField("ToVendorCourier")) request.ToVendorCourier = frm.ToVendorCourier;
                if (HasField("ToVendorTracking")) request.ToVendorTracking = frm.ToVendorTracking;
                if (HasField("TrackingNumber")) request.TrackingNumber = frm.TrackingNumber;
                if (HasField("CheckNumber")) request.CheckNumber = frm.CheckNumber;
                if (HasField("RejectionDate")) request.RejectionDate = frm.RejectionDate;
                if (HasField("LI_CheckNumber")) request.LI_CheckNumber = frm.LI_CheckNumber;
                if (HasField("LI_DateFromDmv")) request.LI_DateFromDmv = frm.LI_DateFromDmv;
                if (HasField("LI_DateToDmv")) request.LI_DateToDmv = frm.LI_DateToDmv;
                if (HasField("LI_ToDmvCourier")) request.LI_ToDmvCourier = frm.LI_ToDmvCourier;
                if (HasField("LI_ToDmvTracking")) request.LI_ToDmvTracking = frm.LI_ToDmvTracking;

                await _context.SaveChangesAsync(user);
                _context.Database.CloseConnection();

                return Ok();
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> VendorEditStatusIndividual(string RequestId, string FieldName, string FieldValue)
        {
            UserInfo user = await GetCurrentUserAsync();

            if (user.IsVendorAgent != true)
                return NotFound();

            Requests request = await _context.Requests
                .FirstOrDefaultAsync(r => r.RequestId == Guid.Parse(RequestId) && r.VendorId == user.VendorId);

            if (request == null || !UserIsMemberOfGroupOrVendor(user, request.GroupId, request.VendorId))
                return NotFound();

            if (HasField("Courier", FieldName)) request.Courier = FieldValue;
            if (HasField("DateFromDmv", FieldName)) request.DateFromDmv = DateTime.Parse(FieldValue);
            if (HasField("DatePrinted", FieldName)) request.DatePrinted = DateTime.Parse(FieldValue);
            if (HasField("DateReceived", FieldName)) request.DateReceived = DateTime.Parse(FieldValue);
            if (HasField("DateShipped", FieldName)) request.DateShipped = DateTime.Parse(FieldValue);
            if (HasField("DateSigned", FieldName)) request.DateSigned = DateTime.Parse(FieldValue);
            if (HasField("DateTitleIssued", FieldName)) request.DateTitleIssued = DateTime.Parse(FieldValue);
            if (HasField("DateToDmv", FieldName)) request.DateToDmv = DateTime.Parse(FieldValue);
            if (HasField("DateToVendor", FieldName)) request.DateToVendor = DateTime.Parse(FieldValue);
            if (HasField("DmvCourier", FieldName)) request.DmvCourier = FieldValue;
            if (HasField("DmvTrackingNumber", FieldName)) request.DmvTrackingNumber = FieldValue;
            if (HasField("ToDmvTrackingNumber", FieldName)) request.ToDmvTrackingNumber = FieldValue;
            if (HasField("ETA", FieldName)) request.Eta = DateTime.Parse(FieldValue); ;
            if (HasField("ToVendorCourier", FieldName)) request.ToVendorCourier = FieldValue;
            if (HasField("ToVendorTracking", FieldName)) request.ToVendorTracking = FieldValue;
            if (HasField("TrackingNumber", FieldName)) request.TrackingNumber = FieldValue;
            if (HasField("CheckNumber", FieldName)) request.CheckNumber = int.Parse(FieldValue);
            if (HasField("RejectionDate", FieldName)) request.RejectionDate = DateTime.Parse(FieldValue);
            if (HasField("LI_CheckNumber", FieldName)) request.LI_CheckNumber = int.Parse(FieldValue);
            if (HasField("LI_DateFromDmv", FieldName)) request.LI_DateFromDmv = DateTime.Parse(FieldValue);
            if (HasField("LI_DateToDmv", FieldName)) request.LI_DateToDmv = DateTime.Parse(FieldValue);
            if (HasField("LI_ToDmvCourier", FieldName)) request.LI_ToDmvCourier = FieldValue;
            if (HasField("LI_ToDmvTracking", FieldName)) request.LI_ToDmvTracking = FieldValue;

            await _context.SaveChangesAsync(user);
            _context.Database.CloseConnection();

            return Ok();
        }

        private bool HasField(string fieldName, string updatedValueFieldName )
        {
            return fieldName == updatedValueFieldName;
        }

        private bool HasField(string fieldName)
        {
            return this.Request.Form.Keys.Contains(fieldName);
        }
        public async Task<IActionResult> ViewStatus(Guid id)
        {
            UserInfo user = await GetCurrentUserAsync();
            // Only vendor's can access this view
            //if (user.IsVendorAgent != true)
            //    return NotFound();

            Requests request = await _context.Requests
                    .Include(r => r.Vendor)
                    .FirstOrDefaultAsync(r => r.RequestId == id && (r.VendorId == user.VendorId || user.GroupId == r.GroupId));

            var eta = request.Eta;
            if (user.GroupId != null && eta != null)
            {
                // If vendor eta is on weekend, then move to next business day
                while (eta.Value.DayOfWeek == DayOfWeek.Saturday || eta.Value.DayOfWeek == DayOfWeek.Sunday)
                {
                    eta = eta.Value.AddDays(1);
                }
                // Add 1 day to get LH eta
                eta = eta.Value.AddDays(1);
                // Get next business day if a weekend
                // Add 1 business day
                while (eta.Value.DayOfWeek == DayOfWeek.Saturday || eta.Value.DayOfWeek == DayOfWeek.Sunday)
                {
                    eta = eta.Value.AddDays(1);
                }
            }
            if (request != null && UserIsMemberOfGroupOrVendor(user, request.GroupId, request.VendorId))
            {
                ProcessStatus model = new()
                {
                    EnabledFields = await GetProcessFieldsForType(request.VendorId, request.AppType, request.State),
                    AppType = request.AppType,
                    AppTypeState = request.State,
                    RequestId = request.RequestId,
                    Code = request.Code,
                    Courier = request.Courier,
                    DateFromDmv = request.DateFromDmv,
                    DatePrinted = request.DatePrinted,
                    DateReceived = request.DateReceived,
                    DateShipped = request.DateShipped,
                    DateSigned = request.DateSigned,
                    DateTitleIssued = request.DateTitleIssued,
                    DateToDmv = request.DateToDmv,
                    DateToVendor = request.DateToVendor,
                    DmvCourier = request.DmvCourier,
                    DmvTrackingNumber = request.DmvTrackingNumber,
                    ToDmvTrackingNumber = request.ToDmvTrackingNumber,
                    ETA = eta,
                    ToVendorCourier = request.ToVendorCourier,
                    ToVendorTracking = request.ToVendorTracking,
                    TrackingNumber = request.TrackingNumber,
                    CheckNumber = request.CheckNumber,
                    RejectionDate = request.RejectionDate,
                    LI_CheckNumber = request.LI_CheckNumber,
                    LI_DateFromDmv = request.LI_DateFromDmv,
                    LI_DateToDmv = request.LI_DateToDmv,
                    LI_ToDmvCourier = request.LI_ToDmvCourier,
                    LI_ToDmvTracking = request.LI_ToDmvTracking
                };

                return PartialView("_ViewProcessStatus", model);
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> SaveForm()
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui != null && (ui.GroupId != null || ui.VendorId != null))
            {
                try
                {
                    Guid? requestId = null;
                    Guid? groupId = null;
                    Guid? userId = null;
                    // If vendor submitting on-behalf of user, the VendorId will of the agent will be used
                    Guid? vendorId = ui.VendorId;

                    if (Request.Form.Keys.Contains("RequestId"))
                    {
                        string g = Request.Form["RequestId"];
                        if (!string.IsNullOrWhiteSpace(g))
                        {
                            if (Guid.TryParse(g, out Guid gId))
                                requestId = gId;
                        }
                    }
                    if (ui.VendorId != null)
                    {
                        if (Request.Form.Keys.Contains("GroupId"))
                        {
                            string g = Request.Form["GroupId"];
                            if (!string.IsNullOrWhiteSpace(g))
                            {
                                if (Guid.TryParse(g, out Guid gId))
                                    groupId = gId;
                            }
                        }
                        if (Request.Form.Keys.Contains("UserId"))
                        {
                            string g = Request.Form["UserId"];
                            if (!string.IsNullOrWhiteSpace(g))
                            {
                                if (Guid.TryParse(g, out Guid gId))
                                    userId = gId;
                            }
                        }
                    }
                    Dictionary<string, string> Values = new();
                    foreach (var f in Request.Form.Keys)
                    {
                        if (f == "__RequestVerificationToken")
                            continue;
                        var v = Request.Form[f];
                        // TODO make into a more maintainable solution
                        if (f == "Vehicle Vin" || f == "VIN")
                        {
                            Values.Add(f, v[0].ToUpperInvariant());
                        }
                        else
                        {
                            Values.Add(f, v);
                        }
                    }

                    try
                    {
                        string appTypeState = Values["AppTypeState"];
                        string appType = Values["AppType"];
                        Guid? newRequestId = null;
                        string jsonRequest = BuildJsonRequest(Values);

                        if (requestId != null && requestId != Guid.Empty)
                        {
                            var request = await _context.Requests.SingleOrDefaultAsync(x => x.RequestId == requestId);
                            _ = request ?? throw new ApplicationException("Record deleted or does not exist");
                            if ((ui.VendorId == request.VendorId) || (ui.GroupId == request.GroupId))
                            {
                                request.JRequest = MergeUpdatedContent(request.JRequest, jsonRequest);
                                await _context.SaveChangesAsync(ui);
                            }
                        }
                        else
                        {
                            if (userId == null && ui.VendorId == null)
                                userId = ui.UserId;
                            if (groupId == null && ui.VendorId == null)
                                groupId = ui.GroupId;
                            newRequestId = Guid.NewGuid();
                            if (appType == "REG")
                            {
                                Requests request = new()
                                {
                                    RequestId = newRequestId.Value,
                                    AppType = appType,
                                    State = appTypeState,
                                    UserId = userId,
                                    GroupId = groupId,
                                    VendorId = vendorId,
                                    JRequest = jsonRequest,
                                    DirectToVendor = true,
                                    DateReceived = ServerDateTime(),
                                    DateToVendor = ServerDateTime(),
                                    StatusId = 1, // Active
                                    ProcessStageId = 60
                                };
                                await _context.AddAsync(request);
                                await _context.SaveChangesAsync(ui);
                            }
                            else
                            {
                                Requests request = new()
                                {
                                    RequestId = newRequestId.Value,
                                    AppType = appType,
                                    State = appTypeState,
                                    UserId = userId,
                                    GroupId = groupId,
                                    VendorId = vendorId,
                                    JRequest = jsonRequest,
                                    DirectToVendor = true,
                                    DateReceived = ServerDateTime(),
                                    DateToVendor = ServerDateTime(),
                                    StatusId = 1, // Active
                                    ProcessStageId = 2
                                };
                                await _context.AddAsync(request);
                                await _context.SaveChangesAsync(ui);
                            }
                        }
                        if (this.Request?.Form?.Files != null)
                        {
                            Microsoft.AspNetCore.Http.IFormFileCollection files = this.Request?.Form?.Files;
                            if (files.Count > 0)
                            {
                                var request = await _context.Requests.SingleOrDefaultAsync(r => r.RequestId == (newRequestId ?? requestId));
                                if (request != null)
                                {
                                    foreach (var file in files)
                                    {
                                        // Attach file to request
                                        var filedata = MyDMVpro.Common.FileHelpers.ProcessBinaryFormFile(file, ModelState);
                                        var filename = System.IO.Path.GetFileName(file.FileName);
                                        var descfldname = (file.Name).Replace("dynform-file-", "dynform-filedesc-");
                                        var description = "";
                                        if (this.Request.Form.ContainsKey(descfldname))
                                        {
                                            description = this.Request.Form[descfldname];
                                        }
                                        RequestAttachments attachment = new()
                                        {
                                            Filename = filename,
                                            Description = description,
                                            UploadedBy = userId,
                                            Image = filedata,
                                            DateAdded = DateTime.UtcNow
                                        };
                                        request.RequestAttachments.Add(attachment);
                                    }
                                    await _context.SaveChangesAsync(ui);
                                }
                                else
                                {
                                    // TBD: Handle request not saved
                                }
                            }
                        }
                        if (appType == "RT")
                        {
                            if (newRequestId != null)
                            {
                                List<Guid> ids = new()
                                {
                                    newRequestId.Value
                                };
                                DataHelpers.TriggerAutoImsDownloadForRequests(groupId.Value, ids, _logger, true).Wait();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("{0}", ex);
                        ValidationProblemDetails detail = new()
                        {
                            Detail = "Invalid form data"
                        };
                        return ValidationProblem(detail);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("{0}", ex);
                    ValidationProblemDetails detail = new()
                    {
                        Detail = ex.Message
                    };
                    return ValidationProblem(detail);
                }
                finally
                {
                    _context.Database.CloseConnection();
                }
            }
            return Ok();
        }
        private string MergeUpdatedContent(string original, string updated)
        {
            if (string.IsNullOrEmpty(original)) return updated;
            if (string.IsNullOrEmpty(updated)) return original;

            JObject jResult = JObject.Parse(original);
            JObject jUpdated = JObject.Parse(updated);

            foreach (var property in jUpdated)
            {
                string name = property.Key;
                JToken value = property.Value;
                jResult[name] = value;
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(jResult);
        }
        [HttpPost]
        public async Task<IActionResult> SaveFormDefaults()
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui != null)
            {
                try
                {
                    Dictionary<string, string> Values = new();
                    foreach (var f in Request.Form.Keys)
                    {
                        var v = Request.Form[f];
                        Values.Add(f, v);
                    }

                    try
                    {
                        string appTypeState = Values["AppTypeState"];
                        string appType = Values["AppType"];
                        bool setGroupDefaults = false;
                        // save form defaults
                        await SaveFormDefaults(ui, setGroupDefaults, appType, appTypeState, Values);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("{0}", ex);
                        ValidationProblemDetails detail = new()
                        {
                            Detail = "Invalid form data"
                        };
                        return ValidationProblem(detail);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("{0}", ex);
                    ValidationProblemDetails detail = new()
                    {
                        Detail = ex.Message
                    };
                    return ValidationProblem(detail);
                }
            }
            return Ok();
        }
        private string BuildJsonRequest(Dictionary<string, string> values)
        {
            var jRequest = Newtonsoft.Json.JsonConvert.SerializeObject(values);
            return jRequest;
        }

        public async Task<IActionResult> ZipLookup(string id)
        {
            ZipCodes zc = await _context.ZipCodes.Where(z => z.ZipCode == id).FirstOrDefaultAsync();
            if (zc != null)
            {
                return Json(new
                {
                    zc.ZipCode,
                    zc.City,
                    State = zc.StateAbbr,
                    zc.County
                });
            }
            return Json(new { });
        }
        private async Task<Dictionary<string, string>> LoadFormDefaults(UserInfo ui, string appType, string appTypeState)
        {
            Dictionary<string, string> defaults = await _context.AppFormDefaults
                .Where(d => d.AppType == appType
                        && d.AppTypeState == appTypeState
                        && d.GroupId == ui.GroupId
                        && (d.UserId == null || d.UserId == ui.UserId))
                .ToDictionaryAsync(dc => dc.ExcelName, dc => dc.DefaultValue, StringComparer.InvariantCultureIgnoreCase);
            return defaults;
        }
        private async Task SaveFormDefaults(UserInfo ui, bool groupDefaults, string appType, string appTypeState, Dictionary<string, string> Values)
        {
            if (!ui.IsGroupAdmin)
            {
                groupDefaults = false;// prevent non-admin setting group defaults
            }
            List<string> fieldsThatAllowDefault = new();

            ApplicationTypes appForm = await GetAppFormNoTracking(appType, appTypeState);
            foreach (var section in appForm.AppFormSections)
            {
                foreach (var afsf in section.AppFormSectionFields)
                {
                    if (afsf.Field.AllowDefault != null && afsf.Field.AllowDefault == true)
                    {
                        fieldsThatAllowDefault.Add(afsf.Field.ExcelName);
                    }
                }
            }

            List<AppFormDefaults> defaults = await _context.AppFormDefaults
                .Where(d => fieldsThatAllowDefault.Contains(d.ExcelName)
                        && d.AppType == appType
                        && d.AppTypeState == appTypeState
                        && d.GroupId == ui.GroupId
                        && ((groupDefaults && d.UserId == null)
                            || (!groupDefaults && d.UserId == ui.UserId)))
                .ToListAsync();

            List<string> existingDefaults = defaults.Select(f => f.ExcelName).ToList();

            // Update those that already have a default saved
            foreach (AppFormDefaults afd in defaults)
            {
                if (Values.ContainsKey(afd.ExcelName))
                {
                    string defaultVal = Values[afd.ExcelName];
                    afd.DefaultValue = defaultVal;
                }
            }
            // add those that do not already have a default
            foreach (string key in fieldsThatAllowDefault)
            {
                if (!existingDefaults.Contains(key) && Values.ContainsKey(key))
                {
                    string defaultVal = Values[key];
                    AppFormDefaults afd = new()
                    {
                        AppType = appType,
                        AppTypeState = appTypeState,
                        GroupId = ui.GroupId,
                        UserId = (groupDefaults ? null : ui.UserId),
                        ExcelName = key,
                        DefaultValue = defaultVal
                    };
                    _context.AppFormDefaults.Add(afd);
                }
            }
            await _context.SaveChangesAsync(ui);
        }
        /// <summary>
        /// Pulls form sections and fields from database
        /// then sets fields as visible or readonly base on VendorMode and EditMode
        /// </summary>
        /// <param name="appType"></param>
        /// <param name="appTypeState"></param>
        /// <param name="VendorMode"></param>
        /// <param name="EditMode"></param>
        /// <returns></returns>
        private async Task<ApplicationTypes> GetAppFormNoTracking(string appType, string appTypeState, bool VendorMode = false, bool EditMode = false, bool ShowPII = false, Guid? vendorId = null)
        {
            return await GetAppFormNoTracking_v2(appType, appTypeState, VendorMode, EditMode, ShowPII, vendorId);
        }
        /// <summary>
        /// Pulls form sections and fields from database
        /// then sets fields as visible or readonly base on VendorMode and EditMode
        /// </summary>
        /// <param name="appType"></param>
        /// <param name="appTypeState"></param>
        /// <param name="VendorMode"></param>
        /// <param name="EditMode"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private async Task<ApplicationTypes> GetAppFormNoTracking_v2(string appType, string appTypeState, bool VendorMode = false, bool EditMode = false, bool ShowPII = false, Guid? vendorId = null)
        {
            try
            {
                ApplicationTypes appForm = await DataHelpers.GetApplicationType(vendorId, appType, appTypeState);

                if (!ShowPII)
                {
                    // Remove PII fields
                    appForm.AppFormSections = appForm.AppFormSections.Where(x => x.IsPII == null || x.IsPII == false).ToList();
                    foreach (var section in appForm.AppFormSections)
                    {
                        section.AppFormSectionFields = section.AppFormSectionFields.Where(x => x.IsPII == null || x.IsPII == false).ToList();
                    }
                }

                // Remove Vendor only fields
                foreach (var section in appForm.AppFormSections)
                {
                    if (section.AppFormSectionFields != null)
                    {
                        foreach (var afsf in section.AppFormSectionFields)
                        {
                            if (VendorMode)
                            {
                                afsf.Field.IsRequired = (afsf.VendorIsRequired ?? afsf.Field.IsRequired);
                                afsf.Field.IsVisible = (afsf.VendorOnlyVisible ?? afsf.Field.IsVisible);
                            }
                            else
                            {
                                if (afsf.VendorOnlyVisible == true || afsf.Field.VendorOnlyVisible == true)
                                {
                                    afsf.Field.IsVisible = false;
                                }
                                if (afsf.VendorOnlyEdit == true || afsf.Field.VendorOnlyEdit == true)
                                {
                                    afsf.Field.IsReadOnly = true;
                                }
                            }
                            if (afsf.AllowDefault != null)
                            {
                                afsf.Field.AllowDefault = afsf.AllowDefault;
                            }
                            else if (afsf.Field.AllowDefault != null)
                            {
                                afsf.AllowDefault = afsf.Field.AllowDefault;
                            }
                        }
                    }
                }
                return appForm;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            return null;
        }
    }
}