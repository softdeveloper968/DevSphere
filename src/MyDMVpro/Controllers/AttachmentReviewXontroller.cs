namespace MyDMVpro.Controllers;

#if false

[Authorize(Policy = "VendorAgentOnly")]
public class AttachmentReviewXontroller : BaseController
{
    public AttachmentReviewXontroller(MaggardDMVContext context, IConfiguration configuration, ILogger<AttachmentReviewXontroller> logger) : base(context, configuration, logger)
    {
    }
    public async Task<IActionResult> Index()
    {
        return View();
    }
}
#endif
