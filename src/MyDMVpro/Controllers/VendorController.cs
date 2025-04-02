using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyDMVpro.Common;
using MyDMVpro.Common.Extensions;
using MyDMVpro.Models;
using MyDMVpro.Models.FormsViewModels;
using MyDMVpro.Models.RegistrationFeeCalculatorViewModels;
using MyDMVpro.Models.Tax;
using MyDMVpro.Models.VendorViewModels;
using MyDMVpro.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;



namespace MyDMVpro.Controllers
{
    [Authorize(Policy = "VendorAgentOnly")]
    public class VendorController : BaseController
    {
        private const string c_ConditionReport = "Condition Report";
        private readonly EncryptionSettings _encryptionSettings;
        private readonly OCRSettings _oCRSettings;

        public VendorController(MaggardDMVContext context, IConfiguration configuration, ILogger<VendorController> logger, IOptions<EncryptionSettings> encryptionSettings, IOptions<OCRSettings> oCRSettings) : base(context, configuration, logger)
        {
            _encryptionSettings = encryptionSettings.Value;
            _oCRSettings = oCRSettings.Value;
        }
        public async Task<IActionResult> Index()
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (!ui.IsVendorAgent)
            {
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("RequestLookup", "Vendor");
        }
        public async Task<IActionResult> Completed()
        {
            AddPageHeader("Completed List", "");
            return View();
        }
        public async Task<IActionResult> PaymentReport()
        {
            var states = _context.States.ToList();
            var appTypes = _context.MdpAppTypeCodes.ToList();

            ViewBag.States = states;
            ViewBag.AppTypes = appTypes;

            AddPageHeader("Payment Report List", "");
            return View();
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
        public async Task<IActionResult> Accounting()
        {
            AddPageHeader("Invoicing", "");
            return View();
        }
        public async Task<IActionResult> Disbursements()
        {
            AddPageHeader("Disbursements", "");
            return View();
        }
        public async Task<IActionResult> AccountingNew()
        {
            AddPageHeader("Invoicing (*New)", "");
            return View();
        }
        public async Task<IActionResult> Reports()
        {
            AddPageHeader("Reports", "");
            var groups = await _context.Groups
                                            .Where(g => g.Active == true)
                                            .OrderBy(g => g.GroupName)
                                            .ToListAsync();
            return View(groups);
        }
        // NOTE: AFAIK, Administration is not currently used 
        // and should be removed as soon as possible (including Administration.cshtml)
        // The BillingInfo page is now used instead
        // (Administration.cshtml has fee info, which I believe was part of an earlier feature
        //   that was cut from the features at some point)
        public async Task<IActionResult> Administration()
        {
            UserInfo ui = await GetCurrentUserAsync(true);
            if (ui.IsVendorAgent)
            {
                List<Groups> groups = await _context.Groups.Where(g => g.Active == true).OrderBy(g => g.GroupName).ToListAsync();

                AdministrationViewModel model = new()
                {
                    Groups = groups
                };
                AddPageHeader("Administration", "");
                return View(model);
            }
            return Unauthorized();
        }
        public async Task<IActionResult> EditShipment(Guid id)
        {
            AddPageHeader("Edit Shipment", "");
            UserInfo user = await GetCurrentUserAsync();
            if (user.IsVendorAgent)
            {
                TitlesShippedViewModel model = await GetShipmentViewModel(id, user.VendorId.Value);
                return View(model);
            }
            return Unauthorized();
        }
        [HttpPost]
        public async Task<IActionResult> EditShipment([FromForm] Guid shipmentId, [FromForm] DateTime? date,
            [FromForm] string courierName, [FromForm] string trackingNumber)
        {
            try
            {
                UserInfo user = await GetCurrentUserAsync();
                if (user.IsVendorAgent)
                {
                    Shipment shipment = await _context.Shipments.Where(s => s.ShipmentId == shipmentId).FirstOrDefaultAsync();
                    if (shipment.VendorId == user.VendorId)
                    {
                        await DataHelpers.EditShipment(shipmentId, user.NameIdentifierClaim, date, courierName, trackingNumber);
                        return Redirect("/Vendor/ShipmentReport/" + shipmentId.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "EditShipment");
                return JsonError(ex);
            }
            return JsonError("User not authorized");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteShipment([FromForm] Guid shipmentId, [FromForm] bool moveToLHQueue)
        {
            try
            {
                UserInfo user = await GetCurrentUserAsync();
                if (user.IsVendorAgent)
                {
                    Shipment shipment = await _context.Shipments.Where(s => s.ShipmentId == shipmentId).FirstOrDefaultAsync();
                    if (shipment.VendorId == user.VendorId)
                    {
                        await DataHelpers.DeleteShipment(shipmentId, user.NameIdentifierClaim, moveToLHQueue);
                    }
                    return JsonSuccess();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "DeleteShipment");
                return JsonError(ex);
            }
            return Unauthorized();
        }

        public IActionResult ShipmentQR(Guid id)
        {
            try
            {
                return Content(GetShipmentQR_SVG(id), "image/svg+xml");
            }
            catch (Exception)
            {
                //TODO: return qr for error page
                return Content("");
            }
        }
        private string GetShipmentQR_URL(Guid id)
        {
            string baseUrl = "https://" + Request.Host;
            string url = baseUrl + "/Vendor/EditShipment/" + id.ToString();
            return url;
        }
        private string GetShipmentQR_SVG(Guid id)
        {
            try
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                string payload = GetShipmentQR_URL(id);
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                SvgQRCode qrCode = new SvgQRCode(qrCodeData);
                System.Drawing.Size viewbox = new System.Drawing.Size(25, 25);
                string qrCodeAsSvg = qrCode.GetGraphic(viewbox, true, SvgQRCode.SizingMode.ViewBoxAttribute);
                return qrCodeAsSvg;
            }
            catch (Exception)
            {
                //TODO: return qr for error page
                return null;
            }
        }
        private IActionResult GetShipmentQR_PNG(Guid id)
        {
            try
            {
                QRCodeGenerator qrGenerator = new();
                string payload = GetShipmentQR_URL(id);
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new(qrCodeData);
                byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);
                return ReturnFile("image/png", qrCodeAsPngByteArr);
            }
            catch (Exception)
            {
                //TODO: return qr for error page
                return null;
            }
        }
        public async Task<IActionResult> ShipmentReport(Guid id, string fmt, string open)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.IsVendorAgent)
            {
                bool pdf = false;
                bool openInBrowser = false;
                if (!string.IsNullOrEmpty(fmt))
                {
                    if (fmt == "pdf") pdf = true;
                }
                if (!string.IsNullOrEmpty(open))
                {
                    if (open == "1") openInBrowser = true;
                }
                return await InternalViewShipment(id, ui.VendorId.Value, pdf, openInBrowser);
            }
            return NotFound();
        }
        [HttpPost]
        public async Task<IActionResult> DirectToBilling([FromForm] List<Guid> ids, [FromForm] string note, [FromForm] string remark,
            [FromForm] string code, [FromForm] string date)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    if (!GetServerDateOrNow(date, out DateTime dt))
                    {
                        return GetErrorResult("Invalid finished date");
                    }
                    await DataHelpers.MarkAsDirectToBilling(ui.UserId.Value, date: dt, note: note, remark: remark, code: code, ids: ids);

                    return JsonSuccess();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "DirectToBilling");
                return JsonError(ex);
            }
            return JsonError("Unauthorized");
        }

        [HttpPost]
        public async Task<IActionResult> ReceivedByLH([FromForm] List<Guid> ids, [FromForm] string note, [FromForm] string remark,
            [FromForm] string code, [FromForm] string date)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    if (!GetServerDateOrNow(date, out DateTime dt))
                    {
                        return GetErrorResult("Invalid finished date");
                    }
                    try
                    {
                        await DataHelpers.MarkAsReceivedByLH(ui.UserId.Value, date: dt, note: note, remark: remark, code: code, ids: ids);

                        return JsonSuccess();
                    }
                    catch (ApplicationException ex)
                    {
                        LogError(ex, "ReceivedByLH");
                        return JsonError(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "ReceivedByLH");
                return JsonError(ex);
            }
            return JsonError("Unauthorized");
        }

        [HttpPost]
        public async Task<IActionResult> ShipmentReports(List<Guid> ids, string fmt, string open)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.IsVendorAgent)
            {
                bool pdf = false;
                bool openInBrowser = false;
                if (!string.IsNullOrEmpty(fmt))
                {
                    if (fmt == "pdf") pdf = true;
                }
                if (!string.IsNullOrEmpty(open))
                {
                    if (open == "1") openInBrowser = true;
                }
                return await InternalViewShipment(ids, ui.VendorId.Value, pdf, openInBrowser);
            }
            return NotFound();
        }
        private async Task<TitlesShippedViewModel> GetShipmentViewModel(Guid id, Guid vendorId)
        {
            List<Guid> list = new()
            {
                id
            };
            return await GetShipmentViewModel(list, vendorId);
        }
        private async Task<TitlesShippedViewModel> GetShipmentViewModel(List<Guid> ids, Guid vendorId)
        {
            TitlesShippedViewModel model = new();
            foreach (var id in ids)
            {
                var shipment = await GetShipment(id, vendorId);
                if (shipment != null)
                    model.Shipments.Add(shipment);
            }
            return model;
        }
        private async Task<Shipment> GetShipment(Guid id, Guid vendorId)
        {
            Shipment shipment = await _context.Shipments.Where(s => s.ShipmentId == id && s.VendorId == vendorId).FirstOrDefaultAsync();
            if (shipment == null)
                return null;

            shipment.QRSVG = GetShipmentQR_SVG(id);
            shipment.EditUrl = GetShipmentQR_URL(id);

            List<ShipmentDetail> shipmentDetails = await _context.ShipmentDetails.Where(s => s.ShipmentId == id).ToListAsync();
            shipment.ShipmentDetails = shipmentDetails;

            List<RequestStatus> requests = new();
            foreach (var sd in shipmentDetails)
            {
                var rs = await _context.RequestStatus.AsNoTracking().Where(r => r.RequestId == sd.RequestId).FirstOrDefaultAsync();
                sd.RequestStatus = rs;
            }

            return shipment;
        }
        private async Task<IActionResult> InternalViewShipment(Guid shipmentId, Guid vendorId, bool viewAsPdf, bool openInBrowser)
        {
            List<Guid> ids = new()
            {
                shipmentId
            };
            return await InternalViewShipment(ids, vendorId, viewAsPdf, openInBrowser);
        }
        private async Task<IActionResult> InternalViewShipment(List<Guid> shipmentIds, Guid vendorId, bool viewAsPdf, bool openInBrowser)
        {
            TitlesShippedViewModel model = await GetShipmentViewModel(shipmentIds, vendorId);
            if (viewAsPdf)
            {
                model.RenderAsPdf = viewAsPdf;
                string html = await this.RenderViewToStringAsync("TitlesShipped", model);
                Stream pdfStream = HtmlToPdf(html);
                return ReturnPdfFile($"Shipments-{DateTime.Today:yyyy-MM-dd}.pdf", pdfStream, DateTime.Now, openInBrowser);
            }
            else
            {
                return PartialView("TitlesShipped", model);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> TitlesShipped(string date)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.IsVendorAgent)
            {
                TitlesShippedViewModel model = new();
                AddPageHeader("Titles Shipped Report", "");
                return View(model);
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> GenerateShippingReport([FromForm] string[] ids, [FromForm] string courierName, [FromForm] string trackingNumber,
            [FromForm] string date)
        {
            try
            {

                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    List<Guid> requestIds = GetGuids(ids);
                    string userName = GetUserSID();
                    Guid? shippingId;
                    if (!DateTime.TryParse(date, out DateTime dt))
                    {
                        dt = DateTime.Now.Date;
                    }
                    if (string.IsNullOrWhiteSpace(courierName) && string.IsNullOrWhiteSpace(trackingNumber))
                    {
                        // Tracking#/Courier# can only be set on a single auction/group combo
                        // so this must be a multiple or just no tracking info supplied 
                        await DataHelpers.GenerateMultipleShippingReports(userName, dt, requestIds);
                    }
                    else
                    {
                        _ = await DataHelpers.GenerateShippingReport(userName, dt, courierName, trackingNumber, requestIds);
                    }
                    return JsonSuccess();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "GenerateShippingReport");
                return JsonError(ex);
            }
            return JsonError("Unauthorized");
        }
        public IActionResult SLA()
        {
            AddPageHeader("SLA Report", "");
            return View();
        }
#if NOT_USED
        public async Task<IActionResult> ManageForms()
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.IsVendorAdmin)
            {
                var user = await _context.Users.SingleAsync(u => u.NameIdentifierClaim == ui.NameIdentifierClaim);
                var vendorAgent = await _context.VendorAgent.SingleAsync(x => x.AgentId == user.UserId);
                var forms = await _context.FormTemplates.Where(x => x.VendorId == vendorAgent.VendorId).ToListAsync();
                return View(forms);
            }
            return Unauthorized();
        }
#endif
        public async Task<IActionResult> UploadRequests()
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.IsVendorAgent)
            {
                Models.VendorViewModels.VendorFileUploadModel data = new()
                {
                    SubmitDirect = true,
                    Groups = await _context.Groups.Where(g => g.Active == true).OrderBy(g => g.GroupName).ToListAsync(),
                    DateToVendor = ServerDate()
                };
                ViewData["GroupId"] = new SelectList(_context.Groups, "GroupId", "GroupName");
                AddPageHeader("Upload Requests", "");
                return View(data);
            }
            return Unauthorized();
        }
        public async Task<IActionResult> UploadRequestsMNET()
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.IsVendorAgent)
            {
                Models.VendorViewModels.VendorFileUploadModel data = new()
                {
                    SubmitDirect = true,
                    Groups = await _context.Groups.Where(g => g.Active == true).OrderBy(g => g.GroupName).ToListAsync(),
                    DateToVendor = ServerDate()
                };
                ViewData["GroupId"] = new SelectList(_context.Groups, "GroupId", "GroupName");
                AddPageHeader("Upload maggard.net Requests", "");
                return View(data);
            }
            return Unauthorized();
        }
        public class UserTuple
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public Guid userId { get; set; }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public string displayName { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> GetUsers(Guid groupId)
        {
            List<UserTuple> users = null;

            UserInfo ui = await GetCurrentUserAsync();
            if (ui.IsVendorAgent)
            {
                users = await _context.Users
                            .Include(u => u.UserGroups)
                            .Where(u => u.UserGroups.Any(ug => ug.GroupId == groupId))
                            .OrderBy(u => u.DisplayName)
                            .AsNoTracking()
                            .Select(u => new UserTuple()
                            { userId = u.UserId, displayName = u.DisplayName })
                            .ToListAsync();
                return new JsonResult(users);
            }
            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, Guid? groupId, Guid? userId, bool submitDirect, DateTime? dateToVendor)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.IsVendorAgent)
            {
                if (file != null)
                {
                    long size = file.Length;
                    submitDirect = true;
                    return await SaveToRequests(file, groupId, userId, submitDirect, dateToVendor, false);
                }
                return RedirectToAction(nameof(VendorController.UploadRequests), "Vendor");
            }
            return Unauthorized();
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile_v2(IFormFile file, Guid? groupId, Guid? userId, bool submitDirect, DateTime? dateToVendor, bool requireUserId)
        {
            UserInfo ui = await GetCurrentUserAsync();
            if (ui.IsVendorAgent)
            {
                if (file != null)
                {
                    long size = file.Length;
                    submitDirect = true;
                    return await SaveToRequests(file, groupId, userId, submitDirect, dateToVendor, requireUserId);
                }
                return RedirectToAction(nameof(VendorController.UploadRequests), "Vendor");
            }
            return Unauthorized();
        }
        private async Task<IActionResult> SaveToRequests(IFormFile formFile, Guid? groupId, Guid? userId, bool submitDirect = true, DateTime? dateToVendor = null, bool requireUserId = false)
        {
            if (formFile.Length == 0)
                return Ok(new { count = 0 });

            // Validate Excel workbook
            using (var stream = new MemoryStream())
            {
                await formFile.CopyToAsync(stream);

                byte[] data = stream.ToArray();
                // Save to database
                string agentName = GetUserSID();
                string filename = Path.GetFileName(formFile.FileName);

                try
                {
                    string errorMsg;
                    Guid? fileUploadId;
                    (fileUploadId, errorMsg) = await DataHelpers.VendorSubmitToFileUploads(_context, filename, groupId, userId, agentName, data, submitDirect, dateToVendor, requireUserId);
                    if (fileUploadId != null)
                    {
                        FileUploads file = await _context.FileUploads.Where(fu => fu.FileUploadId == fileUploadId).FirstOrDefaultAsync();
                        if (file != null)
                        {
                            if (errorMsg == null)
                                file.FileImage = null;
                            else
                                _context.FileUploads.Remove(file);
                            await _context.SaveChangesAsync(await GetCurrentUserAsync());
                        }
                    }
                    if (errorMsg != null)
                    {
                        return JsonError(errorMsg);
                    }
                    return RedirectToAction(nameof(VendorController.UploadRequests), "Vendor");
                }
                catch (Exception ex)
                {
                    LogError(ex, "SaveToRequests");
                    return JsonError(ex);
                }
            }

            return Ok(new { count = 1 });
        }
        public async Task<IActionResult> Abstracts()
        {
            AddPageHeader("Abstracts / Documents", "");
            return View();
        }
        public async Task<IActionResult> ToDoWVClearing()
        {
            AddPageHeader("ToDo - WV Clearinginghouse", "");
            return View();
        }
        public async Task<IActionResult> ToDoOther()
        {
            AddPageHeader("ToDo - Other", "");
            return View();
        }
        public async Task<IActionResult> ToDoTC()
        {
            AddPageHeader("ToDo - Title Correction", "");
            return View();
        }
        public async Task<IActionResult> ToDoLC()
        {
            AddPageHeader("ToDo - Lien Continuation", "");
            return View();
        }
        public async Task<IActionResult> ToDoLI()
        {
            AddPageHeader("ToDo - Lien Inquiry", "");
            return View();
        }
        public async Task<IActionResult> Holds()
        {
            AddPageHeader("Holds", "");
            return View();
        }
        public async Task<IActionResult> Cancelled()
        {
            AddPageHeader("Cancelled", "");
            return View();
        }
        public async Task<IActionResult> Deletes()
        {
            AddPageHeader("Deletes Pending", "");
            return View();
        }
        private async Task<IActionResult> InternalRequestLookup(Guid? id, string vin, int? rno)
        {
            AddPageHeader("Request Lookup", "");
            if (id.HasValue)
            {
                ViewBag.RequestId = id.ToString();
                try
                {
                    var request = await _context.Requests
                                                    .Where(r => r.RequestId == id.Value)
                                                    .FirstOrDefaultAsync();
                    ViewBag.RequestNo = rno;
                    ViewBag.VIN = request.Vin;
                    ViewBag.HelpLink = await GetHelpLink(request);
                }
                catch (Exception ex)
                {
                    //TODO: 
                }
            }
            else
            {
                ViewBag.VIN = "";
                ViewBag.RequestNo = null;
                if (vin != null && rno.HasValue)
                {
                    var request = await _context.Requests.Where(r => r.Vin == vin && r.Id == rno).SingleOrDefaultAsync();
                    if (request != null)
                    {
                        try
                        {
                            ViewBag.RequestId = request.RequestId.ToString();
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, "InternalRequestLookup");
                        }
                        ViewBag.VIN = request.Vin;
                        ViewBag.RequestNo = request.Id;
                        ViewBag.HelpLink = await GetHelpLink(request);
                    }
                }
            }
            return View();
        }
        private async Task<string> GetHelpLink(Requests request)
        {
            var hasHelp = await _context.MdpAppTypeStates
                                            .Include(a => a.AppType)
                                            .Where(a => a.AppType.AppType == request.AppType && a.AppState == request.State)
                                            .Where(a => a.InternalHelpID != null)
                                            .AnyAsync();
            string link = null;
            if (hasHelp)
            {
                link = $"/help/{request.AppType}/{request.State}";
            }
            return link;
        }
        [HttpGet]
        [Route("Vendor/RequestLookup")]
        [Route("Vendor/RequestLookup/{id}")]
        [Route("Vendor/RequestLookup/{vin}/{rno}")]
        public async Task<IActionResult> RequestLookup(Guid? id, string vin, int? rno)
        {
            if (vin == null && rno == null)
            {
                return await InternalRequestLookup(id, vin, rno);
            }
            else
            {
                return await InternalRequestLookup(null, vin, rno);
            }
        }
        public async Task<IActionResult> Master()
        {
            AddPageHeader("Master List", "");
#if USE_VIEWS_FROM_DATABASE
            return View(await GetVendorViewDefinitions("Master"));
#else
            return View(await GetVendorViewDefinitions("Master"));
#endif
        }

        public async Task<IActionResult> Chats()
        {
            AddPageHeader("Chats", "");
            return View();
        }
        public async Task<IActionResult> ToDo()
        {
            AddPageHeader("To Do - Repo/Duplicate Title", "");
            return View();
        }
