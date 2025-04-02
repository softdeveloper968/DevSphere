using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyDMVpro.Models;
using MyDMVpro.Common.Extensions;

namespace MyDMVpro.Controllers;

#if false
//
// Disabling, as this is not used and not protected from unauthorized use
// 
public class UsersController : BaseController
{
    public UsersController(MaggardDMVContext context, IConfiguration configuration) : base(context, configuration)
    {
    }

    // GET: Users
    public async Task<IActionResult> Index()
    {
        return View(await _context.Users.ToListAsync());
    }
    protected string UserPrincipalName()
    {
        return HttpContext.User.Identity.Name;
    }
    public async Task<IActionResult> ManageUsers()
    {
        // First check user is an admin to a group
        Users adminUser = _context.Users.Include(u => u.UserGroups)
            .FirstOrDefault(u => u.NameIdentifierClaim == GetUserSID() && u.UserGroups.Any(ug => ug.IsGroupAdmin == true));
        if (adminUser == null)
        {
            // Not an group admin
            return RedirectToAction(nameof(Index), "Home");
        }
        List<Guid> groupIDs = new List<Guid>();
        groupIDs = adminUser.UserGroups.Where(ug => ug.IsGroupAdmin == true).Select(g => g.GroupId).ToList();

        var users = await _context.Users.Where(u => groupIDs.Contains(u.UserId)).ToListAsync();
        return View("ManageGroup", users);
    }

    // GET: Users/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var users = await _context.Users
            .FirstOrDefaultAsync(m => m.UserId == id);
        if (users == null)
        {
            return NotFound();
        }

        return View(users);
    }

    // GET: Users/Create
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create([Bind("Id,UserId,UserPrincipalName,Active,DisplayName,IsVendorAgent")] Users users)
    {
        if (ModelState.IsValid)
        {
            await _context.AddAsync(users);
            await _context.SaveChangesAsync(await GetCurrentUserAsync());
            return RedirectToAction(nameof(Index));
        }
        return View(users);
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var users = await _context.Users.FindAsync(id);
        if (users == null)
        {
            return NotFound();
        }
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, [Bind("UserId,UserPrincipalName,Active,DisplayName,IsVendorAgent")] Users users)
    {
        if (id != users.UserId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                //_context.Update(users);
                _context.Update<Users>(users, "Active", "DisplayName");
                await _context.SaveChangesAsync(await GetCurrentUserAsync());
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsersExists(users.UserId))
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
        return View(users);
    }

    // GET: Users/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var users = await _context.Users
            .FirstOrDefaultAsync(m => m.UserId == id);
        if (users == null)
        {
            return NotFound();
        }

        return View(users);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var users = await _context.Users.FindAsync(id);
        users.Active = false;
        await _context.SaveChangesAsync(await GetCurrentUserAsync());
        return RedirectToAction(nameof(Index));
    }

    private bool UsersExists(Guid id)
    {
        return _context.Users.Any(e => e.UserId == id);
    }
}
#endif
