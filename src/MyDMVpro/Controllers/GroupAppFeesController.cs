using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Models;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

public class GroupAppFeesController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroupAppFeesController> _logger;
    private readonly MaggardDMVContext _context;

    public GroupAppFeesController(MaggardDMVContext context, IConfiguration configuration, ILogger<GroupAppFeesController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [Route("api/GroupAppFees")]
    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> GroupAppFees()
    {
        var request = this.Request;

        if (request.Method == "post")
        {
        }

        return new JsonResult(new { });
    }
}
