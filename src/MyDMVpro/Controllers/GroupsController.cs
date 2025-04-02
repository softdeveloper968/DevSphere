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
    public class GroupsController : MyDMVpro.Controllers.BaseController
    {
        public GroupsController(MaggardDMVContext context, IConfiguration configuration, ILogger<GroupsController> logger) : base(context, configuration, logger)
        {
        }

        // GET: Groups
        public async Task<IActionResult> Index()
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui == null || !ui.IsVendorAdmin)
                return new UnauthorizedResult();

            return View(await _context.Groups.OrderByDescending(g => g.Active).ThenBy(g => g.GroupName).ToListAsync());
        }

        // GET: Groups/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui == null)
            {
                return new UnauthorizedResult();
            }
            if (!ui.IsVendorAdmin && ui.GroupId != id)
            {
                return new UnauthorizedResult();
            }

            var groups = await _context.Groups.Include(g => g.UserGroups).FirstOrDefaultAsync(m => m.GroupId == id);
            if (groups == null)
            {
                return NotFound();
            }

            return View(groups);
        }

        public async Task<IActionResult> Register(Guid? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }
            try
            {
                GroupInvite invite = _context.GroupInvite.Find(id);
                if (invite != null)
                {
                    UserInfo currentUser = await GetCurrentUserAsync();
                    if (currentUser.IsVendorAgent)
                    {
                        // Don't allow accepting Group invite if a Vendor account
                        return RedirectToAction("Index", "Home");
                    }
                    if (invite.Accepted == true || currentUser.IsGroupMember)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    // "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"

                    // don't require matchup with email
                    //string email = HttpContext.User.GetUserProperty(CustomClaimTypes.Name);
                    //if (invite.UserEmail.Equals(email))
                    {
                        Users user;
                        user = await _context.Users.Include(u => u.UserGroups)
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

                            UserGroups userGroup = new UserGroups()
                            {
                                GroupId = invite.GroupId.Value,
                                IsGroupAdmin = invite.MakeGroupAdmin,
                                UserId = user.UserId
                            };
                            user.UserGroups.Add(userGroup);

                            _context.Users.Add(user);
                        }
                        else
                        {
                            UserGroups userGroup = user.UserGroups.FirstOrDefault(ug => ug.GroupId == invite.GroupId);
                            if (userGroup == null)
                            {
                                userGroup = new UserGroups()
                                {
                                    GroupId = invite.GroupId.Value,
                                    UserId = user.UserId,
                                    IsGroupAdmin = invite.MakeGroupAdmin
                                };
                                user.UserGroups.Add(userGroup);
                            }
                            if (!user.Active)
                                user.Active = true; // Make sure activated
                        }
                        invite.Accepted = true;
                        invite.UserIdCreated = user.UserId;
                        await _context.SaveChangesAsync(user.UserId);

                        return Redirect("/MicrosoftIdentity/Account/Signout");
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
        // GET: Groups/Create
        public IActionResult Create()
        {
            UserInfo ui = GetCurrentUser();
            if (ui == null || !ui.IsVendorAdmin)
                return new UnauthorizedResult();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("GroupName,Active")] Groups groups)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui == null || !ui.IsVendorAdmin)
                return new UnauthorizedResult();

            if (ModelState.IsValid)
            {
                groups.GroupId = Guid.NewGuid();
                groups.AutoIMSEnabled = false;
                await _context.AddAsync(groups);
                await _context.SaveChangesAsync(ui);
                return RedirectToAction(nameof(Index));
            }
            return View(groups);
        }

        // GET: Groups/Create
        public IActionResult Invite(Guid? groupId)
        {
            UserInfo ui = GetCurrentUser();
            if (ui == null)
                return new UnauthorizedResult();

            if (ui.IsGroupAdmin && groupId == null)
                groupId = ui.GroupId;

            if (ui.IsVendorAgent)
            {
                if (!ui.IsVendorAdmin)
                    return new UnauthorizedResult();
            }
            else if (!ui.IsGroupAdmin || ui.GroupId != groupId)
            {
                return new UnauthorizedResult();
            }
            if (groupId == null)
            {
                return new UnauthorizedResult();
            }
            GroupInvite invite = new GroupInvite()
            {
                GroupId = groupId
            };
            return View(invite);
        }

        [HttpPost]
        public async Task<IActionResult> Invite(Guid? id, [Bind("GroupId,UserEmail,MakeGroupAdmin")] GroupInvite invite)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (!ui.IsVendorAdminOrGroupAdmin(invite.GroupId))
                return new UnauthorizedResult();

            Guid groupId = invite.GroupId.Value;

            if (ModelState.IsValid && invite.GroupId.HasValue)
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
                            bool success = await DataHelpers.SendGroupInvite(url, GetUserSID(), invite.GroupId.Value, email, invite.MakeGroupAdmin);
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
            return RedirectToManageUsers(ui, groupId);
        }
        public IActionResult RedirectToManageUsers(UserInfo ui, Guid groupid)
        {
            if (ui.IsSysAdmin || ui.IsVendorAdmin)
            {
                return RedirectToAction("ManageUsers", new { id = groupid });
            }
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public async Task<IActionResult> ResendInvite(Guid inviteId)
        {
            var invite = await _context.GroupInvite.Where(g => g.Id == inviteId && g.Accepted == false).SingleOrDefaultAsync();
            if (invite == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrWhiteSpace(invite.UserEmail))
            {
                string url = (HttpContext.Request.IsHttps ? "https" : "http:") + "://" + HttpContext.Request.Host;

                Groups g = _context.Groups.Find(invite.GroupId);
                bool success = DataHelpers.ResendGroupInvite(url, invite.UserEmail, g.GroupName, invite.Id);
                if (!success)
                {
                    // TODO: Show error
                }
            }
            return Ok();
        }
        private bool IsVendorAdminOrSysAdmin(UserInfo user)
        {
            return (user.IsVendorAdmin || user.IsSysAdmin);
        }
        private bool ValidGroupOrVendorAdmin(UserInfo user, Guid? groupid)
        {
            if (user.VendorId != null && user.IsVendorAdmin)
            {
                // Vendor Admin
                return true;
            }
            if (user.IsSysAdmin)
            {
                // System Admin
                return true;
            }

            if (user.GroupId == groupid && user.IsGroupAdmin)
            {
                // Group Admin
                return true;
            }

            return false;
        }

        [HttpPost]
        public async Task<IActionResult> SetAdminRole([Bind("groupid")] Guid? groupid, [Bind("userid")] Guid? userid, [Bind("isAdmin")] bool isAdmin)
        {
            UserInfo currentUser = GetCurrentUser(true);
            if (!ValidGroupOrVendorAdmin(currentUser, groupid))
            {
                return RedirectToManageUsers(currentUser, groupid.Value);
            }

            UserGroups ug = _context.UserGroups.Find(groupid, userid);
            if (ug != null)
            {
                ug.IsGroupAdmin = isAdmin;
                await _context.SaveChangesAsync(currentUser);
            }
            return Ok();
        }

        // GET: Groups/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            UserInfo currentUser = GetCurrentUser(true);
            if (!ValidGroupOrVendorAdmin(currentUser, id))
            {
                return RedirectToManageUsers(currentUser, id.Value);
            }

            var groups = await _context.Groups.Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User).FirstOrDefaultAsync(m => m.GroupId == id);
            if (groups == null)
            {
                return NotFound();
            }
            AddPageHeader("Manage Users", "");
            return View(groups);
        }

        public async Task<IActionResult> ManageUsers(Guid? id)
        {
            string userName = GetUserSID();

            UserInfo currentUser = GetCurrentUser(true);
            if (id == null)
                id = currentUser.GroupId;
            if (!ValidGroupOrVendorAdmin(currentUser, id))
            {
                return RedirectToAction("Index", "Home");
            }

            var groups = await _context.Groups.Include(g => g.UserGroups)
                                                .ThenInclude(ug => ug.User)
                                                .FirstOrDefaultAsync(m => m.GroupId == id);
            if (groups == null)
            {
                return NotFound();
            }
            AddPageHeader("Manage Users", "");
            this.ViewData["CurrentUserId"] = currentUser.UserId;
            var invites = await _context.GroupInvite.Where(i => i.GroupId == groups.GroupId && i.Accepted == false && i.Deleted == false).ToListAsync();
            this.ViewData["Invites"] = invites;
            return View(groups);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, [Bind("GroupId,GroupName,Active")] Groups groups)
        {
            if (id != groups.GroupId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await GetCurrentUserAsync();
                    _context.Update<Groups>(groups, "GroupName", "Active");
                    await _context.SaveChangesAsync(user);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupsExists(groups.GroupId))
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
            return View(groups);
        }

        public async Task<IActionResult> RemoveMember(Guid groupid, Guid userid)
        {
            var groups = await _context.Groups
                .FirstOrDefaultAsync(m => m.GroupId == groupid);
            if (groups == null)
            {
                return NotFound();
            }
            UserInfo currentUser = GetCurrentUser(true);
            if (!ValidGroupOrVendorAdmin(currentUser, groupid))
            {
                return RedirectToAction("Index", "Home");
            }

            var groupMember = await _context.UserGroups.Include(ug => ug.User).Include(ug => ug.Group)
                .FirstOrDefaultAsync(m => m.GroupId == groupid && m.UserId == userid);
            _context.UserGroups.Remove(groupMember);
            await _context.SaveChangesAsync(currentUser);
            return Ok();
        }
        public async Task<IActionResult> DisableMember(Guid groupid, Guid userid)
        {
            return await InternalEnableMember(groupid, userid, false);
        }
        public async Task<IActionResult> EnableMember(Guid groupid, Guid userid)
        {
            return await InternalEnableMember(groupid, userid, true);
        }
        public async Task<IActionResult> InternalEnableMember(Guid groupid, Guid userid, bool enable)
        {
            var groups = await _context.Groups
                .FirstOrDefaultAsync(m => m.GroupId == groupid);
            if (groups == null)
            {
                return NotFound();
            }
            UserInfo currentUser = GetCurrentUser(true);
            if (!ValidGroupOrVendorAdmin(currentUser, groupid))
            {
                return RedirectToAction("Index", "Home");
            }

            var groupMember = await _context.Users.Include(u => u.UserGroups)
                .FirstOrDefaultAsync(m => m.UserId == userid);
            groupMember.Active = enable;
            // Always disable admin permissions when disabled
            var ug = groupMember.UserGroups.FirstOrDefault();
            if (ug != null && !enable)
            {
                ug.IsGroupAdmin = false;
            }
            await _context.SaveChangesAsync(currentUser);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveInvite(Guid inviteId)
        {
            var invite = await _context.GroupInvite.FindAsync(inviteId);
            if (invite == null)
            {
                return NotFound();
            }
            UserInfo currentUser = GetCurrentUser(true);
            if (!ValidGroupOrVendorAdmin(currentUser, invite.GroupId))
            {
                return RedirectToAction("Index", "Home");
            }
            invite.Deleted = true;
            await _context.SaveChangesAsync(currentUser);

            return Ok();
        }
        // GET: Groups/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            UserInfo currentUser = GetCurrentUser(true);
            if (!IsVendorAdminOrSysAdmin(currentUser))
            {
                return RedirectToAction("Index", "Home");
            }
            var groups = await _context.Groups.FirstOrDefaultAsync(m => m.GroupId == id);
            if (groups == null)
            {
                return NotFound();
            }

            return View(groups);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            UserInfo currentUser = GetCurrentUser(true);
            if (!IsVendorAdminOrSysAdmin(currentUser))
            {
                return RedirectToAction("Index", "Home");
            }
            var groups = await _context.Groups.FindAsync(id);
            groups.Active = false;
            await _context.SaveChangesAsync(currentUser);
            return RedirectToAction(nameof(Index));
        }

        private bool GroupsExists(Guid id)
        {
            return _context.Groups.Any(e => e.GroupId == id);
        }

        [HttpPost]
        public IActionResult GetGroups()
        {
            UserInfo ui = GetCurrentUser();
            if (ui == null || !ui.IsVendorAgent)
            {
                return Json(new
                {
                    status = "error",
                    draw = 0,
                    recordsFiltered = 0,
                    recordsTotal = 0
                });
            }

            var data = _context.GroupVendors
                            .Where(gv => gv.VendorId == ui.VendorId)
                            .Include(g => g.Group)
                            .Select(gv => new { gv.Group.GroupId, gv.Group.GroupName, gv.Group.Active, gv.Group.AutoIMSEnabled })
                            .ToList();

            return Json(new
            {
                status = "success",
                draw = 0,
                recordsFiltered = data.Count,
                recordsTotal = data.Count,
                data = data
            });
        }

        [HttpPost]
        public IActionResult SaveFeaturePermission(Guid groupid, Guid userid, int accessLevel)
        {
            var feature = _context.Features.Where(f => f.FeatureKey == FeatureKey.CLIENTBILLING.ToString()).FirstOrDefault();
            
            if (feature == null) {
                return JsonError("Feature Not found");

            }
            var featurePermission = _context.FeaturePermissions
                .FirstOrDefault(fp => fp.UserId == userid && fp.FeatureId == feature.FeatureId);

            if (featurePermission == null)
            {
                featurePermission = new FeaturePermission
                {
                    FeatureId = feature.FeatureId,
                    UserId = userid,
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
                    featurePermission.ExecuteState =false;
                    featurePermission.NavigateState =false;
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
                    featurePermission.ReadState =   true;
                    featurePermission.WriteState =  true;
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
        public IActionResult GetFeaturePermission(Guid userid)
        {
            var feature = _context.Features.Where(f => f.FeatureKey == FeatureKey.CLIENTBILLING.ToString()).FirstOrDefault();

            if (feature == null)
            {
                return JsonError("Feature Not found");

            }
            var featurePermission = _context.FeaturePermissions
                .FirstOrDefault(fp => fp.UserId == userid && fp.FeatureId == feature.FeatureId);

            if (featurePermission == null)
            {
                return JsonError("Not found");
            }
            
            return Json(new { success = true , data = featurePermission });
        }

    }
}
