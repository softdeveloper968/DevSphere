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
    [Authorize(Policy = "VendorAgentOnly")]
    public class FormAnalyzerController : BaseController
    {
        private FilterHelper<FormAnalyzerUploads> filterHelper;

        public FormAnalyzerController(MaggardDMVContext context, IConfiguration configuration, ILogger<FormAnalyzerController> logger) : base(context, configuration, logger)
        {
            filterHelper = new FilterHelper<FormAnalyzerUploads>(context, configuration, this);
        }

        public async Task<IActionResult> Index()
        {
            List<FormAnalyzerForms> list = new List<FormAnalyzerForms>();
            UserInfo user = await GetCurrentUserAsync();
            if (user != null)
            {
                list = await _context.FormAnalyzerForms.Where(f => f.VendorId == user.VendorId).ToListAsync();
            }
            var model = (list, await GetModelForNewForm());
            return View(model);
        }
        public async Task<IActionResult> Details(Guid id)
        {
            FormAnalyzerUploads frm = null;
            UserInfo user = await GetCurrentUserAsync();
            if (user != null && user.VendorId != null)
            {
                frm = await _context.FormAnalyzerUploads
                                    .Where(f => f.FileUploadId == id && f.VendorId == user.VendorId)
                                    .FirstOrDefaultAsync();
            }
            return PartialView(frm);
        }
        [HttpPost]
        public async Task<IActionResult> FormAnalyzerUploads()
        {
            return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<FormAnalyzerUploads> rows = null;
                rows = _context.FormAnalyzerUploads
                                    .AsNoTracking()
                                    .Where(f => vendorId != null && f.VendorId == vendorId)
                                    .Where(f => f.AttachmentId != null)
                                    .Where(f => f.RequestId == null);
                return rows;
            });
        }
        [HttpPost]
        public async Task<IActionResult> VinSearch([FromForm]string partialVin)
        {
            UserInfo user = await GetCurrentUserAsync();
            if (user == null || !user.IsVendorAgent)
            {
                return JsonError("Record not found or insufficient permissions");
            }
            else
            {
                if (string.IsNullOrEmpty(partialVin))
                    return JsonError("VIN required");

                try
                {
                    List<string> vins = null;
                    var matches = _context.Requests
                                            .Where(f => f.VendorId == user.VendorId);
                    partialVin = partialVin.Trim();
                    if (partialVin.Length == 17)
                    {
                        matches = matches.Where(f => f.Vin == partialVin);
                    }
                    else if (partialVin.Length == 6)
                    {
                        matches = matches.Where(f => f.Last6Vin == partialVin || f.Vin.StartsWith(partialVin));
                    }
                    else if (partialVin.Length > 6)
                    {
                        matches = matches.Where(f => f.Vin.StartsWith(partialVin) || f.Vin.EndsWith(partialVin));
                    }
                    else if (partialVin.Length >= 3)
                    {
                        // if partialVin = 'ABC', it these formats
                        // matches ABC12345678901234
                        // matches 12345678901234ABC
                        // matches 12345678901ABC123
                        matches = matches.Where(f => f.Vin.StartsWith(partialVin) || f.Vin.EndsWith(partialVin) || f.Last6Vin.StartsWith(partialVin));
                    }
                    else
                    {
                        vins = new List<string>();
                    }
                    vins ??= await matches.Select(m => m.Vin).Distinct().ToListAsync();
                    if (vins.Count == 0)
                    {
                        if (partialVin.Length == 17 && !VinHelper.IsValidVIN(partialVin))
                        {
                            List<string> list = VinHelper.GetPotentialVins(partialVin);
                            if (list.Count > 0)
                            {
                                List<VinInfo> pvs = await GetVinPartialDetails(list);
                                if (pvs.Count > 0)
                                {
                                    foreach (var pv in pvs)
                                    {
                                        vins.Add(pv.Vin);
                                    }
                                }
                            }
                        }
                    }
                    return JsonSuccess(vins);
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error in VinSearch");
                    return JsonError(ex.Message);
                }
            }
            return JsonSuccess();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUploads([FromForm]List<Guid> uploadIds)
        {
            if (uploadIds == null || uploadIds.Count == 0)
            {
                return JsonError("Bad request");
            }
            UserInfo user = await GetCurrentUserAsync();
            if (user == null || !user.IsVendorAgent)
            {
                return JsonError("Record not found or insufficient permissions");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (Guid g in uploadIds)
                {
                    string result = await InternalDeleteUpload(user, g);
                    if (!string.IsNullOrEmpty(result))
                        sb.AppendLine(result);
                }
                if (sb.Length > 0)
                {
                    return JsonError(sb.ToString());
                }
                return JsonSuccess();
            }
        }
        private async Task<string> InternalDeleteUpload(UserInfo user, Guid uploadId)
        {
            try
            {
                var upload = await _context.FormAnalyzerUploads
                                        .Where(f => f.FileUploadId == uploadId && f.VendorId == user.VendorId)
                                        .Where(f => f.RequestId == null)
                                        .FirstOrDefaultAsync();
                if (upload != null)
                {
                    var attachment = await _context.RequestAttachments
                                        .Where(r => r.AttachmentId == upload.AttachmentId && r.RequestId == null)
                                        .Select(r => new RequestAttachments() { AttachmentId = r.AttachmentId })
                                        .FirstOrDefaultAsync();
                    if (attachment == null)
                    {
                        return "Record not found or insufficient permissions";
                    }
                    _context.RequestAttachments.Remove(attachment);
                    await _context.SaveChangesAsync(user);
                    return null;
                }
                else
                {
                    return "Record not found or insufficient permissions";
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error in InternalDeleteUpload");
                return ex.Message;
            }
        }
        [HttpPost]
        public async Task<IActionResult> RescanUploads([FromForm]List<Guid> uploadIds)
        {
            if (uploadIds == null)
            {
                return JsonError("Bad request");
            }
            UserInfo user = await GetCurrentUserAsync();
            if (user == null || !user.IsVendorAgent)
            {
                return JsonError("Record not found or insufficient permissions");
            }
            else
            {
                await DataHelpers.TriggerFormAnalyzer(uploadIds, _logger);

                return JsonSuccess();
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteUpload([FromForm]Guid? uploadId)
        {
            if (uploadId == null)
            {
                return JsonError("Bad request");
            }
            UserInfo user = await GetCurrentUserAsync();
            if (user == null || !user.IsVendorAgent)
            {
                return JsonError("Record not found or insufficient permissions");
            }
            else
            {
                string result = await InternalDeleteUpload(user, uploadId.Value);
                if (result == null) return JsonSuccess();
                return JsonError(result);
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateVin([FromForm]Guid? uploadId, [FromForm]string vin)
        {
            if (uploadId == null || string.IsNullOrWhiteSpace(vin))
            {
                return JsonError("Bad request");
            }
            UserInfo user = await GetCurrentUserAsync();
            if (user == null || !user.IsVendorAgent)
            {
                return JsonError("Record not found or insufficient permissions");
            }
            else
            {
                try
                {
                    vin = vin.ToUpperInvariant();
                    await DataHelpers.UpdateFormAnalyzerUploadVin(user.UserId, user.VendorId, uploadId, vin);
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error in UpdateVin");
                    return JsonError(ex);
                }
            }
            return JsonSuccess();
        }
        [HttpPost]
        public async Task<IActionResult> UpdateRequest([FromForm]Guid? uploadId, [FromForm]Guid? requestId, [FromForm]string json)
        {
            if (requestId == null || uploadId == null)
            {
                return JsonError("Bad request");
            }
            try
            {
                UserInfo user = await GetCurrentUserAsync();
                if (user == null || !user.IsVendorAgent)
                {
                    return JsonError("Record not found or insufficient permissions");
                }

                var req = await _context.Requests
                                        .Where(r => r.RequestId == requestId && r.VendorId == user.VendorId)
                                        .FirstOrDefaultAsync();
                if (req == null)
                {
                    return JsonError("Record not found or insufficient permissions");
                }

                var upload = await _context.FormAnalyzerUploads
                                        .Where(f => f.FileUploadId == uploadId && f.VendorId == user.VendorId)
                                        .Where(f => f.RequestId == null)
                                        .FirstOrDefaultAsync();
                if (upload == null)
                {
                    return JsonError("Record not found or insufficient permissions");
                }

                // Assign the upload to the request
                upload.RequestId = requestId;

                if (upload.AttachmentId != null)
                {
                    bool markAsReceived = false;
                    var attachmentInfo = await _context.RequestAttachments
                                                .Include(ra => ra.AttachmentType)
                                                .Where(ra => ra.AttachmentId == upload.AttachmentId.Value)
                                                .Select(ra => new { ra.AttachmentId, ra.AttachmentTypeId, ra.AttachmentType.HardcopyOnly })
                                                .FirstOrDefaultAsync();
                    if (attachmentInfo.AttachmentId == Guid.Empty)
                    {
                        return JsonError("Error processing upload: Attachment not found.");
                    }
                    if (attachmentInfo.HardcopyOnly)
                    {
                        // check for existing hardcopy attachment
                        var existingHardcopy = await _context.RequestAttachments
                                                                    .Where(ra => (ra.AttachmentTypeId != null && ra.AttachmentTypeId == attachmentInfo.AttachmentTypeId)
                                                                                    && ra.RequestId == requestId)
                                                                    .Select(ra => new { ra.AttachmentId, ra.HardcopyReceived })
                                                                    .FirstOrDefaultAsync();
                        if (existingHardcopy != null)
                        {
                            // remove previous hardcopy attachment
                            if (existingHardcopy.AttachmentId != default)
                            {
                                RequestAttachments att = new RequestAttachments() { AttachmentId = existingHardcopy.AttachmentId };
                                _context.RequestAttachments.Remove(att);
                            }
                            // mark as received if previous hardcopy was received
                        }
                        markAsReceived = true;
                    }
                    // Initialize the attachment with the key
                    // and default values for fields being updated
                    // values are set after Attach, so the field change triggers 
                    // saving the changes to the database
                    RequestAttachments attachment = new RequestAttachments()
                    {
                        AttachmentId = attachmentInfo.AttachmentId,
                        RequestId = null,
                        DateAdded = DateTime.UtcNow,
                        HardcopyReceived = false
                    };
                    _context.Attach(attachment);
                    attachment.HardcopyReceived = markAsReceived;
                    attachment.RequestId = requestId;
                }
                if (json != null)
                {
                    ApplyJsonChangesToRequest(req, json);
                }
                await _context.SaveChangesAsync(user);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "Error in UpdateRequest");
                return JsonError(ex);
            }
        }
        private void ApplyJsonChangesToRequest(Requests req, string json)
        {
            try
            {
                Dictionary<string, string> fieldValues =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(req.JRequest);

                JArray jarray = JArray.Parse(json);
                foreach (var jobj in jarray.Children<JObject>())
                {
                    string fieldName = jobj.Value<string>("field");
                    string val = jobj.Value<string>("value");
                    if (fieldValues.ContainsKey(fieldName))
                    {
                        fieldValues[fieldName] = val;
                    }
                    else
                    {
                        fieldValues.Add(fieldName, val);
                    }
                }
                req.JRequest = Newtonsoft.Json.JsonConvert.SerializeObject(fieldValues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApplyJsonChangesToRequest: {Message}", ex.Message);
                throw new ApplicationException("Bad request");
            }
        }

        public int MaxUploadSize = 50 * 1024 * 1024; // 50 MB

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm]string formId, [FromForm]bool? allowDuplicates, [FromForm]bool? splitPages, [FromForm]string filedate)
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
            if (splitPages == null)
            {
                if (Guid.TryParse(formId, out var gFormId) )
                {
                    var isTitleForm = await _context.FormAnalyzerForms.AnyAsync(f => f.FormId == gFormId && f.ProcessAsTitle == true);
                    splitPages = isTitleForm;
                }
            }
            Guid? fileUploadId = null;
            DateTime? lastmod = null;
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

                Guid? fid = null;
                if (Guid.TryParse(formId, out Guid g))
                {
                    fid = g;
                }
                List<byte[]> pdfPages = new List<byte[]>();
                if (splitPages != null && splitPages.Value == true)
                {
                    stream.Position = 0;
                    pdfPages.AddRange(PdfHelper7.SplitPdf(stream));
                }
                else
                {
                    data = stream.ToArray();
                    pdfPages.Add(data);
                }
                int page = 0;
                int errorCount = 0;
                string ext = Path.GetExtension(filename);
                string basefilename = Path.GetFileNameWithoutExtension(filename);
                StringBuilder sbError = new StringBuilder();
                List<Task<HttpResponseMessage>> triggerTasks = new List<Task<HttpResponseMessage>>();

                foreach (byte[] pageData in pdfPages)
                {
                    string pagefilename = filename;
                    if (splitPages.HasValue && splitPages.Value == true)
                    {
                        pagefilename = $"{basefilename}-pg-{++page:D3}{ext}";
                        lastmod = lastmod.Value.AddMilliseconds(page);
                    }
                    _logger.LogInformation("FormAnalyzerUpload: Calling SubmitToFormAnalyzerUploads formId: {FormId}", fid);
                    (fileUploadId, errorMsg) = await DataHelpers.SubmitToFormAnalyzerUploads(pagefilename, null, userName, fid, pageData, allowDuplicates, lastmod);
                    if (fileUploadId != null)
                    {
                        _logger.LogInformation("FormAnalyzerUpload: Calling TriggerFormAnalyzer formId: {FileUploadId}", fileUploadId.Value);
                        triggerTasks.Add(DataHelpers.HttpTriggerAnalyzerAsync(fileUploadId.Value, _logger));
                    }
                    else
                    {
                        errorCount++;
                        sbError.AppendLine(errorMsg);
                        _logger.LogInformation("FormAnalyzerUpload: FileUploadId is NULL");
                    }
                    if (errorMsg != null)
                    {
                        _logger.LogError("FormAnalyzerUpload: SubmitToFormAnalyzerUploads error {ErrorMsg}", errorMsg);
                    }
                }

                await Task.WhenAll(triggerTasks);
                DataHelpers.LogHttpResponseMessages(triggerTasks, _logger);

                if (errorCount > 0)
                {
                    errorMsg = $"{errorCount} errors: {sbError.ToString()}";
                    return JsonError(errorMsg);
                }
            }
            return Ok(new { count = 1 });
        }
        [Route("FormAnalyzer/ViewAttachment")]
        [Route("FormAnalyzer/ViewAttachment/{id?}/{filename?}")]
        public async Task<IActionResult> ViewAttachment(Guid? id, string filename)
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
                if (attachment == null)
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
        [HttpPost]
        public async Task<IActionResult> AttachWithoutUpdate([FromForm]List<Guid> uploadIds)
        {
            if (uploadIds == null || uploadIds.Count == 0)
            {
                return JsonError("Bad request");
            }
            UserInfo user = await GetCurrentUserAsync();
            if (user == null || !user.IsVendorAgent)
            {
                return JsonError("Record not found or insufficient permissions");
            }
            else
            {
                await DataHelpers.FormAnalyzer_AttachWithoutUpdate(user, uploadIds);
                return JsonSuccess();
            }
        }
        [HttpPost]
        public async Task<IActionResult> ProcessTitles([FromForm]List<Guid> uploadIds)
        {
            if (uploadIds == null || uploadIds.Count == 0)
            {
                return JsonError("Bad request");
            }
            UserInfo user = await GetCurrentUserAsync();
            if (user == null || !user.IsVendorAgent)
            {
                return JsonError("Record not found or insufficient permissions");
            }
            else
            {
                await DataHelpers.FormAnalyzer_ProcessTitles(user, uploadIds);
                return JsonSuccess();
            }
        }
    }
}