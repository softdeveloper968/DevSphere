using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

[Authorize(Policy = "VendorAgentOnly")]
//[Route("Vendor/Contacts")]
public class ContactsController : BaseController
{
    public ContactsController(MaggardDMVContext context, IConfiguration configuration, ILogger<ContactsController> logger) : base(context, configuration, logger)
    {
    }

    public async Task<IActionResult> Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetContacts()
    {
        try
        {
            var contacts = await _context.OrganizationContacts.Include(oc => oc.Organization).ToListAsync();
            return Json(new { data = contacts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts");
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetContactsForTags()
    {
        try
        {
            var contacts = await _context.OrganizationContacts.Include(oc => oc.Organization)
                                            .AsNoTracking()
                                            .Select(oc => new { id = oc.ContactId, tag = $"{oc.ContactName} ({oc.Organization.OrganizationName})", desc = oc.Organization.OrganizationName, phone = oc.Phone, email = oc.Email, authorityStates = oc.Organization.AuthorityStatesList })
                                            .ToListAsync();

            return new JsonResult(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts");
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateContact()
    {
        if (ModelState.IsValid)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                await DataHelpers.CheckFeaturePermission(FeatureKey.CONTACTS, ui.UserId, create: true);

                var parser = new CrudParser<OrganizationContact>(this.Request.Form, CrudAction.create);
                var result = await parser.ProcessCreateAsync(_context, _context.OrganizationContacts, null);
                await UpdateOrganizations(result);

                return Json(new { data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
            }
        }

        return BadRequest(ModelState);
    }
    private async Task UpdateOrganizations(List<OrganizationContact> contacts)
    {
        foreach (var contact in contacts)
        {
            if (contact.Organization == null && contact.OrganizationId != null)
            {
                // refresh the contact to include the organization
                contact.Organization = await _context.Organizations.Where(oc => oc.OrganizationId == contact.OrganizationId).FirstOrDefaultAsync();
            }
        }
    }
    StatusCodeResult CreatedResult()
    {
        return new StatusCodeResult((int)HttpStatusCode.Created);
    }

    [HttpPut]
    [HttpPost]
    public async Task<IActionResult> UpdateContact()
    {
        if (ModelState.IsValid)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                await DataHelpers.CheckFeaturePermission(FeatureKey.CONTACTS, ui.UserId, write: true);

                var parser = new CrudParser<OrganizationContact>(this.Request.Form, CrudAction.edit);
                var result = await parser.ProcessEditAsync(_context, _context.OrganizationContacts);

                foreach (var contact in result)
                {
                    if (contact.Organization == null && contact.OrganizationId != null)
                    {
                        // refresh the contact to include the organization
                        contact.Organization = await _context.Organizations.Where(oc => oc.OrganizationId == contact.OrganizationId).FirstOrDefaultAsync();
                    }
                }
                return Json(new { data = result });
            }
            //catch (DbUpdateConcurrencyException)
            //{
            //    if ((await _context.OrganizationContacts.AnyAsync(o => o.ContactId == contact.ContactId)))
            //    {
            //        throw;
            //    }
            //    return NotFound();
            //}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact");
            }
            return Ok();
        }

        return BadRequest(ModelState);
    }

    [HttpDelete]
    [HttpPost]
    public async Task<IActionResult> DeleteContact()
    {
        UserInfo ui = await GetCurrentUserAsync();

        await DataHelpers.CheckFeaturePermission(FeatureKey.CONTACTS, ui.UserId, delete: true);

        var parser = new CrudParser<OrganizationContact>(this.Request.Form, CrudAction.remove);
        var result = await parser.ProcessRemoveAsync(_context, _context.OrganizationContacts);

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
