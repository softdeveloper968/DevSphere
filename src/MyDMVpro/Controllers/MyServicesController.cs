using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using MyDMVpro.Models.AttachmentReviewModels;
using MyDMVpro.Models.DataReviewModels;
using MyDMVpro.Models.RequestsViewModels;
using MyDMVpro.Models.SharedViews;
using MyDMVpro.Models.VendorViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Hashing;
using System.Linq;
using System.Threading.Tasks;
using UserInfo = MyDMVpro.Common.UserInfo;


namespace MyDMVpro.Controllers
{
    [Authorize(Policy = "ActiveUser")]
    public class MyServicesController : BaseController
    {
        public MyServicesController(MaggardDMVContext context, IConfiguration configuration, ILogger<MyServicesController> logger) : base(context, configuration, logger)
        {
        }
        public IActionResult Index()
        {
            return RedirectToAction("MyRequests");
        }
        public IActionResult Chats()
        {
            AddPageHeader("Chats", "");
            return View();
        }
        public IActionResult SLA()
        {
            AddPageHeader("SLA Report", "");
            return View();
        }
        public async Task<IActionResult> Pending()
        {
            AddPageHeader("Unsent", "");
            ViewData["AppFormData"] = await GetModelForForms();
            return View();
        }
        public IActionResult Active()
        {
            AddPageHeader("Active", "");
            return View();
        }
        public IActionResult MyRequests()
        {
            AddPageHeader("My Requests", "");
            return View();
        }
        public IActionResult Communication()
        {
            AddPageHeader("Communication", "");
            return View();
        }
        public IActionResult MyIDR()
        {
            AddPageHeader("My Inquiries, Docs, Redemptions", "");
            return View();
        }
        public IActionResult PALC()
        {
            AddPageHeader("PA Continuations", "");
            return View();
        }
        public IActionResult MyPALC()
        {
            AddPageHeader("My PA Continuations", "");
            return View();
        }
        public IActionResult IDRActive()
        {
            AddPageHeader("Active Inquiries, Docs, Redemptions", "");
            return View();
        }
        public IActionResult Completed()
        {
            AddPageHeader("Completed Requests", "");
            return View();
        }
        public IActionResult IDRCompleted()
        {
            AddPageHeader("Completed Inquiries, Docs, Redemptions", "");
            return View();
        }
        public IActionResult Master()
        {
            AddPageHeader("Master List", "");
            return View();
        }

        public IActionResult Billing()
        {
            AddPageHeader("Billing", "");
            return View();
        }

        public async Task<IActionResult> MyUploads()
        {
            AddPageHeader("My Uploads", "");
            AddBreadcrumb("My Services", "/MyServices/MyUploads");
            ViewData["AppFormData"] = await GetModelForForms();
            return View();
        }
        #region Repo Title Requests methods
        #endregion
        //public static string SqlConnectionString = @"";
        #region Get data method.
        /// <summary>
        /// GET: /Home/GetData
        /// </summary>
        /// <returns>Return data</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<ActionResult> GetFileUploads(bool includeSigned)
        {
            using MaggardDMVContext context = new();
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();

                // Skip number of Rows count  
                var start = Request.Form["start"].FirstOrDefault();

                // Paging Length 10,20  
                var length = Request.Form["length"].FirstOrDefault();

                // Sort Column Name  
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                // Sort Column Direction (asc, desc)  
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();

                // Search Value from (Search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10, 20, 50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;

                int skip = start != null ? Convert.ToInt32(start) : 0;

                int recordsTotal = 0;

                int viewLastXdays = 7;
                DateTime dtCutoff = DateTime.Today.AddDays(-viewLastXdays); // Cutoff for showing in uploads view
                string sid = GetUserSID();
                var fileUploads = context.FileUploads
                                   .Where(x => (x.User.UserPrincipalName == sid || x.User.NameIdentifierClaim == sid) && x.DateUploaded > dtCutoff)
                                   .OrderByDescending(x => x.DateUploaded)
                                   .AsNoTracking()
                                   .Select(row => new
                                   {
                                       row.FileUploadId,
                                       row.ReferenceNo,
                                       row.Filename,
                                       row.DateUploaded,
                                       UploadedBy = row.User.UserPrincipalName,
                                       RequestCount = context.Requests.Count(r => r.FileUploadId == row.FileUploadId)
                                   });
                //Sorting  
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    //fileUploads = fileUploads.OrderBy();
                }
                //Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    fileUploads = fileUploads.Where(m => m.Filename == searchValue);
                }

