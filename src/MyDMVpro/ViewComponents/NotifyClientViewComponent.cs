using Microsoft.AspNetCore.Mvc;
using MyDMVpro.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using MyDMVpro.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MyDMVpro.ViewComponents
{
    public class NotifyClientViewComponent: ViewComponent
    {
        private MaggardDMVContext _context;

        public NotifyClientViewComponent(MaggardDMVContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string filter)
        {
            bool isVendor;
            List<NotifyClient> notifyClients;
            (notifyClients, isVendor) = await GetNotifiedData();
            ViewBag.IsVendor = isVendor;
            return View(notifyClients);
        }

        public async Task<UserInfo> GetCurrentUserAsync(bool forceDbLookup = true)
        {
            try
            {
                UserInfo ui = await UserInfo.GetCurrentUserAsync(this.UserClaimsPrincipal, forceDbLookup);
                return ui;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<(List<NotifyClient>,bool)> GetNotifiedData()
        {
            try
            {
                bool userIsAuthenticated = (HttpContext.User.Identity.IsAuthenticated);

                if (userIsAuthenticated)
                {
                    UserInfo user = await GetCurrentUserAsync();
                    bool isVendor = (user.IsVendorAgent);
                    var result = await _context.NotifyClient.Where(x => x.IsRead == false && x.NotifiedUserId == user.UserId).ToListAsync();
                    return (result, isVendor);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return (new List<NotifyClient>(), false);
        }
        public UserInfo GetCurrentUser()
        {
            UserInfo ui = UserInfo.GetCurrentUser(this, true);
            return ui;
        }
    }
}
