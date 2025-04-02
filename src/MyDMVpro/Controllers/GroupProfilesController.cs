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
using System.Net;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

[Authorize()]
public class GroupProfilesController : BaseController
{

    public GroupProfilesController(MaggardDMVContext context, IConfiguration configuration, ILogger<GroupProfilesController> logger) : base(context, configuration, logger)
    {
    }

    public async Task<IActionResult> Index()
    {
        return View();
    }

    /// <summary>
    /// This api returns a json object with values from a profile 
    /// to update the form entry
    /// </summary>
    /// <param name="groupProfileId"></param>
    /// <param name="appTypeId"></param>
    /// <param name="sectionId"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("GroupProfiles/GetProfileForUpdate/{groupProfileID}/{sectionId}")] // Profiles for specific group and profile category
    public async Task<IActionResult> GetProfileForUpdate(Guid? groupProfileId, Guid? appTypeId, Guid? sectionId, bool includeInactive)
    {
        UserInfo user = await GetCurrentUserAsync();

        if (user.IsGroupMember && groupProfileId != user.GroupId)
        {
            return NotFound();
        }

        // Get profile content
        var profileQuery = _context.GroupProfiles.Where(p => p.GroupProfileID == groupProfileId);
        // TBD: turn on after adding UI to toggle active in profile
        //if (!includeInactive)
        //{
        //    profileQuery = profileQuery.Where(p => p.Active);
        //}
        var profile = await profileQuery.FirstOrDefaultAsync();

        // Get section fields that map from profile fields
        var sectionFields = await _context.MdpAppSectionFields
                                                .Where(f => f.SectionId == sectionId)
                                                .Where(f => f.Field.GroupProfileSourceFieldId != null)
                                                .Select(f => new { ProfileSource = f.Field.GroupProfileSourceField.ExcelName, f.Field.ExcelName })
                                                .ToListAsync();
        Dictionary<string, object> results = new Dictionary<string, object>();
        foreach (var field in sectionFields)
        {
            if (!string.IsNullOrEmpty(field.ProfileSource))
            {
                var fieldValue = profile.Fields[field.ProfileSource];
                results.Add(field.ExcelName, fieldValue ?? "");
            }
        }

        // Translate to names
        return new JsonResult(results);
    }

