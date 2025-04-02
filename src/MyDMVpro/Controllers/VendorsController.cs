using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.Extensions;
using MyDMVpro.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    [Authorize]
    public class VendorsController : MyDMVpro.Controllers.BaseController
    {
        public VendorsController(MaggardDMVContext context, IConfiguration configuration, ILogger<VendorsController> logger) : base(context, configuration, logger)
        {
        }

        // GET: Vendors
        [Authorize(Policy = "SysAdminOnly")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Vendors.OrderByDescending(v => v.Active).ThenBy(v => v.VendorCode).ToListAsync());
        }

        // GET: Vendors/Details/5
        [Authorize(Policy = "SysAdminOnly")]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendors = await _context.Vendors.Include(v => v.VendorAgent).ThenInclude(a => a.Agent)
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendors == null)
            {
                return NotFound();
            }
            UserInfo ui = await GetCurrentUserAsync();

            this.ViewData["CurrentUserId"] = ui.UserId;
            var invites = await _context.VendorInvite.Where(i => i.VendorId == id && i.Accepted == false && i.Deleted == false).ToListAsync();
            this.ViewData["Invites"] = invites;

            return View(vendors);
        }

        // GET: Vendors/Create
        [Authorize(Policy = "SysAdminOnly")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "SysAdminOnly")]
        public async Task<IActionResult> Create([Bind("Id,VendorId,VendorName,VendorCode")] Vendors vendors)
        {
            if (ModelState.IsValid)
            {
                await _context.AddAsync(vendors);
                await _context.SaveChangesAsync(await GetCurrentUserAsync());
                return RedirectToAction(nameof(Index));
            }
            return View(vendors);
        }

        // GET: Vendors/Edit/5
        [Authorize(Policy = "SysAdminOnly")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendors = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendors == null)
            {
                return NotFound();
            }
            return View(vendors);
        }
        [Authorize(Policy = "VendorAdminOnly")]
        public async Task<IActionResult> ManageUsers(Guid? id)
        {
            string userName = GetUserSID();

            (Guid? agentId, Guid? vendorId, string vendorCode, string vendorName, bool isVendorAdmin) vendorInfo = DataHelpers.GetVendorInfoForAgent(userName).Result;

            if (vendorInfo.vendorId != null && id == null)
                id = vendorInfo.vendorId;

            if ((id != null && vendorInfo.vendorId != id) // VendorId mismatch
                    ||
                (id == null && vendorInfo.vendorId == null) // not a vendor
                    ||
                !vendorInfo.isVendorAdmin
               )
            {
                // redirect 
                return RedirectToAction("Index", "Home");
            }

            var vendors = await _context.Vendors.Include(v => v.VendorAgent).ThenInclude(va => va.Agent).FirstOrDefaultAsync(v => v.VendorId == vendorInfo.vendorId);
            if (vendors == null)
            {
                return NotFound();
            }
            ViewData["CurrentUserId"] = vendorInfo.agentId.Value;
            AddPageHeader("Manage Users", "");
            var invites = await _context.VendorInvite.Where(i => i.VendorId == vendors.VendorId && i.Accepted == false && i.Deleted == false).ToListAsync();
            this.ViewData["Invites"] = invites;
            return View(vendors);
        }

        [HttpPost]
        [Authorize(Policy = "SysAdminOnly")]
        public async Task<IActionResult> Edit(Guid id, [Bind("VendorId,VendorName,VendorCode,Active")] Vendors vendors)
        {
            if (id != vendors.VendorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update<Vendors>(vendors, "VendorName", "VendorCode", "Active");
                    await _context.SaveChangesAsync(await GetCurrentUserAsync());
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorsExists(vendors.VendorId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(vendors);
        }

        // GET: Vendors/Delete/5
        [Authorize(Policy = "SysAdminOnly")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendors = await _context.Vendors
                .FirstOrDefaultAsync(m => m.VendorId == id);
            if (vendors == null)
            {
                return NotFound();
            }

            return View(vendors);
        }

        [HttpPost]
        [Authorize(Policy = "VendorAdminOnly")]
        public async Task<IActionResult> RemoveAgent([FromForm] Guid? vendorId, [FromForm] Guid? agentId)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.VendorId != null && ui.IsVendorAdmin && ui.VendorId == vendorId)
            {
                var va = await _context.VendorAgent.Where(v => v.AgentId == agentId && v.VendorId == vendorId).FirstOrDefaultAsync();
                if (va != null && va.VendorId == ui.VendorId)
                {
                    _context.VendorAgent.Remove(va);
                    await _context.SaveChangesAsync(ui);
                }
            }
            return Ok();
        }
        [HttpPost]
        [Authorize(Policy = "VendorAdminOnly")]
        public async Task<IActionResult> EnableAgent([FromForm] Guid? vendorId, [FromForm] Guid? agentId)
        {
            return await InternalUpdateAgent(vendorId, agentId, activate: true, makeAdmin: null);
        }
        [HttpPost]
        [Authorize(Policy = "VendorAdminOnly")]
        public async Task<IActionResult> DisableAgent([FromForm] Guid? vendorId, [FromForm] Guid? agentId)
        {
            return await InternalUpdateAgent(vendorId, agentId, activate: false, makeAdmin: null);
        }
        private async Task<IActionResult> InternalUpdateAgent(Guid? vendorId, Guid? agentId, bool? activate, bool? makeAdmin)
        {
            UserInfo ui = GetCurrentUser(true);
            if (ui.VendorId != null && ui.IsVendorAdmin && ui.VendorId == vendorId)
            {
                var va = await _context.VendorAgent.Include(a => a.Agent).Where(v => v.AgentId == agentId && v.VendorId == vendorId).FirstOrDefaultAsync();
                if (va != null && va.VendorId == ui.VendorId && (va.Agent.Active != activate || va.IsVendorAdmin != makeAdmin))
                {
                    if (activate.HasValue)
                        va.Agent.Active = activate.Value;
                    if (makeAdmin.HasValue)
                        va.IsVendorAdmin = makeAdmin.Value;
                    await _context.SaveChangesAsync(ui);
                }
            }
            return Ok();
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "SysAdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            UserInfo ui = GetCurrentUser(true);
            if (IsSysAdmin(ui.UserId))
            {
                var vendors = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id);
                if (vendors.Active)
                {
                    vendors.Active = false;
                    await _context.SaveChangesAsync(ui);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool VendorsExists(Guid id)
        {
            return _context.Vendors.Any(e => e.VendorId == id);
        }

        [Authorize(Policy = "VendorAdminOnly")]
        public IActionResult Invite(Guid? vendorId)
        {
            UserInfo ui = GetCurrentUser(true);

            if (vendorId == null && ui.VendorId != null)
                vendorId = ui.VendorId;

            if (ui.IsSysAdmin ||
                (ui.IsVendorAdmin && ui.VendorId == vendorId))
            {
                // TODO: Check for Group Admin
                VendorInvite invite = new VendorInvite()
                {
                    VendorId = vendorId
                };
                return View(invite);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize(Policy = "VendorAdminOnly")]
        public async Task<IActionResult> SetAdminRole([Bind("vendorid")] Guid? vendorid, [Bind("agentid")] Guid? agentid, [Bind("isAdmin")] bool isAdmin)
        {
            return await InternalUpdateAgent(vendorid, agentid, activate: null, makeAdmin: isAdmin);
        }

        [HttpPost]
        [Authorize(Policy = "VendorAdminOnly")]
        public async Task<IActionResult> Invite(Guid? id, [Bind("VendorId,UserEmail,MakeVendorAdmin")] VendorInvite invite)
        {
            if (ModelState.IsValid && invite.VendorId.HasValue)
            {
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(invite.UserEmail))
                {
                    string url = (HttpContext.Request.IsHttps ? "https" : "http:") + "://" + HttpContext.Request.Host;
                    string[] emailList = invite.UserEmail.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string email in emailList)
                    {
                        if (!GlobalHelper.IsValidEmailAddress(email))
                        {
                            sb.AppendLine($"Invalid email address: {email}");
                        }
                        else
                        {
                            bool success = await DataHelpers.SendVendorInvite(url, GetUserSID(), invite.VendorId.Value, email, invite.MakeVendorAdmin);
                            if (!success)
                            {
                                // TODO: Show error
                                sb.AppendLine($"Error sending group invite for {email}");
                            }
                        }
                    }
                    if (sb.Length > 0)
                        return Content(sb.ToString());
                }
                else
                {
                    // TODO: Show error
                }
            }
            return RedirectToAction("ManageUsers");
        }

        [Authorize]
        public async Task<IActionResult> Register(Guid? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }
            try
            {
                VendorInvite invite = _context.VendorInvite.Find(id);
                if (invite != null)
                {
                    UserInfo currentUser = await GetCurrentUserAsync();
                    if (currentUser.IsGroupMember)
                    {
                        // Don't allow a group member to also accept a Vendor invite
                        return RedirectToAction("Index", "Home");
                    }
                    if (invite.Accepted == true || currentUser.IsVendorAgent)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    // "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"

                    // don't require matchup with email
                    //string email = HttpContext.User.GetUserProperty(CustomClaimTypes.Name);
                    //if (invite.UserEmail.Equals(email))
                    {
                        string vendorName = null;
                        Users user;
                        user = await _context.Users.Include(u => u.VendorAgent)
                            .FirstOrDefaultAsync(u => u.NameIdentifierClaim == GetUserSID());
                        if (user == null)
                        {
                            user = new Users()
                            {
                                UserId = Guid.NewGuid(),
                                UserPrincipalName = GetUser_Email(),
                                DisplayName = GetUser_Name(),
                                NameIdentifierClaim = GetUserSID(),
                                Active = true
                            };
                            if (user.UserPrincipalName == null)
                                user.UserPrincipalName = invite.UserEmail;
                            VendorAgent vendorAgent = new VendorAgent();
                            vendorAgent.AgentId = user.UserId;
                            vendorAgent.VendorId = invite.VendorId.Value;
                            vendorAgent.IsVendorAdmin = invite.MakeVendorAdmin;
                            user.VendorAgent.Add(vendorAgent);

                            _context.Users.Add(user);
                        }
                        else
                        {
                            VendorAgent vendorAgent = user.VendorAgent.FirstOrDefault(va => va.VendorId == invite.VendorId);
                            if (vendorAgent == null)
                            {
                                vendorAgent = new VendorAgent();
                                vendorAgent.AgentId = user.UserId;
                                vendorAgent.VendorId = invite.VendorId.Value;
                                vendorAgent.IsVendorAdmin = invite.MakeVendorAdmin;
                                user.VendorAgent.Add(vendorAgent);
                            }
                            if (!user.Active)
                                user.Active = true; // make sure it is activated
                        }
                        invite.Accepted = true;
                        invite.UserIdCreated = user.UserId;
                        await _context.SaveChangesAsync(user.UserId);

                        return Redirect("/signout-oidc");
                        //return RedirectToAction("SignOut", "Account", new { area = "MicrosoftIdentity" });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        public async Task<IActionResult> ResendInvite(Guid inviteId)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui != null)
            {
                var invite = await _context.VendorInvite.Where(v => v.Id == inviteId && v.Accepted == false).SingleOrDefaultAsync();
                if (invite != null)
                {
                    if (ui.IsVendorAdmin && ui.VendorId == invite.VendorId)
                    {
                        if (!string.IsNullOrWhiteSpace(invite.UserEmail))
                        {
                            string url = (HttpContext.Request.IsHttps ? "https" : "http:") + "://" + HttpContext.Request.Host;

                            Vendors v = _context.Vendors.Find(invite.VendorId);
                            bool success = DataHelpers.ResendVendorInvite(url, invite.UserEmail, v.VendorName, invite.Id);
                            if (!success)
                            {
                                // TODO: Show error
                            }
                        }
                        return Ok();
                    }
                }
            }
            return NotFound();
        }

        [HttpPost]
        [Authorize(Policy = "VendorAdminOnly")]
        public async Task<IActionResult> RemoveInvite(Guid inviteId)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui != null)
            {
                var invite = await _context.VendorInvite.FindAsync(inviteId);
                if (invite == null)
                {
                    return NotFound();
                }
                if (ui.IsVendorAdmin && ui.VendorId == invite.VendorId)
                {
                    invite.Deleted = true;
                    //_context.GroupInvite.Remove(invite);
                    await _context.SaveChangesAsync(ui);
                    return Ok();
                }
            }
            return NotFound();
        }

        [HttpPost]
        public IActionResult SaveFeaturePermission(Guid agentid, int accessLevel)
        {
            var feature = _context.Features.Where(f => f.FeatureKey == FeatureKey.INVOICING.ToString()).FirstOrDefault();

            if (feature == null)
            {
                return JsonError("Feature Not found");

            }
            var featurePermission = _context.FeaturePermissions
                .FirstOrDefault(fp => fp.UserId == agentid && fp.FeatureId == feature.FeatureId);

            if (featurePermission == null)
            {
                featurePermission = new FeaturePermission
                {
                    FeatureId = feature.FeatureId,
                    UserId = agentid,
                };
                _context.FeaturePermissions.Add(featurePermission);
            }
            switch (accessLevel)
            {
                case 0: // No Access
                    featurePermission.ReadState = false;
                    featurePermission.DenyState = true;
                    featurePermission.WriteState = false;
                    featurePermission.CreateState = false;
                    featurePermission.DeleteState = false;
                    featurePermission.ExecuteState = false;
                    featurePermission.NavigateState = false;
                    break;
                case 1: // Read-Only Access
                    featurePermission.ReadState = true;
                    featurePermission.WriteState = false;
                    featurePermission.CreateState = false;
                    featurePermission.DeleteState = false;
                    featurePermission.ExecuteState = false;
                    featurePermission.NavigateState = false;
                    break;
                case 2: // Full Edit Access
                    featurePermission.ReadState = true;
                    featurePermission.WriteState = true;
                    featurePermission.CreateState = true;
                    featurePermission.DeleteState = true;
                    featurePermission.ExecuteState = true;
                    featurePermission.NavigateState = true;
                    break;
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult GetFeaturePermission(Guid agentid)
        {
            var feature = _context.Features.Where(f => f.FeatureKey == FeatureKey.INVOICING.ToString()).FirstOrDefault();

            if (feature == null)
            {
                return JsonError("Feature Not found");

            }
            var featurePermission = _context.FeaturePermissions
                .FirstOrDefault(fp => fp.UserId == agentid && fp.FeatureId == feature.FeatureId);

            if (featurePermission == null)
            {
                return JsonError("Not found");
            }

            return Json(new { success = true, data = featurePermission });
        }
    }
}
