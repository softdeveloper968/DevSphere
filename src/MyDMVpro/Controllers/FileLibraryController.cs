using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyDMVpro.Models;
using MyDMVpro.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common.ViewHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;

namespace MyDMVpro.Controllers
{
    [Authorize(Policy = "ActiveUser")]
    public class FileLibraryController : BaseController
    {
        private FilterHelper<RequestAttachments> filterHelper;

        public FileLibraryController(MaggardDMVContext context, IConfiguration configuration, ILogger<FileLibraryController> logger) : base(context, configuration, logger)
        {
            filterHelper = new FilterHelper<RequestAttachments>(context, configuration, this);
        }
        public async Task<IActionResult> Index()
        {
            FileLibraryViewModel model = new FileLibraryViewModel();

            model.AttachmentTypes = await _context.MdpAttachmentTypes
                                                    .Where(a => a.HardcopyOnly == false)
                                                    .Where(a => !a.InternalFromVendor && !a.InternalFromClient)
                                                    .OrderBy(a => a.Name)
                                                    .ToListAsync();
            var extraType = model.AttachmentTypes.Where(a => a.Name == "Extra").FirstOrDefault();
            if (extraType != null && model.AttachmentTypes.Count > 1)
            {
                model.AttachmentTypes.Remove(extraType); 
                model.AttachmentTypes.Add(extraType); // append at end
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GetFileLibrary()
        {
            return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<RequestAttachments> rows = null;
                rows = _context.RequestAttachments
                                    .Where(f => f.UploadedBy == userId.Value)
                                    .Where(f => f.AttachmentId != default) //AttachmentId if of value type Guid, which is non-nullable.
                                    .Where(f => f.RequestId == null)
                                    .Include(f => f.AttachmentType)
                                    .Select(f => new RequestAttachments() 
                                    { 
                                        AttachmentId = f.AttachmentId, 
                                        Filename = f.Filename,
                                        //Description = f.Description, // not yet used in File Library uploads
                                        DateAdded = f.DateAdded.Value.Date,
                                        AttachmentTypeId = f.AttachmentTypeId,
                                        AttachmentTypeName = f.AttachmentType.Name
                                    });
                return rows;
            }, active: true, filterOnCurrentUser: true);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteAttachment([FromForm] Guid? attachmentId)
        {
            if (attachmentId == null) {
                return JsonError("Attachment id required");
            }
            UserInfo user = await GetCurrentUserAsync();
            if (user == null)
            {
                return JsonError("Record not found or insufficient permissions");
            }

            string result = await InternalDeleteAttachment(user, attachmentId.Value);
            if (!string.IsNullOrWhiteSpace(result))
            {
                return JsonError(result);
            }
            return JsonSuccess();
        }
        [HttpPost]
        public async Task<IActionResult> DeleteAttachments([FromForm] List<Guid> attachmentIds)
        {
            if (attachmentIds == null || attachmentIds.Count == 0)
            {
                return JsonError("Attachment id required");
            }
            UserInfo user = await GetCurrentUserAsync();
            if (user == null)
            {
                return JsonError("Record not found or insufficient permissions");
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                string result = await InternalDeleteAttachments(user, attachmentIds);
                if (!string.IsNullOrEmpty(result))
                    sb.AppendLine(result);

                if (sb.Length > 0)
                {
                    return JsonError(sb.ToString());
                }
                return JsonSuccess();
            }
        }
        private async Task<string> InternalDeleteAttachments(UserInfo user, List<Guid> attachmentIds)
        {
            try
            {
                string msg = await DataHelpers.FileLibrary_DeleteUnlinkedAttachments(user.UserId, attachmentIds, this._logger);
                return msg;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        private async Task<string> InternalDeleteAttachment(UserInfo user, Guid attachmentId)
        {
            try
            {
                var att = await _context.RequestAttachments
                                        .Where(r => r.AttachmentId == attachmentId 
                                                    && r.RequestId == null
                                                    && r.UploadedBy == user.UserId)
                                        .Select(r => new RequestAttachments() { AttachmentId = r.AttachmentId })
                                        .FirstOrDefaultAsync();
                if (att == null)
                {
                    return "Record not found or has already been deleted";
                }
                if (att.RequestId == null)
                {
                    _context.RequestAttachments.Remove(att);
                    await _context.SaveChangesAsync(user);
                    return null;
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [HttpPost]
        public async Task<IActionResult> AttachToRequest([FromForm] List<Guid> attachmentIds, [FromForm] Guid? requestId, [FromForm] Guid? attachmentTypeId)
        {
            if (requestId == null || attachmentIds == null || attachmentIds.Count == 0)
            {
                return JsonError("Bad request");
            }
            UserInfo user = await GetCurrentUserAsync();
            if (user == null)
            {
                return JsonError("Record not found or insufficient permissions");
            }
            else
            {
                var extraType = await _context.MdpAttachmentTypes.SingleOrDefaultAsync(x => x.Name == "Extra");

                var req = await _context.Requests.Where(r => r.RequestId == requestId && r.GroupId == user.GroupId)
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync();
                if (req == null)
                {
                    return JsonError("Request not found or insufficient permissions");
                }

                var attachments = await _context.RequestAttachments
                                        .Where(f => f.UploadedBy == user.UserId)
                                        .Where(f => f.RequestId == null)
                                        .Where(f => attachmentIds.Contains(f.AttachmentId))
                                        // pull only Id and RequestId to prevent loading attachment image
                                        // then just set the RequestId for each attachment
                                        .Select(f => new RequestAttachments() { 
                                            AttachmentId = f.AttachmentId, 
                                            RequestId = f.RequestId, 
                                            Filename = f.Filename,
                                            AttachmentTypeId = f.AttachmentTypeId
                                        })
                                        .ToListAsync();
                if (attachments == null)
                {
                    return JsonError("Error processing upload: Attachment not found.");
                }
                foreach (var attachment in attachments)
                {
                    _context.RequestAttachments.Attach(attachment);
                    attachment.RequestId = requestId;
                    if (attachmentTypeId == null)
                    {
                        attachment.AttachmentTypeId = extraType.AttachmentTypeId;
                    }
                    else
                    {
                        attachment.AttachmentTypeId = attachmentTypeId;
                    }
                    if (!user.IsVendorAgent)
                    {
                        if (_configuration.GetValue<bool>("AppSettings:AddChatOnAttachmentUpload", false))
                        {
                            await AddChat(requestId.Value, user, $"Uploaded attachment '{attachment.Filename}'", false);
                        }
                    }
                }

                try
                {
                    await _context.SaveChangesAsync(user);
                    return JsonSuccess();
                }
                catch (Exception ex)
                {
                    return JsonError("Error saving changes", ex);
                }
            }
        }
        public int MaxUploadSize = 50 * 1024 * 1024; // 50 MB

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] bool? allowDuplicates, [FromForm] string filedate, [FromForm]Guid? attachmentTypeId)
        {
            UserInfo user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Ok(new { count = 0 });
            }
            if (file == null)
            {
                return BadRequest(new { error = "File not provided" });
            }
            if (file.Length > MaxUploadSize)
            {
                return BadRequest(new { error = "File too large" });
            }
            Guid? attachmentId = null;
            DateTime? lastmod;
            if (DateTime.TryParse(filedate, out DateTime dt))
            {
                lastmod = dt;
            }
            else
            {
                lastmod = DateTime.UtcNow;
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);

                byte[] data = null;
                // Save to database
                string userName = GetUserSID();
                string filename = Path.GetFileName(file.FileName);
                string errorMsg = null;

                data = stream.ToArray();

                int page = 0;
                int errorCount = 0;
                string ext = Path.GetExtension(filename);
                string basefilename = Path.GetFileNameWithoutExtension(filename);
                StringBuilder sbError = new StringBuilder();
                List<Task<HttpResponseMessage>> triggerTasks = new List<Task<HttpResponseMessage>>();

                string pagefilename = filename;
                string msg;
                (attachmentId, msg) = await DataHelpers.FileLibrary_UploadFile(filename, user.UserId, data, allowDuplicates, lastmod, attachmentTypeId);
                if (attachmentId == null)
                {
                    errorCount++;
                    sbError.AppendLine(errorMsg);
                    _logger?.LogInformation("FormLibrary Upload: AttachmentId is NULL");
                }

                if (errorCount > 0)
                {
                    errorMsg = $"{errorCount} errors: {sbError.ToString()}";
                    return JsonError(errorMsg);
                }
            }
            return Ok(new { success = true, count = 1 });
        }

        [Route("FileLibrary/ViewAttachment")]
        [Route("FileLibrary/ViewAttachment/{id?}/{filename?}")]
        public async Task<IActionResult> ViewAttachment(Guid? id, string filename)
        {
            if (id == null || string.IsNullOrWhiteSpace(filename))
            {
                return NotFound();
            }
            this.ViewData["AttachmentId"] = id;
            this.ViewData["Filename"] = filename;
            string ext = Path.GetExtension(filename).ToLower();
            this.ViewData["MimeType"] = MimeTypeMap.GetMimeType(ext);
            return View("ViewAttachment");
        }
        [Route("FileLibrary/DownloadAttachment")]
        [Route("FileLibrary/DownloadAttachment/{id?}/{filename?}")]
        public async Task<IActionResult> DownloadAttachment(Guid? id, string filename)
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
                if (user == null)
                {
                    return NotFound();
                }

                var attachment = await _context.RequestAttachments
                            .FirstOrDefaultAsync(m => m.AttachmentId == id && m.UploadedBy == user.UserId);
                if (attachment == null)
                {
                    return NotFound();
                }

                return ReturnFileWithInlineDisposition(attachment.Filename, attachment.Image);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DownloadAttachment error");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetRequestAttachmentTypes(Guid? id)
        {
            if (id == null)
            {
                return BadRequest();
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
            string attachmentsTypesForUpload = "";

            var availableAttachmentTypes = await _context.MdpAppTypeAttachmentTypes
                                                            .Include(x => x.AttachmentType)
                                                            .Where(x => x.AppTypeStateId == appTypeState.AppTypeStateId)
                                                            .Where(x => x.AttachmentType != null
                                                                        && !x.AttachmentType.HardcopyOnly
                                                                        && !x.AttachmentType.InternalFromVendor
                                                                        && !x.AttachmentType.InternalFromClient)
                                                            .AsNoTracking()
                                                            .ToListAsync();

            var extraAttachmentOption = await _context.MdpAttachmentTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Name == "Extra");

            var requestAttachmentTypes = availableAttachmentTypes
                                                            .Select(x => new
                                                            {
                                                                AttachmentTypeName = x.AttachmentType.Name 
                                                                , x.AttachmentTypeId 
                                                                , NeedsAttachment = true
                                                                , success = true
                                                            })
                                                            .ToList();
            return JsonSuccess(requestAttachmentTypes);
        }
    }
}