#if false
        public async Task<IActionResult> ShipItems()
        {
            return PartialView();
        }
#endif
        [HttpPost]
        public async Task<IActionResult> MergeCRs([FromForm] string[] ids)
        {
            if (ids == null || ids.Length == 0)
                return JsonSuccess();

            UserInfo user = await GetCurrentUserAsync();
            if (user.IsVendorAgent)
            {
                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }
                List<Guid> attachmentIDs = await DataHelpers.GetCRAttachmentsForRequests(requestIds);
                Stream stream = await DataHelpers.GetMergedCR(attachmentIDs);
                string filename = $"MergedCR-{DateTime.Now:yyyy-MM-dd HHmmss}.pdf";
                return File(stream, "application/pdf", filename);
            }
            return Unauthorized();
        }
        [HttpPost]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> RefreshAutoIMS([FromForm] string[] ids, [FromForm] string force)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                UserInfo user = await GetCurrentUserAsync();
                if (user.IsVendorAgent)
                {
                    List<Guid> requestIds = new();

                    foreach (string sGuid in ids)
                    {
                        if (Guid.TryParse(sGuid, out Guid guid))
                        {
                            requestIds.Add(guid);
                        }
                    }
                    var crAttachmentType = await _context.AttachmentTypes.Where(at => at.Name == c_ConditionReport).FirstOrDefaultAsync();
                    Guid? crAttachmentTypeId = crAttachmentType?.AttachmentTypeId;

                    var reqList = await _context.Requests
                                                    .Include(r => r.Group)
                                                    .Include(r => r.RequestAttachments)
                                                    .Where(r => requestIds.Contains(r.RequestId) && r.VendorId == user.VendorId && r.Group.AutoIMSEnabled == true)
                                                    .Where(r => r.AppType == "RT" && r.State == "MD")
                                                    .Select(r => new
                                                    {
                                                        r.RequestId,
                                                        r.GroupId,
                                                        r.AppType,
                                                        r.State,
                                                        HasCRAttachment = (crAttachmentTypeId != null && r.RequestAttachments.Any(ra => ra.AttachmentTypeId == crAttachmentTypeId))
                                                    })
                                                    .ToListAsync();

                    int count = reqList.Count(r => r.HasCRAttachment == false && r.AppType == "RT" && r.State == "MD");
                    int processCount = 0;

                    foreach (var g in reqList.Select(r => r.GroupId).Distinct())
                    {
                        var rids = reqList.Where(r => r.GroupId == g && r.HasCRAttachment == false)
                                            .Select(r => r.RequestId)
                                            .ToList();
                        if (rids.Count > 0)
                        {
                            processCount += rids.Count;
                            await DataHelpers.TriggerAutoImsDownload(g.Value, rids, _logger, true);
                        }
                    }
                    var result = new
                    {
                        count = count,
                        message = count == 0 ? "All requests already have CR attachments" : $"CR attachments are being downloaded for {count} of {reqList.Count} requests"
                    };

                    return JsonSuccess(result);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "RefreshAutoIMS");
                return JsonError(ex);
            }
            return Unauthorized();
        }
        [HttpPost]
        public async Task<IActionResult> DeleteInvoice([FromForm] Guid invoiceId)
        {
            try
            {
                UserInfo user = await GetCurrentUserAsync();

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (!user.IsVendorAdmin)
                        {
                            throw new ApplicationException("Access denied");
                        }
                        Invoice invoice = _context.Invoice
                                    .Include(i => i.InvoiceDetails)
                                    .Where(i => i.InvoiceId == invoiceId && i.VendorId == user.VendorId)
                                    .FirstOrDefault();
                        _ = invoice ?? throw new ApplicationException("Invoice not found");

                        List<InvoiceDetail> details = await _context.InvoiceDetail
                                                                    .Where(i => i.InvoiceId == invoiceId)
                                                                    .ToListAsync();

                        await AddRemarkForInvoiceDelete(invoice, details, user.UserId);

                        _context.InvoiceDetail.RemoveRange(details);
                        _context.Invoice.Remove(invoice);

                        await _context.SaveChangesAsync(user);

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.ToString());
                        return GetErrorResult(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "DeleteInvoice");
                return JsonError(ex);
            }
            return JsonSuccess();
        }

        public async Task<IActionResult> ViewInvoice(Guid id, string fmt, string open)
        {
            bool pdf = false;
            bool openInBrowser = false;
            if (!string.IsNullOrEmpty(fmt))
            {
                if (fmt == "pdf") pdf = true;
            }
            if (!string.IsNullOrEmpty(open))
            {
                if (open == "1") openInBrowser = true;
            }
            return await InternalViewInvoice(new List<Guid>() { id }, pdf, openInBrowser);
        }

        public async Task<IActionResult> ViewMultipleInvoice(List<Guid> ids, string fmt, string open)
        {
            bool pdf = false;
            bool openInBrowser = false;
            if (!string.IsNullOrEmpty(fmt))
            {
                if (fmt == "pdf") pdf = true;
            }
            if (!string.IsNullOrEmpty(open))
            {
                if (open == "1") openInBrowser = true;
            }
            return await InternalViewInvoice(ids, pdf, openInBrowser);
        }

        private async Task<IActionResult> InternalViewInvoice(List<Guid> invoiceIds, bool viewAsPdf, bool openInBrowser)
        {
            UserInfo user = await GetCurrentUserAsync();

            if (user.IsVendorAgent)
            {
                var invoices = await _context.Invoice.Where(i => invoiceIds.Contains(i.InvoiceId)).ToListAsync();

                InvoiceListViewModel invoicesToRender = new();

                foreach (var item in invoices)
                {
                    if (item.VendorId == user.VendorId)
                    {
                        List<InvoiceDetail> details = await _context.InvoiceDetail.Where(i => i.InvoiceId == item.InvoiceId).ToListAsync();

                        GroupSettings gsettings = await _context.GroupSettings
                        .Where(g => g.GroupId == item.GroupId
                                        && g.VendorId == user.VendorId
                                        && g.SettingsName == "Defaults")
                                    .FirstOrDefaultAsync();

                        // REIVEW: What should be done here? error?
                        gsettings ??= new GroupSettings
                        {
                            JSettings = @"{ 
                            ""BillingAddress"": { 
                                ""CompanyName"": """", 
                                ""AddressLine1"": """",
                                ""AddressLine2"": """", 
                                ""City"": """", 
                                ""State"": """",
                                ""ZipCode"": """", 
                                ""Attn"": ""Accounts Payable"" 
                            } 
                            }"
                        };

                        VendorSettings vsettings = _context.VendorSettings.Where(v => v.VendorId == user.VendorId).FirstOrDefault();

                        vsettings ??= new VendorSettings
                        {
                            JSettings = @"{ 
	                                ""Invoice"": {
		                                ""CompanyName"": """", 
		                                ""AddressLine1"": """", 
		                                ""AddressLine2"": """", 
		                                ""City"": """", 
		                                ""State"": """",
		                                ""ZipCode"": """", 
		                                ""Phone"": ""Phone: "",
		                                ""Fax"": ""Fax: "",
		                                ""Email"": """",
		                                ""TaxID"": """"
	                                    }}"
                        };

                        invoicesToRender.InvoiceList.Add(new InvoiceViewModel()
                        {
                            Invoice = item,
                            Details = details,
                            // confirmed dynamic works for this case 12/26/2023
                            Settings = JsonConvert.DeserializeObject<dynamic>(gsettings.JSettings),
                            VendorSettings = JsonConvert.DeserializeObject<dynamic>(vsettings.JSettings),
                        });
                    }
                }

                string groupName = "multiple_groups";

                var allGroups = invoices.Select(i => i.GroupId).Distinct().ToList();

                if (allGroups.Count == 1)
                {
                    groupName = _context.Groups.Find(allGroups[0]).GroupName;
                }

                if (viewAsPdf)
                {
                    string html = await this.RenderViewToStringAsync("ViewInvoice", invoicesToRender);
                    Stream pdfStream = HtmlToPdf(html);

                    return ReturnPdfFile($"invoice-{groupName}-{DateTimeHelpers.ServerDateTime():yyyy-MM-dd-HH-mm}.pdf", pdfStream, DateTime.Now, openInBrowser);
                }
                else
                {
                    return PartialView("ViewInvoice", invoicesToRender);
                }

            }
            return Ok();
        }

        [HttpPost]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> CreateInvoice(
            [FromForm] string details,
            [FromForm] string invoiceDate,
            [FromForm] string invoiceNum,
            [FromForm] string invoiceAmount,
            [FromForm] string custName,
            [FromForm] string custAddr1,
            [FromForm] string custAddr2,
            [FromForm] string custCity,
            [FromForm] string custState,
            [FromForm] string custZip,
            [FromForm] string custAttn,
            [FromForm] bool singleInvoice)
        {
            try
            {
                UserInfo user = await GetCurrentUserAsync();

                if (!user.IsVendorAdmin)
                    return Unauthorized();

                int totalInvoiceCount = 0;

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    if (!Decimal.TryParse(invoiceAmount, out decimal _invoiceAmount))
                        throw new ApplicationException("Invalid invoice amount");

                    DateTime _invoiceDate = DateTime.Now.Date;
                    if (!string.IsNullOrEmpty(invoiceDate))
                        if (!DateTime.TryParse(invoiceDate, out _invoiceDate))
                            throw new ApplicationException("Invalid invoice Date");

                    Vendors vendor = _context.Vendors.Find(user.VendorId);

                    if (vendor.NextInvoiceNum == 0)
                        throw new ApplicationException("Invoice number is required");

                    var requestsData = JsonConvert.DeserializeObject<List<RequestDTO>>(details);

                    List<Guid> requestIDs = new();
                    foreach (var data in requestsData)
                    {
                        if (!Guid.TryParse(data.id.ToString(), out Guid requestId))
                            throw new ApplicationException("Invalid request id");

                        requestIDs.Add(requestId);
                    }

                    var anyRequestIsAlreadyInvoiced = _context.InvoiceDetail.Where(d => requestIDs.Contains(d.RequestId)).Count();

                    if (anyRequestIsAlreadyInvoiced > 0)
                        throw new ApplicationException("Invoice contains request that has already been invoiced");

                    List<Guid?> groupIDs = await _context.Requests.Where(r => requestIDs.Contains(r.RequestId))
                                                                    .Select(r => r.GroupId)
                                                                    .Distinct()
                                                                    .ToListAsync();
                    if (groupIDs.Count != 1)
                        throw new ApplicationException("Invoice cannot include multiple clients");

                    //If true, we create a single invoice for each request id
                    if (singleInvoice)
                    {
                        foreach (var request in requestsData)
                        {
                            int invoiceCount = await HandleInvoiceAndRequestsAsync(
                                    user,
                                    vendor,
                                    new List<RequestDTO>() { request },
                                    groupIDs.SingleOrDefault().Value,
                                    custName,
                                    custAddr1,
                                    custAddr2,
                                    custCity,
                                    custState,
                                    custZip,
                                    custAttn);
                            totalInvoiceCount += invoiceCount;
                        }
                    }
                    else
                    {
                        int invoiceCount = await HandleInvoiceAndRequestsAsync(
                            user,
                            vendor,
                            requestsData,
                            groupIDs.SingleOrDefault().Value,
                            custName,
                            custAddr1,
                            custAddr2,
                            custCity,
                            custState,
                            custZip,
                            custAttn);
                        totalInvoiceCount += invoiceCount;
                    }

                    await _context.SaveChangesAsync(user);

                    await transaction.CommitAsync();
                }
                string msg = $"{totalInvoiceCount} invoice(s) created";
                return JsonSuccess(msg);
            }
            catch (Exception ex)
            {
                LogError(ex, "CreateInvoice");
                return JsonError(ex);
            }
        }

        private async Task<int> HandleInvoiceAndRequestsAsync(
            UserInfo user,
            Vendors vendor,
            List<RequestDTO> requestsData,
            Guid groupId,
            string custName,
            string custAddr1,
            string custAddr2,
            string custCity,
            string custState,
            string custZip,
            string custAttn)
        {
            int invoiceCount = 0;

            var invoiceNum = vendor.NextInvoiceNum.ToString();
            var primarySuffix = _configuration["AppSettings:PrimaryInvoiceSuffix"];
            var secondarySuffix = _configuration["AppSettings:SecondaryInvoiceSuffix"];

            if (_context.Invoice.Any(i => i.VendorId == user.VendorId && i.InvoiceNo == invoiceNum))
                throw new ApplicationException($"Invoice {invoiceNum} already exists");
            else
                vendor.NextInvoiceNum++;

            var _invoiceAmount = requestsData.Sum(x => x.totalFee);
            var hasSecondaryInvoice = requestsData.Any(x => (x.svcFee2 != null && x.svcFee2 != 0)
                || (x.otherFee2 != null && x.otherFee2 != 0)
                || (x.otherDesc2 != null && x.otherDesc2 != ""));

            if (!hasSecondaryInvoice)
            {
                primarySuffix = "";
            }

            Invoice invoice = new()
            {
                CreatedBy = user.UserId,
                VendorId = user.VendorId.Value,
                InvoiceId = Guid.NewGuid(),
                InvoiceAmount = Decimal.Zero,
                DmvFees = Decimal.Zero,
                OtherFees = Decimal.Zero,
                ServiceFees = Decimal.Zero,
                InvoiceDate = DateTimeHelpers.ServerDate(),
                InvoiceNo = $"{invoiceNum}{primarySuffix}",
                GroupId = groupId,
                CustName = custName,
                CustAddr1 = custAddr1,
                CustAddr2 = custAddr2,
                CustCity = custCity,
                CustState = custState,
                CustZip = custZip,
                CustAttn = custAttn,
                SecondaryInvoice = false
            };

            await _context.Invoice.AddAsync(invoice);

            int order = 1;

            decimal calculatedInvoiceTotal = Decimal.Zero;
            decimal dmvFeesTotal = Decimal.Zero;
            decimal otherFeesTotal = Decimal.Zero;
            decimal svcFeesTotal = Decimal.Zero;

            foreach (var detail in requestsData)
            {
                order = await AddInvoiceDetail(invoice, detail, true, order);
            }
            if (invoice.InvoiceAmount != _invoiceAmount)
                throw new ApplicationException($"Invoice total does not match ({invoice.InvoiceAmount:C}/{_invoiceAmount:C}");

            invoiceCount++;

            if (hasSecondaryInvoice)
            {
                var _invoiceAmount2 = requestsData.Sum(x => x.totalFee2);

                Invoice invoice2 = new()
                {
                    CreatedBy = user.UserId,
                    VendorId = user.VendorId.Value,
                    InvoiceId = Guid.NewGuid(),
                    InvoiceAmount = Decimal.Zero,
                    DmvFees = Decimal.Zero,
                    OtherFees = Decimal.Zero,
                    ServiceFees = Decimal.Zero,
                    InvoiceDate = DateTimeHelpers.ServerDate(),
                    InvoiceNo = $"{invoiceNum}{secondarySuffix}",
                    GroupId = groupId,
                    CustName = custName,
                    CustAddr1 = custAddr1,
                    CustAddr2 = custAddr2,
                    CustCity = custCity,
                    CustState = custState,
                    CustZip = custZip,
                    CustAttn = custAttn,
                    SecondaryInvoice = true
                };

                await _context.Invoice.AddAsync(invoice2);

                order = 1;

                invoice2.DmvFees = Decimal.Zero;
                invoice2.OtherFees = Decimal.Zero;
                invoice2.ServiceFees = Decimal.Zero;

                foreach (var detail in requestsData)
                {
                    order = await AddInvoiceDetail(invoice2, detail, false, order);
                }
                invoiceCount++;

                if (invoice2.InvoiceAmount != _invoiceAmount2)
                    throw new ApplicationException($"Secondary invoice total does not match ({calculatedInvoiceTotal:C}/{_invoiceAmount:C}");
            }
            return invoiceCount;
        }

        private async Task<int> AddInvoiceDetail(Invoice invoice, RequestDTO detail, bool primary, int order)
        {
            decimal totalFee;
            decimal dmvFee = (primary ? detail.dmvFee : null) ?? 0;
            decimal svcFee = (primary ? detail.svcFee : detail.svcFee2) ?? 0;
            decimal otherFee = (primary ? detail.otherFee : detail.otherFee2) ?? 0;

            invoice.DmvFees += dmvFee;
            invoice.ServiceFees += svcFee;
            invoice.OtherFees += otherFee;

            totalFee = dmvFee + svcFee + otherFee;
            invoice.InvoiceAmount += totalFee;

            string otherDesc = (primary ? detail.otherDesc : detail.otherDesc2);
            string glCode = detail.glCode ?? "";

            Guid.TryParse(detail.id, out Guid requestId);

            InvoiceDetail invoiceDetails = new()
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceListOrder = order++,
                DmvFee = dmvFee,
                ServiceFee = svcFee,
                OtherFee = otherFee,
                OtherDesc = otherDesc,
                GlCode = glCode,
                TotalDue = totalFee,
                RequestId = requestId,
                SecondaryInvoice = !primary
            };
            await _context.InvoiceDetail.AddAsync(invoiceDetails);

            await AddRemarkForInvoice(requestId, invoice, invoiceDetails);

            return order;
        }
        private async Task AddRemarkForInvoicePaid(Invoice invoice, Guid? userId)
        {
            foreach (var detail in invoice.InvoiceDetails)
            {
                await _context.RequestNotes.AddAsync(new RequestNotes
                {
                    RequestId = detail.RequestId,
                    Remark = $"Invoice {invoice.InvoiceNo} marked paid",
                    Note = null,
                    ModifiedBy = userId
                });
            }
        }
        private async Task AddRemarkForInvoicesPaid(List<Invoice> invoices, Guid? userId)
        {
            foreach (var invoice in invoices)
            {
                foreach (var detail in invoice.InvoiceDetails)
                {
                    await _context.RequestNotes.AddAsync(new RequestNotes
                    {
                        RequestId = detail.RequestId,
                        Remark = $"Invoice {invoice.InvoiceNo} marked paid",
                        Note = null,
                        ModifiedBy = userId
                    });
                }
            }
        }
        private async Task AddRemarkForInvoiceDelete(Invoice invoice, List<InvoiceDetail> details, Guid? userId)
        {
            foreach (var detail in details)
            {
                await _context.RequestNotes.AddAsync(new RequestNotes
                {
                    RequestId = detail.RequestId,
                    Remark = $"Invoice {invoice.InvoiceNo} deleted",
                    Note = null,
                    LastUpdated = DateTime.UtcNow,
                    ModifiedBy = userId
                });
            }
        }
        private async Task AddRemarkForInvoice(Guid requestId, Invoice invoice, InvoiceDetail detail)
        {
            await _context.RequestNotes.AddAsync(new RequestNotes
            {
                RequestId = requestId,
                Remark = $"Invoice {invoice.InvoiceNo} created. \r\n" +
                    $"\tDMV fee: {detail.DmvFee:C2}\r\n" +
                    $"\tService fee: {detail.ServiceFee:C2}\r\n" +
                    $"\tOther fee: {detail.OtherFee:C2}\r\n" +
                    $"\tTotal due: {detail.TotalDue:C2}",
                Note = null
            });
        }


        [HttpPost]
        public async Task<IActionResult> MarkAsInvoiced([FromForm] string[] ids,
            [FromForm] string invoiceDate, [FromForm] string invoiceNum, [FromForm] string invoiceAmount, [FromForm] string invoiceDatePaid, [FromForm] string invoiceNote)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();
                Guid groupId;

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }
                try
                {
                    var groupIds = await _context.Requests.Where(r => requestIds.Contains(r.RequestId))
                                                        .Select(r => r.GroupId)
                                                        .Distinct()
                                                        .ToListAsync();
                    if (groupIds.Count > 1)
                    {
                        return JsonError("Invoices cannot span multiple groups");
                    }
                    groupId = groupIds[0].Value;
                }
                catch (Exception ex)
                {
                    LogError(ex, "MarkAsInvoiced");
                    return JsonError("Unhandled exception");
                }
                invoiceNum = invoiceNum?.Trim();
                invoiceAmount = invoiceAmount?.Trim();
                invoiceDate = invoiceDate?.Trim();
                invoiceDatePaid = invoiceDatePaid?.Trim();

                string userName = GetUserSID();

                if (!GetServerDateOrNow(invoiceDate, out DateTime dtInvoiceDate))
                {
                    return GetErrorResult("Invalid invoice date");
                }

                DateTime? dtInvoiceDatePaid = null;
                if (DateTime.TryParse(invoiceDatePaid, out DateTime dt))
                {
                    // log
                    dtInvoiceDatePaid = dt;
                }
                Decimal? decInvoiceAmount = null;
                if (Decimal.TryParse(invoiceAmount, out Decimal dec))
                {
                    // log
                    decInvoiceAmount = dec;
                }

                try
                {
                    UserInfo ui = await GetCurrentUserAsync();

                    Invoice invoice = new()
                    {
                        InvoiceDate = dtInvoiceDate,
                        InvoiceDatePaid = dtInvoiceDatePaid,
                        InvoiceAmount = decInvoiceAmount,
                        InvoiceNo = invoiceNum,
                        InvoiceNote = invoiceNote,
                        InvoiceId = Guid.NewGuid(),
                        VendorId = ui.VendorId.Value,
                        CreatedBy = ui.UserId,
                        GroupId = groupId
                    };

                    _context.Invoice.Add(invoice);

                    int invoiceListOrder = 0;
                    foreach (Guid rid in requestIds)
                    {
                        InvoiceDetail detail = new()
                        {
                            InvoiceId = invoice.InvoiceId,
                            RequestId = rid,
                            InvoiceListOrder = invoiceListOrder++
                        };
                        _context.InvoiceDetail.Add(detail);
                    }
                    await _context.SaveChangesAsync(ui);
                }
                catch (Exception ex)
                {
                    LogError(ex, "MarkAsInvoiced");
                    return JsonError("Unhandled exception");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "MarkAsInvoiced");
                return JsonError(ex);
            }
            return JsonSuccess();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsPaid([FromForm] Guid invoiceId, [FromForm] string invoiceDatePaid, [FromForm] string invoiceNote)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                var invoice = _context.Invoice
                                        .Include(i => i.InvoiceDetails)
                                        .Where(i => i.InvoiceId == invoiceId && i.VendorId == ui.VendorId)
                                        .FirstOrDefault();
                if (invoice == null)
                {
                    return JsonError("Invoices not found");
                }
                if (!DateTime.TryParse(invoiceDatePaid, out DateTime dt))
                {
                    return JsonError("Invalid date paid");
                }
                invoice.InvoiceDatePaid = dt;
                invoice.InvoiceNote = invoiceNote;

                await AddRemarkForInvoicePaid(invoice, ui.UserId);

                await _context.SaveChangesAsync(ui);
            }
            catch (Exception ex)
            {
                LogError(ex, "MarkAsPaid");
                return JsonError("Unhandled exception");
            }

            return JsonSuccess();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsPaidMutipleInvoices([FromForm] List<Guid> invoiceIds, [FromForm] string invoiceDatePaid, [FromForm] string invoiceNote)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                var invoices = await _context.Invoice
                                        .Include(i => i.InvoiceDetails)
                                        .Where(i => invoiceIds.Contains(i.InvoiceId) && i.VendorId == ui.VendorId)
                                        .ToListAsync();

                if (invoices.Count == 0)
                    return JsonError("Invoices not found");

                if (!DateTime.TryParse(invoiceDatePaid, out var dt))
                    return JsonError("Invalid date paid");

                foreach (var invoice in invoices)
                {
                    invoice.InvoiceDatePaid = dt;
                    invoice.InvoiceNote = invoiceNote;
                }

                await AddRemarkForInvoicesPaid(invoices, ui.UserId);

                await _context.SaveChangesAsync(ui);
            }
            catch (Exception ex)
            {
                LogError(ex, "MarkAsPaidMutipleInvoices");
                return JsonError("Unhandled exception");
            }

            return JsonSuccess();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsFinished([FromForm] string[] ids, [FromForm] string date,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid finish date");
                }
                await DataHelpers.MarkAsFinished(userName, dt, requestIds, code, note, remark, chat);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "MarkAsFinished");
                return JsonError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> MoveItems([FromForm] string[] ids, [FromForm] string stage, [FromForm] string code, [FromForm] string reasonCancelled)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                string userName = GetUserSID();
                if (userName != null)
                {
                    List<Guid> requestIds = new();

                    foreach (string sGuid in ids)
                    {
                        if (Guid.TryParse(sGuid, out Guid guid))
                        {
                            requestIds.Add(guid);
                        }
                    }

                    await DataHelpers.HoldItems(userName, stage, code, reasonCancelled, requestIds);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "MoveItems");
                return JsonError(ex);
            }
            return JsonSuccess();
        }

        [HttpPost]
        public async Task<IActionResult> Documents_Mark_as_Shipped(
            [FromForm] string shippedNote,
            [FromForm] DateTime dateShipped,
            [FromForm] string trackingNumber,
            [FromForm] string vin,
            [FromForm] string[] documentReceivedIds
        )
        {
            try
            {
                if (vin == null || documentReceivedIds.Length == 0)
                    return JsonError("vin is required");

                string userName = GetUserSID();
                if (string.IsNullOrWhiteSpace(userName))
                    return JsonError("User not found.");

                List<Guid> documentIds = new();
                foreach (string sGuid in documentReceivedIds)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        documentIds.Add(guid);
                    }
                }
                await DataHelpers.MarkItemsAsShipped(
                    agentName: userName,
                    vin: vin,
                    trackingNumber: trackingNumber,
                    dateShipped: dateShipped,
                    shippedNote: shippedNote,
                    isShipped: true,
                    documentReceivedIds: documentIds
                );
               
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "MarkItemsAsShipped");
                return JsonError(ex);
            }
        }

        [HttpPost]
       public async Task<IActionResult> MoveItemsWithNotes(
           [FromForm] string[] ids,
           [FromForm] int stage,
           [FromForm] string code,
           [FromForm] bool codeNotRequired,
           [FromForm] string reasonCancelled,
           [FromForm] string note,
           [FromForm] string remark,
           [FromForm] string chat,
           [FromForm] string appType
       )
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                string userName = GetUserSID();
                if (string.IsNullOrWhiteSpace(userName))
                    return JsonError("User not found.");

                List<Guid> requestIds = new();
                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }
                // Validate required fields
                if (stage == (int)ProcessStageIDs.Cancelled && string.IsNullOrWhiteSpace(reasonCancelled))
                    return JsonError("Reason cancelled is required");

                if (string.IsNullOrWhiteSpace(code) && !codeNotRequired)
                    return JsonError("Code is required");

                if (string.IsNullOrWhiteSpace(note) && string.IsNullOrWhiteSpace(remark))
                    return JsonError("Note or remark is required");

               
                await DataHelpers.MoveItems(
                    agentName: userName,
                    stage: stage,
                    code: code,
                    reasonCancelled: reasonCancelled,
                    note: note,
                    remark: remark,
                    ids: requestIds,
                    chat: chat
                );
                

                if (appType == "REG")
                {
                    bool updateSuccessful = await UpdateRequestRecord(requestIds.FirstOrDefault(), stage);
                    if (updateSuccessful)
                    {
                        return JsonSuccess();
                    }
                    else
                    {
                        return JsonError("Failed to update the request record. Please try again.");
                    }
                }
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "MoveItemsWithNotes");
                return JsonError(ex);
            }
        }


      [HttpPost]
      public async Task<IActionResult> MoveItemsWithNotesAndEtaToPending(
          [FromForm] string[] ids,
          [FromForm] int stage,
          [FromForm] string code,
          [FromForm] bool codeNotRequired,
          [FromForm] string reasonCancelled,
          [FromForm] string note,
          [FromForm] string remark,
          [FromForm] string chat,
          [FromForm] string appType,
          [FromForm] DateTime? eta
      )
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                string userName = GetUserSID();
                if (string.IsNullOrWhiteSpace(userName))
                    return JsonError("User not found.");

                List<Guid> requestIds = new();
                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                if (stage == (int)ProcessStageIDs.Cancelled && string.IsNullOrWhiteSpace(reasonCancelled))
                    return JsonError("Reason cancelled is required");

                if (string.IsNullOrWhiteSpace(code) && !codeNotRequired)
                    return JsonError("Code is required");

                if (string.IsNullOrWhiteSpace(note) && string.IsNullOrWhiteSpace(remark))
                    return JsonError("Note or remark is required");

                try
                {

                    if (stage == (int)ProcessStageIDs.TitlePending)
                    {
                        await DataHelpers.MoveItemsToPendingList(
                            agentName: userName,
                            stage: stage,
                            code: code,
                            reasonCancelled: reasonCancelled,
                            note: note,
                            remark: remark,
                            ids: requestIds,
                            chat: chat,
                            eta: eta
                        );
                    }
                    else
                    {
                        await DataHelpers.MoveItems(
                           agentName: userName,
                           stage: stage,
                           code: code,
                           reasonCancelled: reasonCancelled,
                           note: note,
                           remark: remark,
                           ids: requestIds,
                           chat: chat
                       );
                    }
                }
                catch (Exception)
                {

                    throw;
                }


                if (appType == "REG")
                {
                    bool updateSuccessful = await UpdateRequestRecord(requestIds.FirstOrDefault(), stage);
                    if (updateSuccessful)
                    {
                        return JsonSuccess();
                    }
                    else
                    {
                        return JsonError("Failed to update the request record. Please try again.");
                    }
                }
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "MoveItemsWithNotes");
                return JsonError(ex);
            }
        }

        private async Task<bool> UpdateRequestRecord(Guid requestId, int stage)
        {
            var requestRecord = await _context.Requests
                .FirstOrDefaultAsync(x => x.RequestId == requestId);

            if (requestRecord == null)
                return false;

            DateTime currentTime = ServerDateTime();

            switch ((ProcessStageIDs)stage)
            {
                case ProcessStageIDs.MQ_Review:
                    requestRecord.DateToVendor = currentTime;
                    break;
                case ProcessStageIDs.MQ_NotReadyToAccept:
                    requestRecord.NotReadyToAccept = currentTime;
                    break;
                case ProcessStageIDs.MQ_AcceptedIncoming:
                    requestRecord.AcceptedDate = currentTime;
                    break;
                case ProcessStageIDs.MQ_AcceptedNotReadyToProcess:
                    requestRecord.NotReadyToProcess = currentTime;
                    break;
                case ProcessStageIDs.MQ_ReadyToProcess:
                    requestRecord.DateReceived = currentTime;
                    break;
                case ProcessStageIDs.MQ_CouldNotProcess:
                    requestRecord.RejectionDate = currentTime;
                    break;
                case ProcessStageIDs.MQ_ProcessedReconcile:
                    requestRecord.DateToDmv = currentTime;
                    break;
                case ProcessStageIDs.MQ_ReturnedTemporarily:
                    requestRecord.ReturnedDate = currentTime;
                    break;
                default:
                    return true;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        private List<Guid> GetGuids(string[] sGuids)
        {
            List<Guid> list = new();

            foreach (string sGuid in sGuids)
            {
                if (Guid.TryParse(sGuid, out Guid guid))
                {
                    list.Add(guid);
                }
                else
                {
                    throw new ApplicationException("Invalid id");
                }
            }
            return list;
        }
        [HttpPost]
        public async Task<IActionResult> ShipItems([FromForm] string[] ids, [FromForm] string courierName, [FromForm] string trackingNumber, [FromForm] string date,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = GetGuids(ids);

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid ship date");
                }
                courierName = courierName?.Trim();
                trackingNumber = trackingNumber?.Trim();

                if (!string.IsNullOrEmpty(courierName) || !string.IsNullOrEmpty(trackingNumber))
                {
                    await DataHelpers.UpdateShippingDetails(userName, dt, courierName, trackingNumber, requestIds);
                }
                else
                {
                    await DataHelpers.MarkAsShipped(userName, dt, requestIds, code, note, remark, chat);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "ShipItems");
                return JsonError(ex);
            }
            return JsonSuccess();
        }
        [HttpPost]
        public async Task<IActionResult> DmvShipItems([FromForm] string[] ids,
            [FromForm] string courierName,
            [FromForm] string trackingNumber,
            [FromForm] string toDmvTrackingNumber,
            [FromForm] string date,
            [FromForm] string eta)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid ship date");
                }
                courierName = (courierName ?? "").Trim();
                trackingNumber = (trackingNumber ?? "").Trim();
                toDmvTrackingNumber = (toDmvTrackingNumber ?? "").Trim();

                await DataHelpers.UpdateDmvShippingDetails(userName, dt, courierName, trackingNumber, toDmvTrackingNumber, eta, requestIds);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "DmvShipItems");
                return JsonError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveFromLH([FromForm] string[] ids, [FromForm] string date,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return JsonSuccess();
                }

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid receive date");
                }
                await DataHelpers.MarkAsReceivedFromLH(userName, dt, requestIds, code, note, remark, chat);

                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "ReceiveFromLH");
                return JsonError(ex);
            }
        }
        [HttpPost]
        public async Task<IActionResult> ReceiveFromDmv([FromForm] string[] ids, [FromForm] string date,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();
                if (!ui.IsVendorAgent)
                    return new UnauthorizedResult();

                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid receive date");
                }
                await DataHelpers.MarkAsReceivedFromDmv(userName, dt, requestIds, code, note, remark, chat);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "ReceiveFromDmv");
                return JsonError(ex);
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> TitleIssued([FromForm] string dateTitleIssued, [FromForm] string inTransit, [FromForm] string[] ids,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }
                if (!DateTime.TryParse(dateTitleIssued, out DateTime dt))
                {
                    // TODO: How to return info about bad date
                    return JsonError($"Invalid date: {dateTitleIssued}");
                }
                string userName = GetUserSID();
                if ((inTransit ?? "false") == "true")
                {
                    await DataHelpers.MarkAsTitleIssuedInTransit(userName, dt, requestIds, code, note, remark, chat);
                }
                else
                {
                    await DataHelpers.MarkAsTitleIssued(userName, dt, requestIds, code, note, remark, chat);
                }

                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "TitleIssued");
                return JsonError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> WorkingList([FromForm] string dateTitleIssued, [FromForm] string inTransit, [FromForm] string[] ids,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }
               
                string userName = GetUserSID();
               
                await DataHelpers.MoveRequestsToWorkingList(userName, DateTime.Now, requestIds, code, note, remark, chat);
               
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "working list");
                return JsonError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PrintItems([FromForm] string[] ids, [FromForm] string date,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid receive date");
                }
                await DataHelpers.MarkAsPrinted(userName, dt, requestIds, code, note, remark, chat);

                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "PrintItems");
                return JsonError(ex);
            }
        }
        private Microsoft.AspNetCore.Http.IFormCollection GetForm()
        {
            try
            {
                return Request.Form;
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<ActionResult> GetGroupSettings(Guid? groupId, string name)
        {
            UserInfo ui = await GetCurrentUserAsync(true);

            if (ui.IsVendorAgent)
            {
                // TODO: Check vendor id against list of groups they work with
                GroupSettings gs = await _context.GroupSettings
                        .Where(g => g.GroupId == groupId
                                    && g.VendorId == ui.VendorId
                                    && g.SettingsName == name)
                        .FirstOrDefaultAsync();
                string json = "{}";
                if (gs != null && !string.IsNullOrEmpty(gs.JSettings))
                    json = gs.JSettings;
                // This will format consistently and throw an error 
                // instead of returning potentially bad json
                // confirmed 12/26/23 that dynamic does not deserialize
                // correctly and ExpandoObject does
                object o = JsonConvert.DeserializeObject<ExpandoObject>(json);
                return new JsonResult(o);
            }
            return Unauthorized();
        }
        private bool ValidJson(string json)
        {
            try
            {
                JObject person = JObject.Parse(json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<ActionResult> BillingInfo()
        {
            UserInfo ui = await GetCurrentUserAsync(true);
            if (ui.IsVendorAgent)
            {
                List<Groups> groups = await _context.Groups
                                                        .Where(g => g.Active == true)
                                                        .OrderBy(g => g.GroupName)
                                                        .ToListAsync();
                BillingInfoViewModel bi = new()
                {
                    Groups = groups
                };
                AddPageHeader("Billing Info", "");
                return View(bi);
            }
            return Unauthorized();
        }


        // NOTE: GetFeeInfo only referenced by Administration.cshtml
        // which is no longer used, so this is not used
        [HttpPost]
        public async Task<ActionResult> GetFeeInfo(Guid? groupId)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync(true);
                if (ui.IsVendorAgent)
                {
                    // TODO: Check vendor id against list of groups they work with
                    GroupSettings gs = await _context.GroupSettings
                            .Where(g => g.GroupId == groupId
                                    && g.VendorId == ui.VendorId
                                    && g.SettingsName == "Fees")
                            .FirstOrDefaultAsync();
                    string json = "{}";
                    if (gs != null && !string.IsNullOrEmpty(gs.JSettings))
                        json = gs.JSettings;
                    dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(json);
                    return Json(data);
                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                LogError(ex, "GetFeeInfo");
                return JsonError(ex);
            }
        }
        [HttpPost]
        public async Task<ActionResult> SaveFeeInfo(Guid? groupId, string json)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync(true);
                if (ui.IsVendorAgent && groupId != null)
                {
                    // TODO: Check vendor id against list of groups they work with
                    GroupSettings gs = await _context.GroupSettings
                            .Where(g => g.GroupId == groupId
                                && g.VendorId == ui.VendorId
                                && g.SettingsName == "Fees")
                            .FirstOrDefaultAsync();

                    dynamic oldsettings = JsonConvert.DeserializeObject<dynamic>(gs.JSettings);
                    dynamic newsettings = JsonConvert.DeserializeObject<dynamic>(json);

                    if (gs == null)
                    {
                        DateTime now = DateTime.Now;
                        gs = new GroupSettings()
                        {
                            GroupId = groupId.Value,
                            SettingsName = "Fees",
                            DateCreated = now,
                            DateModified = now,
                            VendorId = ui.VendorId.Value,
                            JSettings = "{}"
                        };
                        await _context.GroupSettings.AddAsync(gs);
                    }
                    else
                    {
                        //if (newsettings.Version != oldsettings.Version)
                        //{
                        //    return JsonError("Update conflict");
                        //}
                    }

                    gs.JSettings = JsonConvert.SerializeObject(newsettings);
                    await _context.SaveChangesAsync(ui);
                    return JsonSuccess();
                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                LogError(ex, "SaveFeeInfo");
                return JsonError(ex);
            }
        }

        // Only used by Administration.cshtml which is not used
        [HttpPost]
        public async Task<ActionResult> GetGroupInfo(Guid? groupId)
        {
            UserInfo ui = await GetCurrentUserAsync(true);
            if (ui.IsVendorAgent)
            {
                try
                {
                    GroupSettings gs = null;
                    if (groupId != null)
                    {
                        // TODO: Check vendor id against list of groups they work with
                        gs = await _context.GroupSettings
                            .Where(g => g.GroupId == groupId
                                    && g.VendorId == ui.VendorId
                                    && g.SettingsName == "GroupDetails")
                            .FirstOrDefaultAsync();
                    }
                    gs ??= new GroupSettings();
                    string json = "{}";
                    if (gs != null && !string.IsNullOrEmpty(gs.JSettings))
                        json = gs.JSettings;
                    dynamic data = JsonConvert.DeserializeObject<dynamic>(json);

                    data.GroupId = groupId.Value.ToString("D");
                    data.Version = data.Version ?? 1;
                    return Json(data);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                    throw;
                }
            }
            return Unauthorized();
        }
        /// <summary>
        /// Used by _groupInfo (and subsequently by Administration.cshtml which is not used)
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SaveGroupInfo(Guid? groupId, string json)
        {
            UserInfo ui = await GetCurrentUserAsync(true);
            if (ui.IsVendorAgent && groupId != null)
            {
                try
                {
                    // TODO: Check vendor id against list of groups they work with
                    GroupSettings gs = await _context.GroupSettings
                            .Where(g => g.GroupId == groupId
                                && g.VendorId == ui.VendorId
                                && g.SettingsName == "GroupDetails")
                            .FirstOrDefaultAsync();

                    bool isNew = false;
                    if (gs == null)
                    {
                        DateTime now = DateTime.Now;
                        gs = new GroupSettings()
                        {
                            GroupId = groupId.Value,
                            SettingsName = "GroupDetails",
                            DateCreated = now,
                            DateModified = now,
                            VendorId = ui.VendorId.Value,
                            ModifiedBy = ui.UserId,
                            JSettings = "{ Version: 1 }"
                        };
                        isNew = true;
                        _context.GroupSettings.Add(gs);
                    }
                    else
                    {
                        gs.ModifiedBy = ui.UserId;
                    }
                    dynamic settings = JsonConvert.DeserializeObject<dynamic>(gs.JSettings);
                    dynamic newsettings = JsonConvert.DeserializeObject<dynamic>(json);
                    if (!isNew)
                    {
                        if (settings.Version != newsettings.Version)
                        {
                            return JsonError("Update conflict");
                        }
                        int ver = Convert.ToInt32(newsettings.Version);
                        newsettings.Version = (++ver).ToString();
                    }
                    gs.JSettings = JsonConvert.SerializeObject(newsettings);
                    gs.ModifiedBy = ui.UserId;
                    gs.DateModified = DateTime.UtcNow;

                    await _context.SaveChangesAsync(ui);
                    return JsonSuccess();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                    throw;
                }
            }
            return Unauthorized();
        }
        private static bool PropertyExists(dynamic obj, string name)
        {
            if (obj == null) return false;
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(name);
            if (obj is IDictionary<string, object> dict1)
                return dict1.ContainsKey(name);
            if (obj is IDictionary<string, JToken> dict2)
                return dict2.ContainsKey(name);
            return obj.GetType().GetProperty(name) != null;
        }
        [HttpPost]
        public async Task<ActionResult> GetBillToInfo(Guid? groupId)
        {
            UserInfo ui = await GetCurrentUserAsync(true);
            if (ui.IsVendorAgent)
            {
                try
                {
                    // TODO: Check vendor id against list of groups they work with
                    GroupSettings gs = await _context.GroupSettings
                            .Where(g => g.GroupId == groupId
                                    && g.VendorId == ui.VendorId
                                    && g.SettingsName == "BillToList")
                            .FirstOrDefaultAsync();
                    string json = "{}";
                    if (gs != null && !string.IsNullOrEmpty(gs.JSettings))
                        json = gs.JSettings;
                    // Must be ExpandoObject, as dynamic does not work
                    dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(json);
                    return Json(data);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                    throw;
                }
            }
            return Unauthorized();
        }
        /// <summary>
        /// Used by BillingInfo.cshtml (and unused Administration.cshtml)
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SaveBillToInfo(Guid? groupId, string json)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync(true);
                if (ui.IsVendorAgent && groupId != null)
                {
                    // TODO: Check vendor id against list of groups they work with
                    GroupSettings gs = await _context.GroupSettings
                            .Where(g => g.GroupId == groupId
                                && g.VendorId == ui.VendorId
                                && g.SettingsName == "BillToList")
                            .FirstOrDefaultAsync();

                    if (gs == null)
                    {
                        DateTime now = DateTime.Now;
                        gs = new GroupSettings()
                        {
                            GroupId = groupId.Value,
                            SettingsName = "BillToList",
                            DateCreated = now,
                            DateModified = now,
                            VendorId = ui.VendorId.Value,
                            JSettings = "{}"
                        };
                        _context.GroupSettings.Add(gs);
                    }
                    // Update just the BillableAddresses array
                    // Confirmed 12/26/2023 that DeserializeObject<dynamic> works in this scenario
                    dynamic newsettings = JsonConvert.DeserializeObject<dynamic>(json);
                    dynamic settings = JsonConvert.DeserializeObject<dynamic>(gs.JSettings);
                    if (newsettings.BillingAddresses == null)
                    {
                        settings.BillingAddresses = new object[] { };
                    }
                    else
                    {
                        settings.BillingAddresses = newsettings.BillingAddresses;
                    }
                    gs.JSettings = JsonConvert.SerializeObject(settings);
                    await _context.SaveChangesAsync(ui);
                    return JsonSuccess();
                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                LogError(ex, "SaveBillToInfo");
                return JsonError(ex);
            }
        }
        [HttpGet]
        public async Task<ActionResult> GetApplicationFees(Guid? groupId, string appType, string appState, string lienholderName)
        {
            UserInfo ui = await GetCurrentUserAsync(true);
            if (ui.IsVendorAgent)
            {
                try
                {
                    var fees = await DataHelpers.GetApplicationFees(ui.VendorId, groupId, appType, appState, null, lienholderName);
                    var primaryFee = fees.Where(f => f.SecondaryInvoice == false).FirstOrDefault();
                    var secondaryFee = fees.Where(f => f.SecondaryInvoice == true).FirstOrDefault();

                    var model = new InvoiceBuilderAddItemsModel();
                    if (primaryFee != null)
                    {
                        string otherfeeDesc = "";
                        decimal otherFeeTotal = 0;
                        if (primaryFee.MailingFee.HasValue && primaryFee.MailingFee != Decimal.Zero)
                        {
                            if (otherfeeDesc.Length > 0)
                                otherfeeDesc += ", ";
                            otherfeeDesc += "mailing";
                            otherFeeTotal += primaryFee.MailingFee.Value;
                        }
                        model.Primary = new InvoiceItemFeesModel()
                        {
                            DMVFee = primaryFee.DMVFee,
                            ServiceFee = primaryFee.ServiceFee,
                            OtherFee = otherFeeTotal,
                            OtherFeeDesc = otherfeeDesc
                        };
                    }
                    if (secondaryFee == null && (primaryFee != null && primaryFee.MiscFee1.GetValueOrDefault() != Decimal.Zero))
                    {
                        secondaryFee = new DataHelpers.AppFeeInfo()
                        {
                            DMVFee = 0,
                            ServiceFee = 0,
                            MailingFee = 0,
                            MiscFee1 = 0,
                            MiscDescription = ""
                        };
                    }
                    if (secondaryFee != null)
                    {
                        string otherfeeDesc = "";
                        decimal otherFeeTotal = 0;
                        if (secondaryFee.MailingFee.GetValueOrDefault() != Decimal.Zero)
                        {
                            if (otherfeeDesc.Length > 0)
                                otherfeeDesc += ", ";
                            otherfeeDesc += "mailing";
                            otherFeeTotal += secondaryFee.MailingFee.Value;
                        }
                        if (primaryFee != null && primaryFee.MiscFee1.GetValueOrDefault() != Decimal.Zero)
                        {
                            if (otherfeeDesc.Length > 0)
                                otherfeeDesc += ", ";
                            otherfeeDesc += primaryFee.MiscDescription?.Trim() ?? "";
                            otherFeeTotal += primaryFee.MiscFee1.Value;
                        }
                        model.Secondary = new InvoiceItemFeesModel()
                        {
                            DMVFee = secondaryFee.DMVFee.GetValueOrDefault(),
                            ServiceFee = secondaryFee.ServiceFee.GetValueOrDefault(),
                            OtherFee = otherFeeTotal,
                            OtherFeeDesc = otherfeeDesc
                        };
                    }
                    return PartialView("_invoiceBuilderAddItems", model);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                    throw;
                }
            }
            return Unauthorized();
        }
        [HttpPost]
        public async Task<ActionResult> EditBillingInfo(Guid? groupId)
        {
            UserInfo ui = await GetCurrentUserAsync(true);
            if (ui.IsVendorAgent)
            {
                try
                {
                    // TODO: Check vendor id against list of groups they work with
                    GroupSettings gs = await _context.GroupSettings
                            .Where(g => g.GroupId == groupId
                                    && g.VendorId == ui.VendorId
                                    && g.SettingsName == "BillToList")
                            .FirstOrDefaultAsync();
                    string json = "{}";
                    if (gs != null && !string.IsNullOrEmpty(gs.JSettings))
                        json = gs.JSettings;

                    BillToInfoViewModel bti = new()
                    {
                        GroupId = groupId,
                        VendorId = ui.VendorId,
                    };
                    return PartialView("_billToInfo", bti);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                    throw;
                }
            }
            return Unauthorized();
        }

        [HttpPost]
        public async Task<ActionResult> UpdateGroupSettings(Guid? groupId, string name, string json)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync(true);

                if (ui.IsVendorAgent)
                {
                    try
                    {
                        // TODO: Check vendor id against list of groups they work with
                        GroupSettings gs = await _context.GroupSettings
                                .Where(g => g.GroupId == groupId
                                        && g.VendorId == ui.VendorId
                                        && g.SettingsName == name)
                                .FirstOrDefaultAsync();
                        if (!ValidJson(json))
                        {
                            return BadRequest();
                        }
                        gs.JSettings = json;
                        await _context.SaveChangesAsync(ui);
                        return JsonSuccess();
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "UpdateGroupSettings");
                        return JsonError("Error saving changes");
                    }
                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                LogError(ex, "UpdateGroupSettings");
                return JsonError(ex);
            }
        }
        /// <summary>
        /// GET: /Home/GetData
        /// </summary>
        /// <returns>Return data</returns>
        /// 
        [HttpPost]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<ActionResult> GetFileUploads(bool includeSigned)
        {
            using MaggardDMVContext context = new();
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();

                // Skip number of Rows count  
                var start = Request.Form["start"].FirstOrDefault();

                // Paging Length 10,20  
                var length = Request.Form["length"].FirstOrDefault();

                // Sort Column Name  
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();

                // Sort Column Direction (asc, desc)  
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();

                // Search Value from (Search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10, 20, 50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 10;

                int skip = start != null ? Convert.ToInt32(start) : 0;

                int recordsTotal = 0;

                int viewLastXdays = 7;
                DateTime dtCutoff = DateTime.Today.AddDays(-viewLastXdays); // Cutoff for showing in uploads view
                UserInfo ui = await GetCurrentUserAsync();

                var fileUploads =
                    context.FileUploads
                        .Include(f => f.User)
                            .ThenInclude(u => u.UserGroups)
                        .Where(x => x.AgentUploaded == ui.UserId.Value
                                && x.DateUploaded > dtCutoff)
                        .OrderByDescending(x => x.DateUploaded)
                        .AsNoTracking()
                        .Select(row => new
                        {
                            row.FileUploadId,
                            row.ReferenceNo,
                            row.Filename,
                            row.DateUploaded,
                            UploadedBy = row.User.UserPrincipalName,
                            Lienholder = row.User.UserGroups.FirstOrDefault().Group.GroupName,
                            RequestCount = context.Requests.Count(r => r.FileUploadId == row.FileUploadId)
                        });

                //Sorting  
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    //fileUploads = fileUploads.OrderBy();
                }
                //Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    fileUploads = fileUploads.Where(m => m.Filename == searchValue);
                }

                //total number of rows counts   
                recordsTotal = fileUploads.Count();
                //Paging   
                var data = await fileUploads.Skip(skip).Take(pageSize).ToListAsync();

                //Returning Json Data  
                return Json(new { draw, recordsFiltered = recordsTotal, recordsTotal, data });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
                // Info
                //Console.Write(ex);
                return null;
            }
        }
        private string GetUserGroup(Users user)
        {
            UserGroups groups = _context.UserGroups
                        .Include(x => x.Group)
                        .Where(ug => ug.UserId == user.UserId).SingleOrDefault();
            if (groups != null)
            {
                return groups.Group.GroupName;
            }
            return null;
        }

        public IActionResult SysAdmin()
        {
            if (IsSysAdmin())
            {
                return View("SysAdmin");
            }
            return new ForbidResult();
        }
        private void InitSyncfusion()
        {

        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public static string s_rootFolder { get; set; }

        private Stream HtmlToPdf(string html)
        {
            return PdfHelper7.ConvertHtmlToPdf(html);
        }

        [HttpPost]
        public async Task<IActionResult> LIRequested([FromForm] string[] ids, [FromForm] string date, [FromForm] string tracking, [FromForm] int? checkNumber, [FromForm] bool? autoIncrement,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid finish date");
                }
                await DataHelpers.LIRequested(userName, dt, checkNumber, tracking, requestIds, autoIncrement, code, note, remark, chat);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "LIRequested");
                return JsonError(ex);
            }
        }
        [HttpPost]
        public async Task<IActionResult> LCRequested([FromForm] string[] ids, [FromForm] string date, [FromForm] string tracking, [FromForm] int? checkNumber, [FromForm] bool? autoIncrement)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid date");
                }
                await DataHelpers.LCRequested(userName, dt, checkNumber, tracking, requestIds, autoIncrement);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "LCRequested");
                return JsonError(ex);
            }
        }
        [HttpPost]
        public async Task<IActionResult> LCReceivedFromDmv([FromForm] string[] ids, [FromForm] string date,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid date");
                }
                await DataHelpers.LCReceivedFromDmv(userName, dt, requestIds, code, note, remark, chat);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "LCReceivedFromDmv");
                return JsonError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> LIReceivedFromDmv([FromForm] string[] ids, [FromForm] string date,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid date");
                }
                await DataHelpers.LIReceivedFromDmv(userName, dt, requestIds, code, note, remark, chat);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "LIReceivedFromDmv");
                return JsonError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> LCReceivedFromLH([FromForm] string[] ids, [FromForm] string date,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid date");
                }
                await DataHelpers.LCReceivedFromLH(userName, dt, requestIds, code, note, remark, chat);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "LCReceivedFromLH");
                return JsonError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> LCRejected([FromForm] string[] ids, [FromForm] string date,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                List<Guid> requestIds = new();

                foreach (string sGuid in ids)
                {
                    if (Guid.TryParse(sGuid, out Guid guid))
                    {
                        requestIds.Add(guid);
                    }
                }

                string userName = GetUserSID();
                if (!GetServerDateOrNow(date, out DateTime dt))
                {
                    return GetErrorResult("Invalid date");
                }
                await DataHelpers.LCRejected(userName, dt, requestIds, code, note, remark, chat);
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "LCRejected");
                return JsonError(ex);
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteItems([FromForm] string[] ids)
        {
            try
            {
                if (ids == null || ids.Length != 1)
                    return JsonSuccess();

                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    List<Guid> requestIds = new();

                    foreach (string sGuid in ids)
                    {
                        if (Guid.TryParse(sGuid, out Guid guid))
                        {
                            requestIds.Add(guid);
                        }
                    }

                    if (requestIds.Count >= 1)
                    {
                        List<Requests> requests = await _context.Requests
                                .Where(r => r.VendorId == ui.VendorId)
                                .Where(r => requestIds.Contains(r.RequestId))
                                // Don't set thos already pending or deleted
                                .Where(r => r.StatusId != STATUS_ID_DELETE_PENDING)
                                .ToListAsync();

                        if (requests != null && requests.Count > 0)
                        {
                            requests.ForEach(r => r.StatusId = STATUS_ID_DELETE_PENDING);
                            await _context.SaveChangesAsync(ui);
                        }
                    }
                }
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "DeleteItems");
                return JsonError(ex);
            }
        }
        [HttpPost]
        public async Task<IActionResult> UndeleteItems([FromForm] string[] ids)
        {
            try
            {
                if (ids == null || ids.Length != 1)
                    return JsonSuccess();

                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    List<Guid> requestIds = new();

                    foreach (string sGuid in ids)
                    {
                        if (Guid.TryParse(sGuid, out Guid guid))
                        {
                            requestIds.Add(guid);
                        }
                    }

                    if (requestIds.Count >= 1)
                    {
                        List<Requests> requests = await _context.Requests
                                .Where(r => r.VendorId == ui.VendorId)
                                .Where(r => requestIds.Contains(r.RequestId))
                                // Can only undelete those delete pending
                                .Where(r => r.StatusId == STATUS_ID_DELETE_PENDING)
                                .ToListAsync();

                        if (requests != null && requests.Count > 0)
                        {
                            requests.ForEach(r => r.StatusId = STATUS_ID_HOLD);
                            await _context.SaveChangesAsync(ui);
                        }
                    }
                }
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "UndeleteItems");
                return JsonError(ex);
            }
        }
        public static int DeleteCallCount = 0;
        [HttpPost]
        public async Task<IActionResult> FinalizeDelete([FromForm] string[] ids, [FromForm] string vin)
        {
            try
            {
                DeleteCallCount++;

                System.Diagnostics.Trace.WriteLine($"DeleteCallCount: {DeleteCallCount}");

                if (ids == null || ids.Length != 1)
                    return JsonSuccess();

                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent && ui.IsVendorAdmin)
                {
                    List<Guid> requestIds = new();

                    foreach (string sGuid in ids)
                    {
                        if (Guid.TryParse(sGuid, out Guid guid))
                        {
                            requestIds.Add(guid);
                        }
                    }

                    if (requestIds.Count == 1)
                    {
                        Requests request = await _context.Requests
                                .Where(r => r.VendorId == ui.VendorId)
                                .Where(r => r.RequestId == requestIds[0])
                                .Where(r => r.Vin == vin)
                                .Where(r => r.StatusId == STATUS_ID_DELETE_PENDING)
                                .FirstOrDefaultAsync();

                        if (request != null)
                        {
                            request.StatusId = STATUS_ID_DELETED;
                            await _context.SaveChangesAsync(ui);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"requestIds.Count == {requestIds.Count}");
                    }
                }
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "FinalizeDelete");
                return JsonError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdate([FromForm] string[] ids,
                [FromForm] DateTime? eta, [FromForm] bool? isEtaSet,
                [FromForm] string code,
                [FromForm] string note,
                [FromForm] string remark)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return JsonSuccess();

                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    List<Guid> requestIds = new();

                    foreach (string sGuid in ids)
                    {
                        if (Guid.TryParse(sGuid, out Guid guid))
                        {
                            requestIds.Add(guid);
                        }
                    }

                    if (requestIds.Count >= 1)
                    {
                        List<Requests> requests = await _context.Requests.Include(r => r.RequestNotes)
                                .Where(r => r.VendorId == ui.VendorId)
                                .Where(r => requestIds.Contains(r.RequestId))
                                .ToListAsync();

                        if (requests != null && requests.Count > 0)
                        {
                            bool modified = false;
                            DateTime? dtETA = eta;
                            if (dtETA != null || (isEtaSet.HasValue && isEtaSet.Value))
                            {
                                if (dtETA != null)
                                {
                                    requests.ForEach(r => r.Eta = dtETA);
                                }
                                else
                                {
                                    requests.ForEach(r => r.Eta = null);
                                }
                                modified = true;
                            }
                            if (code != null) // code can be set to blank
                            {
                                requests.ForEach(r => r.Code = code);
                                modified = true;
                            }
                            if (!string.IsNullOrWhiteSpace(note) || !string.IsNullOrWhiteSpace(remark))
                            {
                                foreach (var request in requests)
                                {
                                    var n = new RequestNotes()
                                    {
                                        RequestId = request.RequestId,
                                        ModifiedBy = ui.UserId,
                                        Note = note,
                                        Remark = remark,
                                        LastUpdated = System.DateTime.UtcNow
                                    };
                                    request.RequestNotes.Add(n);
                                    modified = true;
                                }
                            }
                            if (modified)
                            {
                                // anything needed when all 
                                await _context.SaveChangesAsync(ui);
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"requestIds.Count == {requestIds.Count}");
                    }
                }
                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "BulkUpdate");
                return JsonError(ex);
            }
        }
        [HttpGet]
        public async Task<IActionResult> BulkEdit()
        {
            AddPageHeader("Bulk Edit", "");
            var model = new BulkEditViewModel(_context);

            return View(model);
        }
