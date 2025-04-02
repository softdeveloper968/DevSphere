using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using MyDMVpro.Models.RequestsViewModels;
using MyDMVpro.Models.SharedViews;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

public class RequestsController : BaseController
{
    private FilterHelper<RequestCode> filterHelper;
    private FilterHelper<Tag> tags_filterHelper;
    private const string c_TagType = "Request";

    public RequestsController(MaggardDMVContext context, IConfiguration configuration, ILogger<RequestController> logger) : base(context, configuration, logger)
    {
        tags_filterHelper = new FilterHelper<Tag>(context, configuration, this);
        filterHelper = new FilterHelper<RequestCode>(context, configuration, this, FilterBySettings);
    }
#if false
    public IActionResult Index()
    {
        return NotFound();
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var requests = await _context.Requests
            .Include(r => r.Group)
            .Include(r => r.User)
            .FirstOrDefaultAsync(m => m.RequestId == id);
        if (requests == null)
        {
            return NotFound();
        }

        return PartialView("_RequestDetails", GetModelForRequest(requests));
    }
#endif
    [HttpPost]
    public async Task<IActionResult> RemoveAttachment(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        UserInfo user = await GetCurrentUserAsync();

        var att = await _context.RequestAttachments
                                    .Where(ra => ra.AttachmentId == id.Value)
                                    .Select(ra => new RequestAttachments()
                                    {
                                        Id = ra.Id,
                                        AttachmentId = ra.AttachmentId,
                                        RequestId = ra.RequestId,
                                        UploadedBy = ra.UploadedBy,
                                        UploadedByNavigation = ra.UploadedByNavigation
                                    })
                                    .FirstOrDefaultAsync();

        if (att == null)
        {
            return NotFound();
        }

        if (UserHasRequestPermission(user, att.RequestId))
        {
            if (!user.IsVendorAgent)
            {
                if (att?.UploadedByNavigation?.IsVendorAgent ?? false)
                {
                    return BadRequest("Cannot remove attachments uploaded by vendor");
                }
            }
            _context.RequestAttachments.Remove(att);
            await _context.SaveChangesAsync(user);
            return Ok();
        }
        return NotFound();
    }
    [HttpPost]
    public async Task<IActionResult> UploadAttachment(Guid? id,
        IFormFile file,
        string filedesc,
        Guid? attachmentTypeId)
    {
        if (id == null)
        {
            return JsonError("Request not found");
        }

        if (file == null || file.Length == 0)
            return JsonError("File content missing");

        try
        {
            (bool isMember, UserInfo user) memberInfo = new();

            var requests = await _context.Requests.FirstOrDefaultAsync(m => m.RequestId == id);
            if (requests != null)
            {
                memberInfo = await CurrentUserIsMemberOfGroupOrVendorAsync(requests.GroupId, requests.VendorId);
            }
            if (requests == null || !memberInfo.isMember)
            {
                return JsonError("Request not found");
            }

            UserInfo user = memberInfo.user;
            // find the id of the extra attachment type
            var extraTypeId = _context.MdpAttachmentTypes
                                            .Where(a => a.Name == "Extra")
                                            .Select(a => a.AttachmentTypeId)
                                            .FirstOrDefault();
            // Any attachments having the same attachmenttypeid must be reassigned to the extra type
            var previousRequestsWithType = _context.RequestAttachments
                                                            .Where(r => r.RequestId == id && r.AttachmentTypeId == attachmentTypeId)
                                                            // get just the attachmentid and attachmenttypeid
                                                            // as we are only updating the typeid
                                                            .Select(r => new RequestAttachments()
                                                            {
                                                                AttachmentId = r.AttachmentId,
                                                                AttachmentTypeId = r.AttachmentTypeId
                                                            })
                                                            .ToList();
            foreach (var prev in previousRequestsWithType)
            {
                if (prev.AttachmentTypeId != extraTypeId)
                {
                    _context.RequestAttachments.Attach(prev);
                    prev.AttachmentTypeId = extraTypeId;
                }
            }

            var filedata = MyDMVpro.Common.FileHelpers.ProcessBinaryFormFile(file, ModelState);
            var filename = System.IO.Path.GetFileName(file.FileName);
            RequestAttachments attachment = new()
            {
                Filename = filename,
                Description = filedesc,
                UploadedBy = user.UserId,
                Image = filedata,
                DateAdded = DateTime.UtcNow,
                AttachmentTypeId = attachmentTypeId
            };
            requests.RequestAttachments.Add(attachment);

            if (_configuration.GetValue<bool>("AppSettings:AddChatOnAttachmentUpload", false))
            {
                if (!user.IsVendorAgent)
                {
                    var attachmentType = await _context.MdpAttachmentTypes.SingleOrDefaultAsync(x => x.AttachmentTypeId == attachmentTypeId);

                    string message = $"Uploaded attachment '{filename}'";
                    if (attachmentType != null)
                        message = $"Uploaded attachment (Type: {attachmentType?.Name}) '{filename}'";
                    await AddChat(requests.RequestId, user, message, false);
                }
            }
            if (_configuration.GetValue<bool>("AppSettings:AddChatOnAttachmentUploadByVendor", true))
            {
                if (user.IsVendorAgent)
                {
                    //var attachmentType = _context.MdpAttachmentTypes.SingleOrDefault(x => x.AttachmentTypeId == attachmentTypeId);

                    string message = $"Uploaded attachment '{filename}'. You can access the record attachments by clicking the paperclip icon from any list view. Thank you!";
                    // do not specify the attachmen type for vendor uploads
                    //if (attachmentType != null)
                    //    message = $"Uploaded attachment (Type: {attachmentType?.Name}) '{filename}'";
                    await AddChat(requests.RequestId, user, message, false);
                }
            }
            await _context.SaveChangesAsync(user);
            return JsonSuccess();
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestsController.UploadAttachment");
            return JsonError(ex);
        }
    }
    public async Task<IActionResult> GetAttachment(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            UserInfo user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }

            var attachment = await _context.RequestAttachments
                .Include(a => a.Request)
                .Where(a => a.AttachmentId == id)
                .Select(a => new { a.RequestId, a.Request.VendorId, a.Request.UserId, a.Request.GroupId, a.ContainsPII, a.Filename, a.Image })
                .FirstOrDefaultAsync();

            if (attachment == null)
            {
                return NotFound();
            }

