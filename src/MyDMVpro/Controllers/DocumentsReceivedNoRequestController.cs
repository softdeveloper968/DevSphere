using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System;
using MyDMVpro.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using Microsoft.EntityFrameworkCore;
using MyDMVpro.Common.ViewHelpers;
using System.Linq;
using System.Collections.Generic;
using MyDMVpro.Models.DocumentsReceived_NoRequest;
using System.Threading.Tasks;
using UserInfo = MyDMVpro.Common.UserInfo;
using System.Reflection;

namespace MyDMVpro.Controllers
{
    public class DocumentsReceivedNoRequestController : BaseController
    {
        public DocumentsReceivedNoRequestController(MaggardDMVContext context, IConfiguration configuration, ILogger<PaymentReportController> logger) : base(context, configuration, logger)
        {
        }

        public async Task<IActionResult> DocumentsReceived_NoRequest()
        {
            
            List<Groups> Groups = new List<Groups>();
            Groups = await _context.Groups.ToListAsync();

            List<MdpAttachmentTypes> AttachmentTypes = new List<MdpAttachmentTypes>();
            AttachmentTypes = await _context.MdpAttachmentTypes.ToListAsync();

            ViewBag.Groups = Groups;
            ViewBag.AttachmentTypes = AttachmentTypes;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(DocumentReceived model, IFormFile pdfUpload)
        {
            try
            {
                if (pdfUpload == null || pdfUpload.Length == 0)
                    return Json(new { success = false, message = "File is required" });

                if (ModelState.IsValid)
                {
                    UserInfo user = GetCurrentUser();

                    var filedata = MyDMVpro.Common.FileHelpers.ProcessBinaryFormFile(pdfUpload, ModelState);
                    var filename = System.IO.Path.GetFileName(pdfUpload.FileName);

                    var requests = _context.Requests.Where(x => x.Vin == model.Vin && x.GroupId == model.GroupId).ToList();
                
                    if (requests != null && requests.Count() > 0) 
                    {
                        foreach (var request in requests)
                        {
                            request.IsMatched = true;
                        }

                        model.IsMatched = true;
                    }
                    model.IsShow = true;
                    model.IsShipped = false;
                    model.Document = filedata;
                    model.DocumentName = filename;
                    model.DocumentReceivedID = Guid.NewGuid();
                    model.VendorId = user.VendorId;
                    _context.DocumentsReceived.Add(model);
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Successfully added!" });
                }
                return Json(new { success = false, message = "Error ocurred while processing the request" });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> DocumentsShippedNoRequest()
        {
            try
            {
                var filterHelper = new FilterHelper<DocumentReceivedViewModel>((MaggardDMVContext)_context, _configuration, this);

                return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
                {
                    IQueryable<DocumentReceivedViewModel> DocumentReceivedViewModel = _context.DocumentsReceivedNoRequest
                                                                         .AsNoTracking()
                                                                         .Where(r => ((vendorId != null && r.VendorId == vendorId)) && r.IsShipped && r.IsShow == true);
                    return DocumentReceivedViewModel;
                });
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> DocumentsReceivedNoRequest()
        {
            try
            {
                var filterHelper = new FilterHelper<DocumentReceivedViewModel>((MaggardDMVContext)_context, _configuration, this);
                return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
                {
                    IQueryable<DocumentReceivedViewModel> DocumentReceivedViewModel = _context.DocumentsReceivedNoRequest
                                                                        .AsNoTracking()
                                                                        .Where(r => (vendorId != null && r.VendorId == vendorId) && r.IsShipped == false && r.IsShow == true);
                    return DocumentReceivedViewModel;
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Communication_DocumentReceivedNoRequest()
        {
            try
            {
                var filterHelper = new FilterHelper<DocumentReceivedViewModel>((MaggardDMVContext)_context, _configuration, this);

                return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
                {
                    IQueryable<DocumentReceivedViewModel> rows = null;
                    return DocumentsFilterByUser(vendorId, groupId, false);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Communication_DocumentShippedNoRequest()
        {
            var filterHelper = new FilterHelper<DocumentReceivedViewModel>((MaggardDMVContext)_context, _configuration, this);

            return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<DocumentReceivedViewModel> rows = null;
                return DocumentsFilterByUser(vendorId, groupId, true);
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetDuplicateRecords(string vin, Guid groupId)
        {
            var requests = _context.RequestStatus.Where(x => x.Vin == vin && x.GroupId == groupId).Select(x => new
            {
                groupId = x.GroupId, vin = x.Vin, userId = x.UserId, requestId = x.RequestId, appType =x.AppType,
                state = x.State, requestNumber = x.RequestNo, groupName = x.GroupName
            }).ToList();

            return Json(requests);

        }

        [HttpPost]
        public async Task<IActionResult> AttachRequestData([FromBody]AttachedRequestData data)
        {
            try
            {
                if (data == null || data.RequestId == Guid.Empty || data.DataToAttached == null) 
                {
                        return JsonError("Cannot process the empty request");
                }
            
                UserInfo ui = await GetCurrentUserAsync();
                if (ui == null)
                    return NotFound();

                (bool isMember, UserInfo user) memberInfo = new();
                var requests = await _context.Requests.Where(m => m.RequestId == data.RequestId).FirstOrDefaultAsync();
                if (requests != null)
                {
                    memberInfo = await CurrentUserIsMemberOfGroupOrVendorAsync(requests.GroupId, requests.VendorId);
                }
                if (requests == null || !memberInfo.isMember)
                {
                    return JsonError("Request not found");
                }

                UserInfo user = memberInfo.user;
                RequestAttachments attachment = new()
                {
                    RequestId = data.RequestId,
                    Filename = data.DataToAttached.DocumentName,
                    Description = null,
                    UploadedBy = user.UserId,
                    Image = data.DataToAttached.Document,
                    DateAdded = DateTime.UtcNow,
                    AttachmentTypeId = data.DataToAttached.AttachmentTypeId
                };

                var DocumentReceivedUpdate = _context.DocumentsReceived.FirstOrDefault(m => m.DocumentReceivedID == data.DataToAttached.DocumentReceivedID);
               
                if (DocumentReceivedUpdate != null)
                {
                    DocumentReceivedUpdate.IsShow = false;
                }
                requests.IsMatched = false;
                requests.RequestAttachments.Add(attachment);
                if (_configuration.GetValue<bool>("AppSettings:AddChatOnAttachmentUploadByVendor", true))
                {
                    if (user.IsVendorAgent)
                    {
                        string message = $"Attachment is Associted from doucument received - No request'{data.DataToAttached.DocumentName}'. You can access the record attachments by clicking the paperclip icon from any list view. Thank you!";
                        await AddChat(requests.RequestId, user, message, false);
                    }
                }
                await _context.SaveChangesAsync(user);
                return Json(new { success = true, message = "Document is attached to the request" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message.ToString()});

            }

        }
       
        public static string GetEndpointUrl(HttpContext context, string view)
        {
            string controller = "DocumentsReceivedNoRequest";
            return $"/{controller}/{view}";
        }

        protected IQueryable<DocumentReceivedViewModel> DocumentsFilterByUser(Guid? vendorId, Guid? groupId, bool isShipped = false)
        {
            IQueryable<DocumentReceivedViewModel> DocumentsReceived = _context.DocumentsReceivedNoRequest.Where(x=> x.IsShow == true).AsNoTracking().AsQueryable();

            if (isShipped)
            {
                DocumentsReceived = DocumentsReceived.Where(r => r.IsShipped == true);
            }
            else
            {
                DocumentsReceived = DocumentsReceived.Where(r => r.IsShipped == false);
            }

            if (vendorId != null)
            {
                return DocumentsReceived.Where(r => r.VendorId == vendorId.Value);
            }
            else
            {
                return DocumentsReceived.Where(r => r.GroupId == groupId.Value);
            }
            
        }

    }
}