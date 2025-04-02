using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.Attributes;
using MyDMVpro.Models;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(MaggardDMVContext context, IConfiguration configuration, ILogger<HomeController> logger) : base(context, configuration, logger)
        {
        }
        [AllowAnonymous]
        [HelpDefinition]
        public async Task<IActionResult> Index()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    return RedirectToAction("Index", "Vendor");
                }
                if (ui.UserId != null)
                {
                    if (ui.IsGroupMember)
                        return RedirectToAction("MyRequests", "MyServices");
                }
                return View("Invite");
            }
            return View("Welcome");
        }

        [AllowAnonymous]
        [HelpDefinition]
        public IActionResult About()
        {
            AddBreadcrumb("About", "/About");

            return View();
        }

        [AllowAnonymous]
        [HelpDefinition("helpdefault")]
        public IActionResult Contact()
        {
            AddBreadcrumb("Contact", "/Contact");

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        [Authorize]
        public IActionResult Claims()
        {
            return View();
        }
        #region Get data method.
        /// <summary>
        /// GET: /Home/GetData
        /// </summary>
        /// <returns>Return data</returns>
        #endregion
    }
}
