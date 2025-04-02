using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MyDMVpro.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using MyDMVpro.Common.Extensions;
using System.ComponentModel;

namespace MyDMVpro.Common
{
    public class UserInfo
    {
        public string NameIdentifierClaim { get; set; }
        public Guid? UserId { get; set; }
        public Guid? GroupId { get; set; }
        public string GroupName { get; set; }
        public bool IsGroupAdmin { get; set; }
        public Guid? VendorId { get; set; }
        public string VendorName { get; set; }
        public bool IsVendorAdmin { get; set; }
        public bool IsSysAdmin { get; set; }
        //public string UserPrincipalName;
        public string Email { get; set; }
        //public string Claims;
        public bool IsVendorAgent
        {
            get
            {
                return (VendorId != null);
            }
        }
        public bool IsGroupMember
        {
            get
            {
                return (GroupId != null);
            }
        }
        public bool IsVendorAdminOrGroupAdmin(Guid? groupId)
        {
            if (this == null)
                return false;
            if (this.IsVendorAdmin)
            {
                return true;
            }
            else if (this.IsGroupAdmin && this.GroupId == groupId)
            {
                return true;
            }
            return false;
        }
        public void Init(ClaimValues cv)
        {
            if (cv == null)
                return;

            if (cv.Keys.Contains("sid"))
            {
                string sid = cv["sid"];
                if (sid != null)
                {
                    this.NameIdentifierClaim = sid;
                    this.UserId = cv.GetGuidOrNull(MyDmvProClaims.UserId);
                    this.VendorId = cv.GetGuidOrNull(MyDmvProClaims.VendorId);
                    this.VendorName = cv.GetStringOrNull(MyDmvProClaims.VendorName);
                    this.GroupId = cv.GetGuidOrNull(MyDmvProClaims.GroupId);
                    this.GroupName = cv.GetStringOrNull(MyDmvProClaims.GroupName);
                    this.IsVendorAdmin = cv.GetBoolOrFalse(MyDmvProClaims.IsVendorAdmin);
                    this.IsGroupAdmin = cv.GetBoolOrFalse(MyDmvProClaims.IsGroupAdmin);
                    this.IsSysAdmin = cv.GetBoolOrFalse(MyDmvProClaims.IsSysAdmin);
                }
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("No user SID found");
            }
        }
        
        public static UserInfo GetCurrentUser(Microsoft.AspNetCore.Mvc.ViewComponent component, bool forceDbLookup = true)
        {
            return GetCurrentUser(component.UserClaimsPrincipal, forceDbLookup);
        }
        public static async Task<UserInfo> GetCurrentUserAsync(Microsoft.AspNetCore.Mvc.ControllerBase controller, bool forceDbLookup = true)
        {
            return await GetCurrentUserAsync(controller.User, forceDbLookup);
        }
        public static UserInfo GetCurrentUser(Microsoft.AspNetCore.Mvc.ControllerBase controller, bool forceDbLookup = true)
        {
            return GetCurrentUser(controller.User, forceDbLookup);
        }
        public static async Task<UserInfo> GetCurrentUserAsync(ClaimsPrincipal user, bool forceDbLookup = true)
        {
            if (!user.Identity.IsAuthenticated)
                return null;

            UserInfo ui = new UserInfo();
            ClaimValues cv;
            if (forceDbLookup)
            {
                cv = await DataHelpers.GetClaimsForUserAsync(user.GetUserSID());
                ClaimsHelper.GetAuthenticationClaims(user, cv);
            }
            else
            {
                cv = ClaimsHelper.GetUserClaimsForDB(user);
            }
            ui.Init(cv);
            if (ui.NameIdentifierClaim == null)
                return null;
            return ui;
        }
        public static UserInfo GetCurrentUser(ClaimsPrincipal user, bool forceDbLookup = true)
        { 
            if (!user.Identity.IsAuthenticated)
                return null;

            UserInfo ui = new UserInfo();
            ClaimValues cv;
            if (forceDbLookup)
            {
                cv = DataHelpers.GetClaimsForUser(user.GetUserSID());
                ClaimsHelper.GetAuthenticationClaims(user, cv);
            }
            else
            {
                cv = ClaimsHelper.GetUserClaimsForDB(user);
            }
            ui.Init(cv);
            if (ui.NameIdentifierClaim == null)
                return null;
            return ui;
        }
    }

    public class CurrentUserInfo
    {
        public Guid? VendorId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? UserId { get; set; }
    }

}
