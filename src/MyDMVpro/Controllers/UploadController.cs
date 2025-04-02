using iText.Layout.Element;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using MyDMVpro.Models.SharedViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MyDMVpro.Controllers
{
    [AllowAnonymous]
    public class UploadController : BaseController
    {
        public UploadController(MaggardDMVContext context, IConfiguration configuration, ILogger<UploadController> logger) : base(context, configuration, logger)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Upload(string? token)
        {
            var uploadLink = _context.UploadLinks.FirstOrDefault(x => x.Token == token);

            if (uploadLink == null || uploadLink.IsUsed || uploadLink.IsDocumentUploaded)
            {
                return Content("This upload link is invalid or has expired.");
            }

            var request = await _context.Requests.SingleOrDefaultAsync(x => x.RequestId == uploadLink.RequestId);
            if (request == null)
            {
                return BadRequest();
            }
            var appTypeState = await _context.MdpAppTypeStates.SingleOrDefaultAsync(x => x.AppState == request.State && x.AppType.AppType == request.AppType);
            if (appTypeState == null)
            {
                return BadRequest();
            }

            var availableAttachmentTypes = await _context.MdpAppTypeAttachmentTypes
                                                            .Include(x => x.AttachmentType)
                                                            .Where(x => x.AttachmentTypeId == uploadLink.AttachmentTypeId && x.AppTypeStateId == appTypeState.AppTypeStateId)
                                                            .ToListAsync();

            var extraAttachmentOption = await _context.MdpAttachmentTypes.SingleOrDefaultAsync(x => x.Name == "Extra");


            List<MdpAttachmentTypes> requestAttachmentTypes = availableAttachmentTypes
                                                                    .Where(x => x.AttachmentType != null)
                                                                    .Select(x => x.AttachmentType)
                                                                    .ToList();


            UploadAttachmentsViewModel attachmentsViewModel = new()
            {
                RequestId = (Guid)uploadLink.RequestId,
                RequestNo = request.Id,
                AppTypeAttachmentTypes = availableAttachmentTypes,
                AttachmentTypes = requestAttachmentTypes,
                AttachmentNotes = appTypeState.AttachmentNotes,
                Request = request,
                UploadLinkToken = token.ToString()
            };

            return PartialView("_UploadLinkView", attachmentsViewModel);

        }

        [HttpGet]
        public async Task<IActionResult> ReviewUploadAttachments(string? token)
        {
            var uploadLink = _context.UploadLinks.FirstOrDefault(x => x.Token == token);

            if (uploadLink == null || uploadLink.IsUsed || uploadLink.IsDocumentUploaded)
            {
                return Json(new { success = false, message = "This upload link is invalid or has expired." });
            }
            else
            {
                return Json(new { success = true, message = "You can upload the file now"});
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(Guid? id,
        IFormFile file,
        string filedesc,
        Guid? attachmentTypeId,
        string? token)
        {

            var uploadLink = _context.UploadLinks.FirstOrDefault(x => x.Token == token);

            if (uploadLink == null || uploadLink.IsUsed || uploadLink.IsDocumentUploaded)
                return Content("This upload link is invalid or has expired.");
            

            if (id == null)
                return JsonError("Request not found");
            

            if (file == null || file.Length == 0)
                return JsonError("File content missing");

            try
            {

                var requests = await _context.Requests.FirstOrDefaultAsync(m => m.RequestId == id);

                if (requests == null)
                {
                    return JsonError("Request not found");
                }

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
                    Image = filedata,
                    DateAdded = DateTime.UtcNow,
                    AttachmentTypeId = attachmentTypeId
                };
                requests.RequestAttachments.Add(attachment);

                uploadLink.IsUsed = true;
                uploadLink.IsDocumentUploaded = true;

                await _context.SaveChangesAsync();
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "RequestsController.UploadAttachment");
                return JsonError(ex);
            }
        }


    }
}
