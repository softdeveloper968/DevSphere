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

namespace MyDMVpro.Controllers;

[Authorize(Policy = "VendorAgentOnly")]
public class GroupLibraryController : BaseController
{
    private FilterHelper<GroupAttachment> filterHelper;

    public GroupLibraryController(MaggardDMVContext context, IConfiguration configuration, ILogger<FileLibraryController> logger) : base(context, configuration, logger)
    {
        filterHelper = new FilterHelper<GroupAttachment>(context, configuration, this);
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
                                            .Where(g => g.InternalFromClient == true)
                                            .AsNoTracking()
                                            .ToListAsync();
        return JsonSuccess(attachmentTypes);
    }

    [HttpPost]
    public async Task<IActionResult> GetLibrary(Guid? groupId)
    {
        //if (groupId == null)
        //{
        //    return JsonError("Group not found");
        //}
        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? userGroupId, Guid? userId)
        {
            IQueryable<GroupAttachment> rows = null;
            if (groupId == null)
            {
                // no records, just return empty set
                rows = _context.GroupAttachments
#if xDEBUG
                // in debug mode allow seeing all records
                                    .Include(g => g.ModifiedByUser)
                                    .Include(g => g.FileData)
                                    .Include(g => g.Group)
                                    .Where(g => vendorId == null || g.VendorId == vendorId)
#else
                                    .Where(g => false)
#endif
                                    .AsQueryable();
            }
            else {
                rows = _context.GroupAttachments
                                    .Include(g => g.AttachmentType)
                                    .Include(g => g.ModifiedByUser)
                                    .Include(g => g.FileData)
                                    .Include(g => g.Group)
                                    .Where(g => vendorId == null || g.VendorId == vendorId)
                                    .Where(g => (g.GroupId == groupId) || (groupId == null && vendorId != null))
                                    .AsQueryable();
            }
            rows = rows.Select(r => new GroupAttachment()
            {
                VendorId = r.VendorId,
                AttachmentTypeId = r.AttachmentTypeId,
                Description = r.Description,
                FileDataID = r.FileDataID,
                GroupAttachmentId = r.GroupAttachmentId,
                GroupId = r.GroupId,
                GroupName = r.Group.GroupName,
                LastModified = r.LastModified,
                ModifiedBy = r.ModifiedBy,
                DisplayName = r.DisplayName,
                LienholderName = r.LienholderName,
                FileName = r.FileData.FileName,
                ModifiedByName = r.ModifiedByUser.DisplayName,
                AttachmentTypeName = r.AttachmentType.Name
            });

            return rows;
        }, active: true, filterOnCurrentUser: true);
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromForm] Guid? groupId, [FromForm] Guid? attachmentId)
    {
        if (attachmentId == null || groupId == null)
        {
            return JsonError($"{nameof(groupId)} and {nameof(attachmentId)} required");
        }
        UserInfo user = await GetCurrentUserAsync();
        if (user == null)
        {

            return JsonError("Record not found or insufficient permissions");
        }
        if (user.IsVendorAgent)
        {
            // ok
        }
        else
        {
            if (user.GroupId != groupId || !user.IsGroupAdmin)
            {
                return JsonError("Insufficient permissions");
            }
        }

        string result = await InternalDeleteAttachment(user, groupId.Value, attachmentId.Value);
        if (!string.IsNullOrWhiteSpace(result))
        {
            return JsonError(result);
        }
        return JsonSuccess();
    }
    private async Task<string> InternalDeleteAttachment(UserInfo user, Guid groupId, Guid attachmentId)
    {
        try
        {
            if (user.IsVendorAgent)
            {
                // ok
            }
            else if (user.GroupId == groupId && user.IsGroupAdmin)
            {
                // allow group admin
            }
            else
            {
                return "Insufficient permissions";
            }
            var att = await _context.GroupAttachments
                                    .Where(r => user.VendorId == null || r.VendorId == user.VendorId)
                                    .Where(r => r.GroupId == groupId)
                                    .Where(r => r.GroupAttachmentId == attachmentId)
                                    .FirstOrDefaultAsync();
            if (att == null)
            {
                return "Record not found or has already been deleted";
            }
            Guid? fileDataID = att.FileDataID;

            _context.GroupAttachments.Remove(att);
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

    [HttpGet("/GroupLibrary/PreEdit/{attachmentId}")]
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
            var attachment = await _context.GroupAttachments.FindAsync(attachmentId);
            if (attachment == null)
            {
                return JsonError("Record not found");
            }
            attachment.FileData = null;
            return JsonSuccess(attachment);
        }
        catch (Exception ex)
        {
            LogError(ex, "PreEdit error");
            return JsonError($"Upload failed: {ex.Message}");
        }
    }
    [HttpPost]
    public async Task<IActionResult> Edit(IFormFile file,
        [FromForm] Guid? vendorId,
        [FromForm] Guid? groupId,
        [FromForm] Guid? attachmentId,
        [FromForm] string lienholderName,
        [FromForm] string filedate,
        [FromForm] string displayName,
        [FromForm] string description,
        [FromForm] bool? allowDuplicates,
        [FromForm] Guid? attachmentTypeId)
    {
        if (groupId == null)
        {
            return BadRequest(new { error = "Group not provided" });
        }

        UserInfo user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Ok(new { count = 0 });
        }
        if (user.IsVendorAgent)
        {
            if (vendorId == null)
            {
                vendorId = user.VendorId;
            }
            else
            {
                if (vendorId != user.VendorId)
                {
                    return BadRequest(new { error = "VendorId does not match" });
                }
            }
        }
        else
        {
            if (user.GroupId != groupId)
            {
                return BadRequest(new { error = "GroupId does not match" });
            }
        }
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
            var attachment = await _context.GroupAttachments.FindAsync(attachmentId);
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
            LogError(ex, "Group Library Edit error");
            return JsonError($"Upload failed: {ex.Message}");
        }
        return Ok(new { success = true, count = 1 });
    }
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file,
        [FromForm] Guid? vendorId,
        [FromForm] Guid? groupId,
        [FromForm] string lienholderName,
        [FromForm] string filedate,
        [FromForm] string displayName,
        [FromForm] string description,
        [FromForm] bool? allowDuplicates, 
        [FromForm] Guid? attachmentTypeId)
    {
        if (groupId == null)
        {
            return BadRequest(new { error = "Group not provided" });
        }

        UserInfo user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Ok(new { count = 0 });
        }
        if (user.IsVendorAgent)
        {
            if (vendorId == null)
            {
                vendorId = user.VendorId;
            }
            else
            {
                if (vendorId != user.VendorId)
                {
                    return BadRequest(new { error = "VendorId does not match" });
                }
            }
        }
        else
        {
            if (user.GroupId != groupId)
            {
                return BadRequest(new { error = "GroupId does not match" });
            }
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
                GroupAttachment attachment = new GroupAttachment()
                {
                    VendorId = vendorId,
                    GroupAttachmentId = Guid.NewGuid(),
                    GroupId = groupId.Value,
                    FileDataID = fileData.Id,
                    DisplayName = displayName ?? filenameNoExt,
                    Description = description,
                    AttachmentTypeId = attachmentTypeId
                };
                _context.FileData.Add(fileData);
                _context.GroupAttachments.Add(attachment);
                await _context.SaveChangesAsync(user);
            }
            catch (Exception ex)
            {
                LogError(ex, "Group Library Upload error");
                return JsonError($"Upload failed: {ex.Message}");
            }
        }
        return Ok(new { success = true, count = 1 });
    }

    [Route("GroupLibrary/View/{id?}/{filename?}")]
    public async Task<IActionResult> ViewAttachment(Guid? id, string filename)
    {
        if (id == null || string.IsNullOrWhiteSpace(filename))
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
            var attachment = await _context.GroupAttachments
                                                .Where(g => g.GroupAttachmentId == id)
                                                .Where(g => g.GroupId == user.GroupId || user.IsVendorAgent)
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(m => m.GroupAttachmentId == id);
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

    [Route("GroupLibrary/Download")]
    [Route("GroupLibrary/Download/{id}")]
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
            if (user == null)
            {
                return NotFound();
            }

            var attachment = await _context.GroupAttachments
                                                .Include(x => x.FileData)
                                                .Where(g => g.GroupAttachmentId == id)
                                                .Where(g => g.GroupId == user.GroupId || user.IsVendorAgent)
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
