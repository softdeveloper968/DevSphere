using MyDMVpro.Models;
using System.Collections.Generic;
using System.Net;
using System.Web;
using System;

namespace MyDMVpro.Models.SharedViews;

public class AttachmentChangeTypeModel
{
    public bool IsVendorAgent { get; set; }
    public Guid RequestId { get; set; }
    public int RequestNo { get; set; }
    public Guid AttachmentId { get; set; }
    public Guid? SelectedAttachmentTypeId { get; set; }
    public List<MdpAttachmentTypes> AttachmentTypes { get; set; }
    public List<ApprovalStatus> UploadedAttachmentTypeIds { get; set; }

    public AttachmentChangeTypeModel() { }

}

public class ApprovalStatus
{
    public Guid AttachmentTypeId { get; set; }
    public bool? Approved { get; set; }
}