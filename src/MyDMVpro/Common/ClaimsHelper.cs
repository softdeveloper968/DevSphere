using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyDMVpro.Common
{
    public class ClaimsHelper
    {
        public static void GetAuthenticationClaims(ClaimsPrincipal user, ClaimValues dict)
        {
            var iss = user.GetClaimsByShortTypeName("iss").FirstOrDefault();
            dict.Add("iss", (iss != null) ? iss.Value : null);

            var idp = user.GetClaimsByShortTypeName("idp").FirstOrDefault();
            dict.Add("idp", (idp != null) ? idp.Value : null);

            var oid = user.GetClaimsByShortTypeName("oid").FirstOrDefault();
            dict.Add("oid", (oid != null) ? oid.Value : null);

            var sid = user.FindFirst(ClaimTypes.NameIdentifier);
            dict.Add("sid", (sid != null) ? sid.Value : null);

            var sub = user.GetClaimsByShortTypeName("sub").FirstOrDefault();
            dict.Add("sub", (sub != null) ? sub.Value : null);
        }
        public static ClaimValues GetUserClaimsForDB(ClaimsPrincipal user)
        { 
            //System.Security.Claims.ClaimsPrincipal user = controller.User;

            ClaimValues dict = new ClaimValues();

            GetAuthenticationClaims(user, dict);

            var vc = user.Claims.Where(c => c.Type == MyDmvProClaims.UserId).FirstOrDefault();
            if (vc != null)
            {
                dict.Add(MyDmvProClaims.UserId, vc.Value);

                vc = user.Claims.Where(c => c.Type == MyDmvProClaims.VendorId).FirstOrDefault();
                if (vc != null)
                    dict.Add(MyDmvProClaims.VendorId, vc.Value);

                vc = user.Claims.Where(c => c.Type == MyDmvProClaims.VendorName).FirstOrDefault();
                if (vc != null)
                    dict.Add(MyDmvProClaims.VendorName, vc.Value);

                vc = user.Claims.Where(c => c.Type == MyDmvProClaims.GroupId).FirstOrDefault();
                if (vc != null)
                    dict.Add(MyDmvProClaims.GroupId, vc.Value);

                vc = user.Claims.Where(c => c.Type == MyDmvProClaims.GroupName).FirstOrDefault();
                if (vc != null)
                    dict.Add(MyDmvProClaims.GroupName, vc.Value);

                vc = user.Claims.Where(c => c.Type == MyDmvProClaims.IsVendorAdmin).FirstOrDefault();
                if (vc != null)
                    dict.Add(MyDmvProClaims.IsVendorAdmin, vc.Value);

                vc = user.Claims.Where(c => c.Type == MyDmvProClaims.IsGroupAdmin).FirstOrDefault();
                if (vc != null)
                    dict.Add(MyDmvProClaims.IsGroupAdmin, vc.Value);
            }

            return dict;
        }
        public static List<string> EmailsFromClaims(ClaimsPrincipal user)
        {
            var emails = user.GetClaimsByShortTypeName("emails").FirstOrDefault();
            if (emails != null)
            {
                List<string> list = new List<string>();
                list.Add(emails.Value);
                return list;
            }
            return null;
        }
    }
    public class MyDmvProClaims
    {
        public const string UserId = "mydmv.pro/userid";
        public const string VendorId = "mydmv.pro/vendorid";
        public const string VendorName = "mydmv.pro/vendorname";
        public const string GroupId = "mydmv.pro/groupid";
        public const string GroupName = "mydmv.pro/groupname";
        public const string IsVendorAdmin = "mydmv.pro/isvendoradmin";
        public const string IsGroupAdmin = "mydmv.pro/isgroupadmin";
        public const string IsSysAdmin = "mydmv.pro/issysadmin";
        public const string RefreshTimestamp = "mydmv.pro/refreshtimestamp";
    }
    public class ClaimValues : Dictionary<string, string>
    {
        public ClaimValues() : base()
        {

        }
        public Guid? GetGuidOrNull(string claimName)
        {
            if (!this.ContainsKey(claimName))
                return null;

            Guid g;
            if (Guid.TryParse(this[claimName], out g))
                return g;

            return null;
        }
        public bool GetBoolOrFalse(string claimName)
        {
            if (!this.ContainsKey(claimName))
                return false;

            bool b;
            if (bool.TryParse(this[claimName], out b))
                return b;

            return false;
        }
        public string GetStringOrNull(string claimName)
        {
            if (!this.ContainsKey(claimName))
                return null;

            return this[claimName];
        }
    }
    public static class ClaimsPrincipalExtentions
    {
        private const string ShortNameProperty = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/ShortTypeName";

        public static IEnumerable<Claim> GetClaimsByShortTypeName(this ClaimsPrincipal cp, string name)
        {
            return cp.Claims.Where(x => x.GetShortTypeName() == name);
        }

        public static string GetShortTypeName(this Claim claim)
        {
            string shortName;
            return claim.Properties.TryGetValue(ShortNameProperty, out shortName) ? shortName : claim.Type;
        }
    }
}