    [HttpPost]
    //[Route("GroupProfiles/GetProfiles/{vendorGroupId}")] // All profiles for groupId specified (or all groups if user is a vendor and vendorGroupId is null)
    [Route("GroupProfiles/GetProfiles/{vendorGroupId}/{profileCategory?}")] // Profiles for specific group and profile category
    public async Task<IActionResult> GetProfiles(string vendorGroupId, string profileCategory, [FromQuery]bool includeInactive)
    {
        if (Guid.TryParse(vendorGroupId, out _))
        {
            // valid vendorGroupId
            System.Diagnostics.Debug.WriteLine($"vendorGroupId: {vendorGroupId}, profileCategory: {profileCategory}");
        }
        else
        {
            if (profileCategory == null)
            {
                profileCategory = vendorGroupId;
            }
            vendorGroupId = null;
        }
        var filterHelper = new FilterHelper<GroupProfile>((MaggardDMVContext)_context, _configuration, this);

        return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<GroupProfile> rows = null;
            if (vendorId != null && vendorGroupId == null)
            {
                // Vendors must have vendorGroupId, or return nothing
                rows = _context.GroupProfiles.Where(p => false);
                return rows;
            }
            if (vendorId != null)
            {
                if (vendorGroupId == null)
                {
                    rows = _context.GroupProfiles.AsQueryable();
                    return rows;
                }
                else
                {
                    Guid.TryParse(vendorGroupId, out Guid result);
                    rows = _context.GroupProfiles.Where(f => f.GroupId == result);
                }
            }
            else
            {
                rows = _context.GroupProfiles.Where(f => f.GroupId == groupId);
            }
            // TBD: Need to add abiltity to Activate/Deactivate profiles
            //if (!includeInactive){
            //    rows = rows.Where(f => f.Active);
            //}
            if (!string.IsNullOrEmpty(profileCategory))
            {
                rows = rows.Where(p => p.GroupProfileCategory.GroupProfileCategoryName == profileCategory);
            }
            return rows;
        });
    }
    //[HttpGet]
    //[HttpPost]
    //public async Task<IActionResult> GetProfiles(Guid? groupId)
    //{
    //    try
    //    {
    //        UserInfo ui = await GetCurrentUserAsync();

    //        await DataHelpers.CheckFeaturePermission(FeatureKey.MANAGEPROFILES, ui.UserId, read: true);

    //        // TBD: update to filter on user
    //        IQueryable<GroupProfile> query = null;
    //        if (ui.IsVendorAgent)
    //        {
    //            if (groupId == null)
    //            {
    //                query = _context.GroupProfiles.AsQueryable();
    //            }
    //            else
    //            {
    //                query = _context.GroupProfiles.Where(g => g.GroupId == groupId).AsQueryable();
    //            }
    //        }
    //        else
    //        {
    //            query = _context.GroupProfiles.Where(g => g.GroupId == ui.GroupId);
    //        }
    //        List<GroupProfile> profiles = await query.ToListAsync();
    //        return Json(new { data = profiles });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error getting profiles");
    //        return StatusCode((int)HttpStatusCode.InternalServerError);
    //    }
    //}

    [HttpPost]
    public async Task<IActionResult> CreateProfile()
    {
        if (ModelState.IsValid)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                await DataHelpers.CheckFeaturePermission(FeatureKey.MANAGEPROFILES, ui.UserId, create: true);

                var parser = new CrudParser<GroupProfile>(this.Request.Form, CrudAction.create);

                var result = await parser.ProcessCreateAsync(_context, _context.GroupProfiles, async (List<GroupProfile> data) =>
                {
                    if (ui.IsVendorAgent) return;

                    foreach (var rec in data)
                    {
                        if (rec.GroupId != ui.GroupId)
                        {
                            throw new ApplicationException("Invalid group id");
                        }
                    }
                });

                return Json(new { data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating profile");
                return BadRequest("Error creating profile: " + ex.Message);
            }
        }

        return BadRequest(ModelState);
    }

    StatusCodeResult CreatedResult()
    {
        return new StatusCodeResult((int)HttpStatusCode.Created);
    }

    [HttpPut]
    [HttpPost]
    public async Task<IActionResult> UpdateProfile()
    {
        GroupProfile profile = null;

        if (ModelState.IsValid)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                await DataHelpers.CheckFeaturePermission(FeatureKey.MANAGEPROFILES, ui.UserId, write: true);

                var parser = new CrudParser<GroupProfile>(this.Request.Form, CrudAction.edit);
                var result = await parser.ProcessEditAsync(_context, _context.GroupProfiles);

                return Json(new { data = result });
            }
            //catch (DbUpdateConcurrencyException)
            //{
            //    if ((await _context.GroupProfiles.AnyAsync(o => o.GroupProfileID == profile.GroupProfileID)))
            //    {
            //        throw;
            //    }
            //    return NotFound();
            //}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
            }
            return Ok();
        }

        return BadRequest(ModelState);
    }

    [HttpDelete]
    [HttpPost]
    public async Task<IActionResult> DeleteProfile()
    {
        try
        {

            UserInfo ui = await GetCurrentUserAsync();

            await DataHelpers.CheckFeaturePermission(FeatureKey.MANAGEPROFILES, ui.UserId, delete: true);

            var parser = new CrudParser<GroupProfile>(this.Request.Form, CrudAction.remove);
            var result = await parser.ProcessRemoveAsync(_context, _context.GroupProfiles);
            return Json(new { data = result });
        }
        catch (Exception ex)
        {

        }
        return NotFound();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context.Dispose();
        }
        base.Dispose(disposing);
    }
}