#if DEBUG
        [HttpGet]
        [Route("/Vendor/BulkEdit/Test")]
        public async Task<IActionResult> BulkEditPostTest()
        {
            string queue = "In-Process RT/DT";
            string id = "";
            bool profileOnly = false;

            var ids = new List<Guid>() { };
            ids.Add(new Guid("87bc9f01-13de-41a4-ba50-a227b468ea15"));

            return await InternalBulkEditPost(queue, ids, profileOnly);
        }
#endif
        [HttpPost]
        [ActionName("BulkEdit")]
        [Route("/Vendor/BulkEdit")]
        public async Task<IActionResult> BulkEditPost([FromForm] string queue, [FromForm] List<Guid> ids, [FromForm] bool profileOnly)
        {
            return await InternalBulkEditPost(queue, ids, profileOnly);
        }

        private async Task<IActionResult> InternalBulkEditPost([FromForm] string queue, [FromForm] List<Guid> ids, [FromForm] bool profileOnly)
        {
            AddPageHeader($"Bulk Edit", "");
            UserInfo ui = await GetCurrentUserAsync();

            var result = await DataHelpers.UniversalRequestBulkEdit(ui, ids, profileOnly);
            var firstItem = result.ListData[0];
            string appType = firstItem["AppType"];
            string appState = firstItem["AppTypeState"];

            var internalAttachments = await GetInternalAttachmentTypesForAppTypeState(ui, appType, appState);
            foreach (var attachment in internalAttachments)
            {
                var dict = new Dictionary<string, object>
                    {
                        { "data", attachment.ExcelName },
                        { "title", attachment.Name },
                        { "defaultContent", "" },
                        { "className", "editable" }
                    };
                if (attachment.InternalFromClient)
                {
                    dict.Add("render", new OptionsFunction($"function (data, type, row, meta) {{ return datatable_render_groupAttachment(data, type, row, meta, '{attachment.AttachmentTypeId}'); }}"));
                }
                else
                {
                    dict.Add("render", new OptionsFunction($"function (data, type, row, meta) {{ return datatable_render_vendorAttachment(data, type, row, meta, '{attachment.AttachmentTypeId}'); }}"));
                }
                result.ListColumns.Add(dict);
                var selections = await GetInternalAttachments(ui, attachment.AttachmentTypeId, attachment.InternalFromClient);
                AddEditorFieldForAttachment(ui.VendorId, result.EditorFields, attachment, selections);
            }
            var model = new BulkEditViewModel(_context)
            {
                QueueName = queue,
                ListId = "UniversalBulkEdit",
                ListData = result.ListData,
                ListColumns = result.ListColumns,
                EditorFields = result.EditorFields
            };
            // Add internal attachments
            //model.AddInternalAttachmentColumns(model.ListColumns);
            return View("BulkEdit", model);
        }
        [HttpGet]
        [Route("Vendor/BulkEditProfiles/{appType}/{appState}")]
        public async Task<IActionResult> GetBulkEditProfiles(string appType, string appState)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    var result = await DataHelpers.GetBulkEditProfiles(ui.VendorId, appType, appState);
                    try
                    {
                        // deserialize json into BulkEditProfile object
                        var profiles = JsonConvert.DeserializeObject<List<BulkEditProfile>>(result);
                        return JsonSuccess(profiles);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    return JsonSuccess(result);
                }
                return JsonError("Unauthorized");
            }
            catch (Exception ex)
            {
                LogError(ex, "GetBulkEditProfiles");
                return JsonError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetBulkEditData([FromForm] List<Guid> ids)
        {
            UserInfo ui = await GetCurrentUserAsync();

            var result = await DataHelpers.UniversalRequestBulkEdit(ui, ids, false);

            return new OkObjectResult(result);
        }

        [HttpGet]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> GetFormFields(string appType)
        {
            try
            {
                List<string> fields = new();

                //UserInfo ui = await GetCurrentUserAsync();
                //var apptype = _context.MdpAppTypes.Where(a => a.AppType == appType).FirstOrDefault();

                //var sections = _context.MdpAppSections.Where(s => s.AppTypeId == apptype.AppTypeId).ToList();


                //// Remove Vendor only fields
                //foreach (var section in appForm.AppFormSections)
                //{
                //    if (section.AppFormSectionFields != null)
                //    {
                //        foreach (var afsf in section.AppFormSectionFields)
                //        {
                //            fields.Add(afsf.Field.ExcelName);
                //        }
                //    }
                //}
                return new OkObjectResult(fields);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
            return null;
        }

        [HttpPost]
        public async Task<IActionResult> BulkEditSave()
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();
                string jsonAfter = Request.Form["after"];
                string jsonBefore = Request.Form["before"];
                string jsonUpdate;

                var afterList = JsonArrayToDictionaryList(jsonAfter, "RequestId");
                var beforeList = JsonArrayToDictionaryList(jsonBefore, "RequestId");

                List<Dictionary<string, object>> updateList = new();
                foreach (var before in beforeList)
                {
                    var after = afterList.Where(a => a["RequestId"].ToString() == before["RequestId"].ToString()).FirstOrDefault();
                    if (after != null)
                    {
                        var diffs = CompareDictionaries(before, after, "RequestId");
                        if (diffs.Count > 1) // min 1 diff for RequestId
                        {
                            updateList.Add(diffs);
                        }
                    }
                }

                if (updateList.Count > 0)
                {
                    await DataHelpers.UniversalExportBulkSave(ui, JsonConvert.SerializeObject(updateList), null);
                }

                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "BulkEditSave");
                return JsonError(ex);
            }
        }
        [HttpPost]
        public async Task<IActionResult> BulkEditProfileSave(Guid? groupId)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();
                string jsonAfter = Request.Form["after"];
                string jsonBefore = Request.Form["before"];
                string jsonUpdate;

                var afterList = JsonArrayToDictionaryList(jsonAfter, "groupProfileID");
                var beforeList = JsonArrayToDictionaryList(jsonBefore, "groupProfileID");

                List<Dictionary<string, object>> updateList = new();
                foreach (var before in beforeList)
                {
                    var after = afterList.Where(a => a["groupProfileID"].ToString() == before["groupProfileID"].ToString()).FirstOrDefault();
                    if (after != null)
                    {
                        var diffs = CompareDictionaries(before, after, "groupProfileID");
                        if (diffs.Count > 1) // min 1 diff for RequestId
                        {
                            updateList.Add(diffs);
                        }
                    }
                    else
                    {
                        // what scenario would this be?
                    }
                }
                // Add new items to list
                foreach (var after in afterList)
                {
                    var before = beforeList.Where(b => b["groupProfileID"].ToString() == after["groupProfileID"].ToString()).FirstOrDefault();
                    if (before != null)
                    {
                        // already processed
                    }
                    else
                    {
                        // new item
                        string gpid = after["groupProfileID"].ToString();
                        if (!Guid.TryParse(gpid, out Guid guid))
                        {
                            after["groupProfileID"] = null;
                        }
                        updateList.Add(after);
                    }
                }

                if (updateList.Count > 0)
                {
                    await DataHelpers.UniversalExportBulkProfileSave(ui, groupId, JsonConvert.SerializeObject(updateList));
                }

                return JsonSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex, "BulkEditProfileSave");
                return JsonError(ex);
            }
        }
        // enumerate json array and return a list of dictionaries
        private List<Dictionary<string, object>> JsonArrayToDictionaryList(string json, params string[] requiredFields)
        {
            List<Dictionary<string, object>> result = new();

            if (!string.IsNullOrWhiteSpace(json))
            {
                var jArray = JArray.Parse(json);
                foreach (JObject jObject in jArray)
                {
                    // The following just verifies the RequestId is present
                    // and isn't really needed, as we can do this check later
                    foreach (var field in requiredFields)
                    {
                        if (!jObject.ContainsKey(field))
                        {
                            throw new Exception($"Required field {field} not found in json array");
                        }
                    }
                    //_ = jObject["RequestId"].Value<string>();

                    Dictionary<string, object> dict = new();
                    foreach (var item in jObject)
                    {
                        dict.Add(item.Key, item.Value.Value<string>());
                    }
                    result.Add(dict);
                }
            }
            return result;
        }

        // compare two dictionaries and return a dictionary of changes
        private Dictionary<string, object> CompareDictionaries(Dictionary<string, object> before, Dictionary<string, object> after, params string[] requiredFields)
        {
            Dictionary<string, object> result = new();
            foreach (var field in requiredFields)
            {
                if (!after.ContainsKey(field))
                {
                    throw new Exception($"Required field {field} not found in after dictionary");
                }
                if (!before.ContainsKey(field))
                {
                    throw new Exception($"Required field {field} not found in before dictionary");
                }
                result[field] = before[field];
            }

            foreach (var key in after.Keys)
            {
                string beforeVal = null;

                if (before.ContainsKey(key))
                {
                    beforeVal = before[key]?.ToString();
                }
                //beforeVal ??= "";

                var afterVal = after[key]?.ToString(); // ?? "";
                if (beforeVal != afterVal)
                {
                    result.Add(key, afterVal);
                }
            }
            return result;
        }
