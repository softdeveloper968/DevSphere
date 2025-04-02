using MyDMVpro.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using MyDMVpro.Common.Extensions;
using MyDMVpro.Common;
using System.Linq;
using MyDMVpro.Controllers;
using System;
using Microsoft.EntityFrameworkCore;

namespace MyDMVpro.ViewComponents
{
    public class MenuMessageViewComponent : ViewComponent
    {
        private MaggardDMVContext _context;

        public MenuMessageViewComponent(MaggardDMVContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(string filter)
        {
            bool isVendor = false;
            var messages = GetData(ref isVendor);
            ViewBag.IsVendor = isVendor;
            return View(messages);
        }

        private List<ChatMessages> GetData(ref bool isVendor)
        {
            try
            {
                bool userIsAuthenticated = (HttpContext.User.Identity.IsAuthenticated);
                if (userIsAuthenticated)
                {
                    UserInfo user = GetCurrentUser();
                    isVendor = (user.IsVendorAgent == true);
                    if (isVendor)
                    {
                        return _context.UnreadChatMessages.Where(c => c.VendorId == user.VendorId && c.WaitingForUser == false).OrderByDescending(c => c.DateSent).ToList();
                    }
                    else
                    {
                        return _context.UnreadChatMessages.Where(c => c.RequestUserId == user.UserId && c.WaitingForUser == true).OrderByDescending(c => c.DateSent).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
//TODO: Log message
            }
            return new List<ChatMessages>();
        }

        public UserInfo GetCurrentUser()
        {
            UserInfo ui = UserInfo.GetCurrentUser(this, true);
            return ui;
        }
    }
}
