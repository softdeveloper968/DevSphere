using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using MyDMVpro.Models.AttachmentReviewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

[Authorize(Policy = "VendorAgentOnly")]
public class AttachmentReviewController : BaseController
{
    public AttachmentReviewController(MaggardDMVContext context, IConfiguration configuration, ILogger<AttachmentReviewController> logger) : base(context, configuration, logger)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View();
    }
    [HttpGet]
    [Route("/AttachmentReview/Review/{id?}")]
    public async Task<IActionResult> Review(int? id)
    {
        ViewBag.RequestNo = id?.ToString();

        return View("Index");
    }
    [HttpGet]
    public async Task<IActionResult> Reviewed()
    {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> ReviewCount()
    {
        return PartialView("_reviewCount");
    }

    [HttpPost]
    public async Task<IActionResult> NeedReview()
    {
        var filterHelper = new FilterHelper<AttachmentReview>((MaggardDMVContext)_context, _configuration, this);

        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<AttachmentReview> rows = null;
            rows = _context.AttachmentReviews
                                .Where(f => vendorId != null && f.VendorId == vendorId)
                                .Where(f => f.ReviewDate == null);
            return rows;
        });
    }
    [HttpPost]
    public async Task<IActionResult> ReviewCompleted()
    {
        var filterHelper = new FilterHelper<AttachmentReview>((MaggardDMVContext)_context, _configuration, this);

        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<AttachmentReview> rows = null;
            rows = _context.AttachmentReviews
                                .Where(f => vendorId != null && f.VendorId == vendorId)
                                .Where(f => f.ReviewDate != null);
            return rows;
        });
    }
    [HttpGet]
    [Route("/AttachmentReview/View/{id?}")]
    public async Task<IActionResult> ViewAttachment(Guid? id)
    {
        // filename is unused - it just allows the client script to append the filename 
        // which the pdfviewer will show in the top-left 
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            UserInfo user = await GetCurrentUserAsync();
            if (user == null || !user.IsVendorAgent)
            {
                return NotFound();
            }

            var attachment = await _context.RequestAttachments
                                            .FirstOrDefaultAsync(m => m.AttachmentId == id);
            if (attachment == null || attachment.Image == null)
            {
                return NotFound();
            }

            return ReturnFileWithInlineDisposition(attachment.Filename, attachment.Image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ViewAttachment error");
            throw;
        }
    }

    [HttpGet]
    [Route("AttachmentReview/Review/{requestId}/{attachmentTypeId}")]
    public async Task<IActionResult> ReviewFields(Guid requestId, Guid attachmentTypeId)
    {
        try
        {
            var request = await _context.Requests
                                        .FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (request == null)
            {
                return NotFound();
            }

            var appTypeState = await _context.MdpAppTypeStates
                                        .Include(x => x.AppType)
                                        .FirstOrDefaultAsync(a => a.AppType.AppType == request.AppType && a.AppState == request.State);
            if (appTypeState == null)
            {
                return NotFound();
            }

            AttachmentReviewFieldsModel model = new AttachmentReviewFieldsModel
            {
                RequestId = requestId,
                AttachmentTypeId = attachmentTypeId,
                AttachmentType = await _context.AttachmentTypes
                                            .Include(x => x.ReviewColumns)
                                            .ThenInclude(x => x.MasterField)
                                            .FirstOrDefaultAsync(a => a.AttachmentTypeId == attachmentTypeId),
                //AttachmentTypeReviewColumns = await _context.MdpAttachmentTypeReviewColumns
                //                                    .Include(x => x.MasterField)
                //                                    .Where(c => c.AttachmentTypeId == attachmentTypeId)
                //                                    .ToListAsync(),
                AppTypeAttachmentType = await _context.AppTypeAttachmentTypes
                                                        .Include(x => x.ReviewColumns)
                                                        .ThenInclude(x => x.MasterField)
                                                        .FirstOrDefaultAsync(a => a.AttachmentTypeId == attachmentTypeId && a.AppTypeStateId == appTypeState.AppTypeStateId),
                JRequest = request.JRequest
            };
            // cannot populate this until we have the AppTypeAttachmentType
            model.AppTypeAttachmentTypeReviewColumns = model.AppTypeAttachmentType?.ReviewColumns.OrderBy(c => c.MasterField.InternalName).ToList();
            model.AttachmentTypeReviewColumns = model.AttachmentType?.ReviewColumns.OrderBy(c => c.MasterField.InternalName).ToList();

            return PartialView("_reviewFields", model);
        }
        catch (Exception ex)
        {
            return PartialView("Error.cshtml");
        }
        return Ok();
    }

    /// <summary>
    /// Returns the message needed to display to the user 
    /// when the attachment is approved, if any
    /// </summary>
    /// <param name="attachmentId"></param>
    /// <param name="attachmentTypeId"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("AttachmentReview/ApproveCheck/{attachmentId}/{attachmentTypeId}")]
    public async Task<IActionResult> ApproveAttachmentCheck(Guid attachmentId, Guid? attachmentTypeId)
    {
        try
        {
            object o = new { html = "" };

            var user = await GetCurrentUserAsync();
            if (attachmentTypeId == null)
            {
                o = new { html = "This is a message to manually move" };
            }
            else
            {
                var attachment = await _context.AttachmentReviews
                                                .Where(a => a.AttachmentId == attachmentId &&
                                                        (a.AttachmentTypeId == attachmentTypeId ||
                                                            (a.AttachmentTypeId == null && attachmentTypeId == null)))
                                                .Select(x => new
                                                {
                                                    x.AppType,
                                                    x.StatusId,
                                                    x.StatusName,
                                                    x.ProcessStageId,
                                                    x.ProcessStageName
                                                })
                                                .FirstOrDefaultAsync();

                if (attachment != null)
                {
                    var mdpAppType = await _context.MdpAppTypes
                                                        .Where(a => a.AppType == attachment.AppType)
                                                        .FirstOrDefaultAsync();

                    var isRequestQueue = "Requests".Equals(mdpAppType?.QueueName ?? "", StringComparison.OrdinalIgnoreCase);
                    if (mdpAppType == null || !isRequestQueue || attachment.ProcessStageId != (int)ProcessStageIDs.NotReadyForProcessing)
                    {
                        string url = mdpAppType?.QueuePageUrl ?? "#";
                        string htmlTemplate = "Hello, this record is not an RT/DT in the “Not Ready to Process” queue so there may not be conditional logic to move this record into it the next stage. <br/>Please visit the record in the <a target='_blank' href='{0}'>{1}</a> to move the record forward if needed";
                        string html = "";
                        if (attachment.StatusName == "Active")
                        {
                            string stageName = attachment.ProcessStageName;
                            string queueName = mdpAppType?.QueuePageName ?? "";
                            html = string.Format(htmlTemplate, url, $"{queueName} / {stageName}");
                        }
                        else if (attachment.StatusName == "Hold")
                        {
                            html = string.Format(htmlTemplate, url, "Hold queue");
                        }
                        else if (attachment.StatusName == "Cancelled")
                        {
                            html = string.Format(htmlTemplate, url, "Cancelled queue");
                        }
                        else if (attachment.StatusName == "Completed")
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="attachmentId"></param>
    /// <param name="attachmentTypeId"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("AttachmentReview/Approve/{attachmentId}/{attachmentTypeId}")]
    public async Task<IActionResult> ApproveAttachment(Guid attachmentId, Guid? attachmentTypeId)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (attachmentTypeId == null)
            {

            }
            var attachment = await _context.RequestAttachments
                                        .Where(a => a.AttachmentId == attachmentId &&
                                                (a.AttachmentTypeId == attachmentTypeId || a.AttachmentTypeId == null))
                                        .Select(x => new RequestAttachments()
                                        {
                                            Approved = x.Approved,
                                            AttachmentId = x.AttachmentId,
                                            AttachmentTypeId = x.AttachmentTypeId,
                                            ReviewBy = x.ReviewBy,
                                            ReviewDate = x.ReviewDate,
                                            RequestId = x.RequestId,
                                            UploadedBy = x.UploadedBy,
                                            /*Image = x.Image,*/ // don't load image, we don't need it
                                            Filename = x.Filename,
                                            Description = x.Description,
                                            ContainsPII = x.ContainsPII,
                                            HardcopyReceived = x.HardcopyReceived,
                                            MD5Hash = x.MD5Hash
                                        })
                                        .FirstOrDefaultAsync();
            if (attachment == null)
            {
                return JsonError("Attachment not found, or attachment type changed");
            }

            _context.RequestAttachments.Attach(attachment);

            DateTime reviewDate = DateTime.UtcNow; // use same date for both attachment and history
            attachment.ReviewDate = reviewDate;
            attachment.ReviewBy = user.UserId;
            attachment.Approved = true;

            _context.RequestAttachmentReviewHistories.Add(new RequestAttachmentReviewHistory()
            {
                AttachmentId = attachmentId,
                Approved = true,
                ReviewDate = reviewDate,
                ReviewBy = user.UserId.Value,
                ReviewNotes = null
            });

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
    [Route("AttachmentReview/Reject")]
    public async Task<IActionResult> RejectAttachment([FromForm] Guid attachmentId,
            [FromForm] Guid attachmentTypeId,
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
            var attachment = await _context.RequestAttachments
                                        .Include(ra => ra.AttachmentType)
                                        .Where(a => a.AttachmentId == attachmentId
                                                && (a.AttachmentTypeId == attachmentTypeId || a.AttachmentTypeId == null))
                                        .Select(x => new RequestAttachments()
                                        {
                                            Approved = x.Approved,
                                            AttachmentId = x.AttachmentId,
                                            AttachmentTypeId = x.AttachmentTypeId,
                                            ReviewBy = x.ReviewBy,
                                            ReviewDate = x.ReviewDate,
                                            RequestId = x.RequestId,
                                            UploadedBy = x.UploadedBy,
                                            /*Image = x.Image,*/ // don't load image, we don't need it
                                            Filename = x.Filename,
                                            Description = x.Description,
                                            ContainsPII = x.ContainsPII,
                                            HardcopyReceived = x.HardcopyReceived,
                                            MD5Hash = x.MD5Hash
                                        })
                                        .FirstOrDefaultAsync();
            if (attachment == null)
            {
                return JsonError("Attachment not found, or attachment type changed");
            }
            if (attachment.AttachmentTypeId != attachmentTypeId)
            {
                // check against the Extra attachmentTypeId
                var extraAttachmentType = await _context.AttachmentTypes
                                                .Where(at => at.AttachmentTypeId == attachmentTypeId)
                                                .FirstOrDefaultAsync();
                if (extraAttachmentType == null || extraAttachmentType.Name != "Extra")
                {
                    return JsonError("Attachment type changed");
                }
            }
            _context.RequestAttachments.Attach(attachment);

            attachment.ReviewDate = DateTime.UtcNow;
            attachment.ReviewBy = user.UserId;
            attachment.Approved = false;
            attachment.AttachmentTypeId = null;

            await AddNoteAndRemark(attachment.RequestId.Value, user, note, remark);

            await _context.RequestAttachmentReviewHistories.AddAsync(new RequestAttachmentReviewHistory()
            {
                AttachmentId = attachmentId,
                Approved = false,
                ReviewDate = DateTime.UtcNow,
                ReviewBy = user.UserId.Value,
                ReviewNotes = note
            });

            await _context.SaveChangesAsync(user);

            return JsonSuccess();
        }
        catch (Exception ex)
        {
            LogError(ex, "RejectAttachment");
            return JsonError(ex.Message);
        }
    }

}