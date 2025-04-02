using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    public class HelpController : MyDMVpro.Controllers.BaseController
    {
        public HelpController(MaggardDMVContext context, IConfiguration configuration, ILogger<HelpController> logger) : base(context, configuration, logger)
        {
        }

        [HttpGet]
        [ResponseCache(Duration = 30 * 60 /* 30 minutes */, Location = ResponseCacheLocation.Any, NoStore = false)]
        [Route("Help/{appType}/{appState}")]
        public async Task<IActionResult> Index(string appType, string appState)
        {
            UserInfo ui = await GetCurrentUserAsync();
            byte[] bytes = null;
            string filename = null;

            var application = await _context.MdpAppTypeStates
                                        .Include(m => m.AppType)
                                        .Where(m => m.AppType.AppType == appType && m.AppState == appState)
                                        .Select(m => new
                                        {
                                            m.InternalHelpID,
                                            m.PublicHelpID
                                        })
                                        .SingleOrDefaultAsync();

            if (application == null ||
                (application.PublicHelpID == null && application.InternalHelpID == null))
            {
                return NotFound();
            }
            FileData file = null;

            if (ui == null || !ui.IsVendorAgent)
            {
                if (application.PublicHelpID == null)
                {
                    return NotFound();
                }
                file = await _context.FileData
                                            .Where(fd => fd.Id == application.PublicHelpID)
                                            .SingleOrDefaultAsync();
            }
            else
            {
                if (application.InternalHelpID == null)
                {
                    return NotFound();
                }
                file = await _context.FileData
                                            .Where(fd => fd.Id == application.InternalHelpID)
                                            .SingleOrDefaultAsync();
            }
            if (file?.Content == null)
            {
                return NotFound();
            }
            return ReturnPdfFile(file.FileName ?? $"{appType}-{appState}-help.pdf", file.Content, true);
        }
        private IActionResult ReturnPdfFile(string filename, byte[] file, bool displayInline)
        {
            System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
            {
                FileName = filename,
                Inline = displayInline
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            //string lastmod = lastModified.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
            //Response.Headers.Add("Last-Modified", lastmod);

            return File(file, "application/pdf");
        }
    }
}

