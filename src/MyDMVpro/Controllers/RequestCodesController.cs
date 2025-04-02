using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

public class RequestCodesController : BaseController
{
    private FilterHelper<RequestCode> filterHelper;
    private const string c_TagType = "Request";

    public RequestCodesController(MaggardDMVContext context, IConfiguration configuration, ILogger<RequestCodesController> logger) : base(context, configuration, logger)
    {
        filterHelper = new FilterHelper<RequestCode>(context, configuration, this, FilterBySettings);
    }

    [HttpPost("/RequestCodes/Requests/{tags?}")]
    public async Task<IActionResult> VendorRequestsTagged(string tags)
    {
        var tagIDs = (tags ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

        if (tagIDs.Count == 0 || (tagIDs.Count == 1 && tagIDs[0] == 0))
        {
            return null; // new List<RequestCodes>(); // await VendorFollowUps(null);
        }

        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<RequestCode> rows = null;
            rows = _context.RequestCodes.AsNoTracking()
                                        .WhereUserHasAccess(vendorId, groupId)
                                        .WhereIsActive()
                                       .Where(fut => tagIDs.Contains(fut.TagId));
            // .Join(_context.RequestFollowUpStatus, oid => oid.FollowUpId, iid => iid.FollowUpId, (oid, iid) => iid);
            return rows;
        }, true, false);
    }

    [HttpGet("/RequestCodes/Fields/{requestId?}")]
    public async Task<IActionResult> RequestCodeFields(Guid? requestId)
    {
        try
        {
            string appType, appState;

            if (requestId == null)
            {
                List<object> empty = new();
                return Json(empty);
            }

            var requestInfo = await _context.Requests
                                    .Where(r => r.RequestId == requestId)
                                    .AsNoTracking()
                                    .Select(r => new { r.AppType, r.State })
                                    .FirstOrDefaultAsync();

            if (requestInfo.AppType == null || requestInfo.State == null)
                return JsonError("AppType and AppTypeState are required");
            appType = requestInfo.AppType;
            appState = requestInfo.State;

            var fields = await _context.MdpAppSectionFields
                                            .Include(f => f.Section)
                                            .ThenInclude(s => s.AppTypeState)
                                            .ThenInclude(s => s.AppType)
                                            .Where(f => f.Section.AppTypeState.AppType.AppType == appType
                                                            && f.Section.AppTypeState.AppState == appState)
                                            .AsNoTracking()
                                            .Select(f => new { id = f.FieldId, tag = f.Label, desc = f.Field.ExcelName, @class = (string)null })
                                            .ToListAsync();

            return new JsonResult(fields);
        }
        catch (Exception ex)
        {
            var result = new JsonResult(ex.Message);
            result.StatusCode = StatusCodes.Status500InternalServerError;
            return result;
        }
    }

    [HttpPost("/RequestCodes/Tags/{requestId?}")]
    public async Task<IActionResult> RequestCodes(Guid? requestId)
    {
        if (requestId != null && requestId == Guid.Empty)
        {
            return EmptyDataTablesQueryResult();
        }
        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<RequestCode> rows = null;
            rows = _context.RequestCodes
                                    .AsNoTracking()
                                    .WhereIsActive()
                                    .WhereUserHasAccess(vendorId, groupId)
                                    .Where(f => f.RequestId == requestId);
            return rows;
        }, true, false);
    }
    protected enum RequestCodeOperationType
    {
        Add,
        Update,
        Clear
    };

    [HttpPost("/RequestCodes/Add")]
    public async Task<IActionResult> CreateRequestCode([FromBody] RequestCode_Create_Model data)
    {
        if (data?.RequestId == null)
        {
            return NotFound();
        }

        try
        {
            var user = await GetCurrentUserAsync();

            var req = await _context.Requests
                                    .Where(ra => ra.RequestId == data.RequestId && ra.VendorId == user.VendorId)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(); // can only be one with requestid/followupid
            if (req == null)
            {
                return NotFound();
            }

            if (data.TagId == 1)
            {
                Debug.Assert(data.TagId != 1);
            }

            var tag = await _context.Tag
                                    .Where(t => t.TagId == data.TagId && t.TagType == "Request")
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync();

            RequestCode requestCode = new RequestCode()
            {
                RequestId = data.RequestId.Value,
                Note = data.Note,
                Resolution = data.Resolution,
                TagId = data.TagId,
                Cleared = false,
                Fields = data.FieldIds.Select(f => new RequestCodeFields() { FieldId = f }).ToList()
            };

            _context.RequestCodes.Add(requestCode);

            requestCode.Tag = tag;

            await AddNoteAndRemark(RequestCodeOperationType.Add, requestCode, user);

            await _context.SaveChangesAsync(user);
        }
        catch (Exception ex)
        {
            LogError(ex, "Create followup error");
            return JsonError("Error saving record.", ex);
        }

        return JsonSuccess();
    }
    [HttpPost("/RequestCodes/Clear")]
    public async Task<IActionResult> ClearRequestCode([FromBody] RequestCode_Clear_Model data)
    {
        if (data?.RequestCodeId == null)
        {
            return JsonError("Invalid request code id");
        }
        if (string.IsNullOrWhiteSpace(data?.ClearedNote))
        {
            return JsonError("Resolution note is required.");
        }

        try
        {
            var user = await GetCurrentUserAsync();

            var requestCodeId = data.RequestCodeId;

            var req = await _context.RequestCodes
                                        .Include(rt => rt.Tag)
                                        .Include(rt => rt.Fields)
                                        .ThenInclude(f => f.Field)
                                        .Include(rt => rt.Request)
                                        .WhereUserHasAccess(user)
                                        .WhereIsActive()
                                        .Where(ra => ra.RequestCodeId == requestCodeId && ra.RequestId == data.RequestId)
                                        .FirstOrDefaultAsync(); // can only be one with requestid/followupid
            if (req == null)
            {
                return NotFound();
            }
            if (!data.OverrideChecks)
            {
                // verify all fields being cleared have data
                var fields = req.Fields.Select(f => new { f.ExcelName, f.FieldId }).ToList();
                var emptyFields = FindEmptyFields(fields.Select(f => f.ExcelName).ToList(), req.Request.JRequest);
                if (emptyFields.Count > 0)
                {
                    var emptyFieldIds = fields.Where(f => emptyFields.Contains(f.ExcelName)).Select(f => f.FieldId).ToList();
                    var agg = FieldNameList(emptyFieldIds, req.Request.AppType, req.Request.State);
                    return JsonError($"{agg} must be supplied before clearing.");
                }
            }

            req.Cleared = true;
            req.ClearedDate = DateTime.UtcNow;
            req.ClearedBy = user.UserId;
            req.ClearedNote = data.ClearedNote;

            await AddNoteAndRemark(RequestCodeOperationType.Clear, req, user);

            await _context.SaveChangesAsync(user);
        }
        catch (Exception ex)
        {
            LogError(ex, "Create followup error");
            return JsonError("Error saving record.", ex);
        }

        return JsonSuccess();
    }

    private List<string> FindEmptyFields(IEnumerable<string> fields, string json)
    {
        var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        var emptyFields = new List<string>();

        foreach (var field in fields)
        {
            string value = null;
            if (dict.ContainsKey(field))
            {
                value = dict[field];
            }
            if (string.IsNullOrWhiteSpace(value?.ToString()))
            {
                emptyFields.Add(field);
            }
        }
        return emptyFields;
    }

    public IQueryable<RequestCode> FilterBySettings(IQueryable<RequestCode> rows, DatatableFormData dfd)
    {
        object filters;

        if (dfd.includeCleared)
        {
            // no additional filtering
        }
        else
        {
            rows = rows.Where(r => r.Cleared != true);
        }

        return rows;
    }

    /// <summary>
    /// Returns the request code information for the given requestCodeId
    /// This is used to populate the edit form
    /// </summary>
    /// <param name="requestCodeId"></param>
    /// <returns></returns>
    [HttpGet("/RequestCodes/Edit/{requestCodeId?}")]
    public async Task<IActionResult> EditRequestCode(Guid? requestCodeId)
    {
        if (requestCodeId == null || requestCodeId == Guid.Empty)
        {
            return NotFound();
        }
        try
        {
            var user = await GetCurrentUserAsync();

            var requestCode = await _context.RequestCodes
                                    .Include(rt => rt.Fields)
                                    .ThenInclude(f => f.Field)
                                    .Include(rt => rt.Tag)
                                    .Include(rt => rt.ClearedByUser)
                                    .AsNoTracking()
                                    .WhereUserHasAccess(user)
                                    .WhereIsActive()
                                    .Where(f => f.RequestCodeId == requestCodeId)
                                    .ToListAsync();

            return Json(requestCode);
        }
        catch (Exception ex)
        {
            return JsonError(ex);
        }
    }

    /// <summary>
    /// This is used to update the request code information
    /// this is called from the edit form
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost("/RequestCodes/Edit/{requestCodeId?}")]
    public async Task<IActionResult> EditRequestCode([FromBody] RequestCode_Edit_Model_POST data)
    /* Guid? requestCodeId */
    {
        if (data.RequestCodeId == null)
        {
            return NotFound();
        }
        try
        {
            if (data.TagId == 0)
            {
                return JsonError("Valid TagId is required");
            }
            var user = await GetCurrentUserAsync();

            var requestCode = await _context.RequestCodes
                                                .Include(rt => rt.Fields)
                                                .Include(rt => rt.Tag)
                                                .WhereUserHasAccess(user)
                                                .WhereIsActive()
                                                .Where(f => f.RequestCodeId == data.RequestCodeId)
                                                .SingleOrDefaultAsync();
            // update the request code
            if (requestCode == null)
            {
                return NotFound();
            }
            string json = JsonConvert.SerializeObject(requestCode);
            var beforeRequestCode = JsonConvert.DeserializeObject<RequestCode>(json);
            beforeRequestCode.Tag = new Tag()
            {
                // need the tagname below for the note
                TagName = requestCode.Tag.TagName,
            };

            requestCode.Note = data.Note;
            requestCode.Resolution = data.Resolution;
            if (requestCode.TagId != data.TagId)
            {
                System.Diagnostics.Debug.Assert(data.TagId != 1);
                requestCode.TagId = data.TagId;
            }

            // remove fields that are not in the list
            var deleteFieldsList = requestCode.Fields.Where(f => !data.FieldIds.Contains(f.FieldId)).ToList();
            if (deleteFieldsList.Count > 0)
            {
                _context.RequestCodeFields.RemoveRange(deleteFieldsList);
            }

            // add fields that are not already in the list
            foreach (var field in data.FieldIds)
            {
                if (requestCode.Fields.Any(f => f.FieldId == field))
                {
                    continue;
                }
                requestCode.Fields.Add(new RequestCodeFields() { FieldId = field });
            }

            await _context.SaveChangesAsync(user);

            requestCode = await _context.RequestCodes.AsNoTracking()
                                                .Include(rt => rt.Fields)
                                                .Include(rt => rt.Tag)
                                                .WhereUserHasAccess(user)
                                                .WhereIsActive()
                                                .Where(f => f.RequestCodeId == data.RequestCodeId)
                                                .SingleOrDefaultAsync();

            await AddNoteAndRemark(RequestCodeOperationType.Update, beforeRequestCode, data.FieldIds, requestCode, user);
            await _context.SaveChangesAsync(user);

            return Json(requestCode);
        }
        catch (Exception ex)
        {
            return JsonError(ex);
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
    protected async Task AddNoteAndRemark(RequestCodeOperationType optype, RequestCode beforeRequestCode, List<Guid> afterFieldIds, RequestCode afterRequestCode, UserInfo user)
    {
        string note = "";
        string remark = "";

        var request = await _context.Requests
                                    .Where(r => r.RequestId == beforeRequestCode.RequestId)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync();

        // use ExceptBy to get the fields that were added using FieldId as the key
        var beforeFieldIds = beforeRequestCode.Fields.Select(f => f.FieldId).ToList();
        var addedFields = afterFieldIds.Except(beforeFieldIds).ToList();
        var removedFields = beforeFieldIds.Except(afterFieldIds).ToList();

#if false
        if (addedFields.Count() > 0)
        {
            // append the field names to the note
            if (addedFields.Count() > 1)
            {
                note += $"added fields ";
            }
            else
            {
                note += $"added field ";
            }
            note += FieldNameList(addedFields, request.AppType, request.State);
        }
#endif
        /*
        Adding a code – “ <Code> added to <Fields>. Notes:  <notes from what is the issue section>. Suggested Resolution <notes from what is needed to resolve issue>”
        Updating a code – “ <Code> updated for <Fields>. Notes:  <notes from what is the issue section>. Suggested Resolution <notes from what is needed to resolve issue>”
        Clearing a code – “ <Code> cleared. Final Note: <text from final note>”
        */
        if (!string.Equals(beforeRequestCode.Note, afterRequestCode.Note))
        {
            note += $" Notes: {afterRequestCode.Note}.";
        }
        if (!string.Equals(beforeRequestCode.Resolution, afterRequestCode.Resolution))
        {
            note += $" Suggested Resolution: {afterRequestCode.Resolution}.";
        }
        // append the field names to the note that were removed
        if (removedFields.Count() > 0)
        {
            if (removedFields.Count() > 1)
            {
                note += $" Removed fields ";
            }
            else
            {
                note += $" Removed field ";
            }
            note += FieldNameList(removedFields, request.AppType, request.State);
        }
        if (note.Length > 0 || addedFields.Count() > 0)
        {
            note = $"Code {afterRequestCode.TagName} updated for {FieldNameList(afterFieldIds, request.AppType, request.State)}.{note}";
            await AddNoteAndRemark(beforeRequestCode.RequestId, user.UserId, note, remark);
        }
    }

    /*
    Adding a code – “ <Code> added to <Fields>. Notes:  <notes from what is the issue section>. Suggested Resolution <notes from what is needed to resolve issue>”
    Updating a code – “ <Code> updated for <Fields>. Notes:  <notes from what is the issue section>. Suggested Resolution <notes from what is needed to resolve issue>”
    Clearing a code – “ <Code> cleared. Final Note: <text from final note>”
    */
    protected async Task AddNoteAndRemark(RequestCodeOperationType optype, RequestCode requestCode, UserInfo user)
    {
        var request = await _context.Requests
                                    .Where(r => r.RequestId == requestCode.RequestId)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync();

        string note = "";
        string remark = "";

        if (optype == RequestCodeOperationType.Clear)
        {
            note = $"Code {requestCode.TagName} cleared. Final Note: {requestCode.ClearedNote}";
        }
        else if (optype == RequestCodeOperationType.Add)
        {
            var ids = requestCode.Fields.Select(f => f.FieldId).ToList();
            var fieldsText = "";
            if (ids.Count() > 0)
            {
                fieldsText = $" to {FieldNameList(ids, request.AppType, request.State)}";
            }
            note = $"Code {requestCode.TagName} added{fieldsText}. Notes: {requestCode.Note}.  Suggested Resolution: {requestCode.Resolution}";
        }

        await AddNoteAndRemark(requestCode.RequestId, user.UserId, note, remark);
    }

}
