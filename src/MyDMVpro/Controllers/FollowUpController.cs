using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    [Authorize(Policy = "VendorAgentOnly")]
    public class FollowUpController : BaseController
    {
        private FilterHelper<RequestFollowUpStatus> filterHelper;
        private FilterHelper<Tag> tags_filterHelper;

        public FollowUpController(MaggardDMVContext context, IConfiguration configuration, ILogger<FormAnalyzerController> logger) : base(context, configuration, logger)
        {
            filterHelper = new FilterHelper<RequestFollowUpStatus>(context, configuration, this);
            tags_filterHelper = new FilterHelper<Tag>(context, configuration, this);
        }
        public async Task<IActionResult> Index()
        {
            AddPageHeader("Follow-Ups", "");
            return View();
        }
        public async Task<IActionResult> FollowUpCodes()
        {
            AddPageHeader("Follow-Up Codes", "");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Tags()
        {
            var user = await GetCurrentUserAsync();

            var list = await _context.Tag
                                .Where(t => t.VendorId == user.VendorId && t.TagType == "FollowUp")
                                .Select(t => new { id = t.TagId, tag = t.TagName, desc = t.TagDesc, @class = t.TagClass })
                                .ToListAsync();

            return new JsonResult(list);
        }
        [HttpGet]
        public async Task<IActionResult> Contacts()
        {
            var user = await GetCurrentUserAsync();

            var list = _context.FollowUpContacts
                .Join(_context.OrganizationContacts,
                    fc => fc.ContactId,
                    oc => oc.ContactId,
                    (fc, oc) => new { oc.ContactId, oc.ContactName, oc.OrganizationId })
                .Join(_context.Organizations,
                    oc => oc.OrganizationId,
                    org => org.OrganizationId,
                    (oc, org) => new
                    {
                        id = oc.ContactId,
                        tag = $"{oc.ContactName} ({org.OrganizationName})"
                    })
                .Distinct()
                .ToList();

            return new JsonResult(list);
        }
#if false
        [HttpPost]
        public async Task<IActionResult> DeleteTag([FromBody] FollowUpTag_Delete_Model model)
        {
            if (string.IsNullOrWhiteSpace(model?.TagName))
            {
                return NotFound();
            }
            var user = await GetCurrentUserAsync();
            var record = await _context.Tag.Where(t => t.VendorId == user.VendorId && t.TagId == model.TagId && t.TagType == "FollowUp")
                                            .FirstOrDefaultAsync();
            if (record == null)
            {
                return NotFound();
            }
            else
            {
                _context.Tag.Remove(record);
                await _context.SaveChangesAsync(user);
                return JsonSuccess();
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditTag([FromBody] FollowUpTag_Edit_Model model)
        {
            if (string.IsNullOrWhiteSpace(model?.TagName))
            {
                return NotFound();
            }
            var user = await GetCurrentUserAsync();
            var tag = await _context.Tag.Where(t => t.VendorId == user.VendorId && t.TagId == model.TagId && t.TagName == model.TagName).FirstOrDefaultAsync();
            if (tag == null)
            {
                return NotFound();
            }
            else 
            {
                tag.TagDesc = model.TagDesc;
                tag.TagClass = model.TagClass;
                await _context.SaveChangesAsync(user);
            }
            return JsonSuccess();
        }
        [HttpPost]
        public async Task<IActionResult> AddTag([FromBody] FollowUpTag_New_Model model)
        {
            if (string.IsNullOrWhiteSpace(model?.TagName))
            {
                // return error message
                return NotFound();
            }
            var user = await GetCurrentUserAsync();
            var code = model.TagName.ToUpper();
            Tag fup = new Tag()
            {
                VendorId = user.VendorId.Value,
                TagType = "FollowUp",
                TagName = model.TagName,
                TagClass = model.TagClass,
                TagDesc = model.TagDesc
            };
            _context.Tag.Add(fup);
            await _context.SaveChangesAsync(user);
            return JsonSuccess();
        }
#endif
        [HttpPost("/FollowUp/VendorFollowUpsTagged")]
        [HttpPost("/FollowUp/VendorFollowUpsTagged/{tags}/contacts")]
        [HttpPost("/FollowUp/VendorFollowUpsTagged/tags/{contacts}")]
        [HttpPost("/FollowUp/VendorFollowUpsTagged/{tags}/{contacts}")]
        //       [HttpPost("/FollowUp/VendorFollowUpsTagged/{tags}")]
        public async Task<IActionResult> VendorFollowUpsTagged(string tags, string contacts)
        {
            if (tags == "tags")
            {
                tags = null;
            }
            if (contacts == "contacts")
            {
                contacts = null;
            }
            var tagIDs = (tags ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            var contactIds = (contacts ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();

            if (tagIDs.Count == 0 || (tagIDs.Count == 1 && tagIDs[0] == 0))
            {
                tags = null;
            }
            if (contactIds.Count == 0 || (contactIds.Count == 1 && contactIds[0] == Guid.Empty))
            {
                contacts = null;
            }
            if (string.IsNullOrWhiteSpace(tags) && string.IsNullOrWhiteSpace(contacts))
            {
                return await VendorFollowUps(null);
            }

            return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<RequestFollowUpStatus> rows = null;

                if (contactIds.Count > 0 && tagIDs.Count > 0)
                {
                    // TBD: fix to allow filter on tags and contacts
                    rows = _context.FollowUpContacts
                                        .Where(f => contactIds.Contains(f.ContactId))
                                        .GroupBy(fut => fut.FollowUpId)
                                        .Select(g => new { FollowUpId = g.Key })
                                        .Join(_context.RequestFollowUpStatus, oid => oid.FollowUpId, iid => iid.FollowUpId, (oid, iid) => iid);
                }
                else if (contactIds.Count > 0)
                {
                    rows = _context.FollowUpContacts
                                        .Where(f => contactIds.Contains(f.ContactId))
                                        .GroupBy(fut => fut.FollowUpId)
                                        .Select(g => new { FollowUpId = g.Key })
                                        .Join(_context.RequestFollowUpStatus, oid => oid.FollowUpId, iid => iid.FollowUpId, (oid, iid) => iid);
                }
                else if (tagIDs.Count > 0)
                {
                    rows = _context.FollowUpTags
                                    .Where(fut => tagIDs.Contains(fut.TagId))
                                    .GroupBy(fut => fut.FollowUpId)
                                    .Where(g => g.Count() == tagIDs.Count)
                                    .Select(g => new { FollowUpId = g.Key })
                                    .Join(_context.RequestFollowUpStatus, oid => oid.FollowUpId, iid => iid.FollowUpId, (oid, iid) => iid);
                }
                else
                {
                    rows = _context.RequestFollowUpStatus.AsQueryable();
                }
                return rows;
            }, true, false);
        }

        [HttpPost("/FollowUp/VendorFollowForAudit")]
        public async Task<IActionResult> VendorFollowForAudit()
        {
            return await filterHelper.GetRecords((vendorId, groupId, userId) =>
            {
                var query = _context.RequestFollowUpStatus;
                return query.Where(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.FollowUp)
                            .Any() ? query.Where(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.FollowUp)
                                   : query.Where(r => r.CompletedDate == null);
            }, false, false);
        }

        [HttpPost("/FollowUp/VendorFollowUps/{requestId?}")]
        public async Task<IActionResult> VendorFollowUps(Guid? requestId)
        {
            if (requestId != null && requestId == Guid.Empty)
            {
                return EmptyDataTablesQueryResult();
            }
            return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<RequestFollowUpStatus> rows = null;
                rows = _context.RequestFollowUpStatus
                                        .AsNoTracking()
                                        .Where(f => vendorId != null && f.VendorId == vendorId);
                if (requestId != null)
                {
                    rows = rows.Where(r => r.RequestId == requestId);
                }
                else
                {
                    rows = rows.WhereNotCompletedOrRecent();
                }
                return rows;
            }, true, false);
        }
        //public async Task<List<FollowUpCodes>> GetCodes()
        //{
        //    var user = await GetCurrentUserAsync();
        //    return await GetCodesForVendor(user.VendorId.Value).ToListAsync();
        //}
#if false
        [HttpPost]
        public async Task<IActionResult> TagsViewData()
        {
            return await tags_filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                return GetCodesForVendor(vendorId.Value);
            }, true, false);
        }