                //total number of rows counts   
                recordsTotal = fileUploads.Count();
                //Paging   
                var data = await fileUploads.Skip(skip).Take(pageSize).ToListAsync();

                //Returning Json Data  
                return Json(new { draw, recordsFiltered = recordsTotal, recordsTotal, data });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
                // Info
                //Console.Write(ex);
                return null;
            }
        }
        #endregion
        [HttpPost]
        public async Task<IActionResult> DirectToVendor([FromForm] string[] ids, [FromForm] string dateSent)
        {
            if (ids == null || ids.Length == 0)
                return Ok();

            List<Guid> requestIds = new();

            foreach (string sGuid in ids)
            {
                if (Guid.TryParse(sGuid, out Guid guid))
                {
                    requestIds.Add(guid);
                }
            }

            string userName = GetUserSID();

            if (!GetServerDateOrNow(dateSent, out DateTime date))
            {
                return GetErrorResult("Invalid sent date");
            }
            await DataHelpers.MarkAsDirectToVendor(userName, date, requestIds);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ShipItems([FromForm] string[] ids, [FromForm] string courierName, [FromForm] string trackingNumber, [FromForm] string shipDate)
        {
            if (ids == null || ids.Length == 0)
                return Ok();

            List<Guid> requestIds = new();

            foreach (string sGuid in ids)
            {
                if (Guid.TryParse(sGuid, out Guid guid))
                {
                    requestIds.Add(guid);
                }
            }

            if (!GetServerDateOrNow(shipDate, out DateTime dtShipDate))
            {
                return GetErrorResult("Invalid ship date");
            }
            string userName = GetUserSID();
            if (!string.IsNullOrEmpty(courierName))
            {
                courierName = courierName.Trim();
                trackingNumber = (trackingNumber ?? "").Trim();
                await DataHelpers.UpdateShipToVendorDetails(userName, dtShipDate, courierName, trackingNumber, requestIds);
            }
            else
            {
                await DataHelpers.MarkAsShippedToVendor(userName, dtShipDate, requestIds);
            }
            return Ok();
        }
        //RemovePending
        [HttpPost]
        public async Task<IActionResult> RemovePending([FromForm] string[] ids)
        {
            if (ids == null || ids.Length == 0)
                return Ok();

            List<Guid> requestIds = new();

            foreach (string sGuid in ids)
            {
                if (Guid.TryParse(sGuid, out Guid guid))
                {
                    requestIds.Add(guid);
                }
            }

            string userName = GetUserSID();
            await DataHelpers.RemovePending(userName, ServerDateTime(), requestIds);

            return Ok();
        }
        public async Task<MyDMVpro.Models.AppFormModels.NewAppTypeViewModel> GetModelForForms()
        {
            //TBD: Handle different vendors
            Guid vendorId = new("C9B2E999-6CBA-4D3F-8F38-39EE7FEE6DB9");

            MyDMVpro.Models.AppFormModels.NewAppTypeViewModel model = new()
            {
                SupportedAppTypes = _context.MdpAppTypes
                                                    .Where(at => at.Active == true && at.VendorId == vendorId)
                                                    .Select(x => new ApplicationTypes()
                                                    {
                                                        Active = true,
                                                        AliasForAppType = x.AliasForAppType,
                                                        AppType = x.AppType,
                                                        Description = x.Description,
                                                        AutoIMSEnabled = x.AutoIMSEnabled,
                                                        QueueName = x.QueueName,
                                                        Title = x.Title,
                                                    })
                                                    .OrderBy(x => x.Title)
                                                    .ToList()
            };

            UserInfo user = await GetCurrentUserAsync();
            var supportedAppTypes = await DataHelpers.GetApplicationTypes(vendorId, user.GroupId);
            List<string> supportedStates = new();
            foreach (var apptype in supportedAppTypes)
            {
                supportedStates.AddRange(apptype.AppStates);
            }
            supportedStates = supportedStates.Distinct().ToList();
            model.SupportedStates = USState.GetAllStates().Where(s => supportedStates.Contains(s.Abbrev)).OrderBy(s => s.Abbrev).ToList();

            return model;
        }
        private class UserTuple
        {
            public Guid userId;
            public string displayName;
        }
        [HttpPost]
        public async Task<IActionResult> GetUsers(Guid? groupId)
        {
            UserInfo user = await GetCurrentUserAsync();
            if (!user.IsVendorAdmin)
            {
                groupId = user.GroupId;
            }
            List<UserTuple> users = null;

            if (groupId != null)
            {
                users = await _context.Users
                                .Include(u => u.UserGroups)
                                .Where(u => u.Active == true && u.UserGroups.Any(ug => ug.GroupId == groupId))
                                .OrderBy(u => u.DisplayName)
                                .AsNoTracking()
                                .Select(u => new UserTuple()
                                { userId = u.UserId, displayName = u.DisplayName })
                                .ToListAsync();
            }
            return new JsonResult(users);
        }
        [HttpPost]
        public async Task<IActionResult> AssignToUser([FromForm] string[] ids, [FromForm] Guid? userId)
        {
            if (ids == null || ids.Length == 0)
                return Ok();

            if (userId == null || userId == Guid.Empty)
            {
                return JsonError("User id is required");
            }
            List<Guid> requestIds = new();

            foreach (string sGuid in ids)
            {
                if (Guid.TryParse(sGuid, out Guid guid))
                {
                    requestIds.Add(guid);
                }
            }

            UserInfo currentUser = await GetCurrentUserAsync();

            try
            {
                await DataHelpers.AssignToUser(currentUser, requestIds, userId.Value);
            }
            catch (Exception ex)
            {
                return BadRequest("Error reassigning requests: " + ex.Message);
            }

            return Ok();
        }

        private async Task<IActionResult> InternalRequestLookup(Guid? id, string vin, int? rno)
        {

            AddPageHeader("Request Lookup", "");
            UserInfo user = await GetCurrentUserAsync();
            VendorViewModel vendorViewModel = new VendorViewModel();

            if (id.HasValue)
            {
                ViewBag.RequestId = id.ToString();
                try
                {
                    var request = await _context.Requests
                                    .Where(r => r.RequestId == id.Value && user.GroupId == r.GroupId)
                                    .FirstOrDefaultAsync();
                    if (request != null)
                    {
                        ViewBag.VIN = request.Vin;
                        ViewBag.RequestNo = (int?)request.Id;

                        var application = await _context.MdpAppTypeStates
                                                        .Include(a => a.AppType)
                                                        .Where(a => a.AppType.AppType == request.AppType && a.AppState == request.State)
                                                        .Where(a => a.PublicHelpID != null)
                                                        .SingleOrDefaultAsync();
                        if (application?.PublicHelpID != null)
                        {
                            ViewBag.HelpLink = $"/help/{request.AppType}/{request.State}";
                        }
                        else
                        {
                            ViewBag.HelpLink = "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    //TODO: 
                }
            }
            else
            {
                ViewBag.RequestId = "";
                ViewBag.VIN = "";
                ViewBag.RequestNo = null;
                if (vin != null && rno.HasValue)
                {
                    var request = await _context.Requests
                                                .Where(r => r.Vin == vin && r.Id == rno && r.GroupId == user.GroupId)
                                                .Include(x=>x.RequestNotes)
                                                .FirstOrDefaultAsync();
                    if (request != null)
                    {
                        ViewBag.RequestId = request.RequestId.ToString();
                        ViewBag.VIN = request.Vin;
                        ViewBag.RequestNo = rno;
                        ViewBag.HelpLink = $"/help/{request.AppType}/{request.State}";
                        //ViewBag.ProcessStage = ProcessStageHelper.GetProcessStageMessage((int)request.ProcessStageId);

                        var ProcessStageMessage = ProcessStageHelper.GetProcessStageMessageAfterDynamicUpdates((int)request.ProcessStageId, request);
                        vendorViewModel.DateToVendor = request.DateToVendor;
                        vendorViewModel.DateToDmv = request.DateToDmv;
                        vendorViewModel.DateReceived = request.DateReceived;
                        vendorViewModel.DateFromDmv = request.DateFromDmv;
                        vendorViewModel.VIN = request.Vin.ToString();
                        vendorViewModel.Eta = request.LH_ETA;
                        vendorViewModel.DateShipped = request.DateShipped;
                        vendorViewModel.DateTitleIssued = request.DateTitleIssued;
                        vendorViewModel.StatusId = request.StatusId;
                        vendorViewModel.HasIssues = false;
                        vendorViewModel.Make = request.VehicleMake;
                        vendorViewModel.Year = request.VehicleYear;
                        vendorViewModel.ProcessStageID = request.ProcessStageId;
                        vendorViewModel.ProcessStageName = ProcessStageMessage.Stage;
                        vendorViewModel.ProcessStageSummary = ProcessStageMessage.Message;
                        vendorViewModel.ProcessStageSummaryLabel = ProcessStageMessage.Label;
                        vendorViewModel.RequestNotes = request.RequestNotes;
                        vendorViewModel.TrackingNumber = request.TrackingNumber;
                        vendorViewModel.AppType = request.AppType;
                        vendorViewModel.State = request.State;
                        vendorViewModel.ReferenceNumber = request.Id;
                    }
                }
            }
            return View(vendorViewModel);
        }
        
        [HttpGet]
        [Route("MyServices/RequestLookup")]
        [Route("MyServices/RequestLookup/{id}")]
        [Route("MyServices/RequestLookup/{vin}/{rno}")]
        public async Task<IActionResult> RequestLookup(Guid? id, string vin, int? rno)
        {
            if (vin == null && rno == null)
            {
                return await InternalRequestLookup(id, null, null);
            }
            return await InternalRequestLookup(null, vin, rno);
        }
        
        public async Task<IActionResult> FileLibrary()
        {
            UserInfo user = await GetCurrentUserAsync();
            if (user != null)
            {
                return View();
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> GetMissingAttachmentsListOptimized()
        {
            UserInfo user = await GetCurrentUserAsync();

            var processStageIds = new[] { ProcessStageIDs.Incoming.GetHashCode(), ProcessStageIDs.NotReadyToProcess.GetHashCode(), ProcessStageIDs.Hold.GetHashCode() };

            var requestsQuery = _context.Requests
                .Where(x => processStageIds.Contains((int)x.ProcessStageId) && x.GroupId == user.GroupId && x.StatusId == (int)RequestStatusIDs.Active);

            var requestData = await requestsQuery
                .Select(r => new
                {
                    r.RequestId,
                    r.State,
                    r.AppType,
                    r.ProcessStageId,
                    r.DateToVendor,
                    r.Vin,
                    r.Id
                })
                .ToListAsync();

            if (!requestData.Any())
            {
                return BadRequest("No requests found with the specified process stages.");
            }

            var states = requestData.Select(r => r.State).Distinct().ToList();
            var appTypes = requestData.Select(r => r.AppType).Distinct().ToList();
            var requestIds = requestData.Select(r => r.RequestId).ToList();

            // Step 2: Get relevant AppTypeStates and their required attachment types in a single query
            var MdpAppTypeStates = _context.MdpAppTypeStates.Where(x => states.Contains(x.AppState) && appTypes.Contains(x.AppType.AppType));

            var appTypeAttachments = await MdpAppTypeStates
             .Join(_context.MdpAppTypeAttachmentTypes,
                   appTypeState => appTypeState.AppTypeStateId,
                   attachmentType => attachmentType.AppTypeStateId,
                   (appTypeState, attachmentType) => new
                   {
                       appTypeState.AppState,
                       appTypeState.AppType.AppType,
                       attachmentType.AttachmentTypeId,
                       attachmentType.AttachmentType.Name,
                       attachmentType.AttachmentType.Description,
                       attachmentType.AttachmentType.HardcopyOnly,
                       attachmentType.AttachmentType.InternalFromClient,
                       attachmentType.AttachmentType.InternalFromVendor,
                   })
             .Where(at => !at.InternalFromVendor && !at.InternalFromClient)
             .ToListAsync();

            if (!appTypeAttachments.Any())
            {
                return Ok(new List<MissingAttachmentsViewModel>());
            }

            // Step 3: Get actual attachments for the requests
            var uploadedAttachments = await _context.RequestAttachments
                .Where(r => requestIds.Contains((Guid)r.RequestId))
                .Select(r => new { r.RequestId, r.AttachmentTypeId })
                .ToListAsync();

            // Step 4: Identify missing documents for each request
            var missingDocumentsByRequest = requestData
                .SelectMany(request =>
                {
                    var requiredAttachments = appTypeAttachments
                        .Where(a => a.AppState == request.State && a.AppType == request.AppType)
                        .ToList();

                    var uploadedTypeIds = uploadedAttachments
                        .Where(a => a.RequestId == request.RequestId)
                        .Select(a => a.AttachmentTypeId)
                        .ToList();

                    var missingAttachments = requiredAttachments
                        .Where(rt => !uploadedTypeIds.Contains(rt.AttachmentTypeId))
                        .Select(missing => new MissingAttachmentsViewModel
                        {
                            RequestId = request.RequestId,
                            AttachmentType = missing.Name,
                            AttachmentDescription = missing.Description,
                            HardcopyOrDigital = missing.HardcopyOnly,
                            State = request.State,
                            Type = request.AppType,
                            ProcessStageId = (int)request.ProcessStageId,
                            AttachmentTypeId = missing.AttachmentTypeId,
                            AttachmentTypeName = missing.Name,
                            DateToVendor = request.DateToVendor,
                            VIN = request.Vin,
                            RequestNumber = (int)request.Id

                        });

                    return missingAttachments;
                })
                .ToList();

            return Ok(missingDocumentsByRequest);

            //return PartialView("_DocumentsNeeded", missingDocumentsByRequest);
        }

        [HttpPost]
        public async Task<IActionResult> NeededDataRequestCodes()
        {
            UserInfo user = await GetCurrentUserAsync();
            var processStageIds = new[] { ProcessStageIDs.Incoming.GetHashCode(), ProcessStageIDs.NotReadyToProcess.GetHashCode(), ProcessStageIDs.Hold.GetHashCode() };

            var rows = await _context.RequestCodes
             .Include(rt => rt.Fields)
             .ThenInclude(f => f.Field)
             .Include(rt => rt.Tag)
             .Include(rt => rt.Request)
             .WhereUserHasAccess(user)
             .WhereIsActive()
             .Where(x => processStageIds.Contains((int)x.Request.ProcessStageId) && x.Cleared == false)
             .ToListAsync();

            var missingFields = new List<RequestCode>();


            foreach (var requestCode in rows)
            {

                if (requestCode.Fields.Count > 0)
                {
                    foreach (var requestCodeField in requestCode.Fields)
                    {
                        if (requestCodeField.Field != null || !string.IsNullOrEmpty(requestCodeField.Field.ExcelName)) // Or any condition to check if the field is missing
                        {
                            var missingFieldEntry = new RequestCode
                            {
                                Request = requestCode.Request,
                                RequestCodeId = requestCode.RequestCodeId,
                                RequestId = requestCode.RequestId,
                                TagId = requestCode.TagId,
                                Note = requestCode.Note,
                                Resolution = requestCode.Resolution,
                                Cleared = requestCode.Cleared,
                                ClearedDate = requestCode.ClearedDate,
                                ClearedBy = requestCode.ClearedBy,
                                ClearedNote = requestCode.ClearedNote,
                                TagNameForField = requestCode.TagName,
                                Fields = new List<RequestCodeFields> { requestCodeField }
                            };

                            missingFields.Add(missingFieldEntry);
                        }
                    }
                }
            }

            var existingProposedUpdates = await _context.ProposedUpdates.Where(x=> x.ApprovalStatus == null || x.ApprovalStatus == true)
                .Select(pu => pu.RequestCodeId)
                .ToListAsync();

            missingFields = missingFields
                .Where(mf => !existingProposedUpdates.Contains(mf.RequestCodeId))
                .ToList();

            var missingFieldsData = missingFields
                        .Select(missing => new MissingDataViewModel
                        {
                            RequestId = missing.RequestId,
                            Note = missing.Note,
                            Resolution = missing.Resolution,
                            Field = missing.Fields.FirstOrDefault().ExcelName,
                            State = missing.Request.State,
                            Type = missing.Request.AppType,
                            ProcessStageId = (int)missing.Request.ProcessStageId,
                            DateToVendor = missing.Request.DateToVendor,
                            VIN = missing.Request.Vin,
                            RequestNumber = (int)missing.Request.Id,
                            TagName = missing.TagNameForField,
                            RequestCodeId = missing.RequestCodeId,
                            FieldId = missing.Fields.FirstOrDefault().FieldId

                        });

            return Ok(missingFieldsData);
        }

        [HttpPost]
        public async Task<IActionResult> NeededDataRequestCodesFromView()
        {
            var filterHelper = new FilterHelper<NeedMissingDataModel>((MaggardDMVContext)_context, _configuration, this);

            return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<NeedMissingDataModel> rows = null;
                rows = _context.NeedMissingDataModel.Where(x=>x.GroupId == groupId);
                return rows;
            });
        }

        [HttpGet]
        public async Task<IActionResult> EditMissingRequestFields(Guid requestCodeId, Guid fieldId) 
        {
            var user = await GetCurrentUserAsync();

            var rows = await _context.RequestCodes
            .Include(rt => rt.Fields)
            .ThenInclude(f => f.Field)
            .Include(rt => rt.Tag)
            .Include(rt => rt.Request)
            .WhereIsActive()
            .WhereUserHasAccess(user)
            .Where(x => x.RequestCodeId == requestCodeId).FirstOrDefaultAsync();

            if (rows.Fields.Count > 0)
            {
                var test = rows.Fields.Where(x => x.FieldId == (Guid)fieldId).ToList();

                rows.Fields = test;
            }

            this.ViewData["BeforeValues"] = rows.Request.JRequest;
            ViewBag.BeforeValues = rows.Request.JRequest;
            return PartialView("_MissingFieldsWidget", rows);
        }

        [HttpPost]
        public async Task<IActionResult> AddProposedUpdates([FromBody] ProposedUpdatesViewModel proposedUpdatesViewModel)
        {
            UserInfo ui = await GetCurrentUserAsync();

            var rows = await _context.RequestCodes
            .Include(rt => rt.Fields)
            .ThenInclude(f => f.Field)
            .Include(rt => rt.Tag)
            .Include(rt => rt.Request)
            .WhereUserHasAccess(ui)
            .WhereIsActive()
            .Where(x => x.RequestCodeId == proposedUpdatesViewModel.RequestCodeId).FirstOrDefaultAsync();


            if (ui.UserId != null)
            {
                var fieldsData = rows.Fields.Where(x => x.FieldId == proposedUpdatesViewModel.FieldId).FirstOrDefault();

                if (fieldsData.FieldId == proposedUpdatesViewModel.FieldId && rows.RequestId == proposedUpdatesViewModel.RequestId)
                {
                    var newData = fieldsData.Field.ExcelName;

                    
                    var jRequestOld = rows.Request.JRequest;

                    if (jRequestOld != null)
                    {
                        var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jRequestOld);

                        // Update a specific property
                        jsonDict[newData] = proposedUpdatesViewModel.NewValue;

                        // Serialize back to JSON string
                        string updatedJson = JsonConvert.SerializeObject(jsonDict, Formatting.Indented);
                        string newJson = Newtonsoft.Json.JsonConvert.SerializeObject(jsonDict); ;

                        Console.WriteLine(updatedJson);

                        var proposedUpdateModel = new ProposedUpdates()
                        {
                            RequestId = proposedUpdatesViewModel.RequestId,
                            jRequest_Before = jRequestOld,
                            jRequest_Proposed = newJson,
                            CreatedBy = (Guid)ui.UserId,
                            CreatedDate = DateTime.Now,
                            FieldId = proposedUpdatesViewModel.FieldId,
                            RequestCodeId = proposedUpdatesViewModel.RequestCodeId,
                            ApprovalNotes = proposedUpdatesViewModel.NewNote.Trim(),
                            CreatedByName = GetUser_Name()
                        };

                        var save_proposedData = _context.ProposedUpdates.Add(proposedUpdateModel);

                        await AddNoteAndRemark(RequestCodeOperationType.Add, rows, ui, proposedUpdatesViewModel);
                     
                        await _context.SaveChangesAsync();

                    }
                }

                return Ok();

            }
            else
            {
                return BadRequest();
            }

        }

        private string FieldNameList(IEnumerable<Guid> fieldIds, string appType, string state)
        {
            var fields = _context.MdpAppSectionFields
                                        .Include(s => s.Section)
                                        .ThenInclude(x => x.AppTypeState)
                                        .ThenInclude(x => x.AppType)
                                        .Where(f => fieldIds.Contains(f.FieldId))
                                        .Where(p => p.Section.AppTypeState.AppType.AppType == appType && p.Section.AppTypeState.AppState == state)
                                        .Select(f => new { fieldId = f.FieldId, label = f.Label })
                                        .ToList();
            var fieldLabels = fields.Select(f => f.label).ToList();
           
            // concatenate the field names with a comma but no trailing comma
            var agg = fieldLabels.Aggregate((list, current) => (current + ", ") + list);
            return agg;
        }
        protected enum RequestCodeOperationType
        {
            Add,
            Update,
            Clear
        };
        protected async Task AddNoteAndRemark(RequestCodeOperationType optype, RequestCode requestCode, UserInfo user, ProposedUpdatesViewModel proposedUpdatesViewModel)
        {
            var request = await _context.Requests
                                        .Where(r => r.RequestId == requestCode.RequestId)
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync();

            string note = "";
            string remark = "";
          
            if (optype == RequestCodeOperationType.Add)
            {
                var ids = requestCode.Fields.Select(f => f.FieldId).ToList();
                var fieldsText = "";
                if (ids.Count() > 0)
                {
                    fieldsText = $" to {FieldNameList(ids, request.AppType, request.State)}";
                }
                note = $"Code {requestCode.TagName}. New value [{proposedUpdatesViewModel.NewValue}] is added to {fieldsText} by {GetUser_Name()}. New Note Added: {proposedUpdatesViewModel.NewNote}.";

            }

            await AddNoteAndRemark(requestCode.RequestId, user.UserId, note, remark);
        }
    }

    public static class RequestCode_extensions
    {
        public static IQueryable<RequestCode> WhereIsActive(this IQueryable<RequestCode> query)
        {
            return query.Where(x => x.Request.StatusId == (short?)RequestStatusIDs.Active || x.Request.StatusId == (short?)RequestStatusIDs.Hold);
        }
        public static IQueryable<RequestCode> WhereUserHasAccess(this IQueryable<RequestCode> query, UserInfo user)
        {
            return query.Where(x => x.Request.GroupId == user.GroupId || x.Request.VendorId == user.VendorId);
        }
        public static IQueryable<RequestCode> WhereUserHasAccess(this IQueryable<RequestCode> query, Guid? vendorId, Guid? groupId)
        {
            return query.Where(x => x.Request.GroupId == groupId || x.Request.VendorId == vendorId);
        }
    }
}