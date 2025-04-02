using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    public class RequestController : BaseController
    {
        public RequestController(MaggardDMVContext context, IConfiguration configuration, ILogger<RequestController> logger) : base(context, configuration, logger)
        {
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null)
            {
                return await SaveToRequests(file, false);
            }
            return RedirectToAction(nameof(MyServicesController.MyUploads), "MyServices");
        }

        [HttpPost]
        public async Task<IActionResult> Upload_v2(IFormFile file, bool requireUserId)
        {
            if (file != null)
            {
                return await SaveToRequests(file, requireUserId);
            }
            return RedirectToAction(nameof(MyServicesController.MyUploads), "MyServices");
        }
        [HttpPost]
        public async Task<IActionResult> ValidateUpload(string excelJson)
        {
            if (excelJson != null)
            {
                return await ValidateExcelFile(excelJson);
            }
            return JsonError("Invalid parameter");
        }

        private class ValidateExcelFileResultCounts
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public int validCount { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public int invalidCount { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public int blankCount { get; set; }
        }

        private class ValidateExcelFileResult
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public string status { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public string message { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public ValidateExcelFileResultCounts counts { get; set; }

            public List<ValidateGroupUser> validation { get; set; } = new();
        }

        private class ValidateGroupUser
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public bool valid { get; set; }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public string userId { get; set; }
        }

        private async Task<IActionResult> ValidateExcelFile(string excelJson)
        {
            ValidateExcelFileResult result = new()
            {
                status = "error",
                counts = new ValidateExcelFileResultCounts()
            };

            UserInfo user = await GetCurrentUserAsync();
            if (user != null)
            {
                JArray worksheet = JArray.Parse(excelJson);

                string json = await DataHelpers.GetGroupUserIDs(user.GroupId.Value);
                // Confirmed dynamic works for this case 12/26/2023
                dynamic groupUsers = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
                List<string> users = new();
                foreach (var gu in groupUsers)
                {
                    string uid = gu["userPrincipalName"].Value; // will be email address for myDMV.pro accounts
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                    string email = gu["email"].Value; // alternative lookup
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                    uid = uid.ToLowerInvariant();
                    users.Add(uid);
                }

                int validCount = 0;
                int invalidCount = 0;
                int blankCount = 0;
                foreach (var row in worksheet)
                {
                    var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(row.ToString());
                    ValidateGroupUser vgu = new();
                    if (values.ContainsKey("User ID"))
                    {
                        string userId = values["User ID"] ?? "";
                        userId = userId.Trim().ToLowerInvariant();

                        // check if userid in group users
                        if (string.IsNullOrEmpty(userId))
                        {
                            blankCount++;
                        }
                        else
                        {
                            vgu.valid = users.Contains(userId);
                            vgu.userId = userId;
                            if (vgu.valid)
                            {
                                validCount++;
                            }
                            else
                            {
                                invalidCount++;
                            }
                        }
                    }
                    else
                    {
                        // leave as default values
                        blankCount++;
                    }
                    result.validation.Add(vgu);
                }

                result.status = "success";
                result.counts.invalidCount = invalidCount;
                result.counts.validCount = validCount;
                result.counts.blankCount = blankCount;

                result.message = $"{invalidCount + validCount + blankCount} rows checked. Found {invalidCount} invalid and {blankCount} blank User IDs";
            }
            else
            {
                result.status = "error";
                result.message = "Current user invalid.";
            }
            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> UploadAndValidate(IFormFile file, bool requireUserId)
        {
            if (file != null)
            {
                return await SaveToRequests(file, requireUserId);
            }
            return RedirectToAction(nameof(MyServicesController.MyUploads), "MyServices");
        }
        private async Task<IActionResult> SaveToRequests(IFormFile formFile, bool requireUserId)
        {
            if (formFile.Length == 0)
                return Ok(new { count = 0 });

            Guid? fileUploadId = null;

            UserInfo user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Ok(new { count = 0 });
            }

            // Validate Excel workbook
            using (var stream = new MemoryStream())
            {
                await formFile.CopyToAsync(stream);

                byte[] data = stream.ToArray();
                // Save to database
                string userName = GetUserSID();
                string filename = Path.GetFileName(formFile.FileName);
                string errorMsg;
                (fileUploadId, errorMsg) = await DataHelpers.SubmitToFileUploads(_context, filename, null, user.GroupId, userName, data, requireUserId);

                FileUploads file = await _context.FileUploads.Where(fu => fu.FileUploadId == fileUploadId).FirstOrDefaultAsync();
                if (file != null)
                {
                    if (errorMsg == null)
                        file.FileImage = null;
                    else
                        _context.FileUploads.Remove(file);
                    await _context.SaveChangesAsync(user);
                }
                if (errorMsg != null)
                {
                    return JsonError(errorMsg);
                }
            }
            if (fileUploadId != null)
            {
                await DataHelpers.TriggerAutoImsDownloadForFileUpload(fileUploadId.Value, _logger);
            }

            return Ok(new { count = 1 });
        }

        [HttpPost]
        public async Task<IActionResult> AddAttachmentRequirement(Guid requestId, Guid attachmentTypeId, string comment)
        {
            try
            {
                var user = await GetCurrentUserAsync();

                var condition = new RequestAttachmentCondition()
                {
                    RequestId = requestId,
                    AttachmentTypeId = attachmentTypeId,
                    AttachmentComment = comment,
                    CreatedBy = user.UserId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.RequestAttachmentConditions.Add(condition);
                await _context.SaveChangesAsync(user);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}