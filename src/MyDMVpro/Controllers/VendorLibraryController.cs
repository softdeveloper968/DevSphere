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
using Microsoft.DotNet.MSIdentity.Shared;

namespace MyDMVpro.Controllers;

[Authorize(Policy = "VendorAgentOnly")]
public class VendorLibraryController : BaseController
{
    private FilterHelper<VendorAttachment> filterHelper;

    public VendorLibraryController(MaggardDMVContext context, IConfiguration configuration, ILogger<FileLibraryController> logger) : base(context, configuration, logger)
    {
        filterHelper = new FilterHelper<VendorAttachment>(context, configuration, this);
    }
    public async Task<IActionResult> Index()
    {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> AttachmentTypes()
    {
        UserInfo userInfo = await GetCurrentUserAsync();
        if (userInfo == null)
        {
            return JsonError("User not found");
        }
        var attachmentTypes = await _context.AttachmentTypes
                                            .Where(g => g.InternalFromVendor == true)
                                            .AsNoTracking()
                                            .ToListAsync();
        return JsonSuccess(attachmentTypes);
    }

    [HttpPost]
    public async Task<IActionResult> GetLibrary()
    {
        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? userGroupId, Guid? userId)
        {
            IQueryable<VendorAttachment> rows = null;

            if (vendorId == null)
            {
                // non-vendors should not have access to this api, so should never get here
                return null;
            }
            rows = _context.VendorAttachments
                                .Include(g => g.AttachmentType)
                                .Include(g => g.ModifiedByUser)
                                .Include(g => g.FileData)
                                .Where(g => g.VendorId == vendorId)
                                .AsQueryable();

            rows = rows.Select(r => new VendorAttachment()
            {
                VendorId = r.VendorId,
                VendorAttachmentId = r.VendorAttachmentId,
                AttachmentTypeId = r.AttachmentTypeId,
                Description = r.Description,
                FileDataID = r.FileDataID,
                LastModified = r.LastModified,
                ModifiedBy = r.ModifiedBy,
                DisplayName = r.DisplayName,
                FileName = r.FileData.FileName,
                AttachmentTypeName = r.AttachmentType.Name,
                ModifiedByName = r.ModifiedByUser.DisplayName
            });

            return rows;
        }, active: true, filterOnCurrentUser: true);
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromForm] Guid? vendorId, [FromForm] Guid? attachmentId)
    {
        if (attachmentId == null || vendorId == null)
        {
            return JsonError($"{nameof(vendorId)} and {nameof(attachmentId)} required");
        }
        UserInfo user = await GetCurrentUserAsync();
        if (user == null)
        {

            return JsonError("Record not found or insufficient permissions");
        }
        if (!user.IsVendorAgent)
        {
            // ok
            return JsonError("Insufficient permissions");
        }

        string result = await InternalDeleteAttachment(user, vendorId.Value, attachmentId.Value);
        if (!string.IsNullOrWhiteSpace(result))
        {
            return JsonError(result);
        }
        return JsonSuccess();
    }
    private async Task<string> InternalDeleteAttachment(UserInfo user, Guid vendorId, Guid attachmentId)
    {
        try
        {
            if (!user.IsVendorAgent)
            {
                // ok
                return "Insufficient permissions";
            }

            var att = await _context.VendorAttachments
                                    .Where(r => user.VendorId == null || r.VendorId == user.VendorId)
                                    .Where(r => r.VendorAttachmentId == attachmentId)
                                    .FirstOrDefaultAsync();
            if (att == null)
            {
                return "Record not found or has already been deleted";
            }
            Guid? fileDataID = att.FileDataID;

            _context.VendorAttachments.Remove(att);
            await _context.SaveChangesAsync(user);
            if (fileDataID != null)
            {
                try
                {
                    // Now try to delete the filedata record, if it's not used by any other attachment
                    var exists = await _context.FileData.AnyAsync(f => f.Id == fileDataID);
                    if (exists)
                    {
                        // deleting this way to prevent downloading the file content
                        FileData fd = new FileData() { Id = fileDataID.Value };
                        _context.FileData.Remove(fd);
                        await _context.SaveChangesAsync(user);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    // ignore and continue
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public int MaxUploadSize = 50 * 1024 * 1024; // 50 MB

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file,
        [FromForm] string filedate,
        [FromForm] string displayName,
        [FromForm] string description,
        [FromForm] bool? allowDuplicates,
        [FromForm] Guid? attachmentTypeId)
    {
        UserInfo user = await GetCurrentUserAsync();
        if (user == null || !user.IsVendorAgent)
        {
            return Ok(new { count = 0 });
        }

        Guid? vendorId = user.VendorId;

        if (file == null)
        {
            return BadRequest(new { error = "File not provided" });
        }
        if (file.Length > MaxUploadSize)
        {
            return BadRequest(new { error = "File too large" });
        }
        Guid? attachmentId = null;
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
            string filename = Path.GetFileName(file.FileName);
            string errorMsg = null;

            data = stream.ToArray();

            int page = 0;
            int errorCount = 0;
            string ext = Path.GetExtension(filename);
            string basefilename = Path.GetFileNameWithoutExtension(filename);
            StringBuilder sbError = new StringBuilder();

            try
            {
                FileData fileData = new FileData()
                {
                    Id = Guid.NewGuid(),
                    Size = data.Length,
                    FileName = filename,
                    Content = data
                };
                string filenameNoExt = Path.GetFileNameWithoutExtension(filename);
                VendorAttachment attachment = new VendorAttachment()
                {
                    VendorId = vendorId,
                    VendorAttachmentId = Guid.NewGuid(),
                    FileDataID = fileData.Id,
                    DisplayName = displayName ?? filenameNoExt,
                    Description = description,
                    AttachmentTypeId = attachmentTypeId
                };
                _context.FileData.Add(fileData);
                _context.VendorAttachments.Add(attachment);
                await _context.SaveChangesAsync(user);
            }
            catch (Exception ex)
            {
                return JsonError($"Upload failed: {ex.Message}");
            }
        }
        return Ok(new { success = true, count = 1 });
    }

    [HttpGet("/VendorLibrary/PreEdit/{attachmentId}")]
    public async Task<IActionResult> PreEdit(Guid? attachmentId)
    {
        UserInfo user = await GetCurrentUserAsync();
        if (user == null || !user.IsVendorAgent)
        {
            return Ok(new { count = 0 });
        }

        Guid? vendorId = user.VendorId;

        try
        {
            var attachment = await _context.VendorAttachments.FindAsync(attachmentId);
            if (attachment == null)
            {
                return JsonError("Record not found");
            }
            attachment.FileData = null;
            return JsonSuccess(attachment);
        }
        catch (Exception ex)
        {
            return JsonError($"Upload failed: {ex.Message}");
        }
    }
    [HttpPost]
    public async Task<IActionResult> Edit(IFormFile file,
        [FromForm] Guid? attachmentId,
        [FromForm] string filedate,
        [FromForm] string displayName,
        [FromForm] string description,
        [FromForm] bool? allowDuplicates,
        [FromForm] Guid? attachmentTypeId)
    {
        UserInfo user = await GetCurrentUserAsync();
        if (user == null || !user.IsVendorAgent)
        {
            return Ok(new { count = 0 });
        }

        Guid? vendorId = user.VendorId;

        if (file != null && file.Length > MaxUploadSize)
        {
            return BadRequest(new { error = "File too large" });
        }
        DateTime? lastmod = null;
        if (DateTime.TryParse(filedate, out DateTime dt))
        {
            lastmod = dt;
        }
        else
        {
            lastmod = DateTime.UtcNow;
        }

        byte[] data = null;
        string filename = null;

        if (file != null)
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);

                // Save to database
                filename = Path.GetFileName(file.FileName);
                string errorMsg = null;

                data = stream.ToArray();
            }
        }

        try
        {
            var attachment = await _context.VendorAttachments.FindAsync(attachmentId);
            if (attachment == null)
            {
                return JsonError("Record not found");
            }
            FileData fileData = null;
            string filenameNoExt = null;
            if (data != null)
            {
                fileData = await _context.FileData.FindAsync(attachment.FileDataID);
                if (fileData != null)
                {
                    fileData.Content = data;
                    fileData.Size = data.Length;
                    fileData.FileName = filename;
                    _context.FileData.Update(fileData);
                }
                else
                {
                    fileData = new FileData()
                    {
                        Id = Guid.NewGuid(),
                        Size = data.Length,
                        FileName = filename,
                        Content = data
                    };
                    _context.FileData.Add(fileData);
                    attachment.FileDataID = fileData.Id;
                }
                filenameNoExt = Path.GetFileNameWithoutExtension(filename);
            }
            attachment.DisplayName = displayName ?? filenameNoExt;
            attachment.Description = description;
            attachment.AttachmentTypeId = attachmentTypeId;

            await _context.SaveChangesAsync(user);
        }
        catch (Exception ex)
        {
            return JsonError($"Upload failed: {ex.Message}");
        }
        return Ok(new { success = true, count = 1 });
    }

    [Route("VendorLibrary/View/{id?}/{filename?}")]
    public async Task<IActionResult> ViewAttachment(Guid? id, string filename)
    {
        if (id == null || string.IsNullOrWhiteSpace(filename))
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
            var attachment = await _context.VendorAttachments
                                                .Where(g => g.VendorId == user.VendorId)
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(m => m.VendorAttachmentId == id);
            if (attachment == null)
            {
                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ViewAttachment error");
            throw;
        }
        this.ViewData["AttachmentId"] = id;
        this.ViewData["Filename"] = filename;
        string ext = Path.GetExtension(filename).ToLower();
        this.ViewData["MimeType"] = MimeTypeMap.GetMimeType(ext);
        return View("ViewAttachment");
    }

    [Route("VendorLibrary/Download")]
    [Route("VendorLibrary/Download/{id}")]
    public async Task<IActionResult> DownloadAttachment(Guid? id)
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

            var attachment = await _context.VendorAttachments
                                                .Include(x => x.FileData)
                                                .Where(g => g.VendorAttachmentId == id)
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync();
            if (attachment == null
                || attachment.FileDataID == null
                || attachment.FileData?.Content == null)
            {
                return NotFound();
            }

            return ReturnFileWithInlineDisposition(attachment.FileData.FileName, attachment.FileData.Content);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Download error");
            throw;
        }
    }

}
