using Microsoft.AspNetCore.Mvc;
using MyDMVpro.Models;
using System.Threading.Tasks;
using System;
using MyDMVpro.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MyDMVpro.ViewComponents
{
    public class DataReviewViewComponent: ViewComponent
    {
        private MaggardDMVContext _context;

        public DataReviewViewComponent(MaggardDMVContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string filter)
        {
            DataReviewInfo data = await GetAttachmentReviewCount();
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
        private async Task<DataReviewInfo> GetAttachmentReviewCount()
        {
            DataReviewInfo info = new DataReviewInfo()
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
                        info.PendingReviewCount = await _context.DataReviews.Where(f => f.Cleared == false && f.ApprovalBy == null && f.ProposedUpdateId != null).CountAsync();
                                                                
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
        public UserInfo GetCurrentUser()
        {
            UserInfo ui = UserInfo.GetCurrentUser(this, true);
            return ui;
        }
    }
}
