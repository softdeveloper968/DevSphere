using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models.AttachmentReviewModels;
using MyDMVpro.Models;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyDMVpro.Models.DataReviewModels;
using MyDMVpro.Models.RequestsViewModels;
using System.Net.Mail;
using System.Collections.Generic;
using MyDMVpro.Common;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using UserInfo = MyDMVpro.Common.UserInfo;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;

namespace MyDMVpro.Controllers
{
    [Authorize(Policy = "VendorAgentOnly")]
    public class DataReviewController : BaseController
    {
        public DataReviewController(MaggardDMVContext context, IConfiguration configuration, ILogger<AttachmentReviewController> logger) : base(context, configuration, logger)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NeedDataReview()
        {
            var filterHelper = new FilterHelper<DataReview>((MaggardDMVContext)_context, _configuration, this);

            return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<DataReview> rows = null;
                rows = _context.DataReviews.Where(x => (x.Cleared == false 
                                                            && x.ApprovalBy == null 
                                                            && x.ProposedUpdateRequestId != null 
                                                            && x.ProposedUpdateId !=null) 
                                                        && x.VendorId == vendorId);
                return rows;
            });
        }
        
        [HttpGet]
        [Route("DataReview/Review/{requestCodeId}/{fieldId}")]
        public async Task<IActionResult> ReviewFields(Guid? requestCodeId, Guid? fieldId)
        {
            try
            {
                var user = await GetCurrentUserAsync();

                var requestCodes = await _context.RequestCodes.Include(x => x.Fields)
                                                                .ThenInclude(x => x.Field)
                                                                .Include(x => x.Tag)
                                                                .WhereUserHasAccess(user)
                                                                .WhereIsActive()
                                                                .Where(r => r.RequestCodeId == requestCodeId && r.Cleared == false)
                                                                .FirstOrDefaultAsync();

                if (requestCodes == null)
                {
                    return NotFound();
                }

                var proposedUpdates = await _context.ProposedUpdates
                                    .Where(x => x.RequestCodeId == requestCodeId && x.FieldId == fieldId)
                                    .FirstOrDefaultAsync();

                if (proposedUpdates == null)
                {
                    return NotFound();
                }

                var requestNotes = await _context.RequestNotes.Where(x => x.RequestId == requestCodes.RequestId).ToListAsync();

                if (requestCodes.Fields.Count > 0)
                {
                    var result = requestCodes.Fields.Where(x => x.FieldId == (Guid)fieldId).ToList();

                    requestCodes.Fields = result;
                }

                DataReviewFieldsModel model = new DataReviewFieldsModel()
                {
                    RequestId = proposedUpdates.RequestId,
                    RequestCodeId = (Guid)proposedUpdates.RequestCodeId,
                    RequestCodeFields = requestCodes.Fields,
                    JRequest = proposedUpdates.jRequest_Proposed,
                    BeforeJRequest = proposedUpdates.jRequest_Before,
                    RequestCode = requestCodes,
                    ProposedUpdates = proposedUpdates,

                };
               
                return PartialView("_dataReviewFields", model);
            }
            catch (Exception ex)
            {
                return PartialView("Error.cshtml");
            }
            return Ok();
        }


        [HttpGet]
        [Route("DataReview/ApproveCheck/{proposedUpdateId}/{requestId}")]
        public async Task<IActionResult> ApproveDataFieldCheck(Guid? proposedUpdateId, Guid? requestId)
        {
            try
            {
                object o = new { html = "" };

                var user = await GetCurrentUserAsync();
                if (proposedUpdateId == null && requestId == null)
                {
                    o = new { html = "This is a message to manually move" };
                }
                else
                {
                    var dataAttachment = await _context.DataReviews
                                                    .Where(a => a.ProposedUpdateId == proposedUpdateId 
                                                                && a.ProposedUpdateRequestId == requestId 
                                                                && a.Cleared== false 
                                                                && a.ApprovalBy == null)
                                                    .FirstOrDefaultAsync();

                    if (dataAttachment != null)
                    {
                        var mdpAppType = await _context.MdpAppTypes
                                                            .Where(a => a.AppType == dataAttachment.AppType)
                                                            .FirstOrDefaultAsync();

                        var isRequestQueue = "Requests".Equals(mdpAppType?.QueueName ?? "", StringComparison.OrdinalIgnoreCase);
                        if (mdpAppType == null || !isRequestQueue || dataAttachment.ProcessStageId != (int)ProcessStageIDs.NotReadyForProcessing)
                        {
                            string url = mdpAppType?.QueuePageUrl ?? "#";
                            string htmlTemplate = "Hello, this record is not an RT/DT in the “Not Ready to Process” queue so there may not be conditional logic to move this record into it the next stage. <br/>Please visit the record in the <a target='_blank' href='{0}'>{1}</a> to move the record forward if needed";
                            string html = "";
                            if (dataAttachment.StatusName == "Active")
                            {
                                string stageName = dataAttachment.ProcessStageName;
                                string queueName = mdpAppType?.QueuePageName ?? "";
                                html = string.Format(htmlTemplate, url, $"{queueName} / {stageName}");
                            }
                            else if (dataAttachment.StatusName == "Hold")
                            {
                                html = string.Format(htmlTemplate, url, "Hold queue");
                            }
                            else if (dataAttachment.StatusName == "Cancelled")
                            {
                                html = string.Format(htmlTemplate, url, "Cancelled queue");
                            }
                            else if (dataAttachment.StatusName == "Completed")
                            {
                                html = string.Format(htmlTemplate, url, "Completed queue");
                            }
                            o = new { html };
                        }
                    }
                }

                return JsonSuccess(o);
            }
            catch (Exception ex)
            {
                LogError(ex, "ApproveCheck");
                return JsonError(ex.Message);
            }
        }


        [HttpPost]
        [Route("DataReview/Approve/{proposedUpdateId}/{fieldId}")]
        public async Task<IActionResult> ApproveDataField(Guid proposedUpdateId,Guid? fieldId)
        {
            try
            {
                var user = await GetCurrentUserAsync();

                if (user.UserId == null)
                {
                    return JsonError("User Not Found.");
                }

                var proposedUpdates = await _context.ProposedUpdates.Where(x => x.ProposedUpdateId == proposedUpdateId && x.FieldId == fieldId).FirstOrDefaultAsync();

                var RequestCodes = await _context.RequestCodes.Include(x=>x.Fields).ThenInclude(x=>x.Field)
                                                    .WhereUserHasAccess(user)
                                                    .WhereIsActive()
                                                    .Where(a => a.RequestId == proposedUpdates.RequestId &&
                                                            (a.RequestCodeId == proposedUpdates.RequestCodeId))
                                                    .FirstOrDefaultAsync();
                if (RequestCodes == null)
                {
                    return JsonError("Data field not found.");
                }

                _context.RequestCodes.Attach(RequestCodes);
             
                var request = await _context.Requests.Where(x => x.RequestId == RequestCodes.RequestId).FirstOrDefaultAsync(); ;

                request.JRequest = proposedUpdates.jRequest_Proposed;

                DateTime reviewDate = DateTime.UtcNow; // use same date for both attachment and history
                RequestCodes.ClearedDate = reviewDate;
                RequestCodes.ClearedBy = user.UserId;
                RequestCodes.Cleared = true;

                await AddNoteAndRemark(RequestCodeOperationType.Approve, RequestCodes, user, proposedUpdates, proposedUpdates.ApprovalNotes);

                proposedUpdates.ApprovalBy = user.UserId;
                proposedUpdates.ApprovalStatus = true;
                proposedUpdates.ApprovalDate = DateTime.UtcNow;

                _context.ProposedUpdates.Update(proposedUpdates);

                await _context.SaveChangesAsync(user);

                return JsonSuccess();

            }
            catch (Exception ex)
            {
                LogError(ex, "ApproveAttachment");
                return JsonError(ex.Message);
            }
        }


        [HttpPost]
        [Route("DataReview/Reject")]
        public async Task<IActionResult> RejectAttachment([FromForm] Guid proposedUpdateId,
            [FromForm] Guid fieldId,
            [FromForm] string note,
            [FromForm] string remark,
            [FromForm] string code,
            [FromForm] bool codeNotRequired)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code) && !codeNotRequired)
                    return JsonError("Code required");
                if (string.IsNullOrWhiteSpace(note) && string.IsNullOrWhiteSpace(remark))
                    return JsonError("Note or remark is required");

                var user = await GetCurrentUserAsync();
                
                if (user.UserId == null)
                {
                    return JsonError("User Not Found.");
                }

                var proposedUpdates = await _context.ProposedUpdates.Where(x => x.ProposedUpdateId == proposedUpdateId && x.FieldId == fieldId).FirstOrDefaultAsync();

               

                var RequestCodes = await _context.RequestCodes.Include(x=>x.Fields).ThenInclude(x=>x.Field)
                                                    .WhereUserHasAccess(user)
                                                    .WhereIsActive()
                                                    .Where(a => a.RequestId == proposedUpdates.RequestId &&
                                                            (a.RequestCodeId == proposedUpdates.RequestCodeId))
                                                    .FirstOrDefaultAsync();

                if (RequestCodes == null)
                {
                    return JsonError("Data field not found.");
                }

                proposedUpdates.ApprovalBy = user.UserId;
                proposedUpdates.ApprovalStatus = false;
                proposedUpdates.ApprovalDate = DateTime.UtcNow;
                _context.ProposedUpdates.Update(proposedUpdates);

                await AddNoteAndRemark(RequestCodeOperationType.Reject, RequestCodes, user, proposedUpdates, note);

                await _context.SaveChangesAsync(user);

                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "RejectAttachment");
                return JsonError(ex.Message);
            }
        }

        protected enum RequestCodeOperationType
        {
            Add,
            Clear,
            Approve,
            Reject
        };

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

        protected async Task AddNoteAndRemark(RequestCodeOperationType optype, RequestCode requestCode, UserInfo user, ProposedUpdates proposedUpdates, string ApproveOrRejectNote)
        {
            var request = await _context.Requests
                                        .Where(r => r.RequestId == requestCode.RequestId)
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync();
            string note = "";
            string remark = "";       
            string jRequestOld = "";       
            string jPropsedRequest = "";
            string excelName = "";    
            

            var fieldsData = requestCode.Fields.Where(x => x.FieldId == proposedUpdates.FieldId).FirstOrDefault();

            if (fieldsData.FieldId == proposedUpdates.FieldId && proposedUpdates.RequestId == proposedUpdates.RequestId)
            {
                excelName = fieldsData.Field.ExcelName;

                jRequestOld = proposedUpdates.jRequest_Before;
                jPropsedRequest = proposedUpdates.jRequest_Proposed;
               
            }

            if (optype == RequestCodeOperationType.Approve)
            {
                var ids = requestCode.Fields.Select(f => f.FieldId).ToList();
                var fieldsText = "";

                if (ids.Count() > 0)
                {
                    fieldsText = $" to {FieldNameList(ids, request.AppType, request.State)}";

                    if (jRequestOld != null && jPropsedRequest != null)
                    {
                        var jsonOldDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jRequestOld);
                        var jsonNewDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jPropsedRequest);

                        // Update a specific property
                        var newValue = jsonNewDict[excelName];
                        string oldValue;

                        // Check if the old dictionary contains the key
                        if (jsonOldDict.ContainsKey(excelName))
                        {
                            oldValue = jsonOldDict[excelName];
                        }
                        else
                        {
                            oldValue = "no previous value"; // Or any other message to indicate absence
                        }
                        note = $"{excelName} was updated from {oldValue} to {newValue}. This new value was submitted by {proposedUpdates.CreatedByName} and approved by {GetUser_Name()}.";

                    }
                }
            }
            else if (optype == RequestCodeOperationType.Clear)
            {
                note = $"Code {requestCode.TagName} cleared. Final Note: {requestCode.ClearedNote}";

            }
            else if(optype == RequestCodeOperationType.Reject)
            {
                if (jRequestOld != null && jPropsedRequest != null)
                {
                    var jsonOldDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jRequestOld);
                    var jsonNewDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jPropsedRequest);

                    // Update a specific property
                    var newValue = jsonNewDict[excelName];
                    string oldValue;

                    // Check if the old dictionary contains the key
                    if (jsonOldDict.ContainsKey(excelName))
                    {
                        oldValue = jsonOldDict[excelName];
                    }
                    else
                    {
                        oldValue = "no previous value"; // Or any other message to indicate absence
                    }

                    note = $"{newValue} was submitted to replace {oldValue} by {proposedUpdates.CreatedByName}, however {GetUser_Name()} has marked this value as not approved with the following note {ApproveOrRejectNote}";

                }
            }
            else
            {
                note = "";
            }
            

            await AddNoteAndRemark(requestCode.RequestId, user.UserId, note, remark);
        }
    }
}
