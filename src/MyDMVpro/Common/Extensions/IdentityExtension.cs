using System.Linq;
using System.Security.Claims;

namespace MyDMVpro.Common.Extensions
{
    public static class IdentityExtension
    {
        /// <summary>
        /// //Use CustomClaimTypes when using this method
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claimType">Use [CustomClaimTypes] when using this method</param>
        /// <returns></returns>
        public static string GetUserProperty(this ClaimsPrincipal user, string claimType)
        {
            if (user.Identity.IsAuthenticated)
            {
                // Changed to allow easily setting a breakpoint when a claim is not found
                Claim claim = user.Claims.FirstOrDefault(v => v.Type == claimType);
                if (claim == null)
                {
                    return string.Empty;
                }
                return claim.Value ?? string.Empty;
                //return user.Claims.FirstOrDefault(v => v.Type == claimType)?.Value ?? string.Empty;
            }

            return string.Empty;
        }
        public static string GetUserSID(this ClaimsPrincipal user)
        {
            if (user == null)
                return null;

            string sid = null;
            Claim claim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
                sid = claim.Value;
            return sid;
        }
    }
}
