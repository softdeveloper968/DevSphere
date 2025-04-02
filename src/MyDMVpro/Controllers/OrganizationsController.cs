using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

[Authorize(Policy = "VendorAgentOnly")]
//[Route("Vendor/Organizations")]
public class OrganizationsController : BaseController
{
    public OrganizationsController(MaggardDMVContext context, IConfiguration configuration, ILogger<OrganizationsController> logger) : base(context, configuration, logger)
    {
    }

    public async Task<IActionResult> Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganizations()
    {
        try
        {
            var organizations = await _context.Organizations.ToListAsync();
            return Json(new { data = organizations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations");
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization()
    {
        if (ModelState.IsValid)
        {
            UserInfo ui = await GetCurrentUserAsync();

            await DataHelpers.CheckFeaturePermission(FeatureKey.CONTACTS, ui.UserId, create: true);

            var parser = new CrudParser<Organization>(this.Request.Form, CrudAction.create);
            var result = await parser.ProcessCreateAsync(_context, _context.Organizations, null);

            //if (parser.KeyValue != null)
            //{
            //    throw new Exception("CreateOrganization called with KeyValue");
            //}

            //Organization organization = new();
            //parser.UpdateInstance(organization);
            //await _context.SaveChangesAsync();

            //_context.Organizations.Add(organization);
            //await _context.SaveChangesAsync();

            //List<object> result = new();
            //result.Add(organization);

            return Json(new { data = result });
        }

        return BadRequest(ModelState);
    }

    StatusCodeResult CreatedResult()
    {
        return new StatusCodeResult((int)HttpStatusCode.Created);
    }

    [HttpPut]
    [HttpPost]
    public async Task<IActionResult> UpdateOrganization()
    {
        Organization organization = null;

        if (ModelState.IsValid)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                await DataHelpers.CheckFeaturePermission(FeatureKey.CONTACTS, ui.UserId, write: true);

                var parser = new CrudParser<Organization>(this.Request.Form, CrudAction.edit);
                var result = await parser.ProcessEditAsync(_context, _context.Organizations);
                return Json(new { data = result });
            }
            catch (DbUpdateConcurrencyException)
            {
                if ((await _context.OrganizationContacts.AnyAsync(o => o.OrganizationId == organization.OrganizationId)))
                {
                    throw;
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization");
            }
            return Ok();
        }

        return BadRequest(ModelState);
    }

    [HttpDelete]
    [HttpPost]
    public async Task<IActionResult> DeleteOrganization()
    {
        UserInfo ui = await GetCurrentUserAsync();

        await DataHelpers.CheckFeaturePermission(FeatureKey.CONTACTS, ui.UserId, delete: true);

        var parser = new CrudParser<Organization>(this.Request.Form, CrudAction.remove);
        var result = await parser.ProcessRemoveAsync(_context, _context.Organizations);
        return Json(new { data = result });
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