            if (!UserIsMemberOfGroupOrVendor(user, attachment.GroupId, attachment.VendorId))
            {
                return NotFound();
            }
            // Block documents containing PII to submitter and admins
            if ((attachment.ContainsPII == null || attachment.ContainsPII == false)
                || (user.UserId == attachment.UserId || user.IsGroupAdmin)
                || (user.VendorId == attachment.VendorId))
            {
                //TODO: LOG ACCESS TO FILE
                return ReturnFileWithDisposition(attachment.Filename, attachment.Image);
            }
            return Ok();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            throw;
        }
    }
    private IQueryable<Requests> RequestsFilteredByUser(UserInfo user)
    {
        IQueryable<Requests> requests;
        if (user == null || user.IsVendorAgent)
        {
            requests = _context.Requests.Where(r => user != null && r.VendorId == user.VendorId);
        }
        else
        {
            requests = _context.Requests.Where(r => user != null && r.GroupId == user.GroupId);
        }
        return requests;
    }
    private IQueryable<RequestAttachments> RequestAttachmentsFilteredByUser(UserInfo user)
    {
        IQueryable<RequestAttachments> attachments;
        if (user == null || user.IsVendorAgent)
        {
            try
            {
                attachments = _context.RequestAttachments
                                            .Where(r => user != null && r.Request.VendorId == user.VendorId)
                                            .Include(r => r.AttachmentType);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        else
        {
            attachments = _context.RequestAttachments
                                            .Where(r => user != null && r.Request.GroupId == user.GroupId)
                                            .Include(r => r.AttachmentType);
        }
        return attachments;
    }

    private IActionResult ReturnFileWithDisposition(string filename, byte[] data)
    {
        string contentType = FileHelpers.GetContentType(filename);
        System.Net.Mime.ContentDisposition cd = new()
        {
            FileName = filename
        };
        Response.Headers.Add("Content-Disposition", cd.ToString());
        return File(data, contentType);
    }
    public async Task<IActionResult> AttachmentStatus(Guid? id)
    {
        int status = AttachmentStatusEnum.NoConditionsRequired; // default of no conditions required
        if (id != null)
        {
            var result = from p in _context.Requests
                         where p.RequestId == id
                         select _context.GetAttachmentStatus(id.Value);
            status = await result.FirstOrDefaultAsync();
        }
        return Json(new
        {
            attachmentStatus = status
        });
    }
    private Guid? GetAttachmentid(dynamic jRequest, string excelName)
    {
        try
        {
            string gs = jRequest[excelName];
            if (string.IsNullOrWhiteSpace(gs))
            {
                return null;
            }
            Guid g = new Guid(gs);
            return g;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    [HttpPost]
    public async Task<IActionResult> ChangeType([FromForm] Guid? attachmentId, [FromForm] Guid? attachmentTypeId)
    {
        if (attachmentId == null)
        {
            return NotFound();
        }

        try
        {
            var user = await GetCurrentUserAsync();

            var attachment = await _context.RequestAttachments
                                                    .Include(x => x.Request)
                                                    .Where(x => x.Request.VendorId == user.VendorId || x.Request.GroupId == user.GroupId)
                                                    .Where(r => r.AttachmentId == attachmentId)
                                                    .Select(r => new RequestAttachments()
                                                    {
                                                        AttachmentId = r.AttachmentId,
                                                        AttachmentTypeId = r.AttachmentTypeId,
                                                        ReviewBy = r.ReviewBy,
                                                        ReviewDate = r.ReviewDate,
                                                        Approved = r.Approved
                                                    })
                                                    .FirstOrDefaultAsync();
            if (attachment == null)
            {
                return JsonError("Attachment not found");
            }
            if (attachment.AttachmentTypeId != attachmentTypeId)
            {
                // If there is a previous attachment with the same attachment type, move it back to Extra
                var previousAttachment = await _context.RequestAttachments
                                                        .Where(r => r.RequestId == attachment.RequestId && r.AttachmentTypeId == attachmentTypeId)
                                                        .Select(r => new RequestAttachments()
                                                        {
                                                            AttachmentId = r.AttachmentId,
                                                            AttachmentTypeId = r.AttachmentTypeId,
                                                            ReviewBy = r.ReviewBy,
                                                            ReviewDate = r.ReviewDate,
                                                            Approved = r.Approved
                                                        })
                                                        .FirstOrDefaultAsync();
                if (previousAttachment != null)
                {
                    _context.RequestAttachments.Attach(previousAttachment);
                    previousAttachment.AttachmentTypeId = null; // Move back to Extra
                    previousAttachment.ReviewBy = null;
                    previousAttachment.ReviewDate = null;
                    previousAttachment.Approved = null;
                }
                _context.RequestAttachments.Attach(attachment);
                attachment.AttachmentTypeId = attachmentTypeId;
                attachment.ReviewBy = null;
                attachment.ReviewDate = null;
                attachment.Approved = null;
                await _context.SaveChangesAsync(user);
            }

            return JsonSuccess();
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestsController.ChangeType");
            return JsonError(ex);
        }
    }

    [HttpGet]
    [Route("Requests/ChangeTypes/{attachmentId}")]
    public async Task<IActionResult> ChangeTypes(Guid? attachmentId)
    {
        if (attachmentId == null)
        {
            return NotFound();
        }

        var attachment = await _context.RequestAttachments
                                            .Include(x => x.AttachmentType)
                                            .Include(x => x.Request)
                                            .Where(x => x.AttachmentId == attachmentId)
                                            .Select(x => new RequestAttachments()
                                            {
                                                AttachmentId = x.AttachmentId,
                                                AttachmentTypeId = x.AttachmentTypeId,
                                                RequestId = x.RequestId,
                                                AttachmentType = x.AttachmentType,
                                                Request = x.Request
                                            })
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync();
        var request = attachment.Request;
        var appTypeState = await _context.MdpAppTypeStates.SingleOrDefaultAsync(x => x.AppState == request.State && x.AppType.AppType == request.AppType);
        if (appTypeState == null)
        {
            return BadRequest();
        }
        UserInfo user = await GetCurrentUserAsync();
        Guid? id = attachment.RequestId;

        try
        {
            // Get all attachment types for the current application type/state
            var availableAttachmentTypes = await _context.MdpAppTypeAttachmentTypes
                                                            .Include(x => x.AttachmentType)
                                                            .Where(x => x.AppTypeStateId == appTypeState.AppTypeStateId)
                                                            .Where(x => !x.AttachmentType.InternalFromVendor && !x.AttachmentType.InternalFromClient && !x.AttachmentType.HardcopyOnly)
                                                            .ToListAsync();

            var extraAttachmentOption = await _context.MdpAttachmentTypes.SingleOrDefaultAsync(x => x.Name == "Extra");

            List<MdpAttachmentTypes> requestAttachmentTypes = availableAttachmentTypes
                                                                    .Where(x => x.AttachmentType != null)
                                                                    .Select(x => x.AttachmentType)
                                                                    .ToList();

            List<ApprovalStatus> uploadedAttachmentTypeIds = await _context.RequestAttachments
                                                                    .Where(x => x.RequestId == request.RequestId)
                                                                    //.Where(x => x.AttachmentTypeId != null)
                                                                    //.Where(x => x.ReviewDate != null && x.Approved == true)
                                                                    .Select(x => new ApprovalStatus()
                                                                    {
                                                                        AttachmentTypeId = x.AttachmentTypeId ?? extraAttachmentOption.AttachmentTypeId,
                                                                        Approved = x.Approved
                                                                    })
                                                                    .Distinct()
                                                                    .ToListAsync();

            if (extraAttachmentOption != null)
            {
                requestAttachmentTypes.Add(extraAttachmentOption);
            }

            // This should already have been returned in the attachments query
            //attachments.ForEach(x => x.AttachmentType = requestAttachmentTypes.SingleOrDefault(y => y.AttachmentTypeId == x.AttachmentTypeId));

            AttachmentChangeTypeModel attachmentsViewModel = new()
            {
                RequestId = request.RequestId,
                RequestNo = request.Id,
                IsVendorAgent = user.IsVendorAgent,
                UploadedAttachmentTypeIds = uploadedAttachmentTypeIds,
                AttachmentTypes = requestAttachmentTypes,
                SelectedAttachmentTypeId = attachment.AttachmentTypeId
            };

            return PartialView("_attachmentChangeTypes", attachmentsViewModel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("{0}", ex);
            throw;
        }
    }
    public async Task<IActionResult> Attachments(Guid? id)
    {
#if DEBUG
        // test id before upload completed
        //            id = new Guid("B453B5E1-36C4-42D0-A3D4-B38AA9FE503C");
#endif
        if (id == null)
        {
            return NotFound();
        }

        var request = await _context.Requests.SingleOrDefaultAsync(x => x.RequestId == id);
        if (request == null)
        {
            return BadRequest();
        }
        var appTypeState = await _context.MdpAppTypeStates.SingleOrDefaultAsync(x => x.AppState == request.State && x.AppType.AppType == request.AppType);
        if (appTypeState == null)
        {
            return BadRequest();
        }
        UserInfo user = await GetCurrentUserAsync();

        var attachments = await RequestAttachmentsFilteredByUser(user)
                        .Where(m => m.RequestId == id)
                        // do select here to avoid including the image
                        .Select(a => new RequestAttachments()
                        {
                            AttachmentId = a.AttachmentId,
                            Description = a.Description,
                            Filename = a.Filename,
                            UploadedByNavigation = a.UploadedByNavigation,
                            AttachmentTypeId = a.AttachmentTypeId,
                            AttachmentType = a.AttachmentType,
                            RequestId = a.RequestId,
                            //AppTypeAttachmentTypeId = a.AppTypeAttachmentTypeId,
                            //AppTypeAttachmentType = a.AppTypeAttachmentType,
                            HardcopyReceived = a.HardcopyReceived,
                            ReviewBy = a.ReviewBy,
                            ReviewDate = a.ReviewDate,
                            Approved = a.Approved,
                            MD5Hash = a.MD5Hash /* necessary to determine if HardcopyOnly attachment has been uploaded */
                        })
                        .AsNoTracking()
                        .ToListAsync();

        attachments = attachments
                    .OrderBy(a => string.IsNullOrEmpty(a.Filename) &&
                     (a.MD5Hash == null || a.MD5Hash.Length == 0) ? 0 : 1)
                    .ToList();


        List<InternalAttachment> internalAttachments = new();

        if (user.IsVendorAgent)
        {
            List<GroupAttachment> groupAttachments = await _context.GroupAttachments.Where(a => a.GroupId == request.GroupId && a.AttachmentTypeId != null).ToListAsync();
            List<VendorAttachment> vendorAttachments = await _context.VendorAttachments.Where(a => a.VendorId == user.VendorId).ToListAsync();
            var jsonRequestObject = JsonConvert.DeserializeObject<dynamic>(request.JRequest);

            // load internal group and vendor attachments
            var att = await _context.MdpAppTypeAttachmentTypes
                                                .Include(x => x.AttachmentType)
                                                .Include(x => x.AppTypeState)
                                                .ThenInclude(x => x.AppType)
                                                .Where(x => x.AttachmentType.InternalFromVendor || x.AttachmentType.InternalFromClient)
                                                .Where(x => x.AppTypeState.AppType.AppType == request.AppType && x.AppTypeState.AppState == request.State)
                                                .Select(ga => new InternalAttachment()
                                                {
                                                    ExcelName = ga.AttachmentType.ExcelName,
                                                    InternalFromClient = ga.AttachmentType.InternalFromClient,
                                                    Description = ga.Description,
                                                    AttachmentTypeId = ga.AttachmentTypeId,
                                                    AttachmentTypeName = ga.AttachmentType.Name
                                                })
                                                .ToListAsync();
            // iterate att and extract the attachmentid from the jRequest
            foreach (var a in att)
            {
                a.AttachmentId = GetAttachmentid(jsonRequestObject, a.ExcelName);
                if (a.AttachmentId != null)
                {
                    if (a.InternalFromClient)
                    {
                        a.DisplayName = groupAttachments.SingleOrDefault(x => x.GroupAttachmentId == a.AttachmentId)?.DisplayName;
                    }
                    else
                    {
                        a.DisplayName = vendorAttachments.SingleOrDefault(x => x.VendorAttachmentId == a.AttachmentId)?.DisplayName;
                    }
                    a.DisplayName ??= "(selection no longer available)";
                }
                else
                {
                    a.DisplayName = "(not selected)";
                }
            }
            internalAttachments = att;
        }

        if (attachments == null)
        {
            return NotFound();
        }
        try
        {
            // Get all attachment types for the current application type/state
            var availableAttachmentTypes = await _context.MdpAppTypeAttachmentTypes
                                                            .Include(x => x.AttachmentType)
                                                            .Where(x => x.AppTypeStateId == appTypeState.AppTypeStateId)
                                                            .Where(x => !x.AttachmentType.InternalFromVendor && !x.AttachmentType.InternalFromClient)
                                                            .OrderBy(x => x.SortOrder)
                                                            .ToListAsync();
            var extraAttachmentOption = await _context.MdpAttachmentTypes.SingleOrDefaultAsync(x => x.Name == "Extra");

            List<MdpAttachmentTypes> requestAttachmentTypes = availableAttachmentTypes
                                                                    .Where(x => x.AttachmentType != null)
                                                                    .Select(x => x.AttachmentType)
                                                                    .ToList();

            if (extraAttachmentOption != null)
            {
                requestAttachmentTypes.Add(extraAttachmentOption);
            }

            var miscAttachmentConditions = await _context.RequestAttachmentConditions.Include(a => a.AttachmentType)
                                                            .Where(r => r.RequestId == id)
                                                            .OrderBy(a => a.AttachmentType.Name)
                                                            .ToListAsync();

            foreach (var condition in miscAttachmentConditions)
            {
                //// Add types that should be in the add attachment list
                MdpAppTypeAttachmentTypes atype1 = new MdpAppTypeAttachmentTypes()
                {
                    AttachmentType = condition.AttachmentType,
                    AttachmentTypeId = condition.AttachmentTypeId,
                    Description = condition.AttachmentComment,
                    IsRequired = true,
                    IsSpecialRequirement = true,
                    SpecialRequirementComment = condition.AttachmentComment
                };
                availableAttachmentTypes.Add(atype1);

                // Add to request attachment types
                MdpAttachmentTypes atype2 = new MdpAttachmentTypes()
                {
                    AttachmentTypeId = condition.AttachmentTypeId,
                    Description = condition.AttachmentType.Description,
                    Name = condition.AttachmentType.Name,
                    ReviewTextLine1 = condition.AttachmentComment
                };
                requestAttachmentTypes.Add(atype2);
            }
            var miscAttachmentTypes = await _context.MdpAttachmentTypes.Where(x => x.ExcludeFromOneOffs == null || x.ExcludeFromOneOffs == false).OrderBy(x => x.Name).ToListAsync();

            // This should already have been returned in the attachments query
            //attachments.ForEach(x => x.AttachmentType = requestAttachmentTypes.SingleOrDefault(y => y.AttachmentTypeId == x.AttachmentTypeId));
            var insDocuments = await _context.PdfTemplates.Where(x => x.AppTypeStateId == appTypeState.AppTypeStateId)
                                                            .WhereClientVisible()
                                                            .OrderByClientSortOrder()
                                                            .Select(x => new RequestAttachments
                                                            {
                                                                AttachmentId = x.TemplateId,
                                                                Filename = x.DisplayName,
                                                                Description = x.Notes,
                                                            })
                                                            .FirstOrDefaultAsync();

            AttachmentsViewModel attachmentsViewModel = new()
            {
                RequestId = id.Value,
                RequestNo = request.Id,
                IsVendorAgent = user.IsVendorAgent,
                Attachments = attachments,
                AppTypeAttachmentTypes = availableAttachmentTypes,
                AttachmentTypes = requestAttachmentTypes,
                MiscAttachmentTypes = miscAttachmentTypes,
                AttachmentNotes = appTypeState.AttachmentNotes,
                InternalAttachments = internalAttachments,
                Request = request,
                InstructionPacket = insDocuments
            };

            return PartialView("_Attachments", attachmentsViewModel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("{0}", ex);
            throw;
        }
    }
    public async Task UpdateRequestAttachmentTypeFromRequest(List<Guid> attachmentIds, List<Guid> requestAttachmentTypeIds)
    {
        var user = await GetCurrentUserAsync();

        // TBD: check user permissions
        for (int i = 0; i < attachmentIds.Count; i++)
        {
            var attachmentId = attachmentIds[i];
            var attachmentTypeId = requestAttachmentTypeIds[i];

            // Query only what we need to verify user permissions and update the attachment type without reading the attachment image
            var requestAttachment = await _context.RequestAttachments
                                                        .Include(x => x.Request)
                                                        .Where(x => x.Request.VendorId == user.VendorId || x.Request.GroupId == user.GroupId)
                                                        .Where(x => x.AttachmentId == attachmentId)
                                                        .Select(x => new RequestAttachments()
                                                        {
                                                            AttachmentId = x.AttachmentId,
                                                            AttachmentTypeId = x.AttachmentTypeId,
                                                            RequestId = x.RequestId
                                                        })
                                                        .FirstOrDefaultAsync();
            if (requestAttachment != null)
            {
                // update just the attachmentTypeId
                var attachment = new RequestAttachments()
                {
                    AttachmentId = attachmentId,
                    AttachmentTypeId = attachmentTypeId
                };
                _context.RequestAttachments.Attach(attachment);
                _context.Entry(attachment).Property(r => r.AttachmentTypeId).IsModified = true;
                await _context.SaveChangesAsync(await GetCurrentUserAsync());
            }
        }
    }
    public async Task UpdateAttachmentType(Guid requestId, Guid attachmentId, Guid newAttachmentTypeId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var attachment = await _context.RequestAttachments
                                        .Where(x => x.Request.VendorId == user.VendorId || x.Request.GroupId == user.GroupId)
                                        .Where(x => x.AttachmentId == attachmentId)
                                        .Select(x => new RequestAttachments()
                                        {
                                            AttachmentId = x.AttachmentId,
                                            AttachmentTypeId = x.AttachmentTypeId,
                                            ReviewBy = x.ReviewBy,
                                            ReviewDate = x.ReviewDate,
                                            Approved = x.Approved
                                        })
                                        .FirstOrDefaultAsync();
        if (attachment != null)
        {
            _context.RequestAttachments.Attach(attachment);
            // Reset the review status when type is changing
            if (attachment.AttachmentTypeId != newAttachmentTypeId)
            {
                attachment.AttachmentTypeId = newAttachmentTypeId;

                attachment.ReviewBy = null;
                attachment.ReviewDate = null;
                attachment.Approved = null;
                await _context.SaveChangesAsync(await GetCurrentUserAsync());
            }
        }
    }

    [Authorize("VendorAgentOnly")]
    public async Task<IActionResult> UpdateHardcopyReceived(Guid? requestId, Guid? attachmentId, Guid? attachmentTypeId, bool received)
    {
        try
        {
            var user = await GetCurrentUserAsync();

            if (requestId == null)
                throw new ArgumentNullException(nameof(requestId));

            if (attachmentId == null && attachmentTypeId == null)
                throw new ArgumentNullException($"{nameof(attachmentId)} or {nameof(attachmentTypeId)} is required");

            var request = await _context.Requests.Where(r => r.RequestId == requestId).SingleOrDefaultAsync();
            if (request == null)
                return new BadRequestResult();

            var attachment = await _context.RequestAttachments
                                                    .Where(x => x.AttachmentId == attachmentId)
                                                    .Select(x => new RequestAttachments()
                                                    {
                                                        AttachmentId = x.AttachmentId,
                                                        RequestId = x.RequestId,
                                                        AttachmentTypeId = x.AttachmentTypeId,
                                                        HardcopyReceived = x.HardcopyReceived
                                                    })
                                                    .FirstOrDefaultAsync();

            if (attachment == null && received == false)
            {
                // nothing to do, no need to create an attachment yet
                return Ok();
            }
            else if (attachment == null && received == true)
            {
                // Must create an attachment so we can save it as received
                attachment = new RequestAttachments()
                {
                    RequestId = requestId,
                    AttachmentTypeId = attachmentTypeId,
                    HardcopyReceived = received,
                    UploadedBy = user.UserId,
                    DateAdded = DateTime.UtcNow,
                };
                _context.RequestAttachments.Add(attachment);
            }
            //else if (attachment != null && received == false && attachment.MD5Hash != null && attachment.MD5Hash.Length > 0)
            //{
            //    // TBD: handle breaking the link to the hardcopy attachment 
            //}
            else if (attachment != null)
            {
                _context.RequestAttachments.Attach(attachment);
                attachment.HardcopyReceived = received;
                _context.Entry(attachment).Property(r => r.HardcopyReceived).IsModified = true;
            }
            await _context.SaveChangesAsync(await GetCurrentUserAsync());
            return Ok();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("{0}", ex);
            throw;
        }
    }
#if NO_LONGER_USED
    public async Task<IActionResult> Notices(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var requests = await _context.Requests
            .Include(r => r.Group)
            .Include(r => r.User)
            .FirstOrDefaultAsync(m => m.RequestId == id);
        if (requests == null)
        {
            return NotFound();
        }
        NoticesView viewModel = new NoticesView()
        {
            Request = GetModelForRequest(requests),
            Dmvoffices = _context.Dmvoffice.OrderBy(d => d.Commissioner).ToList(),
            PoliceAgencies = _context.PoliceAgency.OrderBy(p => p.Agency).ToList()
        };
        try
        {
            return PartialView("_Notices", viewModel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("{0}", ex);
            throw;
        }
    }
#endif
    private bool ValidateModelForProperties(string[] properties)
    {
        foreach (string property in properties)
        {
            if (ModelState.GetValidationState(property) != Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid)
            {
                return false;
            }
        }
        return true;
    }

    [HttpPost]
    public async Task<IActionResult> UpdateNotices(NoticesView rew)
    //[Bind("Request.RequestId", "Request.DmvDistrictOfficeNotified", "Request.DmvNotificationDate", "Request.OtherNotifiedName", "Request.OtherNotifiedDate", "Request.PoliceAgencyNotified", "Request.PoliceNotificationDate")] NoticesView rew)
    {
        //if (ModelState.IsValid)
        if (ValidateModelForProperties(new string[]
            {
                "Request.RequestId",
                "Request.DmvDistrictOfficeNotified",
                //"Request.DmvNotificationDate",
                "Request.OtherNotifiedName",
                //"Request.OtherNotifiedDate",
                "Request.PoliceAgencyNotified"
                //"Request.PoliceNotificationDate"
            }))
        {
            try
            {
                var requests = await _context.Requests
                    //.Include(r => r.Group)
                    //.Include(r => r.User)
                    .FirstOrDefaultAsync(m => m.RequestId == rew.Request.RequestId);
                if (requests == null)
                {
                    return NotFound();
                }
                // Unable to validate DeserializeObject<dynamic> 
                // UpdateNotices appears no longer to be used
                dynamic jRequest = JsonConvert.DeserializeObject<dynamic>(requests.JRequest);
                jRequest["DMV District Office Notified"] = rew.Request.DmvDistrictOfficeNotified;
                jRequest["DMV Notification Date"] = rew.Request.DmvNotificationDate;
                jRequest["Other Notified Name"] = rew.Request.OtherNotifiedName;
                jRequest["Other Notified Date"] = rew.Request.OtherNotifiedDate;
                jRequest["Police Agency Notified"] = rew.Request.PoliceAgencyNotified;
                jRequest["Police Notification Date"] = rew.Request.PoliceNotificationDate;
                requests.JRequest = JsonConvert.SerializeObject(jRequest);
                await _context.SaveChangesAsync(await GetCurrentUserAsync());
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RequestsExists(rew.Request.RequestId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok();
        }
        return NoContent();
    }
    private static DateTime? ToDateTime(dynamic oDate)
    {
        if (oDate is DateTime time)
        {
            return time;
        }
        if (oDate.Value is DateTime time1)
        {
            return time1;
        }
        return ToDateTime(oDate.ToString());
    }
    private static DateTime? ToDateTime(string sDate)
    {
        if (!string.IsNullOrEmpty(sDate))
        {
            if (DateTime.TryParse(sDate, out DateTime dt))
            {
                return dt;
            }
        }
        return null;
    }
    public IActionResult Create()
    {
        return NotFound();
    }
#if false
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var requests = await _context.FindAsync(id);
        if (requests == null)
        {
            return NotFound();
        }
        ViewData["GroupId"] = new SelectList(_context.Groups, "GroupId", "GroupName", requests.GroupId);
        ViewData["Id"] = new SelectList(_context.Requests, "Id", "Id", requests.Id);
        ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserPrincipalName", requests.UserId);
        return View(requests);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(long id, [Bind("Id,RequestId,JRequest,GroupId,UserId,FileUploadId,Vin,Last6Vin,State")] Requests requests)
    {
        if (id != requests.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                //_context.Update<Requests>(requests, "JRequest");
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RequestsExists(requests.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["GroupId"] = new SelectList(_context.Groups, "GroupId", "GroupName", requests.GroupId);
        ViewData["Id"] = new SelectList(_context.Requests, "Id", "Id", requests.Id);
        ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserPrincipalName", requests.UserId);
        return View(requests);
    }
#endif

    [HttpPost]
    [RequestFormLimits(ValueCountLimit = 6000)]
    public async Task<IActionResult> Export([FromForm] string[] ids)
    {
        UserInfo ci = await GetCurrentUserAsync();
        if (ci == null || (ci.VendorId == null && ci.GroupId == null))
            return NotFound();
        try
        {
            List<Guid> list = new();
            foreach (string id in ids)
            {
                list.Add(new Guid(id));
            }
            string data = await DataHelpers.ExportRequests(ci.VendorId.Value, ci.UserId.Value, ci.GroupId, list);
            DateTime now = ServerDateTime();
            string filename = $"myDMVpro-export-{now:yyyy-MM-dd-HHmmss}.csv";
            return ReturnCsvFile(filename, data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("{0}", ex);
            return NotFound();
        }
    }
    [HttpPost]
    [RequestFormLimits(ValueCountLimit = 6000)]
    public async Task<IActionResult> UniversalExport([FromForm] string[] ids)
    {
        UserInfo ci = await GetCurrentUserAsync();
        if (ci == null || (ci.VendorId == null && ci.GroupId == null))
            return NotFound();
        try
        {
            List<Guid> list = new();
            foreach (string id in ids)
            {
                list.Add(new Guid(id));
            }
            string data = await DataHelpers.UniversalExportRequests(ci.VendorId.Value, ci.UserId.Value, ci.GroupId, list);
            DateTime now = ServerDateTime();
            string filename = $"myDMVpro-export-{now:yyyy-MM-dd-HHmmss}.csv";
            return ReturnCsvFile(filename, data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("{0}", ex);
            return NotFound();
        }
    }
    private IActionResult ReturnCsvFile(string filename, string fileContent)
    {
        System.Net.Mime.ContentDisposition cd = new()
        {
            FileName = filename
        };
        Response.Headers.Add("Content-Disposition", cd.ToString());
        byte[] file = System.Text.UTF8Encoding.UTF8.GetBytes(fileContent);
        return File(file, "text/csv");
    }
#if false
    public void Test()
    {

        var requests = _context.RequestStatus.ToList();

        foreach (var request in requests)
        {
            Console.WriteLine($"{request.Vin}");
            Console.WriteLine();
        }
    }
#endif
    private bool RequestsExists(long id)
    {
        return _context.Requests.Any(e => e.Id == id);
    }
    private bool RequestsExists(Guid id)
    {
        return _context.Requests.Any(e => e.RequestId == id);
    }
#if false
    public IActionResult Purge()
    {
        UserInfo ui = GetCurrentUser();
        if (ui.IsGroupAdmin)
        {
            if (ui.GroupId != null)
            {
                MyDMVpro.Models.RequestsViewModels.PurgeDataViewModel model = new Models.RequestsViewModels.PurgeDataViewModel();
                model.id = ui.GroupId.Value;
                return View(model);
            }
        }
        return NotFound();
    }

    [HttpPost, ActionName("Purge")]
    public async Task<IActionResult> PurgeConfirmed([Bind("id,Email")]MyDMVpro.Models.RequestsViewModels.PurgeDataViewModel model)
    {
        if (ModelState.IsValid)
        {
            UserInfo ui = GetCurrentUser();
            if (ui.IsGroupAdmin)
            {
                if (ui.GroupId != null && model.id == ui.GroupId.Value)
                {
                    if (ui.Email.Equals(model.Email, StringComparison.InvariantCultureIgnoreCase))
                    {
                        await DataHelpers.PurgeLienholderData(ui.GroupId, ui.UserId);
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
        }
        return NotFound();
    }
#endif
    public async Task<IActionResult> ViewNotes(Guid id)
    {
        UserInfo ui = await GetCurrentUserAsync();
        if (ui.UserId != null)
        {
            MyDMVpro.Models.RequestsViewModels.EditNotesModel data = new();

            Requests request = await _context.Requests
                .Include(r => r.RequestNotes).ThenInclude(x => x.ModifiedByNavigation)
                .Where(r => r.RequestId == id && (r.GroupId == ui.GroupId || r.UserId == ui.UserId))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            data.CurrentNotes = request.RequestNotes.OrderByDescending(rn => rn.LastUpdated).ToList();
            data.RequestId = request.RequestId;
            if (Request.Query.ContainsKey("v"))
            {
                string sVal = Request.Query["v"][0];
                data.StandaloneForm = (sVal == "s");
            }
            ViewBag.HelpLink = await GetHelpLink(request, ui.IsVendorAgent);
            return PartialView("_ViewNotes", data);
        }
        return Ok();
    }
    public async Task<IActionResult> ViewClientInternalNotes(Guid id)
    {
        UserInfo ui = await GetCurrentUserAsync();
        if (ui.UserId != null)
        {
            MyDMVpro.Models.RequestsViewModels.EditNotesModel data = new();

            Requests request = await _context.Requests
                .Include(r => r.RequestNotes).ThenInclude(x => x.ModifiedByNavigation)
                .Where(r => r.RequestId == id && (r.GroupId == ui.GroupId || r.UserId == ui.UserId))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            data.CurrentNotes = request.RequestNotes.OrderByDescending(rn => rn.LastUpdated).ToList();
            data.RequestId = request.RequestId;
            if (Request.Query.ContainsKey("v"))
            {
                string sVal = Request.Query["v"][0];
                data.StandaloneForm = (sVal == "s");
            }
            ViewBag.HelpLink = await GetHelpLink(request, ui.IsVendorAgent);
            return PartialView("_ClientRemarks", data.CurrentNotes);
        }
        return Ok();
    }
    private async Task<string> GetHelpLink(Requests request, bool isVendorAgent)
    {
        var hasHelp = await _context.MdpAppTypeStates
                                        .Include(a => a.AppType)
                                        .Where(a => a.AppType.AppType == request.AppType && a.AppState == request.State)
                                        .Where(a => (isVendorAgent && a.InternalHelpID != null)
                                                    || (!isVendorAgent && a.PublicHelpID != null))
                                        .AnyAsync();
        string link = null;
        if (hasHelp)
        {
            link = $"/help/{request.AppType}/{request.State}";
        }
        return link;
    }
    private string StageName(int? stageId)
    {
        return GlobalHelper.ProcessStageName(stageId);
    }
    private string StatusName(int? statusid)
    {
        if (statusid == null) return "blank";
        return Enum.GetName(typeof(RequestStatusIDs), statusid);
    }
    [HttpGet]
    public IActionResult History(Guid id)
    {
        UserInfo ui = GetCurrentUser();
        if (ui.VendorId != null)
        {
            List<RequestTracking> rtList = new();
            List<RequestHistory> rhList = new();

            var request = _context.RequestStatus
                                    .Where(r => r.RequestId == id && r.VendorId == ui.VendorId)
                                    .AsNoTracking()
                                    .FirstOrDefault();
            if (request != null)
            {
                rtList = _context.RequestTracking
                                    .Where(rn => rn.RequestId == id)
                                    .AsNoTracking()
                                    .OrderBy(h => h.ChangeDate)
                                    .ToList();

                foreach (var d in rtList)
                {
                    rhList.Add(new RequestHistory(d));
                }
            }

            var processHistory = new List<ProcessHistory>();

#if INCLUDE_PROCESS_DATE_EVENTS
            // DO NOT INCLUDE DATES ENTERED IN HISTORY 5/12/2023
            //
            // just include history of when these values were changed
            // 
            processHistory.AddAction("ETA", request.Eta);
            processHistory.AddAction("CANCELLED", request.CancelledDate);
            processHistory.AddAction("RECEIVED", request.DateReceived);
            processHistory.AddAction("RECEIVED FROM DMV", request.DateFromDmv);
            processHistory.AddAction("PRINTED", request.DatePrinted);
            processHistory.AddAction("SHIPPED", request.DateShipped);
            processHistory.AddAction("TITLE ISSUED", request.DateTitleIssued);
            processHistory.AddAction("SENT TO DMV", request.DateToDmv);
            //processHistory.AddAction("DATE TO VENDOR", request.DateToVendor);
            processHistory.AddAction("LI RECEIVED FROM DMV", request.LI_DateFromDmv);
            processHistory.AddAction("LI SENT TO DMV", request.LI_DateToDmv);
            processHistory.AddAction("REJECTED", request.RejectionDate);
            processHistory.AddAction("TITLE SCANNED", request.TitleScanTime);
#endif
            var invoiceHistory = _context.InvoiceDetail
                                .Include(i => i.Invoice)
                                .AsNoTracking()
                                .Where(i => i.RequestId == id)
                                .Select(i => new InvoiceHistory()
                                {
                                    //Action = "",
                                    SortDate = i.Invoice.DateCreated,
                                    CreatedBy = i.Invoice.CreatedBy,
                                    CreatedDate = i.Invoice.DateCreated,
                                    InvoiceNo = i.Invoice.InvoiceNo,
                                    InvoiceDate = i.Invoice.InvoiceDate,
                                    DatePaid = i.Invoice.InvoiceDatePaid,
                                    DmvFee = i.DmvFee,
                                    OtherFee = i.OtherFee,
                                    MailingFee = i.MailingFee,
                                    ServiceFee = i.ServiceFee,
                                    TotalDue = i.TotalDue
                                })
                                .ToList();

            var paymentHistory = _context.PaymentsAndDisbursementHistory.Where(x => x.RequestId == id)
                                .Select(i => new PaymentAndDisbursementHistory()
                                {
                                    SortDate = i.ModifiedAt,
                                    CreatedBy = i.CreatedBy,
                                    CreatedDate = i.CreatedAt,
                                    PaymentID = i.PaymentID,
                                    PaymentDate = i.PaymentDate,
                                    Amount = i.Amount,
                                    ProcessingFee = i.ProcessingFee,
                                    TotalCharge = i.TotalCharge,
                                    PaymentTypeID = i.PaymentTypeID,
                                    ModifiedBy = i.ModifiedBy,
                                    ModifiedDate = i.UpdatedAt,
                                    ChangeType = i.ChangeType,

                                })
                                .ToList();

            var chats = _context.Chats.Where(c => c.RequestId == id)
                                        .AsNoTracking()
                                        .Select(c => new ChatHistory()
                                        {
                                            SortDate = c.Created,
                                            CreatedBy = c.UserId,
                                            CreatedDate = c.Created,
                                            ModifiedBy = c.UserId,
                                            ModifiedDate = c.Modified,
                                            Message = c.Message,
                                            ReadOn = c.ReadOn,
                                            ReadBy = c.ReadBy
                                        })
                                        .ToList();

            var attachments = _context.RequestAttachments
                                        .Include(r => r.UploadedByNavigation)
                                        .AsNoTracking()
                                        .Where(r => r.RequestId == id && r.DateAdded != null)
                                        .Select(r => new AttachmentHistory()
                                        {
                                            SortDate = r.DateAdded.Value,
                                            CreatedBy = r.UploadedBy,
                                            CreatedDate = r.DateAdded,
                                            FileName = r.Filename,
                                            ModifiedBy = r.UploadedBy,
                                            ModifiedDate = r.DateAdded,
                                            Description = r.Description
                                        })
                                        .ToList();

            var notes = _context.RequestNotes
                                        .AsNoTracking()
                                        .Where(r => r.RequestId == id && r.LastUpdated != null)
                                        .Select(r => new NoteHistory()
                                        {
                                            SortDate = r.LastUpdated,
                                            CreatedBy = r.ModifiedBy,
                                            CreatedDate = r.LastUpdated,
                                            ModifiedBy = r.ModifiedBy,
                                            ModifiedDate = r.LastUpdated,
                                            Note = r.Note,
                                            Remark = r.Remark
                                        })
                                        .ToList();

            var followups = _context.RequestFollowUps
                                        .Include(r => r.FollowUpTags)
                                        .ThenInclude(t => t.Tag)
                                        .AsNoTracking()
                                        .Where(f => f.RequestId == id)
                                        .ToList();

            // Add usernames here and then query all at once to get displayname
            var followuphist = new List<FollowUpHistory>();

            List<int> followUpIds = followups.Select(f => f.FollowUpId).ToList();
            var followUpHistoryList = _context.RequestFollowUpHistory
                                                    .Where(r => r.RequestId == r.RequestId)
                                                    .OrderBy(f => f.ModifiedDate)
                                                    .ToList();

            foreach (var fid in followUpIds)
            {

                // This is the current item, the followuphist 
                // Add created by entry
                var followup = followups.Where(f => f.FollowUpId == fid).FirstOrDefault();

                RequestFollowUpHistory prevFollowUpHist = null;

                foreach (var fuhist in followUpHistoryList)
                {
                    if (fuhist.FollowUpId != fid) continue;

                    FollowUpHistory fhist = new()
                    {
                        SortDate = fuhist.ModifiedDate,
                        CreatedBy = followup.CreatedBy,
                        CreatedDate = followup.CreatedDate,
                        ModifiedBy = fuhist.ModifiedBy,
                        ModifiedDate = fuhist.ModifiedDate,
                        DueDate = fuhist.DueDate,
                        Note = fuhist.Notes,
                        Title = fuhist.Title,
                        Code = fuhist.Code,
                        CompletedDate = fuhist.CompletedDate,
                        Deleted = fuhist.Deleted,
                        FollowUp = followup,
                        Diff = CompareRequestFollowUps(prevFollowUpHist, fuhist)
                    };
                    prevFollowUpHist = fuhist;
                    followuphist.Add(fhist);
                }
                FollowUpHistory fh = new()
                {
                    SortDate = followup.ModifiedDate ?? followup.CreatedDate,
                    CreatedBy = followup.CreatedBy,
                    CreatedDate = followup.CreatedDate,
                    ModifiedBy = followup.ModifiedBy ?? followup.CreatedBy,
                    ModifiedDate = followup.ModifiedDate ?? followup.CreatedDate,
                    DueDate = followup.DueDate,
                    Note = followup.Notes,
                    FollowUp = followup,
                    Title = followup.Title,
                    Code = followup.Code,
                    CompletedDate = followup.CompletedDate,
                    Deleted = followup.Deleted,
                    Diff = CompareRequestFollowUps(prevFollowUpHist, followup)
                };
                followuphist.Add(fh);
            }
            followuphist = followuphist.OrderByDescending(f => f.ModifiedDate).ToList();

            string assignedToBefore = null;
            if (rhList.Count > 0)
            {
                assignedToBefore = rhList[0].AssignedToName;
            }

            List<RequestHistory> removalList = new();
            for (int i = 0; i < rhList.Count; i++)
            {
                bool hasDiff = false;
                int? statusBefore, statusNow;
                if (i == 0)
                {
                    // First item will not have a diff
                    statusBefore = rhList[i].StatusId;
                }
                else
                {
                    statusBefore = rhList[i - 1].StatusId;
                }
                statusNow = rhList[i].StatusId;
                if (statusBefore != statusNow)
                {
                    rhList[i].StatusChangeMsg = $@"Status changed from <span class='reqhist_status_before'>{StatusName(statusBefore)}</span> to <span class='reqhist_status_after'>{StatusName(statusNow)}</span>";
                    hasDiff = true;
                }

                int? stageBefore, stageNow;
                if (i == 0)
                {
                    stageBefore = rhList[i].ProcessStageId;
                }
                else
                {
                    stageBefore = rhList[i - 1].ProcessStageId;
                }
                stageNow = rhList[i].ProcessStageId;
                if (stageBefore != stageNow)
                {
                    rhList[i].StageChangeMsg = $"Stage changed from <span class='reqhist_stage_before'>{StageName(stageBefore)}</span> to <span class='reqhist_stage_after'>{StageName(stageNow)}</span>";
                    hasDiff = true;
                }
                string assignedToNow;
                if (i == 0)
                {
                    assignedToNow = rhList[i].AssignedToName;
                }
                else
                {
                    assignedToNow = rhList[i - 1].AssignedToName;
                }
                if (!string.IsNullOrWhiteSpace(assignedToNow) && assignedToNow != assignedToBefore)
                {
                    if (string.IsNullOrWhiteSpace(assignedToBefore)) assignedToBefore = "blank";
                    rhList[i].AssignedToChangeMsg = $"Request assigned from <span class='reqhist_assignedto_before'>{assignedToBefore}</span> to <span class='reqhist_assignedto_after'>{assignedToNow}</span>";
                    assignedToBefore = assignedToNow;
                    hasDiff = true;
                }
                if (rhList[i].RequestDiff?.Count() > 0)
                {
                    hasDiff = true;
                }
                if (rhList[i].ProcessDiff?.Count() > 0)
                {
                    hasDiff = true;
                }
                if (!hasDiff)
                {
                    removalList.Add(rhList[i]);
                }
            }
            rhList.RemoveAll(r => removalList.Contains(r));

            var list = new List<BaseHistory>();
            list.AddRange(rhList);
            list.AddRange(chats);
            list.AddRange(attachments);
            list.AddRange(notes);
            list.AddRange(followuphist);
            list.AddRange(processHistory);
            list.AddRange(invoiceHistory);
            list.AddRange(paymentHistory);

            var userkeylookup = new List<Guid>();

            foreach (var hist in list)
            {
                if (hist.CreatedBy != null && hist.CreatedBy != Guid.Empty)
                {
                    if (!userkeylookup.Contains(hist.CreatedBy.Value))
                    {
                        userkeylookup.Add(hist.CreatedBy.Value);
                    }
                }
                if (hist.ModifiedBy != null && hist.ModifiedBy != Guid.Empty)
                {
                    if (!userkeylookup.Contains(hist.ModifiedBy.Value))
                    {
                        userkeylookup.Add(hist.ModifiedBy.Value);
                    }
                }
                if (hist.ReadBy != null && hist.ReadBy != Guid.Empty)
                {
                    if (!userkeylookup.Contains(hist.ReadBy.Value))
                    {
                        userkeylookup.Add(hist.ReadBy.Value);
                    }
                }
            }
            // Now query names 
            var usernames = _context.Users.Where(u => userkeylookup.Contains(u.UserId))
                                            .AsNoTracking()
                                            .Select(u => new { u.UserId, u.DisplayName })
                                            .ToList();

            foreach (var user in usernames)
            {
                foreach (var item in list)
                {
                    if (item.CreatedBy == user.UserId)
                    {
                        item.DisplayCreatedBy = user.DisplayName;
                    }
                    if (item.ModifiedBy == user.UserId)
                    {
                        item.DisplayModifiedBy = user.DisplayName;
                    }
                    if (item.ReadBy == user.UserId)
                    {
                        item.DisplayReadBy = user.DisplayName;
                    }
                }
            }
            list = list.OrderBy(h => h.SortDate).ToList();

            RequestHistoryModel model = new()
            {
                Request = request,
                History = list
            };
            return View("_RequestHistory", model);
            //return View("_RequestHistory", rhList);
        }
        return Ok();
    }
#if false
    public class RequestFollowUpDiff
    {
        public string FieldName { get; set; }
        public string Before { get; set; }
        public string After { get; set; }
    }
    // NOTE: the function below is not used, but left here as an example of the copilot generation capabilities
    //
    // generate a function to compare two RequestFollowUps instances and return a list of differences
    private List<RequestFollowUpDiff> CompareRequestFollowUps(RequestFollowUps before, RequestFollowUps after)
    {
        List<RequestFollowUpDiff> diffs = new List<RequestFollowUpDiff>();
        if (before != null && after != null)
        {
            if (before.Code != after.Code)
            {
                diffs.Add(new RequestFollowUpDiff()
                {
                    FieldName = "Code",
                    Before = before.Code.ToString(),
                    After = after.Code.ToString()
                });
            }
            if (before.Title != after.Title)
            {
                diffs.Add(new RequestFollowUpDiff()
                {
                    FieldName = "Title",
                    Before = before.Title,
                    After = after.Title
                });
            }
            if (before.DueDate != after.DueDate)
            {
                diffs.Add(new RequestFollowUpDiff()
                {
                    FieldName = "DueDate",
                    Before = before.DueDate.ToString(),
                    After = after.DueDate.ToString()
                });
            }
            if (before.CompletedDate != after.CompletedDate)
            {
                diffs.Add(new RequestFollowUpDiff()
                {
                    FieldName = "CompletedDate",
                    Before = before.CompletedDate.ToString(),
                    After = after.CompletedDate.ToString()
                });
            }
        }
        return diffs;
    }
#endif
    private DiffDictionary CompareRequestFollowUps(RequestFollowUpHistory before, RequestFollowUps after)
    {
        DiffDictionary diffs = new();
        if (before != null && after != null)
        {
            if (before.Code != after.Code)
            {
                diffs.Add("Code", new JsonDiff.LeftRightValue(before.Code, after.Code));
            }
            if (before.Title != after.Title)
            {
                diffs.Add("Title", new JsonDiff.LeftRightValue(before.Title, after.Title));
            }
            if (before.Notes != after.Notes)
            {
                diffs.Add("Noted Status", new JsonDiff.LeftRightValue(before.Notes, after.Notes));
            }
            if (before.DueDate != after.DueDate)
            {
                diffs.Add("Due Date", new JsonDiff.LeftRightValue(before.DueDate, after.DueDate));
            }
            if (before.CompletedDate != after.CompletedDate)
            {
                diffs.Add("Completed Date", new JsonDiff.LeftRightValue(before.CompletedDate, after.CompletedDate));
            }
        }
        return diffs;
    }
    private DiffDictionary CompareRequestFollowUps(RequestFollowUpHistory before, RequestFollowUpHistory after)
    {
        DiffDictionary diffs = new();
        if (before != null && after != null)
        {
            if (before.Code != after.Code)
            {
                diffs.Add("Code", new JsonDiff.LeftRightValue(before.Code, after.Code));
            }
            if (before.Title != after.Title)
            {
                diffs.Add("Title", new JsonDiff.LeftRightValue(before.Title, after.Title));
            }
            if (before.Notes != after.Notes)
            {
                diffs.Add("Noted Status", new JsonDiff.LeftRightValue(before.Notes, after.Notes));
            }
            if (before.DueDate != after.DueDate)
            {
                diffs.Add("Due Date", new JsonDiff.LeftRightValue(before.DueDate, after.DueDate));
            }
            if (before.CompletedDate != after.CompletedDate)
            {
                diffs.Add("Completed Date", new JsonDiff.LeftRightValue(before.CompletedDate, after.CompletedDate));
            }
        }
        return diffs;
    }
    [HttpPost]
    public async Task<IActionResult> ViewHistory(Guid id)
    {
        UserInfo ui = await GetCurrentUserAsync();
        if (ui.VendorId != null)
        {
            List<RequestTracking> rtList = new();
            List<RequestHistory> rhList = new();

            var request = await _context.Requests
                .Where(r => r.RequestId == id && r.VendorId == ui.VendorId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            if (request != null)
            {
                rtList = await _context.RequestTracking
                                    .Where(rn => rn.RequestId == id)
                                    .AsNoTracking()
                                    .OrderBy(h => h.ChangeDate)
                                    .ToListAsync();

                foreach (var d in rtList)
                {
                    rhList.Add(new RequestHistory(d));
                }
            }

            string assignedToBefore = null;
            if (rhList.Count > 0)
            {
                assignedToBefore = rhList[0].AssignedToName;
            }

            List<RequestHistory> removalList = new();
            for (int i = 1; i < rhList.Count; i++)
            {
                bool hasDiff = false;
                int? statusBefore, statusNow;
                statusBefore = rhList[i - 1].StatusId;
                statusNow = rhList[i].StatusId;
                if (statusBefore != statusNow)
                {
                    rhList[i].StatusChangeMsg = $@"Status changed from <span class='reqhist_status_before'>{StatusName(statusBefore)}</span> to <span class='reqhist_status_after'>{StatusName(statusNow)}</span>";
                    hasDiff = true;
                }

                int? stageBefore, stageNow;
                stageBefore = rhList[i - 1].ProcessStageId;
                stageNow = rhList[i].ProcessStageId;
                if (stageBefore != stageNow)
                {

                    rhList[i].StageChangeMsg = $"Stage changed from <span class='reqhist_stage_before'>{StageName(stageBefore)}</span> to <span class='reqhist_stage_after'>{StageName(stageNow)}</span>";
                    hasDiff = true;
                }
                string assignedToNow;
                assignedToNow = rhList[i - 1].AssignedToName;
                if (!string.IsNullOrWhiteSpace(assignedToNow) && assignedToNow != assignedToBefore)
                {
                    rhList[i].AssignedToChangeMsg = $"Request assigned from <span class='reqhist_assignedto_before'>{assignedToBefore}</span> to <span class='reqhist_assignedto_after'>{assignedToNow}</span>";
                    hasDiff = true;
                }
                if (rhList[i].RequestDiff?.Count() > 0)
                {
                    hasDiff = true;
                }
                if (rhList[i].ProcessDiff?.Count() > 0)
                {
                    hasDiff = true;
                }
                if (!hasDiff)
                {
                    removalList.Add(rhList[i]);
                }
            }
            rhList.RemoveAll(r => removalList.Contains(r));

            return PartialView("_RequestHistory", rhList);
        }
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> EditNotes(Guid id, [FromForm] bool? standaloneForm)
    {
        UserInfo ui = await GetCurrentUserAsync();
        if (ui.VendorId != null)
        {
            MyDMVpro.Models.RequestsViewModels.EditNotesModel data = new();

            var request = await _context.Requests
                .Where(r => r.RequestId == id && r.VendorId == ui.VendorId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (request != null)
            {
                var requestNotes = await _context.RequestNotes
                                                .Include(rn => rn.ModifiedByNavigation)
                                                .Where(rn => rn.RequestId == id)
                                                .AsNoTracking()
                                                .OrderByDescending(rn => rn.LastUpdated)
                                                .ToListAsync();
                ViewBag.HelpLink = await GetHelpLink(request, ui.IsVendorAgent);

                data.CurrentNotes = requestNotes;
                data.RequestId = request.RequestId;
                data.Code = request.Code;
                data.Note = "";
                data.Remark = "";
                data.SpecialBilling = request.SpecialBilling ?? false;
                data.StandaloneForm = (standaloneForm.HasValue && standaloneForm.Value == true);
            }
            return PartialView("_EditNotes", data);
        }
        return Ok();
    }
    [HttpPost]
    public async Task<IActionResult> UpdateNotes([FromForm] Guid RequestId, [FromForm] string Note, [FromForm] string Remark, [FromForm] string Code, [FromForm] string SpecialBilling, [FromForm] string? ClientRemarks)
    {
        try
        {

            UserInfo ui = await GetCurrentUserAsync();
            if (ui.VendorId != null)
            {
                string trimmedNote = (Note ?? "").Trim();
                string trimmedRemark = (Remark ?? "").Trim();
                string trimmedClientRemarks = (ClientRemarks ?? "").Trim();
                SpecialBilling = (SpecialBilling ?? "").Trim().ToLower();
                // TBD: review why browser sending "on" for checkbox
                bool specialBilling = (SpecialBilling == "true" || SpecialBilling == "on" || SpecialBilling == "1");

                Requests req = _context.Requests.Where(r => r.RequestId == RequestId).FirstOrDefault();
                req.Code = Code;
                if ((req.SpecialBilling ?? false) != specialBilling)
                {
                    if (string.IsNullOrWhiteSpace(trimmedRemark))
                    {
                        throw new ApplicationException("Changing Special Billing indicator requires an internal remark");
                    }
                    req.SpecialBilling = specialBilling;
                }

                RequestNotes reqnote = null;

                //RequestNotes reqnote = _context.RequestNotes.Include(n => n.Request)
                //    .Where(n => n.RequestId == RequestId && n.Request.VendorId == ui.VendorId)
                //    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(trimmedNote) || !string.IsNullOrWhiteSpace(trimmedRemark))
                {
                    _context.RequestNotes.Add(
                        reqnote = new RequestNotes()
                        {
                            RequestId = RequestId,
                            ModifiedBy = ui.UserId,
                            Note = (trimmedNote.Length == 0) ? null : trimmedNote,
                            Remark = (trimmedRemark.Length == 0) ? null : trimmedRemark,
                            ClientRemarks = (trimmedClientRemarks.Length == 0) ? null : trimmedClientRemarks,
                            LastUpdated = System.DateTime.UtcNow
                        });
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return JsonError("Access Denied");
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestsController.UpdateNotes");
            return JsonError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateClientInternalNotes([FromBody] TaggedMessageViewModel data)
    {
        try
        {
            UserInfo ui = await GetCurrentUserAsync();
            string trimmedClientRemarks = (data.ClientRemarks ?? "").Trim();
            RequestNotes reqNote = null;

            _context.RequestNotes.Add(
            reqNote = new RequestNotes()
            {
                RequestId = data.RequestId,
                ModifiedBy = ui.UserId,
                Note = null,
                Remark = null,
                ClientRemarks = (trimmedClientRemarks.Length == 0) ? null : trimmedClientRemarks,
                LastUpdated = System.DateTime.UtcNow
            });

            foreach (var user in data.Users)
            {
                Requests req = await _context.Requests
                    .Where(r => r.RequestId == data.RequestId && r.GroupId == ui.GroupId)
                    .FirstOrDefaultAsync();

                if(req == null)
                {
                    continue;
                }

                Users users = await _context.Users
                    .Where(x => x.UserId == ui.UserId)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(trimmedClientRemarks))
                {
                    if (user.TaggedUserId != Guid.Empty)
                    {
                        // Instantiate a new NotifyClient object inside the loop
                        NotifyClient notifyClient = new NotifyClient
                        {
                            RequestId = data.RequestId,
                            Note = trimmedClientRemarks,
                            Vin = req.Vin,
                            SubmittedUserId = (Guid)ui.UserId,
                            NotifiedUserId = user.TaggedUserId,
                            RequestNumber = req.Id,
                            SubmittedUserName = users.DisplayName
                        };

                        _context.NotifyClient.Add(notifyClient);
                    }
                }
            }
            await _context.SaveChangesAsync(); // Save all changes once after loop
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestsController.UpdateNotes");
            return JsonError(ex);
        }
    }

    //[HttpPost]
    //public async Task<IActionResult> GetNotifiedData()
    //{
    //    try
    //    {
    //        var ui = await GetCurrentUserAsync();
    //        if (ui == null)
    //            return Json(new { success = false });

    //        var resultDto = await _context.NotifyClient
    //            .Where(x => x.NotifiedUserId == ui.UserId && x.IsRead == false)
    //            .Include(x => x.Request)
    //            .AsNoTracking()
    //            .Select(nc => new NotifyClientViewModal
    //            {
    //                Id = nc.Id,
    //                RequestId = nc.RequestId,
    //                Vin = nc.Vin,
    //                Note = nc.Note,
    //                SubmittedUserName = nc.SubmittedUserName,
    //                NotifiedUserName = nc.NotifiedUserName,
    //                IsRead = nc.IsRead,
    //                CreatedDate = nc.CreatedDate,
    //                RequestNumber = nc.RequestNumber,
    //                SubmittedUserId = nc.SubmittedUserId,
    //                Request = nc.Request == null ? null : new RequestDto
    //                {
    //                    Id = nc.Request.Id,
    //                    AppType = nc.Request.AppType,
    //                    Vin = nc.Request.Vin,
    //                    State = nc.Request.State
    //                }
    //            })
    //            .ToListAsync();

    //        return Json(new { success = true, data = resultDto });
    //    }
    //    catch (Exception ex)
    //    {
    //        LogError(ex, "RequestsController.UpdateNotes");
    //        return JsonError(ex);
    //    }

    //}

    [HttpPost]
    public async Task<IActionResult> GetNotifiedData()
    {
        try
        {
            var filterHelper = new FilterHelper<NotifyClientViewModal>((MaggardDMVContext)_context, _configuration, this);

            return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<NotifyClientViewModal> rows = null;
                rows = _context.NotifyClientView.Where(x => x.NotifiedUserId == userId && x.IsRead == false && x.GroupId == groupId);
                return rows;
            }, filterOnCurrentUser : true);
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestsController.UpdateNotes");
            return JsonError(ex);
        }

    }

    [HttpPost]
    public async Task<IActionResult> UpdateNotifyClientData(Guid requestId)
    {
        try
        {

            UserInfo ui = await GetCurrentUserAsync();
            if (ui != null)
            {
                var result = await _context.NotifyClient.Where(x => x.RequestId == requestId && x.NotifiedUserId == ui.UserId).ToListAsync();
                foreach (var item in result)
                {
                    item.IsRead = true;
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });

            }
            else
            {
                return Json(new { success = false });
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestsController.UpdateNotes");
            return JsonError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetNotifyClientData(Guid requestId)
    {
        try
        {

            UserInfo ui = await GetCurrentUserAsync();
            if (ui != null)
            {
                var result = await _context.NotifyClient.Where(x => x.RequestId == requestId && x.NotifiedUserId == ui.UserId && x.IsRead == false).ToListAsync();
                return Json(new { success = true, data = result });
            }
            else
            {
                return JsonError("User not found!");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestsController.UpdateNotes");
            return JsonError(ex);
        }
    }


    [HttpPost]
    public async Task<IActionResult> LinkRequest([FromBody] LinkRequest_Create_Model model)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user.IsVendorAgent)
            {
                await DataHelpers.AddRequestLink(user.VendorId, user.UserId, model.RequestId, model.LinkRequestId, model.LinkRequestNo);
            }
            else
            {
                return NotFound();
            }
            return JsonSuccess();
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestsController.LinkRequest");
            return JsonError(ex);
        }
        return Ok();
    }
    [HttpPost]
    public async Task<IActionResult> UnlinkRequest([FromBody] UnlinkRequest_Model model)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user.IsVendorAgent)
            {
                await DataHelpers.RemoveRequestLink(user.VendorId, user.UserId, model.RequestId, model.LinkRequestId);
            }
            else
            {
                return NotFound();
            }
            return JsonSuccess();
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestsController.UnlinkRequest");
            return JsonError(ex);
        }
        return Ok();
    }
    public class DropDownListCollection : Dictionary<string, List<string>>
    {

    }
    [HttpGet]
    public async Task<IActionResult> Transactions(Guid id)
    {
        UserInfo ui = await GetCurrentUserAsync();
        if (ui.VendorId != null)
        {
            MyDMVpro.Models.RequestsViewModels.RequestAccountingViewModel data = new();

            var request = await _context.Requests
                .Where(r => r.RequestId == id && r.VendorId == ui.VendorId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            if (request != null)
            {
                data.Request = request;
                data.Disbursements = await _context.RequestDisbursements
                                                .Where(d => d.RequestId == id)
                                                .AsNoTracking()
                                                .OrderByDescending(d => d.CreatedDate)
                                                .ToListAsync();
                data.InvoiceDetails = await _context.InvoiceDetail
                                                .Include(i => i.Invoice)
                                                .Where(i => i.RequestId == id)
                                                .AsNoTracking()
                                                .OrderByDescending(i => i.Invoice.DateCreated)
                                                .ToListAsync();
            }
            return PartialView("_Transactions", data);
        }
        return Ok();
    }

    public async Task<IActionResult> RequestCodes()
    {
        AddPageHeader("Request Codes", "");
        return View();
    }
#if false
        [HttpPost]
        public async Task<IActionResult> TagsViewData()
        {
            return await tags_filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                return GetCodesForVendor(vendorId.Value);
            }, true, false);
        }
        private IQueryable<Tag> GetCodesForVendor(Guid vendorId)
        {
            IQueryable<Tag> rows = null;

            rows = _context.Tag.Where(t => t.VendorId == vendorId && t.TagType == c_TagType)
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
#endif
#if false
        [HttpPost]
        public async Task<IActionResult> DeleteTag([FromBody] FollowUpTag_Delete_Model model)
        {
            if (string.IsNullOrWhiteSpace(model?.TagName))
            {
                return NotFound();
            }
            var user = await GetCurrentUserAsync();
            try
            {
                var usedTags = await _context.RequestCodes.Where(rc => rc.TagId == model.TagId).AnyAsync();
                if (usedTags)
                {
                    return JsonError("Tag is in use and cannot be deleted");
                }
                var record = await _context.Tag.Where(t => t.VendorId == user.VendorId && t.TagName == model.TagName && t.TagType == c_TagType).FirstOrDefaultAsync();
                if (record == null)
                {
                    return NotFound();
                }
                else
                {
                    _context.Tag.Remove(record);
                    await _context.SaveChangesAsync(user);
                    return JsonSuccess();
                };
            }
            catch (SqlException ex)
            {
                return JsonError("Unable to delete", ex);
            }
            catch (Exception ex2)
            {
                return JsonError("Unable to delete", ex2);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Tags()
        {
            var user = await GetCurrentUserAsync();

            var list = await _context.Tag
                                .Where(t => t.VendorId == user.VendorId && t.TagType == c_TagType)
                                .Select(t => new { id = t.TagId, tag = t.TagName, desc = t.TagDesc, @class = t.TagClass })
                                .ToListAsync();

            return new JsonResult(list);
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
                TagType = c_TagType,
                TagName = model.TagName,
                TagClass = model.TagClass,
                TagDesc = model.TagDesc
            };
            _context.Tag.Add(fup);
            await _context.SaveChangesAsync(user);
            return JsonSuccess();
        }
#endif
    [HttpPost("/Requests/VendorRequestsTagged/{tags?}")]
    public async Task<IActionResult> VendorRequestsTagged(string tags)
    {
        var tagIDs = (tags ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

        if (tagIDs.Count == 0 || (tagIDs.Count == 1 && tagIDs[0] == 0))
        {
            return null; // new List<RequestTags>(); // await VendorFollowUps(null);
        }

        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<RequestCode> rows = null;
            rows = _context.RequestCodes
                                .Where(x => x.Request.GroupId == groupId || x.Request.VendorId == vendorId)
                                .WhereIsActive()
                                .Where(fut => tagIDs.Contains(fut.TagId));
            //                                   .Join(_context.RequestFollowUpStatus, oid => oid.FollowUpId, iid => iid.FollowUpId, (oid, iid) => iid);
            return rows;
        }, true, false);
    }

    [HttpPost("/Requests/RequestCodes/{requestId?}")]
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
                                    .Include(rt => rt.Fields)
                                    .ThenInclude(f => f.Field)
                                    .Include(rt => rt.Tag)
                                    .AsNoTracking()
                                    .Where(x => x.Request.GroupId == groupId || x.Request.VendorId == vendorId)
                                    .WhereIsActive()
                                    .Where(f => f.RequestId == requestId);
            return rows;
        }, true, false);
    }

    [HttpPost]
    public async Task<IActionResult> NeededDataRequestCodes()
    {
        var processStageIds = new[] { 2, 12, 99 };

        var user = await GetCurrentUserAsync();

        var rows = await _context.RequestCodes
                                    .Include(rt => rt.Fields)
                                    .ThenInclude(f => f.Field)
                                    .Include(rt => rt.Tag)
                                    .Include(rt => rt.Request)
                                    .WhereUserHasAccess(user)
                                    .WhereIsActive()
                                    .Where(x => processStageIds.Contains((int)x.Request.ProcessStageId) && x.Cleared == false).ToListAsync();

        return PartialView("_DataNeeded", rows);
    }

    [HttpPost("/Requests/InformationCodes/{requestId?}")]
    public async Task<IActionResult> InformationCodes(Guid? requestId)
    {

        if (requestId != null && requestId == Guid.Empty)
        {
            return BadRequest();
        }
        var user = await GetCurrentUserAsync();
        var rows = await _context.RequestCodes
                                    .Include(rt => rt.Fields)
                                    .ThenInclude(f => f.Field)
                                    .Include(rt => rt.Tag)
                                    .AsNoTracking()
                                    .WhereUserHasAccess(user)
                                    .WhereIsActive()
                                    .Where(f => f.RequestId == requestId && f.Cleared == false).ToListAsync();

        var existingProposedUpdates = await _context.ProposedUpdates.Where(x => (x.ApprovalStatus == null || x.ApprovalStatus == true))
            .Select(pu => pu.RequestCodeId)
            .ToListAsync();

        rows = rows.Where(row => !existingProposedUpdates.Contains(row.RequestCodeId)).ToList();


        return PartialView("_InformationFields", rows);
    }


    [HttpPost("/Requests/RequestCode")]
    public async Task<IActionResult> CreateRequestCode()
    {
        try
        {
            //var requestCode = _context.RequestCodes
            //                        .Include(rt => rt.Fields)
            //                        .Include(rt => rt.Tag)
            //                        .AsNoTracking()
            //                        .Where(f => f.RequestCodeId == requestCodeId);
            return JsonSuccess();
        }
        catch (Exception ex)
        {
            return JsonError(ex);
        }
    }
    [HttpPost("/Requests/RequestCode/{requestCodeId?}")]
    public async Task<IActionResult> UpdateRequestCode(Guid? requestCodeId)
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
                                                .Include(rt => rt.Tag)
                                                .WhereUserHasAccess(user)
                                                .WhereIsActive()
                                                .Where(f => f.RequestCodeId == requestCodeId)
                                                .SingleOrDefaultAsync();
            // update the request code
            //requestCode.Note
            //requestCode.Resolution

            await _context.SaveChangesAsync();

            return Json(requestCode);
        }
        catch (Exception ex)
        {
            return JsonError(ex);
        }
    }

    [HttpGet("/Requests/RequestCode/{requestCodeId?}")]
    public async Task<IActionResult> GetRequestCode(Guid? requestCodeId)
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
    public IQueryable<RequestCode> FilterBySettings(IQueryable<RequestCode> rows, DatatableFormData dfd)
    {
        object filters;

        if (dfd.includeCleared)
        {
            // no additional filtering
        }
        else
        {
            rows = rows.Where(r => r.Cleared == false);
        }

        return rows;
    }

    [HttpGet("/Requests/GetNewAppTypes/{requestId?}")]
    public async Task<IActionResult> GetNewAppTypes(Guid? requestId)
    {
        if (requestId == null || requestId == Guid.Empty)
        {
            return NotFound();
        }
        try
        {
            var request = await _context.Requests
                                                .Where(r => r.RequestId == requestId)
                                                .FirstOrDefaultAsync();

            var currentAppType = await _context.MdpAppTypes
                                                .Where(a => a.AppType == request.AppType)
                                                .FirstOrDefaultAsync();

            var states = await _context.MdpAppTypeStates
                                                .Where(a => a.AppType.AppType == request.AppType)
                                                .Select(a => new SelectListItem(a.AppState, a.AppState))
                                                .ToListAsync();
            foreach (var state in states)
            {
                state.Text = USState.GetStateName(state.Value);
                if (state.Text.Length == 0) state.Text = state.Value;
                if (state.Value == request.State)
                {
                    state.Selected = true;
                }
            }
            states = states.OrderBy(a => a.Text).ToList();

            var appTypes = await _context.MdpAppTypes
                                                .Where(a => a.QueueName == currentAppType.QueueName)
                                                .Select(a => new SelectListItem(a.Title, a.AppType))
                                                .ToListAsync();

            foreach (var appType in appTypes)
            {
                if (appType.Value == request.AppType)
                {
                    appType.Selected = true;
                }
            }
            // sort appTypes by title
            appTypes = appTypes.OrderBy(a => a.Text).ToList();


            return new JsonResult(new
            {
                states = states,
                appTypes = appTypes
            });
        }
        catch (Exception ex)
        {
            return JsonError(ex);
        }
    }
    [HttpPost]
    public async Task<IActionResult> ChangeAppType(Guid? id, string newAppType, string newAppState)
    {
        const string REQUEST_TAG_NAME = "APP TYPE CHANGE";
        try
        {
            var user = await GetCurrentUserAsync();
            if (user.IsVendorAgent)
            {
                if (newAppType == null || newAppState == null)
                {
                    throw new ApplicationException("AppType and State are required");
                }
                var request = await _context.Requests
                    .Where(r => r.RequestId == id && r.VendorId == user.VendorId)
                    .FirstOrDefaultAsync();

                if (request.StatusId != 1)
                {
                    throw new ApplicationException("Request must be in new status to change AppType/State");
                }
                switch ((ProcessStageIDs)request.ProcessStageId.Value)
                {
                    case ProcessStageIDs.Incoming: break;
                    case ProcessStageIDs.NotReadyForProcessing: break;
                    case ProcessStageIDs.ReadyToProcess: break;
                    default:
                        throw new ApplicationException("AppType/State cannot be changed after the Ready For Processing stage");
                }
                if (request != null)
                {
                    var curAppType = _context.MdpAppTypes.Where(a => a.AppType == request.AppType).FirstOrDefault();
                    var newAppTypeQueue = _context.MdpAppTypeStates.Where(a => a.AppType.AppType == newAppType && a.AppState == newAppState).Select(a => a.AppType.QueueName).FirstOrDefault();
                    //if (string.IsNullOrWhiteSpace(state) || !IsValidState(state))
                    //{
                    //    throw new ApplicationException($"Invalid state: {state}");
                    //}

                    if (newAppTypeQueue == null)
                    {
                        throw new ApplicationException("Unknown AppType/State combination");
                    }
                    if (curAppType.QueueName != newAppTypeQueue)
                    {
                        throw new ApplicationException("App type must be in the same queue as original submission");
                    }

                    var oldAppType = request.AppType;
                    var oldAppState = request.State;

                    request.AppType = newAppType;
                    request.State = newAppState;
                    if (request.JRequest != null)
                    {
                        try
                        {
                            var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(request.JRequest);
                            values["AppType"] = newAppType;
                            values["AppTypeState"] = newAppState;

                            if (values.ContainsKey("State"))
                            {
                                // State is an old field and may not be present
                                // and if present, may be empty 
                                // if empty, leave blank and use AppTypeState
                                string state = (values["State"] ?? "").Trim();
                                if (state.Length > 0)
                                {
                                    values["State"] = newAppState;
                                }
                            }

                            request.JRequest = Newtonsoft.Json.JsonConvert.SerializeObject(values);
                        }
                        catch (Exception ex)
                        {
                            throw new ApplicationException("Unable to update AppType/State in request");
                        }
                    }

                    // “Application type was changed from <Prior App Type/State> to <new app type / state >
                    string note = $"Application type was changed from {oldAppType}/{oldAppState} to {newAppType}/{newAppState}";

                    await AddRequestCode(request.RequestId, REQUEST_TAG_NAME, note);
                    // Add public facing note
                    await AddNoteAndRemark(request.RequestId, user, note, "");

                    await _context.SaveChangesAsync(user);

                    // Refresh attachment condition status 
                    // This is done after saving changes, otherwise the app type will not yet have had the change
                    await _context.Database.ExecuteSqlRawAsync("EXEC [dbo].[AttachmentStatus_Refresh_Single] @id", new SqlParameter("@id", request.RequestId));

                    return JsonSuccess();
                }
            }
            return NotFound();
        }
        catch (Exception ex)
        {
            return JsonError(ex);
        }

        return null;
    }

    [HttpGet]
    [Route("Requests/GetAttachmentForUpload")]
    public async Task<IActionResult> GetAttachmentForUpload(Guid? attachmentId, Guid? requestId)
    {
        var request = await _context.Requests.SingleOrDefaultAsync(x => x.RequestId == requestId);
        if (request == null)
        {
            return BadRequest();
        }
        var appTypeState = await _context.MdpAppTypeStates.Include(x => x.AppType).SingleOrDefaultAsync(x => x.AppState == request.State && x.AppType.AppType == request.AppType);
        if (appTypeState == null)
        {
            return BadRequest();
        }
        UserInfo user = await GetCurrentUserAsync();

        // Get all attachment types for the current application type/state
        var availableAttachmentTypes = await _context.MdpAppTypeAttachmentTypes
                                                        .Include(x => x.AttachmentType)
                                                        .Where(x => x.AttachmentTypeId == attachmentId && x.AppTypeStateId == appTypeState.AppTypeStateId)
                                                        .ToListAsync();

        var extraAttachmentOption = await _context.MdpAttachmentTypes.SingleOrDefaultAsync(x => x.Name == "Extra");


        List<MdpAttachmentTypes> requestAttachmentTypes = availableAttachmentTypes
                                                                .Where(x => x.AttachmentType != null)
                                                                .Select(x => x.AttachmentType)
                                                                .ToList();
        if (appTypeState?.PublicHelpID != null)
        {
            ViewBag.HelpLink = $"/help/{appTypeState?.AppType.AppType}/{appTypeState?.AppState}";
        }
        else
        {
            ViewBag.HelpLink = "";
        }

        AttachmentsViewModel attachmentsViewModel = new()
        {
            RequestId = (Guid)requestId,
            RequestNo = request.Id,
            IsVendorAgent = user.IsVendorAgent,
            AppTypeAttachmentTypes = availableAttachmentTypes,
            AttachmentTypes = requestAttachmentTypes,
            AttachmentNotes = appTypeState.AttachmentNotes,
            Request = request
        };

        var test = attachmentsViewModel;

        return PartialView("_MissingDocumentUploader", attachmentsViewModel);


    }

    [HttpPost]
    [Route("Requests/GenerateUploadLink")]
    public async Task<IActionResult> GenerateLink(Guid? requestId, Guid? attachmentTypeId)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expirationDate = DateTime.UtcNow.AddDays(1); // 1-day expiration

        var uploadLink = new UploadLinks
        {
            RequestId = (Guid)requestId,
            AttachmentId = (Guid)attachmentTypeId,
            AttachmentTypeId = (Guid)attachmentTypeId,
            ExpirationDate = expirationDate,
            Token = token,
            CreatedBy = "",
            CreatedDate = DateTime.UtcNow,
            IsDocumentUploaded = false,
            IsUsed = false,
        };

        _context.UploadLinks.Add(uploadLink);
        _context.SaveChanges();

        var link = Url.Action("Upload", "Upload", new { token }, Request.Scheme);
        return Json(new { success = true, uploadLink = link });

    }
    [HttpGet]
    public async Task<IActionResult> DataTags(Guid id)
    {
        try
        {
            UserInfo ui = await GetCurrentUserAsync();

            // Check if the VendorId is not null
            if (ui?.VendorId != null)
            {
                var request = await _context.Requests
                    .Where(r => r.RequestId == id && r.VendorId == ui.VendorId)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                // If the request is found
                if (request != null)
                {
                    // Check if jDataTags is not null or empty before deserializing
                    if (!string.IsNullOrEmpty(request.jDataTags))
                    {
                        // Deserialize the jDataTags to get the tag fields
                        var dataTags = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>>(request.jDataTags);

                        // If deserialization fails, return a bad request with an error message
                        if (dataTags == null || !dataTags.Any())
                        {
                            return BadRequest("Failed to deserialize jDataTags.");
                        }

                        // Deserialize the jRequest to get the corrected values, ensure it defaults to an empty dictionary if null
                        var existingJsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.JRequest) ?? new Dictionary<string, object>();

                        // Loop through each document in the dataTags
                        foreach (var document in dataTags.Keys.ToList())
                        {
                            var documentSections = dataTags[document];

                            // Loop through each section (C1, C2, etc.)
                            foreach (var section in documentSections.Keys.ToList())
                            {
                                var fields = documentSections[section];

                                // Loop through each field in the section
                                foreach (var field in fields)
                                {
                                    var fieldName = field["fieldName"];

                                    // Check if fieldName exists and exists in the jRequest data
                                    if (!string.IsNullOrEmpty(fieldName) && existingJsonData.ContainsKey(fieldName))
                                    {
                                        // Update the correctValue with the value from jRequest
                                        field["correctValue"] = existingJsonData[fieldName]?.ToString();
                                    }
                                }
                            }
                        }

                        return Ok(dataTags);
                    }
                    else
                    {
                        return BadRequest("jDataTags is null or empty.");
                    }
                }
                else
                {
                    return NotFound("Request not found.");
                }
            }
            else
            {
                return Unauthorized("VendorId is missing or invalid.");
            }
        }
        catch (JsonException ex)
        {
            // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
            return StatusCode(500, $"Error processing request: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Catch any other exceptions that were not anticipated
            return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> TaxAndRegFee(Guid id)
    {
        try
        {
            UserInfo ui = await GetCurrentUserAsync();

            // Check if the VendorId is not null
            if (ui?.VendorId != null)
            {
                var request = await _context.Requests
                    .Where(r => r.RequestId == id && r.VendorId == ui.VendorId)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                // If the request is found
                if (request != null)
                {
                    // Check if jDataTags is not null or empty before deserializing
                    if (!string.IsNullOrEmpty(request.jFeesData))
                    {
                        var fee = JsonConvert.DeserializeObject<TaxAndRegFeeModel>(request.jFeesData);
                        return Ok(fee);
                    }
                    else
                    {
                        return BadRequest("jDataTags is null or empty.");
                    }
                }
                else
                {
                    return NotFound("Request not found.");
                }
            }
            else
            {
                return Unauthorized("VendorId is missing or invalid.");
            }
        }
        catch (JsonException ex)
        {
            // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
            return StatusCode(500, $"Error processing request: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Catch any other exceptions that were not anticipated
            return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentTypes()
    {

        var creditPaymentTypes = _context.PaymentTypes.Where(x => x.IsCredit == true).OrderBy(x => x.PaymentTypeID).ToList();
        var debitPaymentTypes = _context.PaymentTypes.Where(x => x.IsCredit == false).OrderBy(x => x.PaymentTypeID).ToList();

        ViewBag.DebitPaymentTypes = debitPaymentTypes;

        ViewBag.CreditPaymentTypes = creditPaymentTypes;

        return PartialView("_PaymentsAndDisbursement");

    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentAndDisbursement([FromBody] PaymentsAndDisbursementViewModel paymentsAndDisbursementViewModel)
    {
        UserInfo user = GetCurrentUser();
        if (user == null)
        {
            return Json(new { success = false, message = "Received null object." });
        }
        if (paymentsAndDisbursementViewModel == null)
        {
            return Json(new { success = false, message = "Received null object." });
        }

        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Received null object." });

        }

        PaymentsAndDisbursement paymentsAndDisbursement = new PaymentsAndDisbursement();
        paymentsAndDisbursement.Amount = paymentsAndDisbursementViewModel.Amount;
        paymentsAndDisbursement.ProcessingFee = paymentsAndDisbursementViewModel.ProcessingFee;
        paymentsAndDisbursement.Vin = paymentsAndDisbursementViewModel.Vin;
        paymentsAndDisbursement.ReferenceNumber = paymentsAndDisbursementViewModel.ReferenceNumber;
        paymentsAndDisbursement.RequestId = paymentsAndDisbursementViewModel.RequestId;
        paymentsAndDisbursement.PaymentTypeID = paymentsAndDisbursementViewModel.PaymentTypeID;
        paymentsAndDisbursement.PaymentDate = paymentsAndDisbursementViewModel.PaymentDate;
        paymentsAndDisbursement.IsCredit = paymentsAndDisbursementViewModel.IsCredit;



        if (paymentsAndDisbursementViewModel.PaymentID <= 0)
        {
            paymentsAndDisbursement.CreatedBy = (Guid)user.UserId;
            paymentsAndDisbursement.CreatedAt = DateTime.UtcNow;
            _context.PaymentsAndDisbursement.Add(paymentsAndDisbursement);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Payment created successfully!" });
        }
        else
        {
            paymentsAndDisbursement.ModifiedBy = (Guid)user.UserId;
            paymentsAndDisbursement.UpdatedAt = DateTime.UtcNow;
            _context.PaymentsAndDisbursement.Update(paymentsAndDisbursement);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Payment created successfully!" });
        }

        return Json(new
        {
            success = false,
            message = "Validation failed.",
            errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
        });
    }

    [HttpPost]
    public async Task<IActionResult> GetCalculatedFieldsSummary(Guid requestId)
    {
        UserInfo user = GetCurrentUser();
        if (user == null)
        {
            return Json(new { success = false, message = "Received null object." });
        }
        if (requestId == Guid.Empty)
        {
            return BadRequest("Invalid requestId");
        }

        try
        {
            var totalDue = _context.PaymentsAndDisbursement
                  .Where(d => d.RequestId == requestId && (d.PaymentTypeID == 4 || d.PaymentTypeID == 5 || d.PaymentTypeID == 6))
                  .AsEnumerable()
                  .Sum(d => d.TotalCharge);

            var totalPaid = _context.PaymentsAndDisbursement
                                    .Where(p => p.RequestId == requestId && (p.PaymentTypeID == 1 || p.PaymentTypeID == 2 || p.PaymentTypeID == 3))
                                    .AsEnumerable()
                                    .Sum(p => p.TotalCharge);

            var calculatedFieldsData = new CalculatedFieldsSummaryViewModal
            {
                RequestId = requestId,
                TotalDueToMaggard = totalDue,
                TotalPaidToMaggard = totalPaid,
            };

            return PartialView("_CalculatedFields", calculatedFieldsData);
        }
        catch (Exception ex)
        {

            throw ex;
        }


    }

    public IActionResult GetAllPayments(bool IsCredit, Guid? requestId)
    {
        UserInfo user = GetCurrentUser();
        if (user == null)
        {
            return Json(new { success = false, message = "Received null object." });
        }

        if (IsCredit)
        {
            var creditPaymentTypes = _context.PaymentsAndDisbursement.Where(x => x.IsCredit == true && x.RequestId == requestId).OrderBy(x => x.PaymentTypeID).ToList();
            return Json(new { success = false, data = creditPaymentTypes });

        }
        else
        {
            var debitPaymentTypes = _context.PaymentsAndDisbursement.Where(x => x.IsCredit == false && x.RequestId == requestId).OrderBy(x => x.PaymentTypeID).ToList();
            return Json(new { success = false, data = debitPaymentTypes });

        }

    }

    private static bool IsValidState(string state)
    {
        return USState.GetAllStates().Any(s => s.Abbrev == state);
    }

    public async Task<IActionResult> DeletePaymentAndDisbursement(int paymentID)
    {
        UserInfo user = GetCurrentUser();
        if (user == null)
        {
            return Json(new { success = false, message = "Received null object." });
        }

        if (paymentID <= 0)
        {
            return Json(new { success = false, message = "Payment id must be greater than 0." });
        }

        var result = await _context.PaymentsAndDisbursement.Where(x => x.PaymentID == paymentID).FirstOrDefaultAsync();
        _context.PaymentsAndDisbursement.Remove(result);
        _context.SaveChanges();

        var history = await _context.PaymentsAndDisbursementHistory.Where(x => x.PaymentID == result.PaymentID && x.ChangeType == "DELETE").FirstOrDefaultAsync();
        history.ModifiedAt = DateTime.UtcNow;
        history.ModifiedBy = user.UserId;
        _context.SaveChanges();

        return Json(new { success = true, data = "successfully deleted!" });

    }

    public async Task<IActionResult> GetInstructionPacket(Guid requestId, Guid templateId)
    {
        if (requestId == null)
        {
            return NotFound();
        }

        try
        {
            UserInfo user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                var payload = new InstructionPacketPrintRequest
                {
                    Requests = new Dictionary<Guid, Guid> { { requestId, templateId } },
                    VendorId = (Guid)user.VendorId
                };

                return await SendInstructionPacketRequestAsync(payload);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error during payload creation or request processing: {ex.ToString()}");
                return StatusCode(500, "Internal server error during payload creation or request processing.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Error in GetInstructionPacket method: {ex.ToString()}");
            return StatusCode(500, "Internal server error in GetInstructionPacket method.");
        }
    }

    private async Task<FileContentResult> SendInstructionPacketRequestAsync(InstructionPacketPrintRequest payload)
    {
        try
        {
            using var httpClient = new HttpClient();
            string apiUrl = _configuration.GetValue<string>("BulkPrint:InstructionPacket");
            if (string.IsNullOrEmpty(apiUrl))
            {
                throw new ArgumentException("API URL cannot be null or empty.");
            }
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            // Read response as byte array
            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

            // Extract the file type and name from headers
            string fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ;
            string contentType = response.Content.Headers.ContentType?.ToString() ;

            return new FileContentResult(fileBytes, contentType)
            {
                FileDownloadName = fileName
            };
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Trace.WriteLine($"HTTP request error: {httpEx.ToString()}");
            throw new InvalidOperationException("Error occurred while making the HTTP request.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Error in SendInstructionPacketRequestAsync: {ex.ToString()}");
            throw new InvalidOperationException("An unexpected error occurred while processing the instruction packet request.");
        }
    }

    public async Task<IActionResult> PrintInstructionPackets(List<Guid> ids)
    {
        try
        {
            UserInfo user = await GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }
            if (ids == null || !ids.Any())
            {
                return BadRequest("No IDs provided.");
            }

            var payload = new InstructionPacketPrintRequest();

            foreach (var id in ids)
            {
                var request = await _context.Requests.SingleOrDefaultAsync(x => x.RequestId == id && x.GroupId == user.GroupId);
                if (request == null)
                {
                    continue;
                }
                var appTypeState = await _context.MdpAppTypeStates.SingleOrDefaultAsync(x => x.AppState == request.State && x.AppType.AppType == request.AppType);
                if (appTypeState == null)
                {
                   continue;
                }
                // TBD: This only supports a single instruction document type 
                //      based on the ClientVisible or GenerateForClient flags 
                //      on the PdfTemplate record.  Need to update so user can select
                var insDocument = await _context.PdfTemplates.Where(x => x.AppTypeStateId == appTypeState.AppTypeStateId)
                                                                .WhereClientVisible()
                                                                .OrderByClientSortOrder()
                                                                .FirstOrDefaultAsync();
                if (insDocument == null && insDocument?.TemplateId != Guid.Empty)
                {
                    continue;
                }
                payload.Requests.Add(request.RequestId, insDocument.TemplateId);
            }
            payload.VendorId = user.VendorId;
            payload.GroupId = user.GroupId;

            if (!payload.Requests.Any())
            {
                return BadRequest("No valid instruction packets found for printing.");
            }

            return await SendInstructionPacketRequestAsync(payload);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Error in PrintInstructionPackets: {ex.ToString()}");
            return StatusCode(500, "Internal server error while processing the print instruction packets.");
        }
    }

}
public class DropDownListCollection : Dictionary<string, List<string>>
{

}

public class ResultsDataset
{
    public List<RequestStatus> Page { get; set; }
    public int TotalCount { get; set; }
    public DropDownListCollection ColumnFilters { get; set; } = new DropDownListCollection();
}


