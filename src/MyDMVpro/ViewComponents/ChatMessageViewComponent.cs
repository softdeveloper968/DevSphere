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
using System.Threading.Tasks;

namespace MyDMVpro.ViewComponents
{
    public class ChatMessageViewComponent : ViewComponent
    {
        private MaggardDMVContext _context;

        public ChatMessageViewComponent(MaggardDMVContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string filter)
        {
            bool isVendor;
            List<RequestChatThread> messages;
            (messages, isVendor) = await GetChatThreads();
            //var messages = GetData(ref isVendor);
            ViewBag.IsVendor = isVendor;
            
            
            return View(messages);
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
        private async Task<(List<RequestChatThread>,bool)> GetChatThreads()
        {
            try
            {
                List<RequestChatThread> threads = new List<RequestChatThread>();

                bool userIsAuthenticated = (HttpContext.User.Identity.IsAuthenticated);
                if (userIsAuthenticated)
                {
                    UserInfo user = await GetCurrentUserAsync();
                    bool isVendor = (user.IsVendorAgent == true);
                    List<ChatMessages> msgs;
                    if (isVendor)
                    {
                        msgs = await _context.UnreadChatMessages
                            .Where(c => c.VendorId == user.VendorId && c.WaitingForUser == false)
                            .OrderBy(c => c.VIN)
                            .ThenByDescending(c => c.DateSent)
                            .ToListAsync();
                    }
                    else
                    {
                        msgs = await _context.UnreadChatMessages
                            .Where(c => c.RequestUserId == user.UserId && c.WaitingForUser == true)
                            .OrderBy(c => c.VIN)
                            .ThenByDescending(c => c.DateSent)
                            .ToListAsync();
                    }
                    RequestChatThread rct = null;

                    foreach (var ucm in msgs)
                    {
                        if (threads.Count == 0 || ucm.RequestId != threads[threads.Count - 1].RequestId)
                        {
                            rct = new RequestChatThread();
                            rct.RequestId = ucm.RequestId;
                            rct.RequestUserId = ucm.RequestUserId;
                            rct.VIN = ucm.VIN;
                            rct.ChatMessages = new List<ChatMessages>();

                            threads.Add(rct);
                        }
                        rct.ChatMessages.Add(ucm);
                    }
                    return (threads, isVendor);
                }
            }
            catch (Exception ex)
            {
                //TODO: Log message
            }
            return (new List<RequestChatThread>(), false);
        }
        //private List<ChatMessages> GetData(ref bool isVendor)
        //{
        //    try
        //    {
        //        bool userIsAuthenticated = (HttpContext.User.Identity.IsAuthenticated);
        //        if (userIsAuthenticated)
        //        {
        //            UserInfo user = await GetCurrentUserAsync();
        //            isVendor = (user.IsVendorAgent == true);
        //            if (isVendor)
        //            {
        //                return _context.UnreadChatMessages
        //                    .Where(c => c.VendorId == user.VendorId && c.WaitingForUser == false)
        //                    .OrderBy(c=>c.VIN)
        //                    .ThenByDescending(c => c.DateSent)
        //                    .ToList();
        //            }
        //            else
        //            {
        //                return _context.UnreadChatMessages
        //                    .Where(c => c.RequestUserId == user.UserId && c.WaitingForUser == true)
        //                    .OrderBy(c=>c.VIN)
        //                    .ThenByDescending(c => c.DateSent)
        //                    .ToList();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //TODO: Log message
        //    }
        //    return new List<ChatMessages>();
        //}
        public UserInfo GetCurrentUser()
        {
            UserInfo ui = UserInfo.GetCurrentUser(this, true);
            return ui;
        }
    }
}
