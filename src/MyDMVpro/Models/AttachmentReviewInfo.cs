using Microsoft.Identity.Client;

namespace MyDMVpro.Models;

public class AttachmentReviewInfo
{
    public int PendingReviewCount { get; set; }
    public bool IsVendorAgent { get; set; }
}
