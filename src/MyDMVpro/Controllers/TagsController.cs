using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

[Authorize]
public class TagsController : BaseController
{
    private FilterHelper<Tag> filterHelper;

    public TagsController(MaggardDMVContext context, IConfiguration configuration, ILogger<TagsController> logger) : base(context, configuration, logger)
    {
        filterHelper = new FilterHelper<Tag>(context, configuration, this, FilterBySettings);
    }

    [HttpPost("/Tags/FollowUpTags")]
    public async Task<IActionResult> PostFollowUpTags()
    {
        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<Tag> rows = null;

            rows = _context.Tag.Where(t => t.VendorId == vendorId.Value);
            rows = rows.Where(t => t.TagType == "FollowUp");

            return rows;
        }, true, false);
    }

    [HttpGet("/Tags/FollowUpTags")]
    public async Task<IActionResult> GetFollowUpTags()
    {
        var user = await GetCurrentUserAsync();

        var list = await _context.Tag
                                    .Where(t => t.VendorId == user.VendorId && t.TagType == "FollowUp")
                                    .AsNoTracking()
                                    .Select(t => new { id = t.TagId, tag = t.TagName, desc = t.TagDesc, @class = t.TagClass })
                                    .ToListAsync();

        return new JsonResult(list);
    }

    [HttpPost("/Tags/RequestCodeTags")]
    public async Task<IActionResult> PostRequestCodeTags()
    {
        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<Tag> rows = null;

            rows = _context.Tag
                                .Include(t => t.TagCategory)
                                .AsNoTracking()
                                .Where(t => t.VendorId == vendorId.Value && t.TagType == "Request");

            return rows;
        }, true, false);
    }

    [HttpGet("/Tags/RequestCodeTags")]
    public async Task<IActionResult> GetRequestCodeTags()
    {
        var user = await GetCurrentUserAsync();

        var list = await _context.Tag
                                    .Where(t => t.VendorId == user.VendorId && t.TagType == "Request")
                                    .AsNoTracking()
                                    .Select(t => new { id = t.TagId, tag = t.TagName, desc = t.TagDesc, @class = t.TagClass, category = t.TagCategory, categoryId = t.TagCategoryId })
                                    .ToListAsync();

        return new JsonResult(list);
    }
    [HttpGet("/Tags/Categories")]
    public async Task<IActionResult> GetTagCategories()
    {
        var user = await GetCurrentUserAsync();

        var list = await _context.TagCategories
                                    .ToListAsync();

        return new JsonResult(list);
    }

    [HttpGet("/Tags/Get/{tagId}")]
    public async Task<IActionResult> GetTag(int tagId)
    {
        try
        {
            var user = await GetCurrentUserAsync();

            var tag = await _context.Tag.Where(t => t.VendorId == user.VendorId && t.TagId == tagId)
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync();

            return new JsonResult(tag);
        }
        catch (Exception ex)
        {
            var result = new JsonResult(ex.Message);
            result.StatusCode = StatusCodes.Status500InternalServerError;
            return result;
        }
    }

    [HttpPost("/Tags/Delete")]
    public async Task<IActionResult> DeleteTag([FromBody] RequestTag_Delete_Model data)
    {
        try
        {
            var user = await GetCurrentUserAsync();

            var tag = await _context.Tag.Where(t => t.VendorId == user.VendorId && t.TagId == data.TagId && t.TagType == data.TagType)
                                        .FirstOrDefaultAsync();
            if (tag == null)
            {
                return JsonError("Tag not found.");
            }

            if (await TagHasDependencies(tag))
            {
                return JsonError("Tag is in use and cannot be deleted");
            }

            _context.Tag.Remove(tag);
            await _context.SaveChangesAsync(user);

            return new JsonResult(tag);
        }
        catch (Exception ex)
        {
            var result = new JsonResult(ex.Message);
            result.StatusCode = StatusCodes.Status500InternalServerError;
            return result;
        }
    }

    private async Task<bool> TagHasDependencies(Tag tag)
    {
        if (tag.TagType == "Request")
        {
            return await _context.RequestCodes.AnyAsync(rc => rc.TagId == tag.TagId);
        }
        else if (tag.TagType == "FollowUp")
        {
            return await _context.FollowUpTags.AnyAsync(fuc => fuc.TagId == tag.TagId);
        }
        return false;
    }

    [HttpPost("/Tags/Edit")]
    public async Task<IActionResult> EditTag([FromBody] RequestTag_Edit_Model data)
    {
        try
        {
            var user = await GetCurrentUserAsync();

            var tag = await _context.Tag.Where(t => t.VendorId == user.VendorId && t.TagId == data.TagId && t.TagType == data.TagType)
                                        .FirstOrDefaultAsync();
            if (tag == null)
            {
                return JsonError("Tag not found.");
            }
            tag.TagDesc = data.TagDesc;
            tag.TagClass = data.TagClass;
            tag.TagCategoryId = data.TagCategoryId;
            tag.Disabled = data.Disabled;

            await _context.SaveChangesAsync(user);

            return JsonSuccess();
        }
        catch (Exception ex)
        {
            var result = new JsonResult(ex.Message);
            result.StatusCode = StatusCodes.Status500InternalServerError;
            return result;
        }
    }

    [HttpPost("/Tags/Add")]
    public async Task<IActionResult> AddTag([FromBody] RequestTag_New_Model data)
    {
        if (data?.TagName == null)
        {
            return JsonError("Tag name is required.");
        }

        try
        {
            var user = await GetCurrentUserAsync();

            var tag = await _context.Tag
                                    .Where(t => t.VendorId == user.VendorId && t.TagName == data.TagName && t.TagType == data.TagType)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(); // can only be one with requestid/followupid
            if (tag != null)
            {
                return NotFound();
            }

            tag = new Tag()
            {
                VendorId = user.VendorId.Value,
                TagName = data.TagName,
                TagType = data.TagType,
                TagDesc = data.TagDesc,
                TagClass = data.TagClass,
                TagCategoryId = data.TagCategoryId,
                Disabled = data.Disabled
            };

            _context.Tag.Add(tag);

            await _context.SaveChangesAsync(user);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error creating tag");
            return JsonError("Error saving record.", ex);
        }
        return JsonSuccess();
    }

    public IQueryable<Tag> FilterBySettings(IQueryable<Tag> rows, DatatableFormData dfd)
    {
        return rows;
    }
}