#endif
        private IQueryable<Tag> GetCodesForVendor(Guid vendorId)
        {
            IQueryable<Tag> rows = null;

            rows = _context.Tag.Where(t => t.VendorId == vendorId && t.TagType == "FollowUp")
                                .AsNoTracking();

            return rows;
        }
        private static bool IsMatch(List<FollowUpIdentifier> list, Guid requestId, int followUpId)
        {
            foreach (var fid in list)
            {
                if (fid.RequestId == requestId && fid.FollowUpId == followUpId)
                {
                    return true;
                }
            }
            return false;
        }
        [HttpPost]
        public async Task<IActionResult> Delete(FollowUp_Delete_Model data)
        {
            if (data == null || data.FollowUpIDs.Count == 0)
            {
                return NotFound();
            }
            var user = await GetCurrentUserAsync();

            await DataHelpers.CheckFeaturePermission(FeatureKey.FOLLOWUPS, user.UserId, delete: true);

            #region Update to handle exception NET6 gives when clientside query execution required
            var ridList = new List<Guid>();
            ridList = data.FollowUpIDs.Select(f => f.RequestId).Distinct().ToList();

            var idList = new List<int>();
            idList = data.FollowUpIDs.Select(f => f.FollowUpId).Distinct().ToList();

            // Remove any unauthorized requests
            var validatedFollowUps = await _context.RequestFollowUpStatus
                                    .Where(ra => ridList.Contains(ra.RequestId) && ra.VendorId == user.VendorId)
                                    .Select(f => f.RequestId)
                                    .Distinct()
                                    .ToListAsync();

            // Since we have excluded all RequestIds that are not authorized, we can now check just the FollowUpId is requested
            // even if the followupId doesn't match the requestid, we'll check in the foreach loop below
            var followUps = await _context.RequestFollowUps
                                    .Where(ra => ridList.Contains(ra.RequestId) && idList.Contains(ra.FollowUpId))
                                    .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var followUp in followUps)
            {
                if (data.FollowUpIDs.Any(f => f.RequestId == followUp.RequestId && f.FollowUpId == followUp.FollowUpId))
                {
                    followUp.Deleted = true;
                    followUp.ModifiedBy = user.UserId;
                    followUp.ModifiedDate = DateTime.UtcNow;
                }
            }
            #endregion
            await _context.SaveChangesAsync(user);
            return JsonSuccess();
        }
        [HttpPost]
        public async Task<IActionResult> Complete([FromBody] FollowUp_Complete_Model completeData)
        {
            if (completeData?.FollowUpIDs == null || completeData.FollowUpIDs.Count == 0)
            {
                return NotFound();
            }

            var user = await GetCurrentUserAsync();

            #region Update to handle exception NET6 gives when clientside query execution required
            var ridList = new List<Guid>();
            ridList = completeData.FollowUpIDs.Select(f => f.RequestId).Distinct().ToList();

            var idList = new List<int>();
            idList = completeData.FollowUpIDs.Select(f => f.FollowUpId).Distinct().ToList();

            // Remove any unauthorized requests
            var validatedFollowUps = await _context.RequestFollowUpStatus
                                    .Where(ra => ridList.Contains(ra.RequestId) && ra.VendorId == user.VendorId)
                                    .Select(f => f.RequestId)
                                    .Distinct()
                                    .ToListAsync();

            // Since we have excluded all RequestIds that are not authorized, we can now check just the FollowUpId is requested
            // even if the followupId doesn't match the requestid, we'll check in the foreach loop below
            var followUps = await _context.RequestFollowUps
                                    .Where(ra => ridList.Contains(ra.RequestId) && idList.Contains(ra.FollowUpId))
                                    .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var followUp in followUps)
            {
                if (completeData.FollowUpIDs.Any(f => f.RequestId == followUp.RequestId && f.FollowUpId == followUp.FollowUpId))
                {
                    followUp.CompletedDate = completeData.CompletedDate;
                    followUp.ModifiedBy = user.UserId;
                    followUp.ModifiedDate = now;
                }
            }
            #endregion

            await _context.SaveChangesAsync(user);
            return JsonSuccess();
        }
        public class EditFollowUp
        {
            public Guid RequestId { get; set; }
            public int FollowUpId { get; set; }
            public string Notes { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? CompletedDate { get; set; }
        }
        [HttpGet("/FollowUp/{followUpId}/{requestId}/")]
        public async Task<IActionResult> Edit(int followUpId, Guid requestId)
        {
            var user = await GetCurrentUserAsync();

            var rfs = await _context.RequestFollowUpStatus
                                    .Where(ra => ra.RequestId == requestId && ra.FollowUpId == followUpId && ra.VendorId == user.VendorId)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(); // can only be one with requestid/followupid
            if (rfs == null || rfs.VendorId != user.VendorId)
            {
                return NotFound();
            }
            var followUp = await _context.RequestFollowUps.Include(r => r.FollowUpTags)
                                    .Where(ra => ra.RequestId == requestId && ra.FollowUpId == followUpId)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync();
            var modusername = "";
            if (followUp != null)
            {
                modusername = await _context.Users.Where(u => u.UserId == followUp.ModifiedBy).Select(u => u.DisplayName).FirstOrDefaultAsync();
            }
            FollowUp_Edit_Model model = new FollowUp_Edit_Model()
            {
                RequestId = rfs.RequestId,
                FollowUpId = rfs.FollowUpId,
                CompletedDate = rfs.CompletedDate,
                DueDate = rfs.DueDate,
                StatusOfRecord = rfs.Notes,
                TagIds = (rfs.TagIds ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList(),
                Tags = (rfs.Tags ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).ToList(),
                ContactAction = rfs.ContactAction,
                ContactIds = (rfs.ContactIds ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList(),
                Contacts = (rfs.Contacts ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).ToList(),
                Title = rfs.Title,
                LastModified = rfs.LastModified,
                LastModifiedBy = modusername,
                CreatedBy = rfs.CreatedBy,
                CreatedDate = rfs.CreatedDate
            };

            return new JsonResult(model, new JsonSerializerOptions()
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] FollowUp_Edit_Model data)
        {
            if (data == null)
            {
                return NotFound();
            }

            var user = await GetCurrentUserAsync();

            var rfs = await _context.RequestFollowUpStatus
                                    .Where(ra => ra.RequestId == data.RequestId && ra.FollowUpId == data.FollowUpId && ra.VendorId == user.VendorId)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(); // can only be one with requestid/followupid
            if (rfs == null || rfs.VendorId != user.VendorId)
            {
                return NotFound();
            }

            var followUp = await _context.RequestFollowUps.Include(f => f.FollowUpTags).Include(f => f.FollowUpContacts).ThenInclude(fc => fc.Contact).ThenInclude(c => c.Organization)
                                    .Where(ra => ra.RequestId == data.RequestId && ra.FollowUpId == data.FollowUpId)
                                    .FirstOrDefaultAsync();

            var now = DateTime.UtcNow;

            if (data.IsCompleted.HasValue)
            {
                if (data.IsCompleted.Value)
                {
                    if (followUp.CompletedDate == null)
                    {
                        followUp.CompletedDate = DateTime.UtcNow;
                    }
                    else
                    {
                        // do not change existing completion date
                        // user can uncheck the completed, save, then edit again to reset
                    }
                }
                else
                {
                    followUp.CompletedDate = null;
                }
            }
            if (data.DueDate != followUp.DueDate)
            {
                // TBD: add a internal remark
            }
            followUp.DueDate = data.DueDate;

            followUp.Title = data.Title;
            followUp.Notes = data.StatusOfRecord;
            followUp.ModifiedBy = user.UserId;
            followUp.ModifiedDate = now;
            followUp.ContactAction = data.ContactAction;

            await AddNoteAndRemark(data.RequestId, user, data.NewNote, data.NewRemark);

            await UpdateFollowUpTags(user, followUp, data);
            await UpdateFollowUpContacts(user, followUp, data);

            await _context.SaveChangesAsync(user);

            return JsonSuccess();
        }
        private async Task UpdateFollowUpContacts(UserInfo user, RequestFollowUps followUp, FollowUp_Edit_Model data)
        {
            var tagIdList = data.ContactIds ?? new List<Guid>();

            // Remove any that are no longer referenced
            foreach (var contact in new List<FollowUpContacts>(followUp.FollowUpContacts))
            {
                if (data.ContactIds.Contains(contact.ContactId))
                {
                    // ignore
                }
                else
                {
                    // remove
                    followUp.FollowUpContacts.Remove(contact);
                }
            }
            foreach (var contactId in tagIdList)
            {
                if (followUp.FollowUpContacts.Any(c => c.ContactId == contactId))
                {
                    // ignore
                }
                else
                {
                    // Add
                    followUp.FollowUpContacts.Add(new FollowUpContacts() { ContactId = contactId, FollowUpId = followUp.FollowUpId });
                }
            }
        }
        private async Task UpdateFollowUpTags(UserInfo user, RequestFollowUps followUp, FollowUp_Edit_Model data)
        {
            var tagLookup = await _context.Tag
                    .Where(t => t.VendorId == user.VendorId && t.TagType == "FollowUp")
                    .Select(t => new { tagName = t.TagName, tagId = t.TagId }).ToListAsync();

            var tagList = data.Tags;
            var tagIdList = data.TagIds;

            if (tagList.Count > 0)
            {
            }
            else if (tagIdList.Count > 0)
            {
                foreach (var tagid in tagIdList)
                {
                    var tag = tagLookup.Where(t => t.tagId == tagid).Select(t => t.tagName).FirstOrDefault();
                    if (tag != null)
                    {
                        if (!tagList.Contains(tag))
                            tagList.Add(tag);
                    }
                }
            }
            // Remove existing tags not in the new list
            List<FollowUpTags> removeList = new();

            foreach (var fut in followUp.FollowUpTags)
            {
                var tag = tagLookup.Where(t => t.tagId == fut.TagId).FirstOrDefault();
                if (!tagList.Contains(tag.tagName))
                {
                    removeList.Add(fut);
                }
                else
                {
                    // remove from list so we don't add again
                    tagList.Remove(tag.tagName);
                }
            }
            foreach (var fut in removeList)
            {
                followUp.FollowUpTags.Remove(fut);
            }
            // Add tags that aren't already attached
            foreach (var tagName in tagList)
            {
                var f = tagLookup.Where(t => t.tagName == tagName).FirstOrDefault();
                if (f != null)
                {
                    followUp.FollowUpTags.Add(new FollowUpTags()
                    {
                        TagId = f.tagId,
                        FollowUpId = followUp.FollowUpId
                    });
                }
                else
                {
                    var newTag = new Tag() { TagName = tagName.ToUpper() };

                    followUp.FollowUpTags.Add(new FollowUpTags()
                    {
                        FollowUpId = followUp.FollowUpId,
                        Tag = new Tag() { TagName = tagName.ToUpper() }
                    });
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FollowUp_Create_Model data)
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

                RequestFollowUps followUp = new RequestFollowUps()
                {
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = user.UserId,
                    RequestId = data.RequestId.Value,
                    DueDate = data.DueDate,
                    CompletedDate = data.CompletedDate,
                    Title = data.Title,
                    Notes = data.StatusOfRecord,
                    ContactAction = data.ContactAction
                };

                await AddNoteAndRemark(req.RequestId, user, data.NewNote, data.NewRemark);

                var tagLookup = await _context.Tag
                        .Where(t => t.VendorId == user.VendorId && t.TagType == "FollowUp")
                        .Select(t => new { tagName = t.TagName, tagId = t.TagId }).ToListAsync();

                foreach (var tagName in data.Tags)
                {
                    var f = tagLookup.Where(t => t.tagName == tagName).FirstOrDefault();
                    if (f != null)
                    {
                        followUp.FollowUpTags.Add(new FollowUpTags()
                        {
                            TagId = f.tagId,
                            FollowUpId = followUp.FollowUpId
                        });
                    }
                    else
                    {
                        var newTag = new Tag() { TagName = tagName.ToUpper() };

                        followUp.FollowUpTags.Add(new FollowUpTags()
                        {
                            FollowUpId = followUp.FollowUpId,
                            Tag = new Tag() { TagName = tagName.ToUpper() }
                        });
                    }
                }
                _context.RequestFollowUps.Add(followUp);

                await _context.SaveChangesAsync(user);
            }
            catch (Exception ex)
            {
                LogError(ex, "Create followup error");
                return JsonError("Error saving record.", ex);
            }
            return JsonSuccess();
        }
        [HttpGet("/FollowUp/NoteHistoryForFollowUp/{requestId}")]
        public async Task<IActionResult> NoteHistoryForFollowUp(Guid requestId)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.UserId != null)
            {
                FollowUp_NotesHistoryModel data = new FollowUp_NotesHistoryModel();

                Requests request = await _context.Requests
                    .Include(r => r.RequestNotes)
                    .Where(r => r.RequestId == requestId)
                    .Where(r => (r.GroupId == ui.GroupId || r.UserId == ui.UserId) || (ui.VendorId == r.VendorId))
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                data.CurrentNotes = request.RequestNotes.OrderByDescending(rn => rn.LastUpdated).ToList();
                data.RequestId = request.RequestId;
                try
                {
                    return PartialView("_followUpNoteHistory", data);
                }
                catch (Exception ex)
                {
                    //TBD: return 
                    LogError(ex, "Error getting note history");
                }
            }
            return Ok();
        }
    }
    public static class IQueryableRequestFollowUpStatusExtension
    {
        public static IQueryable<RequestFollowUpStatus> WhereNotCompletedOrRecent(this IQueryable<RequestFollowUpStatus> followUps, int? days = null, bool include = true)
        {
            if (days == null)
                days = 7;
            DateTime tomorrow = DateTime.Today.AddDays(1);
            DateTime cutoffDate = DateTime.Today.AddDays(-days.Value);
            return followUps.Where(r => ((r.CompletedDate == null || r.CompletedDate > cutoffDate)
                                                || r.DueDate < tomorrow) == include);
        }
    }
}