#if DEBUG
        public bool ThrowException = false;
#endif
        [HttpPost]
        public async Task<IActionResult> NotReadyForProcessing([FromForm] string[] ids,
            [FromForm] string note, [FromForm] string remark, [FromForm] string code, [FromForm] string chat)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    List<Guid> requestIds = GetGuids(ids);
                    try
                    {
#if DEBUG
                        if (ThrowException == true)
                            throw new Exception("Test exception");
#endif
                        await DataHelpers.MarkAsNotReadyForProcessing(ui.UserId.Value, note: note, remark: remark, code: code, chat: chat, ids: requestIds);
                        return JsonSuccess();
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "NotReadyForProcessing");
                        return JsonError(ex);
                    }
                }
                return JsonError("Unauthorized");
            }
            catch (Exception ex)
            {
                LogError(ex, "NotReadyForProcessing");
                return JsonError(ex);
            }
        }
        public async Task<IActionResult> GroupAppFees()
        {
            UserInfo userInfo = await GetCurrentUserAsync(true);
            if (userInfo.IsVendorAgent)
            {
                // TBD: Check features
                //var features = await _context.VendorFeatures.Where(vf => vf.VendorId == userInfo.VendorId).ToListAsync();

                List<Groups> groups = await _context.Groups.Where(g => g.Active).OrderBy(g => g.GroupName).ToListAsync();

                var viewModel = new GroupAppFeesViewModel(groups)
                {
                    ListId = "GroupAppFees",
                    ListData = new(),
                    ListColumns = new(),
                    EditorFields = new()
                };
                AddPageHeader("Manage App Fees", "");
                return View(viewModel);
            }
            return Unauthorized();
        }

        [Authorize(Policy = "ActiveUser")]
        public async Task<IActionResult> GroupProfile()
        {
            UserInfo userInfo = await GetCurrentUserAsync(true);
            if (userInfo == null)
            {
                return Unauthorized();
            }

            List<Groups> groups = null;
            if (userInfo.IsVendorAgent)
            {
                groups = await _context.Groups.Where(g => g.Active).OrderBy(g => g.GroupName).ToListAsync();
            }
            else
            {
                groups = await _context.Groups.Where(g => g.Active && g.GroupId == userInfo.GroupId).ToListAsync();
            }
            var result = await DataHelpers.UniversalProfileBulkEdit(userInfo, true, "Lienholder");
            // Append internal attachment columns
            var internalAttachments = await GetInternalAttachmentTypes(userInfo, true);
            foreach (var attachment in internalAttachments)
            {
                var dict = new Dictionary<string, object>
                {
                    { "data", attachment.ExcelName },
                    { "title", attachment.Name },
                    { "defaultContent", "" },
                    { "className", "editable" }
                };
                if (attachment.InternalFromClient)
                {
                    dict.Add("render", $"function (data, type, row, meta) {{ return datatable_render_groupAttachment(data, type, row, meta, '{attachment.AttachmentTypeId}'); }}");
                }
                else
                {
                    dict.Add("render", $"function (data, type, row, meta) {{ return datatable_render_vendorAttachment(data, type, row, meta, '{attachment.AttachmentTypeId}'); }}");
                }
                result.ListColumns.Add(dict);
                var selections = await GetInternalAttachments(userInfo, attachment.AttachmentTypeId, attachment.InternalFromClient);
                AddEditorFieldForAttachment(userInfo.VendorId, result.EditorFields, attachment, selections);
            }
            GroupProfileDataViewModel viewModel = new GroupProfileDataViewModel(groups)
            {
                ListId = "GroupProfile",
                ListData = result.ListData, // Should always be empty here, as we are just getting the column names
                ListColumns = result.ListColumns,
                EditorFields = result.EditorFields
            };
            AddPageHeader("Manage Bulk Profile Data", "");
            return View(viewModel);
        }

        public async Task<IActionResult> GroupProfileData()
        {
            UserInfo userInfo = await GetCurrentUserAsync(true);
            if (userInfo.IsVendorAgent)
            {
                List<Groups> groups = await _context.Groups.Where(g => g.Active).OrderBy(g => g.GroupName).ToListAsync();
                var result = await DataHelpers.UniversalProfileBulkEdit(userInfo);
                // Append internal attachment columns
                var internalAttachments = await GetInternalAttachmentTypes(userInfo, true);
                foreach (var attachment in internalAttachments)
                {
                    var dict = new Dictionary<string, object>
                    {
                        { "data", attachment.ExcelName },
                        { "title", attachment.Name },
                        { "defaultContent", "" },
                        { "className", "editable" }
                    };
                    if (attachment.InternalFromClient)
                    {
                        dict.Add("render", new OptionsFunction($"function (data, type, row, meta) {{ return datatable_render_groupAttachment(data, type, row, meta, '{attachment.AttachmentTypeId}'); }}"));
                    }
                    else
                    {
                        dict.Add("render", new OptionsFunction($"function (data, type, row, meta) {{ return datatable_render_vendorAttachment(data, type, row, meta, '{attachment.AttachmentTypeId}'); }}"));
                    }
                    result.ListColumns.Add(dict);
                    var selections = await GetInternalAttachments(userInfo, attachment.AttachmentTypeId, attachment.InternalFromClient);
                    AddEditorFieldForAttachment(userInfo.VendorId, result.EditorFields, attachment, selections);
                }
                GroupProfileDataViewModel viewModel = new GroupProfileDataViewModel(groups)
                {
                    ListId = "UniversalBulkEdit",
                    ListData = result.ListData, // Should always be empty here, as we are just getting the column names
                    ListColumns = result.ListColumns,
                    EditorFields = result.EditorFields
                };
                AddPageHeader("Manage Bulk Profile Data", "");
                return View(viewModel);
            }
            return Unauthorized();
        }

        public async Task<IActionResult> GroupProfiles()
        {
            UserInfo userInfo = await GetCurrentUserAsync(true);
            if (userInfo.IsVendorAgent)
            {
                List<Groups> groups = await _context.Groups.Where(g => g.Active).OrderBy(g => g.GroupName).ToListAsync();
                var result = await DataHelpers.UniversalGroupProfiles(userInfo);
                // Append internal attachment columns
                var internalAttachments = await GetInternalAttachmentTypes(userInfo, true);
                foreach (var attachment in internalAttachments)
                {
                    var dict = new Dictionary<string, object>
                    {
                        { "data", new OptionsFunction($"getfn_datatable_data_dictionary_handler('fields', '{attachment.ExcelName}')") },
                        { "title", attachment.Name },
                        { "defaultContent", "" },
                        { "className", "editable" }
                    };
                    if (attachment.InternalFromClient)
                    {
                        dict.Add("render", new OptionsFunction($"function (data, type, row, meta) {{ return datatable_render_groupAttachment(data, type, row, meta, '{attachment.AttachmentTypeId}'); }}"));
                    }
                    else
                    {
                        dict.Add("render", new OptionsFunction($"function (data, type, row, meta) {{ return datatable_render_vendorAttachment(data, type, row, meta, '{attachment.AttachmentTypeId}'); }}"));
                    }
                    foreach (var profileCategory in result.Categories)
                    {
                        profileCategory.ListColumns.Add(dict);
                        var selections = await GetInternalAttachments(userInfo, attachment.AttachmentTypeId, attachment.InternalFromClient);
                        AddEditorFieldForAttachment(userInfo.VendorId, profileCategory.EditorFields, attachment, selections);
                    }
                }
                MultiGroupProfileViewModel viewModel = new MultiGroupProfileViewModel(groups)
                {
                    GroupProfileViewModels = new()
                };
                var profile = result.GetProfile("Lienholder");

                GroupProfileViewModel viewModel_LH = new()
                {
                    Title = "Lienholder",
                    ProfileCategoryName = "Lienholder",
                    GroupProfileCategoryId = profile.GroupProfileCategoryID?.ToString(),
                    Description = "LH will be used for RT and DT-LH apps, Inquiries, etc.",
                    ListId = "UniversalBulkEdit_LH",
                    ProfileColumns = new(profile.Columns),
                    ListColumns = new(profile.ListColumns),
                    EditorFields = new(profile.EditorFields)
                };
                profile = result.GetProfile("Owner");
                GroupProfileViewModel viewModel_Owner = new()
                {
                    Title = "Lease Company",
                    ProfileCategoryName = "Owner",
                    GroupProfileCategoryId = profile.GroupProfileCategoryID?.ToString(),
                    Description = "Company as owner will be used for DT apps, inquiries, etc. ",
                    ListId = "UniversalBulkEdit_LeaseCompany",
                    ProfileColumns = new(profile.Columns),
                    ListColumns = new(profile.ListColumns),
                    EditorFields = new(profile.EditorFields)
                };
                viewModel.GroupProfileViewModels.Add(viewModel_LH);
                viewModel.GroupProfileViewModels.Add(viewModel_Owner);

                AddPageHeader("Manage Bulk Profile Data", "");
                return View(viewModel);
            }

            return Unauthorized();
        }

        private static string MakeHtmlId(string name)
        {
            return name.Replace(" ", "_");
        }

        public void AddEditorFieldForAttachment(Guid? vendorId, List<Dictionary<string, object>> editorFields, MdpAttachmentTypes attachmentType, List<InternalAttachmentUpload> selections)
        {
            var editorField = new Dictionary<string, object>()
                {
                    { "label", attachmentType.Name },
                    { "name", attachmentType.ExcelName },
                    { "data", new OptionsFunction($"getfn_datatable_data_dictionary_handler('fields', '{attachmentType.ExcelName}')") },
                    { "id", MakeHtmlId(attachmentType.ExcelName) }
                };
            if (attachmentType.InternalFromVendor)
            {
                editorField.Add("type", "select");
                editorField.Add("options", new OptionsFunction($"vendor_getVendorDocuments('{attachmentType.ExcelName}','{vendorId}','{attachmentType.AttachmentTypeId}')"));
                editorField.Add("placeholder", "");
                editorField.Add("className", "editable internalVendorDoc");
            }
            else
            {
                editorField.Add("type", "select");
                editorField.Add("options", new OptionsFunction($"vendor_getGroupDocuments('{attachmentType.ExcelName}','{vendorId}','{attachmentType.AttachmentTypeId}', true)"));
                editorField.Add("placeholder", "");
                editorField.Add("className", "editable internalGroupDoc");
            }
            editorFields.Add(editorField);
        }
        /*
         * Example options layout:
            options: [
                { label: '1 (highest)', value: '1' },
                { label: '2', value: '2' },
                { label: '3', value: '3' },
                { label: '4', value: '4' },
                { label: '5 (lowest)', value: '5' }
            ]
         */
        public class SelectOption
        {
            public string Label { get; set; }
            public string Value { get; set; }
        }
        public List<Dictionary<string, object>> GetSelectOptions(List<InternalAttachmentUpload> values)
        {
            List<Dictionary<string, object>> list = new();
            //list.Add(new Dictionary<string, object> { { "label", "Select value" }, { "value", "" } });
            foreach (var value in values)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                dict.Add("label", value.DisplayName);
                dict.Add("value", value.AttachmentId);
                list.Add(dict);
            }
            return list;
        }
        public async Task<List<MdpAttachmentTypes>> GetInternalAttachmentTypes(UserInfo user, bool groupDocumentsOnly)
        {
            if (user.IsVendorAgent)
            {
                var result = await _context.MdpAttachmentTypes.Where(a => (!groupDocumentsOnly && a.InternalFromVendor) || a.InternalFromClient).ToListAsync();
                return result;
            }
            else if (user.IsGroupMember)
            {
                var result = await _context.MdpAttachmentTypes.Where(a => a.InternalFromClient).ToListAsync();
                return result;
            }
            return new();
        }
        public async Task<List<MdpAttachmentTypes>> GetInternalAttachmentTypesForAppTypeState(UserInfo user, string appType, string appState)
        {
            var result = await _context.MdpAppTypeAttachmentTypes
                .Include(a => a.AttachmentType)
                .Include(a => a.AppTypeState)
                .Where(a => (user.IsVendorAgent && a.AttachmentType.InternalFromVendor)
                        || a.AttachmentType.InternalFromClient)
                .Where(a => a.AppTypeState.AppType.AppType == appType && a.AppTypeState.AppState == appState)
                .Select(a => new MdpAttachmentTypes()
                {
                    AttachmentTypeId = a.AttachmentTypeId,
                    Name = a.AttachmentType.Name,
                    Description = a.AttachmentType.Description,
                    ExcelName = a.AttachmentType.ExcelName,
                    InternalFromVendor = a.AttachmentType.InternalFromVendor,
                    InternalFromClient = a.AttachmentType.InternalFromClient
                })
                .ToListAsync();
            return result;
        }
        public class InternalAttachmentUpload
        {
            public string DisplayName { get; set; }
            // Using string here to allow blank value used in <select> tag
            public Guid? AttachmentId { get; set; }
            public Guid? AttachmentTypeId { get; set; }
            public Guid? GroupId { get; set; }
            public string ExcelName { get; set; }
        }
        [HttpPost]
        public async Task<List<InternalAttachmentUpload>> GetInternalAttachments(Guid vendorId, Guid? attachmentTypeId, Guid? groupId, bool isClientDocument)
        {
            if (isClientDocument)
            {
                var result = await _context.GroupAttachments
                                                .Include(a => a.AttachmentType)
                                                .Where(a => ((attachmentTypeId == null) || (a.AttachmentTypeId == attachmentTypeId))
                                                            && (a.VendorId == vendorId)
                                                            && (groupId == null || a.GroupId == groupId))
                                                .Select(a => new InternalAttachmentUpload()
                                                {
                                                    DisplayName = a.DisplayName,
                                                    AttachmentId = a.GroupAttachmentId,
                                                    AttachmentTypeId = a.AttachmentTypeId,
                                                    GroupId = a.GroupId,
                                                    ExcelName = a.AttachmentType.ExcelName
                                                })
                                                .ToListAsync();
                return result;
            }
            else
            {
                var result = await _context.VendorAttachments
                                                .Include(a => a.AttachmentType)
                                                .Where(a => attachmentTypeId == null || a.AttachmentTypeId == attachmentTypeId)
                                                .Select(a => new InternalAttachmentUpload()
                                                {
                                                    DisplayName = a.DisplayName,
                                                    AttachmentId = a.VendorAttachmentId,
                                                    AttachmentTypeId = a.AttachmentTypeId,
                                                    GroupId = null,
                                                    ExcelName = a.AttachmentType.ExcelName
                                                })
                                                .ToListAsync();
                return result;
            }
        }

        public async Task<List<InternalAttachmentUpload>> GetInternalAttachments(UserInfo user, Guid attachmentTypeId, bool isClientDocument)
        {
            if (isClientDocument)
            {
                var result = await _context.GroupAttachments
                                                .Where(a => a.AttachmentTypeId == attachmentTypeId && a.VendorId == user.VendorId)
                                                .Select(a => new InternalAttachmentUpload()
                                                {
                                                    DisplayName = a.DisplayName,
                                                    AttachmentId = a.GroupAttachmentId,
                                                    AttachmentTypeId = a.AttachmentTypeId
                                                })
                                                .ToListAsync();
                return result;
            }
            else
            {
                var result = await _context.VendorAttachments
                                                .Where(a => a.AttachmentTypeId == attachmentTypeId)
                                                .Select(a => new InternalAttachmentUpload()
                                                {
                                                    DisplayName = a.DisplayName,
                                                    AttachmentId = a.VendorAttachmentId,
                                                    AttachmentTypeId = a.AttachmentTypeId
                                                })
                                                .ToListAsync();
                return result;
            }
        }
        public async Task<IActionResult> GetProfilesFromGroupId(Guid groupId, string groupProfileCategory)
        {

            List<GroupProfile> profiles = new();
            var query = _context.GroupProfiles.Where(p => p.GroupId == groupId);
            if (!string.IsNullOrWhiteSpace(groupProfileCategory))
            {
                query = query.Where(p => p.GroupProfileCategory.GroupProfileCategoryName == groupProfileCategory);
            }
            profiles = await query.ToListAsync();

            return new JsonResult(profiles);
        }
        public async Task<IActionResult> GetProfilesForAppType(Guid groupId, string appType, string appTypeState, bool includeInactive)
        {
            // The number of profiles per group will be small, so we can just get them all
            // and filter them in memory
            var profileQuery = _context.GroupProfiles
                                            .Include(p => p.GroupProfileType)
                                            .ThenInclude(pt => pt.GroupProfileTypeUsages)
                                            .Where(p => p.GroupId == groupId);
            // TBD: activate once ui able to activate/deactivate
            //if (!includeInactive)
            //{
            //    profileQuery = profileQuery.Where(p => p.Active);
            //}
            var profiles = await profileQuery.AsNoTrackingWithIdentityResolution()
                                                .OrderBy(p => p.GroupProfileName)
                                                .ToListAsync();
            // Conditions
            // 1) GroupProfileType is null
            // 2) GroupProfileTypeUsages is null
            // 3) GroupProfileTypeUsages is not null and Include = true
            // 4) GroupProfileTypeUsages is not null and Exclude = true
            List<GroupProfile> filteredProfiles = new();
            foreach (var profile in profiles)
            {
                if (profile.GroupProfileType == null)
                {
                    filteredProfiles.Add(profile);
                    continue;
                }
                if (profile.GroupProfileType.GroupProfileTypeUsages == null
                    || profile.GroupProfileType.GroupProfileTypeUsages.Count == 0)
                {
                    // no usages, so include
                    filteredProfiles.Add(profile);
                    continue;
                }
                if (profile.GroupProfileType.GroupProfileTypeUsages.Any(ptu => ptu.AppType == appType && ptu.AppTypeState == null && ptu.Include))
                {
                    filteredProfiles.Add(profile);
                    continue;
                }
                if (profile.GroupProfileType.GroupProfileTypeUsages.Any(ptu => ptu.AppType == appType && ptu.AppTypeState == appTypeState && ptu.Include))
                {
                    filteredProfiles.Add(profile);
                    continue;
                }
                if (profile.GroupProfileType.GroupProfileTypeUsages.Any(ptu => ptu.AppType == appType && ptu.AppTypeState == appTypeState && ptu.Exclude))
                {
                    // specifically excluded, so do not include
                    continue;
                }
                System.Diagnostics.Trace.WriteLine($"Profile filtered from list: {profile.GroupProfileName}");
            }
            foreach (var profile in filteredProfiles)
            {
                if (profile.GroupProfileType != null)
                {
                    profile.GroupProfileType.GroupProfiles = null;
                    profile.GroupProfileType.GroupProfileTypeUsages = null;
                }
            }
            return new JsonResult(filteredProfiles);
        }
        public async Task<IActionResult> GetProfileTypesForGroupId(Guid? groupId)
        {
            // NULL groupId means return all types
            List<GroupProfileType> types = await _context.GroupProfileType.AsNoTracking().ToListAsync();
            foreach (var type in types)
            {
                type.GroupProfiles = null;
                type.GroupProfileTypeUsages = null;
            }
            return new JsonResult(types);
        }

        [HttpPost]
        public async Task<IActionResult> GetGroupProfileData([FromForm] Guid? groupId, [FromForm] Guid? groupProfileId, [FromForm] string categoryName)
        {
            try
            {
                if (groupId is null)
                {
                    return new JsonResult(new List<GroupProfile>());
                }
                var query = _context.GroupProfiles
                                            .Where(p => p.GroupId == groupId && (groupProfileId == null || groupProfileId == p.GroupProfileID))
                                            .Where(p => string.IsNullOrWhiteSpace(categoryName) || p.GroupProfileCategory.GroupProfileCategoryName == categoryName)
                                            .Select(p => new { p.GroupProfileID, p.GroupId, p.GroupProfileTypeID, p.GroupProfileName, p.GroupProfileType.GroupProfileTypeName, p.JRequest });

                var profiles = await query.ToListAsync();
                return new JsonResult(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProfileData error");
                return new JsonResult(new List<object>() { });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetProfileData([FromForm] Guid? groupId, [FromForm] Guid? groupProfileId)
        {
            if (groupId is null)
            {
                return new JsonResult(new List<GroupProfile>());
            }
            var profiles = await _context.GroupProfiles
                                        .Where(p => p.GroupId == groupId && (groupProfileId == null || groupProfileId == p.GroupProfileID))
                                        .Select(p => new { p.GroupProfileID, p.GroupId, p.GroupProfileTypeID, p.GroupProfileName, p.GroupProfileType.GroupProfileTypeName, p.JRequest })
                                        .ToListAsync();
            return new JsonResult(profiles);
        }

        [HttpPost]
        public async Task<IActionResult> SaveProfileData([FromForm] Guid? groupId, [FromForm] Guid? groupProfileId, [FromForm] string categoryName, [FromForm] string jRequest)
        {
            if (groupId is null)
            {
                return BadRequest();
            }
            var profiles = await _context.GroupProfiles.Where(p => p.GroupId == groupId && (groupProfileId == null || groupProfileId == p.GroupProfileID)).ToListAsync();
            return new JsonResult(profiles);
        }
        [HttpPost]
        public async Task<IActionResult> RollbackRequest([FromForm] List<Guid> ids, [FromForm] string note, [FromForm] string remark, [FromForm] string code)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();
                if (ui.IsVendorAgent)
                {
                    await DataHelpers.RollbackRequests(ui.UserId.Value, note: note, remark: remark, code: code, ids: ids);

                    return JsonSuccess();
                }
                return JsonError("Unauthorized");
            }
            catch (Exception ex)
            {
                LogError(ex, "RollbackRequest");
                return JsonError(ex);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetVendorAttachmentNames()
        {
            UserInfo user = await GetCurrentUserAsync();

            var result = await _context.VendorAttachments
                            .Include(a => a.AttachmentType)
                            .Where(va => va.VendorId == user.VendorId)
                            .Select(va => new
                            {
                                attachmentName = va.DisplayName,
                                attachmentTypeId = va.AttachmentTypeId,
                                attachmentId = va.VendorAttachmentId,
                                excelName = va.AttachmentType.ExcelName,
                                vendorId = va.VendorId
                            })
                            .ToListAsync();
            return JsonSuccess(result);
        }
        [HttpGet]
        [Route("Vendor/GetGroupAttachmentNames")]
        [Route("Vendor/GetGroupAttachmentNames/{groupId}")]
        public async Task<IActionResult> GetGroupAttachmentNames(Guid? groupId)
        {
            UserInfo user = await GetCurrentUserAsync();

            var result = await _context.GroupAttachments
                            .Include(a => a.AttachmentType)
                            .Where(a => a.VendorId == user.VendorId)
                            .Where(a => groupId == null || a.GroupId == groupId.Value)
                            .Select(a => new
                            {
                                attachmentName = a.DisplayName,
                                attachmentTypeId = a.AttachmentTypeId,
                                attachmentId = a.GroupAttachmentId,
                                excelName = a.AttachmentType.ExcelName,
                                groupId = a.GroupId
                            })
                            .ToListAsync();
            return JsonSuccess(result);
        }
        public async Task<IActionResult> ManualQueue()
        {
            AddPageHeader("Manual Queue", "");
            return View();
        }
        public async Task<IActionResult> Audits()
        {
            AddPageHeader("Audits", "");
            return View();
        }

        [HttpGet]
        [Route("Vendor/AuditLookup/{name}/{id}")]
        public async Task<IActionResult> AuditLookup(string id, string name)
        {
            string updatedName = name != "ETA" ? char.ToUpper(name[0]) + name.Substring(1).ToLower() : name;
            string pageTitle = $"Completed {updatedName} Audit Data";
            AddPageHeader(pageTitle, "");
            var auditData = await GetAuditDataByBatchId(id);
            List<string> columnsToDisplay = GlobalHelper.GetColumnsToShow(name);
            ViewData["ColumnsToShow"] = columnsToDisplay;
            return View(auditData);
        }
        private async Task<List<AuditCompleteResponse>> GetAuditDataByBatchId(string id)
        {
            var auditData = await _context.AuditCompleteResponse
                .Where(a => a.AuditBatchId == id)
                .ToListAsync();

            return auditData;
        }

        [HttpGet]
        [Route("Vendor/GetBatchId/{id}")]
        public async Task<IActionResult> GetBatchId(string id)
        {
            var batchData = await _context.AuditBatch
                .Where(a => a.AuditBatchId == id)
                .ToListAsync();

            return Ok(batchData);
        }

        [HttpGet]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/GetAssignedUsersDataForCombobox")]
        public async Task<IActionResult> GetAssignedUsersDataForCombobox()
        {
            try
            {
                var data = await _context.Users
                                         .Where(u => u.IsVendorAgent == true)
                                         .Select(u => new { UserId = u.DisplayName, DisplayName = u.DisplayName, })
                                         .OrderBy(u => u.DisplayName)
                                         .ToListAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting assigned users data for combobox");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/GetAllAuditOutcomeComboboxData")]
        public async Task<IActionResult> GetAllAuditOutcomeComboboxData()
        {
            try
            {
                var data = await _context.AuditOutcomeComboboxData
                                         .OrderBy(a => a.AuditOrder)
                                         .ToListAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting audit outcome combobox data");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/PriorAuditDataList")]
        public async Task<IActionResult> PriorAuditDataList([FromBody] List<AuditBatch> auditBatches)
        {
            var newdata = GetUser_Name();

            if (auditBatches == null || auditBatches.Count == 0)
            {
                return BadRequest("No audit data provided.");
            }
            try
            {
                foreach (var auditBatch in auditBatches)
                {
                    var existingAudit = await _context.AuditBatch.FindAsync(auditBatch.AuditBatchId);

                    if (existingAudit != null)
                    {
                        existingAudit.AuditFinalNote = auditBatch.AuditFinalNote;
                        existingAudit.AuditFinalTime = DateTime.UtcNow;
                        existingAudit.AuditCompletedBy = newdata;
                        existingAudit.AuditName = auditBatch.AuditName;
                        existingAudit.AuditType = auditBatch.AuditType;
                        existingAudit.StartTime = DateTime.UtcNow;

                    }
                    else
                    {
                        var newAudit = new AuditBatch
                        {
                            AuditBatchId = auditBatch.AuditBatchId,
                            AuditFinalNote = auditBatch.AuditFinalNote,
                            AuditFinalTime = DateTime.UtcNow,
                            AuditCompletedBy = newdata,
                            AuditName = auditBatch.AuditName,
                            AuditType = auditBatch.AuditType,
                            StartTime = auditBatch.StartTime,
                        };

                        _context.AuditBatch.Add(newAudit);
                    }
                }
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Audits updated successfully." });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in PriorAuditDataList");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/BulkUpdateAudit")]
        public async Task<IActionResult> BulkUpdateAudit([FromBody] List<AuditResponse> Audits)
        {
            var newdata = GetUser_Name();

            if (Audits == null || Audits.Count == 0)
            {
                return BadRequest("No audit data provided.");
            }
            try
            {
                foreach (var audit in Audits)
                {
                    var existingAudit = await _context.AuditResponse.FindAsync(audit.RequestNo);

                    if (existingAudit != null)
                    {
                        existingAudit.Outcome = audit.Outcome;
                        existingAudit.Notes = audit.Notes;
                        existingAudit.Internal = audit.Internal;
                        existingAudit.AssignedUser = audit.AssignedUser;
                    }
                    else
                    {
                        var newAudit = new AuditResponse
                        {
                            RequestNo = audit.RequestNo,
                            Outcome = audit.Outcome,
                            Notes = audit.Notes,
                            Internal = audit.Internal,
                            AssignedUser = audit.AssignedUser,
                        };

                        _context.AuditResponse.Add(newAudit);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok("Audits updated successfully.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in BulkUpdateAudit");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/BulkUpdateFollowUpAudit")]
        public async Task<IActionResult> BulkUpdateFollowUpAudit([FromBody] List<AuditFollowUpResponse> Audits)
        {
            var newdata = GetUser_Name();

            if (Audits == null || Audits.Count == 0)
            {
                return BadRequest("No audit data provided.");
            }
            try
            {
                foreach (var audit in Audits)
                {
                    var existingAudit = await _context.AuditFollowUpResponse.FindAsync(audit.FollowUpId);

                    if (existingAudit != null)
                    {
                        existingAudit.Outcome = audit.Outcome;
                        existingAudit.AuditNotes = audit.AuditNotes;
                        existingAudit.Internal = audit.Internal;
                        existingAudit.AssignedUser = audit.AssignedUser;
                    }
                    else
                    {
                        var newAudit = new AuditFollowUpResponse
                        {
                            FollowUpId = audit.FollowUpId,
                            Outcome = audit.Outcome,
                            AuditNotes = audit.AuditNotes,
                            Internal = audit.Internal,
                            AssignedUser = audit.AssignedUser,
                        };

                        _context.AuditFollowUpResponse.Add(newAudit);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok("Audits updated successfully.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in BulkUpdateAudit");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/StartFollowUpAudit")]
        public IActionResult StartFollowUpAudit([FromBody] AuditFollowUpResponse[] items)
        {
            var newdata = GetUser_Name();
            var updatedAudits = new List<AuditFollowUpResponse>();
            foreach (var item in items)
            {
                var existingAudit = _context.AuditFollowUpResponse.FirstOrDefault(a => a.FollowUpId == item.FollowUpId);

                if (existingAudit != null)
                {
                    existingAudit.Auditstatus = item.Auditstatus;
                    existingAudit.AuditType = item.AuditType;
                    existingAudit.AssignedBy = newdata;
                    existingAudit.AuditBatchId = item.AuditBatchId;
                    if (existingAudit.Auditstatus == "Completed")
                    {
                        existingAudit.AuditCompleteTime = DateTime.UtcNow;
                        var allRecords = _context.AuditFollowUpResponse.ToList();
                        if (allRecords.Any())
                        {
                            _context.AuditFollowUpResponse.RemoveRange(allRecords);
                        }
                    }
                    else
                    {
                        existingAudit.AuditTime = DateTime.UtcNow;
                    }
                    updatedAudits.Add(existingAudit);
                }
                else
                {
                    var newAudit = new AuditFollowUpResponse
                    {
                        FollowUpId = item.FollowUpId,
                        AuditTime = DateTime.UtcNow,
                        AssignedBy = newdata,
                        Auditstatus = item.Auditstatus,
                        AuditType = item.AuditType,
                        AuditBatchId = item.AuditBatchId
                    };
                    _context.AuditFollowUpResponse.Add(newAudit);
                    updatedAudits.Add(newAudit);
                }
            }

            _context.SaveChanges();

            return Ok(updatedAudits);
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/StartAudit")]
        public IActionResult StartAudit([FromBody] AuditResponse[] items)
        {
            var newdata = GetUser_Name();
            var updatedAudits = new List<AuditResponse>();
            foreach (var item in items)
            {
                var existingAudit = _context.AuditResponse.FirstOrDefault(a => a.RequestNo == item.RequestNo);

                if (existingAudit != null)
                {
                    existingAudit.Auditstatus = item.Auditstatus;
                    existingAudit.AuditType = item.AuditType;
                    existingAudit.AssignedBy = newdata;
                    existingAudit.AuditBatchId = item.AuditBatchId;
                    if (existingAudit.Auditstatus == "Completed")
                    {
                        existingAudit.AuditCompleteTime = DateTime.UtcNow;
                        var existingAuditType = existingAudit.AuditType;
                        var matchingRecords = _context.AuditResponse.Where(ar => ar.AuditType == existingAuditType).ToList();
                        _context.AuditResponse.RemoveRange(matchingRecords);
                    }
                    else
                    {
                        existingAudit.AuditTime = DateTime.UtcNow;
                    }
                    updatedAudits.Add(existingAudit);
                }
                else
                {
                    var newAudit = new AuditResponse
                    {
                        RequestNo = item.RequestNo,
                        AuditTime = DateTime.UtcNow,
                        AssignedBy = newdata,
                        Auditstatus = item.Auditstatus,
                        AuditType = item.AuditType,
                        AuditBatchId = item.AuditBatchId
                    };
                    _context.AuditResponse.Add(newAudit);
                    updatedAudits.Add(newAudit);
                }
            }

            _context.SaveChanges();

            return Ok(updatedAudits);
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/SaveCompletedAuditData")]
        public IActionResult SaveCompletedAuditData([FromBody] AuditCompleteResponse[] saveditems)
        {
            if (saveditems == null || saveditems.Length == 0)
            {
                return BadRequest("No data received");
            }

            var newdata = GetUser_Name();
            var newAudits = saveditems.Select(item =>
            {
                item.ValidateCreatedDate();
                return new AuditCompleteResponse
                {
                    AuditBatchId = item.AuditBatchId,
                    ProcessStageName = item.ProcessStageName,
                    Status = item.Status,
                    Type = item.Type,
                    State = item.State,
                    Vin = item.Vin,
                    RequestNo = item.RequestNo,
                    VehicleMake = item.VehicleMake,
                    VehicleYear = item.VehicleYear,
                    GroupName = item.GroupName,
                    RT_LH = item.RT_LH,
                    DT_LH_Name = item.DT_LH_Name,
                    Eta = item.Eta,
                    DateToDmv = item.DateToDmv,
                    LH_SToV = item.LH_SToV,
                    RepoDate = item.RepoDate,
                    Odometer = item.Odometer,
                    Created = item.Created,
                    CompletedDate = item.CompletedDate,
                    DueDate = item.DueDate,
                    Title = item.Title,
                    Outcome = item.Outcome,
                    Notes = item.Notes,
                    Internal = item.Internal,
                    AssignedUser = item.AssignedUser,
                    AuditType = item.AuditType,
                    AuditstartTime = item.AuditstartTime,
                    AuditCompleteTime = DateTime.UtcNow,
                    AuditCompletedBy = newdata,
                    AuditstartedBy = item.AuditstartedBy
                };
            }).ToList();

            _context.AuditCompleteResponse.AddRange(newAudits);
            _context.SaveChanges();

            return Ok();
        }

        [HttpGet]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/GetAuditPriorData")]
        public JsonResult GetAuditPriorData(string filterData)
        {
            var Audits = _context.AuditBatch
                .Where(a => a.AuditType == filterData)
                .OrderByDescending(a => a.AuditFinalTime)
                .Select(a => new
                {
                    a.AuditBatchId,
                    a.AuditName,
                    a.AuditFinalNote,
                    a.AuditFinalTime,
                    a.AuditCompletedBy,
                    a.AuditType,
                    a.StartTime,
                    a.TotalTime
                })
                .ToList();

            return Json(Audits);
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/AppendNotesData")]
        public async Task<IActionResult> AppendNotesData([FromBody] RequestNotes requestNote)
        {
            if (requestNote == null)
            {
                return BadRequest();
            }
            UserInfo ui = await GetCurrentUserAsync();
            requestNote.LastUpdated = DateTime.UtcNow;
            requestNote.ModifiedBy = ui.UserId;
            _context.RequestNotes.Add(requestNote);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/GetLatestAuditByTypeAsync")]
        public async Task<AuditBatch> GetLatestAuditByTypeAsync(string auditType)
        {

            var latestAudit = await _context.AuditBatch
                .Where(a => a.AuditType == auditType)
                .OrderByDescending(a => a.AuditFinalTime)
                .FirstOrDefaultAsync();

            return latestAudit;
        }

        [HttpPost("Vendor/GetAttachmentsByRequestId")]
        public IActionResult GetAttachmentsByRequestId([FromBody] RequestIdWrapper wrapper)
        {
            GetUserSID();

            if (wrapper?.RequestId == null)
            {
                return BadRequest("Request ID cannot be null.");
            }

            try
            {
                var requestId = wrapper.RequestId;

                var attachment = _context.RequestAttachments
                    .Where(a => a.RequestId == requestId && a.AttachmentTypeId == null)
                    .Select(a => new
                    {
                        a.AttachmentId,
                        a.RequestId,
                        a.Filename,
                        a.Image,
                        a.DateAdded,
                        a.Description,
                        a.ContainsPII,
                        a.HardcopyReceived,
                        a.ReviewBy,
                        a.ReviewDate,
                        a.Approved,
                        a.AttachmentTypeName
                    })
                    .FirstOrDefault();

                if (attachment == null)
                {
                    return NotFound("No attachment found for the provided Request ID.");
                }

                if (attachment.Image == null || attachment.Image.Length == 0)
                {
                    return BadRequest("No data found.");
                }

                byte[] key = Encoding.UTF8.GetBytes(_encryptionSettings.Key ?? string.Empty);
                byte[] iv = Encoding.UTF8.GetBytes(_encryptionSettings.IV ?? string.Empty);
                int numberToEncrypt = _encryptionSettings.NumberToEncrypt;

                var base64String = Convert.ToBase64String(attachment.Image);
                var fileName = attachment.Filename ?? "UnknownFileName";
                var request = HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
                List<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> headersList = HttpContext.Request.Headers.ToList();

                // Now you can iterate through the list or use it as needed
                foreach (var header in headersList)
                {
                    string headerName = header.Key;
                    string headerValue = header.Value;
                    // Do something with each header
                }
                var fileResponse = new FileResponse
                {
                    FileContent = base64String,
                    Filename = fileName,
                    RequestId = requestId.ToString(),
                    AuthNumber = AesEncryption.EncryptNumber(numberToEncrypt, key, iv) ?? string.Empty,
                    BaseUrl = baseUrl,
                    Cookies = headersList.Where(x => x.Key == "Cookie").FirstOrDefault().Value,
                    BackendBaseUrl = _oCRSettings.OCRBackendBaseUrl,
                    WebBaseUrl = _oCRSettings.OCRWebBaseUrl,
                };

                return Ok(fileResponse);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest($"A required argument was null: {ex.ParamName}");
            }
            catch (FormatException ex)
            {
                return BadRequest($"Error in formatting data: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<IActionResult> NeedToProcess()
        {
            AddPageHeader("Need To Process", "");
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/BulkUpdateNeedToProcess")]
        public async Task<IActionResult> BulkUpdateNeedToProcess([FromBody] List<NeedToProcessResponse> responses)
        {
            var newdata = GetUser_Name();

            if (responses == null || responses.Count == 0)
            {
                return BadRequest("No audit data provided.");
            }
            try
            {
                foreach (var response in responses)
                {
                    var existingProcess = await _context.NeedToProcessResponses.FindAsync(response.RequestNo);

                    if (existingProcess != null)
                    {
                        existingProcess.AssignedProcessor = response.AssignedProcessor;
                        existingProcess.AssignedShipper = response.AssignedShipper;
                        existingProcess.Assignedby = newdata;
                        existingProcess.NeedToProcessStageName = response.NeedToProcessStageName;
                    }
                    else
                    {
                        var newProcessResponse = new NeedToProcessResponse
                        {
                            RequestNo = response.RequestNo,
                            AssignedShipper = response.AssignedShipper,
                            AssignedProcessor = response.AssignedProcessor,
                            Assignedby = newdata,
                            NeedToProcessStageName = response.NeedToProcessStageName
                        };

                        _context.NeedToProcessResponses.Add(newProcessResponse);
                    }
                }
                await _context.SaveChangesAsync();
                return Ok("Audits updated successfully.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in BulkUpdateAudit");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        [Authorize(Policy = "VendorAgentOnly")]
        [Route("Vendor/GetProcessingDay")]
        public async Task<IActionResult> GetProcessingDay()
        {
            try
            {
                // dayid values are flags,
                //   we don't want those with multiple bits set
                // Day1 == 1
                // Day2 == 2
                // Day3 == 4 
                // Day4 == 8
                // Both == (Day1|Day2)

                var data = await _context.ProcessingDayEnum.ToListAsync();
                data = data.Where(d => CountSetBits(d.dayid) == 1).ToList();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting assigned users data for combobox");
                return StatusCode(500, "Internal server error");
            }
        }
        private static int CountSetBits(int n)
        {
            int count = 0;
            while (n > 0)
            {
                count += n & 1;
                n >>= 1;
            }
            return count;
        }

        [IgnoreAntiforgeryToken]
        [AllowAnonymous]
        [HttpPost]
        [EnableCors("AllowOCR")]
        [Route("Vendor/UploadDocument")]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadRequestModel model)
        {

            try
            {
                var authNumber = HttpContext.Request.Headers["AuthNumber"].FirstOrDefault();

                if (string.IsNullOrEmpty(authNumber))
                {
                    return Unauthorized(new { Message = "Auth number is required." });
                }

                if (_encryptionSettings.Key == null || _encryptionSettings.IV == null)
                {
                    throw new InvalidOperationException("Encryption key or IV is missing");
                }

                byte[] key = Encoding.UTF8.GetBytes(_encryptionSettings.Key);
                byte[] iv = Encoding.UTF8.GetBytes(_encryptionSettings.IV);

                // Decrypt the authNumber
                int decryptedAuthNumber = AesEncryption.DecryptNumber(authNumber, key, iv);

                if (decryptedAuthNumber != _encryptionSettings.NumberToEncrypt)
                {
                    return Unauthorized(new { Message = "Invalid auth number" });
                }

                const string ReviewToolAcceptNote = "Request accepted. Application moved from Review to Accepted Incoming stage";
                const string ReviewToolNotAcceptNote = "Request not ready to accept. Application moved to Not Ready to Accept stage";

                if (model.File is null || string.IsNullOrEmpty(model.RequestId) || model is null)
                {
                    return BadRequest(new { Message = "Invalid data.", StatusCode = 400 });
                }

                byte[] pdfData;
                using (var memoryStream = new MemoryStream())
                {
                    await model.File.CopyToAsync(memoryStream);
                    pdfData = memoryStream.ToArray();
                }
                byte[] summaryData = null;
                if (model.Summary != null)
                { 
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.Summary.CopyToAsync(memoryStream);
                        summaryData = memoryStream.ToArray();
                    }
                }

                var existingRecord = await _context.UploadedOCRDocuments
                                                   .FirstOrDefaultAsync(x => x.RequestId == model.RequestId);

                if (existingRecord != null)
                {
                    existingRecord.PdfData = pdfData;
                    existingRecord.JsonData = model.JsonData;
                    existingRecord.UploadTime = DateTime.UtcNow;
                    existingRecord.Status = model.Status;
                    existingRecord.SummaryData=summaryData;
                    _context.UploadedOCRDocuments.Update(existingRecord);
                }
                else
                {
                    var newRecord = new UploadedOCRDocuments
                    {
                        RequestId = model.RequestId,
                        PdfData = pdfData,
                        JsonData = model.JsonData,
                        Status = model.Status,
                        UploadTime = DateTime.UtcNow
                    };

                    await _context.UploadedOCRDocuments.AddAsync(newRecord);
                }

                await _context.SaveChangesAsync();

                var requestRecord = await _context.Requests
                                                  .FirstOrDefaultAsync(x => x.RequestId == Guid.Parse(model.RequestId));

                if (requestRecord == null)
                {
                    return NotFound(new
                    {
                        Message = "Request record not found.",
                        StatusCode = 404,
                    });
                }

                var existingJsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestRecord.JRequest) ?? new Dictionary<string, object>();
                var newJsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(model.JsonData) ?? new Dictionary<string, object>();

                var ocrProperties = typeof(OCRRoot).GetProperties();
                foreach (var prop in ocrProperties)
                {
                    var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                    var jsonKey = columnAttr?.Name ?? prop.Name;

                    if (newJsonData.ContainsKey(prop.Name))
                    {
                        if (prop.Name == "TypeOfPower")
                        {
                            // Convert the power type value using the GetConvertedPowerType function
                            var convertedPowerType = GlobalHelper.GetConvertedPowerType(newJsonData[prop.Name].ToString());
                            existingJsonData[jsonKey] = convertedPowerType;
                        }
                        else
                        {
                            // For other keys, simply update as usual
                            existingJsonData[jsonKey] = newJsonData[prop.Name];
                        }
                    }
                }

                requestRecord.JRequest = JsonConvert.SerializeObject(existingJsonData);

                if (model.Documents is not null)
                {
                    var documentList = JsonConvert.DeserializeObject<Dictionary<string, object>>(model.Documents);
                    foreach (var doc in documentList)
                    {
                        if (doc.Value is not null && doc.Key is not null)
                            AddOrUpdateAttachment(requestRecord, doc.Value.ToString(), doc.Key);
                    }
                }

                if (!string.IsNullOrEmpty(model.DataTags))
                {
                    requestRecord.jDataTags = model.DataTags;
                }

                var statusActionMap = new Dictionary<string, Func<Task>>
                {
                    ["MQ_NotReadyToAccept"] = async () =>
                    {
                        requestRecord.NotReadyToAccept = ServerDateTime();
                        await MoveItemsWithNotes(new[] { model.RequestId }, 61, null, true, null, ReviewToolNotAcceptNote, null, null, null);
                    },
                    ["MQ_FollowUpReview"] = async () =>
                    {
                        await MoveItemsWithNotes(new[] { model.RequestId }, 62, null, true, null, null, null, null, null);
                    },
                    ["MQ_AcceptedIncoming"] = async () =>
                    {
                        requestRecord.AcceptedDate = ServerDateTime();
                        await MoveItemsWithNotes(new[] { model.RequestId }, 63, null, true, null, ReviewToolAcceptNote, null, null, null);
                    }
                };

                if (statusActionMap.ContainsKey(model.Status))
                {
                    await statusActionMap[model.Status].Invoke();
                }

                int? jurisdictionId = int.TryParse(model.JurisdictionId, out int parsedId) ? parsedId : (int?)null;

                // Parse Tax Items (only selected properties)
                List<CalculatedTaxItem> taxItems = string.IsNullOrEmpty(model.TaxItems)
                    ? new List<CalculatedTaxItem>()
                    : JsonConvert.DeserializeObject<List<CalculatedTaxItem>>(model.TaxItems);

                // Parse TotalTaxItems (assign values directly)
                CalculatedTaxResult taxResult = string.IsNullOrEmpty(model.TotalTaxItems)
                    ? null
                    : JsonConvert.DeserializeObject<CalculatedTaxResult>(model.TotalTaxItems);

                TotalCollectionTaxDetailsDto totalCollectionTaxBreakDown= string.IsNullOrEmpty(model.TaxBreakDown)
                    ? null
                    : JsonConvert.DeserializeObject<TotalCollectionTaxDetailsDto>(model.TaxBreakDown);

                // Ensure only selected tax item fields are included
                if (taxResult != null)
                {
                    taxResult.CalculatedTaxItems = taxItems ?? new List<CalculatedTaxItem>();
                }

                if (totalCollectionTaxBreakDown != null)
                {
                    taxResult.TotalCollectionTaxDetails = totalCollectionTaxBreakDown ?? new TotalCollectionTaxDetailsDto();
                }

                // Parse Vehicle Info from RegInputs
                VehicleInfo vehicleInfo = string.IsNullOrEmpty(model.RegInputs)
                    ? null
                    : JsonConvert.DeserializeObject<VehicleInfo>(model.RegInputs);

                // Assign State Code to VehicleInfo
                if (vehicleInfo != null)
                {
                    vehicleInfo.StateCode = model.StateId;
                }

                // Parse Calculated Fee Values from RegItems
                List<CalculatedFeeValue> feeValues = string.IsNullOrEmpty(model.RegItems)
                    ? new List<CalculatedFeeValue>()
                    : JsonConvert.DeserializeObject<List<CalculatedFeeValue>>(model.RegItems);

                var fee = new TaxAndRegFeeModel
                {
                    StateId = model.StateId,
                    JurisdictionId = jurisdictionId,
                    CalculatedTaxResult = taxResult,
                    RegItems = vehicleInfo,
                    CalculatedFeeResult = feeValues,
                };

                requestRecord.jFeesData = JsonConvert.SerializeObject(fee);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = model.Status,
                    StatusCode = 200,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while processing your request.",
                    Details = ex.Message
                });
            }
        }


        private void AddOrUpdateAttachment(Requests requestRecord, string base64FileData, string key)
        {
            var documentType = GlobalHelper.GetDocumentType(key);
            var filename = $"{documentType}.pdf";

            // Decode the Base64 string into a byte array
            byte[] fileData;
            try
            {
                fileData = Convert.FromBase64String(base64FileData);
            }
            catch (FormatException)
            {
                // Handle invalid Base64 format here if needed
                throw new ArgumentException("Invalid Base64 string provided.");
            }

            var attachmentTypeId = _context.AttachmentTypes
                                     .Where(x => x.Name == documentType)
                                     .FirstOrDefault()?.AttachmentTypeId;

            if (attachmentTypeId is not null)
            {
                var existingAttachment = requestRecord.RequestAttachments
                    .FirstOrDefault(att => att.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase)
                                        && att.AttachmentTypeId == attachmentTypeId);

                if (existingAttachment != null)
                {
                    // Update the existing attachment
                    existingAttachment.Image = fileData;
                    existingAttachment.DateAdded = DateTime.UtcNow;
                    existingAttachment.AttachmentTypeId = attachmentTypeId;
                }
                else
                {
                    RequestAttachments attachment = new()
                    {
                        Filename = filename,
                        Description = documentType,
                        Image = fileData,
                        DateAdded = DateTime.UtcNow,
                        AttachmentTypeId = attachmentTypeId,
                    };

                    requestRecord.RequestAttachments.Add(attachment);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> SpecialAttachmentTypes()
        {
            var result = await _context.MdpAttachmentTypes
                                            .Where(x => x.ExcludeFromOneOffs == null || x.ExcludeFromOneOffs == false)
                                            .ToListAsync();
            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> InitiateChampRequest_WithTitle(List<Guid> requestIds)
        {
            List<Requests> requests = null;

            try
            {
                var user = await GetCurrentUserAsync();

                // All apis in this class are VendorAgentOnly
                // But verify all requestIds are for the current user's vendor
                requests = await _context.Requests
                    .Where(r => requestIds.Contains(r.RequestId))
                    .Where(r => r.VendorId == user.VendorId)
                    .ToListAsync();

                var champsRequests = requests.Select(r => new ChampHelper.ChampInitiateRequest() { RequestId = r.RequestId }).ToList();
                var results = await ChampHelper.InitiateChampRequest_RepossessionWithTitle(champsRequests, _logger);

                foreach (var request in requests)
                {
                    request.ProcessStageId = (int)ProcessStageIDs.WVSendQueue;
                }
                await _context.SaveChangesAsync();

                if (results.ErrorCount == 0) return JsonSuccess();
                if (results.SuccessCount == 0) return JsonError("No requests were successfully initiated.");
                return JsonError($"{results.SuccessCount} successfully requested, {results.ErrorCount} errors.");
            }
            catch (Exception ex)
            {
                return JsonError($"Error submitting to champs queue: {ex}");
            }
        }

        [HttpGet]
        [Route("Vendor/GetFeaturePermissionForParticularUser")]
        public async Task<IActionResult> GetFeaturePermissionForParticularUserAsync()
        {
            UserInfo currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return JsonError("User not found");
            }

            var feature = _context.Features.Where(f => f.FeatureKey == FeatureKey.INVOICING.ToString()).FirstOrDefault();

            if (feature == null)
            {
                return JsonError("Feature Not found");

            }
            var featurePermission = _context.FeaturePermissions
                .FirstOrDefault(fp => fp.UserId == currentUser.UserId && fp.FeatureId == feature.FeatureId);

            if (featurePermission == null)
            {
                return JsonError("Not found");
            }

            return Json(new { success = true, data = featurePermission });
        }

    }
}