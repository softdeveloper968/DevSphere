using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDMVpro.Common;
using MyDMVpro.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.ViewComponents
{
    public class AttachmentReviewViewComponent : ViewComponent
    {
        private MaggardDMVContext _context;

        public AttachmentReviewViewComponent(MaggardDMVContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string filter)
        {
            AttachmentReviewInfo data = await GetAttachmentReviewCount();
            return View(data);
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
        private async Task<AttachmentReviewInfo> GetAttachmentReviewCount()
        {
            AttachmentReviewInfo info = new AttachmentReviewInfo()
            {
                PendingReviewCount = 0,
                IsVendorAgent = false
            };

            try
            {
                bool userIsAuthenticated = (HttpContext.User.Identity.IsAuthenticated);
                if (userIsAuthenticated)
                {
                    UserInfo user = await GetCurrentUserAsync();
                    if (user.IsVendorAgent)
                    {
                        info.IsVendorAgent = true;
                        info.PendingReviewCount = await _context.AttachmentReviews
                                                                .Where(f => f.VendorId == user.VendorId)
                                                                .Where(f => f.ReviewDate == null)
                                                                .CountAsync();
                    }

                    return info;
                }
            }
            catch (Exception ex)
            {
                //TODO: Log message
            }
            return info;
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
