using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using System;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    public class SystemCheckController : BaseController
    {
        private IConfiguration _config;
        public SystemCheckController(MaggardDMVContext context, IConfiguration configuration, ILogger<SystemCheckController> logger) : base(context, configuration, logger)
        {
            _config = configuration;
        }
        [AllowAnonymous]
        public async Task<ActionResult> VerifyConfiguration(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (Guid.TryParse(_config["SystemCheckCode"], out Guid code))
            {
                if (id == code)
                {
                    try
                    {
                        var result = await DataHelpers.PerformSystemCheck();
                        return new OkObjectResult(result);
                    }
                    catch (Exception ex)
                    {
                        // TEMPORARY: remove this info after debugging
                        string msg = $"{DataHelpers.SqlConnectionString}\r\n{ex.ToString()}";
                        return new OkObjectResult(msg);
                    }
                }
            }
            return NotFound();
        }
    }
}
