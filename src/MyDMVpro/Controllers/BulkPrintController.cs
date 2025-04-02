using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using MyDMVpro.Models.RequestsViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

[Authorize(Policy = "VendorAgentOnly")]
public class BulkPrintController : BaseController
{
    public delegate IQueryable<BulkPrintFile> QueryRecords(Guid? vendorId, Guid? groupId, Guid? userId);

    public BulkPrintController(MaggardDMVContext context, IConfiguration configuration, ILogger<BulkPrintController> logger)
        : base(context, configuration, logger)
    {
    }

    static BulkPrintController()
    {
        InitExpressions();
    }

    #region endpoints for bulk print user operations

    [HttpPost]
    public async Task<IActionResult> BulkPrintAll([FromForm] List<Guid> requestIds, Guid attachmentTypeId, Guid templateId)
    {
        return JsonError("Not implemented");
    }

    [HttpPost]
    public async Task<IActionResult> BulkPrintAttachments([FromForm] List<Guid> requestIds, Guid attachmentTypeId)
    {
        if (requestIds == null || requestIds.Count == 0)
            return BadRequest("No requests were selected.");

        try
        {
            var attachmentType = await _context.AttachmentTypes
                                                    .AsNoTracking()
                                                    .Where(x => x.AttachmentTypeId == attachmentTypeId)
                                                    .FirstOrDefaultAsync();

            // we expect all requests to have the same AppType and State
            var appTypeStates = await _context.Requests
                                            .Where(x => x.RequestId == requestIds[0])
                                            .Select(x => new { x.AppType, x.State })
                                            .Distinct()
                                            .ToListAsync();
            if (appTypeStates.Count > 1)
            {
                return JsonError("You can only perform a bulk print for requests with same State and App Type.");
            }

            List<MetadataAttachmentInfo> attachments = null;

            if (attachmentType.InternalFromClient)
            {
                string excelName = attachmentType.ExcelName;
                string fieldPath = $"$.\"{excelName}\"";
                // get the requests to get the attachment ids
                var groupAttachments = await _context.Requests
                                                .Where(x => requestIds.Contains(x.RequestId))
                                                .Select(x => new MetadataAttachmentInfo
                                                {
                                                    RequestId = x.RequestId,
                                                    VIN = x.Vin,
                                                    AttachmentId = _context.TryCastJsonValueAsGuid(x.JRequest, fieldPath)
                                                })
                                                .ToListAsync();

                attachments = groupAttachments;
            }
            else if (attachmentType.InternalFromVendor)
            {
                string excelName = attachmentType.ExcelName;
                string fieldPath = $"$.\"{excelName}\"";
                // get the requests to get the attachment ids
                var vendorAttachments = await _context.Requests
                                                .Where(x => requestIds.Contains(x.RequestId))
                                                .Select(x => new MetadataAttachmentInfo
                                                {
                                                    RequestId = x.RequestId,
                                                    VIN = x.Vin,
                                                    AttachmentId = _context.TryCastJsonValueAsGuid(x.JRequest, fieldPath)
                                                })
                                                .AsNoTracking()
                                                .ToListAsync();
                attachments = vendorAttachments;
            }
            else
            {
                // get the attachments of the requested type for all requestIds and merge them into a single file
                attachments = await _context.RequestAttachments
                                                        .Where(x => (x.RequestId != null
                                                                        && requestIds.Contains(x.RequestId.Value))
                                                                        && x.AttachmentTypeId == attachmentTypeId)
                                                        .Where(x => x.AttachmentType.HardcopyOnly == false && x.AttachmentType.Name != "Extra")
                                                        .Include(x => x.AttachmentType)
                                                        .Select(a => new MetadataAttachmentInfo
                                                        {
                                                            RequestId = a.RequestId.Value,
                                                            AttachmentId = a.AttachmentId,
                                                            VIN = a.Request.Vin
                                                        })
                                                        .ToListAsync();

                // These will throw an exception if the attachments are not all of the same type, which should not be able to occur from the above query
            }
            string attachmentTypeName = attachmentType.Name;
            string appType = appTypeStates[0].AppType;
            string appState = appTypeStates[0].State;

            MetadataInfo metaObject = new MetadataInfo()
            {
                appType = appType,
                state = appState,
                requestIds = requestIds,
                letterheads = null,
                attachmentTypeId = attachmentTypeId,
                attachmentTypeName = attachmentTypeName,
                attachments = attachments,
                internalFromClient = attachmentType.InternalFromClient,
                internalFromVendor = attachmentType.InternalFromVendor
            };

            var userInfo = await GetCurrentUserAsync();

            DateTime utcNow = DateTime.UtcNow;
            var dtEasternTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            string easternTime = $"{dtEasternTime.ToString("yyyy-MM-dd_HHmm")}";

            BulkPrintFile bulkPrintFile = new()
            {
                Filename = $"{appState}-{appType}-{attachmentTypeName}-{easternTime}.pdf",
                UploadedBy = userInfo.UserId,
                AppType = appType,
                AppTypeState = appState,
                DateRequested = utcNow,
                Metadata = JsonConvert.SerializeObject(metaObject),
                BulkPrintFileId = Guid.NewGuid(),
                RequestCount = requestIds.Count
            };

            await _context.AddAsync(bulkPrintFile);
            await _context.SaveChangesAsync(userInfo);

            // add bulkPrintId to queue
            string queueConnectionString = _configuration.GetValue<string>("BulkPrint:QueueConnectionString");
            string queueEndpointUrl = _configuration.GetValue<string>("BulkPrint:QueueEndpointUrl");
            string queueName = _configuration.GetValue<string>("BulkPrint:QueueName");

            System.Diagnostics.Trace.WriteLine($"BulkPrintFileId: {bulkPrintFile.BulkPrintFileId}");
            System.Diagnostics.Trace.WriteLine($"queueConnectionString: {queueConnectionString}");
            System.Diagnostics.Trace.WriteLine($"queueEndpointUrl: {queueEndpointUrl}");
            System.Diagnostics.Trace.WriteLine($"queueName: {queueName}");

            await StorageHelpers.SendMessageToQueueAsync(queueConnectionString, queueEndpointUrl, queueName, bulkPrintFile.BulkPrintFileId.ToString());

            return JsonSuccess(bulkPrintFile.Filename);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in BulkPrintAttachments");
            return JsonError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Reprocess([FromForm] Guid bulkPrintFileId, [FromForm] bool? force)
    {
        if (bulkPrintFileId == Guid.Empty)
            return BadRequest("No item selected.");

        try
        {
            var bulkPrintFile = await _context.BulkPrintFiles
                                                .AsNoTracking()
                                                .Where(x => x.BulkPrintFileId == bulkPrintFileId && ((force ?? false) || x.DateCompleted == null))
                                                .FirstOrDefaultAsync();

            if (bulkPrintFile == null)
            {
                return JsonError("File already processed.");
            }
            // add bulkPrintId to queue
            string queueConnectionString = _configuration.GetValue<string>("BulkPrint:QueueConnectionString");
            string queueEndpointUrl = _configuration.GetValue<string>("BulkPrint:QueueEndpointUrl");
            string queueName = _configuration.GetValue<string>("BulkPrint:QueueName");

            await StorageHelpers.SendMessageToQueueAsync(queueConnectionString, queueEndpointUrl, queueName, bulkPrintFile.BulkPrintFileId.ToString());

            return JsonSuccess(bulkPrintFile.Filename);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in BulkMergeApplications");
            return JsonError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> BulkMergeApplications([FromForm] List<Guid> requestIds, [FromForm] Guid templateId, [FromForm] bool? includeChecks)
    {
        if (requestIds == null || requestIds.Count == 0)
            return BadRequest("No requests were selected.");

        try
        {
            var userInfo = await GetCurrentUserAsync();
            // get the attachments of the requested type for all requestIds and merge them into a single file
            // These will throw an exception if the attachments are not all of the same type, which should not be able to occur from the above query
            var application = await _context.PdfTemplates.AsNoTracking().Where(x => x.TemplateId == templateId).FirstOrDefaultAsync();

            if (application == null)
            {
                throw new ApplicationException("Template not found");
            }
            var data = await DataHelpers.GetUniversalBulkEditData(userInfo, requestIds);
            MetadataInfo metadataInfo = new()
            {
                requestIds = requestIds,
                templateId = templateId,
                useClientLetterhead = application.UseClientLetterhead,
                letterheadPages = application.LetterheadPages,
                data = data
            };

            DateTime utcNow = DateTime.UtcNow;
            var dtEasternTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            string easternTime = $"{dtEasternTime.ToString("yyyy-MM-dd_HHmm")}";
            string appType = application.AppType;
            string appState = application.AppState;

            BulkPrintFile bulkPrintFile = new()
            {
                Filename = $"{appState}-{appType}-{application.DisplayName}-{easternTime}.pdf",
                UploadedBy = userInfo.UserId,
                AppType = appType,
                AppTypeState = appState,
                DateRequested = utcNow,
                Metadata = JsonConvert.SerializeObject(metadataInfo),
                BulkPrintFileId = Guid.NewGuid(),
                RequestCount = requestIds.Count,
                IsApplication = true,
                IsCheckMerge = false
            };

            await _context.AddAsync(bulkPrintFile);
            await _context.SaveChangesAsync(userInfo);

            // add bulkPrintId to queue
            string queueConnectionString = _configuration.GetValue<string>("BulkPrint:QueueConnectionString");
            string queueEndpointUrl = _configuration.GetValue<string>("BulkPrint:QueueEndpointUrl");
            string queueName = _configuration.GetValue<string>("BulkPrint:QueueName");

            System.Diagnostics.Trace.WriteLine($"BulkPrintFileId: {bulkPrintFile.BulkPrintFileId}");
            System.Diagnostics.Trace.WriteLine($"queueConnectionString: {queueConnectionString}");
            System.Diagnostics.Trace.WriteLine($"queueEndpointUrl: {queueEndpointUrl}");
            System.Diagnostics.Trace.WriteLine($"queueName: {queueName}");

            await StorageHelpers.SendMessageToQueueAsync(queueConnectionString, queueEndpointUrl, queueName, bulkPrintFile.BulkPrintFileId.ToString());

            return JsonSuccess(bulkPrintFile.Filename);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in BulkMergeApplications");
            return JsonError(ex);
        }
    }
    public class CheckMergeData
    {
        public Guid RequestId { get; set; }
        public int RequestNo { get; set; }
        public string Vin { get; set; }
        public string AppType { get; set; }
        public string State { get; set; }
        public int PageNo { get; set; }
        public bool UsePlaceholder { get; set; }
        // 0, 1, 2
        public int CheckPosition { get; set; }
        public int CheckNumber { get; set; }
        public Decimal CheckAmount { get; set; }
        public string CheckAmountLonghand { get; set; }

        public string PayeeName { get; set; }

    }
    [HttpPost]
    public async Task<IActionResult> BulkMergeChecks([FromForm] List<Guid> requestIds, [FromForm] List<Guid> createNewCheck, [FromForm] List<string> useExistingCheck)
    {
        if (requestIds == null || requestIds.Count == 0)
            return BadRequest("No requests were selected.");

        try
        {
            var userInfo = await GetCurrentUserAsync();
            var templateId = _context.Vendors.Where(v => v.VendorId == userInfo.VendorId).Select(v => v.CheckTemplateID).FirstOrDefault();
            if (templateId == null)
                return JsonError("No check template found for the vendor.");

            // verify requestIds contains all values in createNewCheck
            if (createNewCheck != null && createNewCheck.Count > 0)
            {
                if (createNewCheck.Except(requestIds).Any())
                {
                    return JsonError("Invalid requestIds");
                }
            }
            Dictionary<Guid, int> useExistingCheckDict = new();
            if (useExistingCheck != null && useExistingCheck.Count > 0)
            {
                foreach (var item in useExistingCheck)
                {
                    var parts = item.Split('|');
                    if (parts.Length != 2)
                    {
                        return JsonError("Invalid useExistingCheck");
                    }
                    if (!Guid.TryParse(parts[0], out Guid requestId))
                    {
                        return JsonError("Invalid useExistingCheck");
                    }
                    if (!Int32.TryParse(parts[1], out int checkNumber))
                    {
                        return JsonError("Invalid checkNumber");
                    }
                    useExistingCheckDict[requestId] = checkNumber;
                }
            }
            // verify requestIds contains all values in useExistingCheck
            List<Guid> useExistingCheckRequestIds = useExistingCheckDict.Keys.ToList();

            // verify useExistingCheck and createNewCheck do not have any common values
            if (createNewCheck != null && useExistingCheck != null)
            {
                if (createNewCheck.Intersect(useExistingCheckRequestIds).Any())
                {
                    return JsonError("Invalid requestIds");
                }
            }
            var requests = await _context.Requests
                                            .Where(x => requestIds.Contains(x.RequestId))
                                            .Select(x => new CheckMergeData()
                                            {
                                                RequestId = x.RequestId,
                                                RequestNo = x.Id,
                                                Vin = x.Vin,
                                                AppType = x.AppType,
                                                State = x.State,
                                                UsePlaceholder = useExistingCheckRequestIds.Contains(x.RequestId)
                                            })
                                            .ToListAsync();
            if (requests.Count != requestIds.Count)
            {
                throw new Exception("Request deleted or not found");
            }
            // force order of requests to match the order of requestIds
            requests = requests.OrderBy(x => requestIds.IndexOf(x.RequestId)).ToList();

            // Get the mdpAppTypeState for the requests
            var applications = requests.Select(x => new { x.AppType, x.State })
                                            .Distinct()
                                            .ToList();

            if (applications.Count > 1)
            {
                throw new Exception("You can only perform a bulk print for requests with same State and App Type.");
            }
            var application = applications[0];

            DateTime utcNow = DateTime.UtcNow;
            var dtEasternTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            string easternTime = $"{dtEasternTime.ToString("yyyy-MM-dd_HHmm")}";
            string appType = application.AppType;
            string appState = application.State;

            var fees = await DataHelpers.GetApplicationFees(null, null, appType, appState, null, null);
            if (fees.Count == 0)
            {
                throw new Exception("Unexpected error: fees not defined for the application.");
            }
            if (fees.Count > 1)
            {
                throw new Exception("Unexpected error: multiple fees found for the application.");
            }
            var fee = fees.FirstOrDefault();

            var checkAmount = fee.DmvTotal;
            var payeeName = fee.DMVPayToName;
            var checkAmountLonghand = CurrencyHelper.ConvertToWords(checkAmount, true, false);
            foreach (var check in requests)
            {
                check.CheckAmount = checkAmount;
                check.CheckAmountLonghand = checkAmountLonghand;
                check.PayeeName = payeeName;
            }

            bool forceNewChecks = false;
            BulkPrintFile bulkPrintFile = null;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var disbursements = await GenerateChecks(userInfo, requestIds, checkAmount, payeeName, useExistingCheckDict, createNewCheck);

                var data = await BuildCheckVariables(disbursements, fee, DateTime.Today, requests);
                object metaObject = new
                {
                    requestIds,
                    templateId,
                    data
                };

                bulkPrintFile = new()
                {
                    Filename = $"{appState}-{appType}-CHECKS-{easternTime}.pdf",
                    BatchNumber = null,
                    BatchTimestamp = null,
                    UploadedBy = userInfo.UserId,
                    AppType = appType,
                    AppTypeState = appState,
                    DateRequested = utcNow,
                    Metadata = JsonConvert.SerializeObject(metaObject),
                    BulkPrintFileId = Guid.NewGuid(),
                    RequestCount = requestIds.Count,
                    IsApplication = false,
                    IsCheckMerge = true
                };

                await _context.BulkPrintFiles.AddAsync(bulkPrintFile);

                await _context.SaveChangesAsync(userInfo);

                await transaction.CommitAsync();
            }
            // add bulkPrintId to queue
            if (bulkPrintFile == null)
            {
                throw new Exception("Unexpected error: bulkPrintFile not found.");
            }
            string queueConnectionString = _configuration.GetValue<string>("BulkPrint:QueueConnectionString");
            string queueEndpointUrl = _configuration.GetValue<string>("BulkPrint:QueueEndpointUrl");
            string queueName = _configuration.GetValue<string>("BulkPrint:QueueName");

            System.Diagnostics.Trace.WriteLine($"BulkPrintFileId: {bulkPrintFile.BulkPrintFileId}");
            System.Diagnostics.Trace.WriteLine($"queueConnectionString: {queueConnectionString}");
            System.Diagnostics.Trace.WriteLine($"queueEndpointUrl: {queueEndpointUrl}");
            System.Diagnostics.Trace.WriteLine($"queueName: {queueName}");

            await StorageHelpers.SendMessageToQueueAsync(queueConnectionString, queueEndpointUrl, queueName, bulkPrintFile.BulkPrintFileId.ToString());

            return JsonSuccess(bulkPrintFile.Filename);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in BulkMergeApplications");
            return JsonError(ex);
        }
    }
    
    /// <summary>
    /// Add checks to the database for the selected requests
    /// </summary>
    /// <param name="user"></param>
    /// <param name="requestIds"></param>
    /// <param name="amount"></param>
    /// <param name="payToName"></param>
    /// <param name="forceNewChecks">Forces a new check even if existing unvoided check exists</param>
    /// <returns></returns>
    public async Task<List<RequestDisbursement>> GenerateChecks(UserInfo user, List<Guid> requestIds, Decimal amount, string payToName, Dictionary<Guid, int> useExistingCheckDict, List<Guid> createNewCheck)
    {
        try
        {
            useExistingCheckDict ??= new();

            {
                var existingChecks = await _context.RequestDisbursements
                                                    .Where(x => (x.Voided ?? false) == false)
                                                    .Where(x => requestIds.Contains(x.RequestId))
                                                    .ToListAsync();
                // exclude values that are in createNewCheck
                existingChecks = existingChecks.Where(x => createNewCheck.Contains(x.RequestId)).ToList();
                // exclude values that are in useExistingCheck
                existingChecks = existingChecks.Where(x => useExistingCheckDict.ContainsKey(x.RequestId)).ToList();

                // what scenarios will we allow to generate new checks?
                if (existingChecks.Count > 0)
                {
                    throw new Exception("Checks already exist for the selected requests.");
                }
            }

            var vendor = await _context.Vendors
                                    .Where(x => x.VendorId == user.VendorId)
                                    .FirstOrDefaultAsync();
            if (vendor?.NextCheckNum == null)
            {
                throw new Exception("Next check number not found for the vendor.");
            }
            int nextCheckNumber = vendor.NextCheckNum.Value;

            var newChecks = new List<RequestDisbursement>();
            foreach (var requestId in requestIds)
            {
                if (useExistingCheckDict.ContainsKey(requestId))
                {
                    // use existing check
                    var useCheckNumber = useExistingCheckDict[requestId];
                    var existingCheck = await _context.RequestDisbursements
                                                    .AsNoTracking()
                                                    .Where(x => x.RequestId == requestId && x.CheckNumber == useCheckNumber)
                                                    .Where(x => (x.Voided ?? false) == false)
                                                    .FirstOrDefaultAsync();
                    if (existingCheck == null)
                    {
                        existingCheck = new RequestDisbursement()
                        {
                            RequestId = requestId
                        };
                        throw new Exception($"Existing check# {useCheckNumber} not found for request {requestId}");
                    }
                    existingCheck.UseExisting = true;
                    newChecks.Add(existingCheck);
                    continue;
                }
                var disbursement = new RequestDisbursement()
                {
                    RequestId = requestId,
                    Amount = amount,
                    Date = DateTime.Today,
                    CreatedBy = user.UserId,
                    CreatedDate = DateTime.UtcNow,
                    Voided = false,
                    CheckNumber = nextCheckNumber++,
                    PayToName = payToName
                };
                _context.RequestDisbursements.Add(disbursement);
                newChecks.Add(disbursement);
            }
            vendor.NextCheckNum = nextCheckNumber;

            return newChecks;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            throw;
        }
    }
    
    public async Task<List<Dictionary<string, string>>> BuildCheckVariables(List<RequestDisbursement> disbursements, DataHelpers.AppFeeInfo feeinfo, DateTime checkDate, List<CheckMergeData> checks)
    {
        string e13bWrapperCharacter = _configuration.GetValue<string>("BulkPrint:E13BCheckNumberWrapper");
        var result = new List<Dictionary<string, string>>();

        // checks are done 3 per page
        // 1) Determine # of pages required to print checks
        var checkMatrix = GenerateCheckMatrix(checks.Count);

        // 2) determine check position
        // given 5 checks to print, we need 2 pages
        // the first check is in the top position of the first page
        // the second check is in the top position of the second page
        // the third check is in the middle position of the first page
        // the fourth check is in the middle position of the second page
        // the fifth check is in the bottom position of the first page
        // the sixth check is in the bottom position of the second page
        // example
        // checks per page 3
        // check count = 10
        // pages = 4
        // first check on page 1 is position 1
        // second check is on page 2 position 1
        // third check is on page 3 position 1
        // fourth check is on page 4 position 1
        // fifth check is on page 1 position 2
        // sixth check is on page 2 position 2
        // seventh check is on page 3 position 2
        // eighth check is on page 4 position 2
        // ninth check is on page 1 position 3
        // tenth check is on page 2 position 3

        for (int page = 0; page <= checkMatrix.GetUpperBound(0); page++)
        {
            for (int position = 0; position <= checkMatrix.GetUpperBound(1); position++)
            {
                //Console.WriteLine($"{page}, {position} = {checkMatrix[page, position]}");
                int idx = checkMatrix[page, position];
                if (idx != 0)
                {
                    checks[idx - 1].PageNo = page;
                    checks[idx - 1].CheckPosition = position;
                }
            }
        }
        int pageCount = checkMatrix.GetUpperBound(0) + 1;
        int checksPerPage = 3;

        foreach (var check in checks)
        {
            try
            {
                var checkNumber = disbursements.Single(d => d.RequestId == check.RequestId).CheckNumber;
                if (checkNumber == null) throw new Exception($"Check number not found for request {check.RequestNo}");
                check.CheckNumber = checkNumber.Value;
            }
            catch (Exception ex)
            {
                LogError(ex, $"Error in BuildCheckVariables for request {check.RequestNo}");
                throw;
            }
        }

        string strCheckDate = checkDate.ToString("MM/dd/yyyy");

        // 3 ) build the data for the checks
        for (var pageNo = 0; pageNo < pageCount; pageNo++)
        {
            var pageChecks = checks.Where(x => x.PageNo == pageNo).ToList();
            var pageData = new Dictionary<string, string>();

            // void all checks so unused positions are marked as void
            for (var idx = 1; idx <= checksPerPage; idx++)
            {
                pageData[$"CheckNumber{idx}"] = "VOID";
                pageData[$"EncodedCheckNumber{idx}"] = "VOID";
                pageData[$"Date{idx}"] = "VOID";
                pageData[$"Memo{idx}"] = "VOID";
                pageData[$"Amount{idx}"] = "VOID";
                pageData[$"PayeeName{idx}"] = "VOID";
                pageData[$"AmountLonghand{idx}"] = "VOID";
            }

            for (var idx = 1; idx <= checksPerPage; idx++)
            {
                if (idx > pageChecks.Count) break;
                var check = pageChecks[idx - 1];
                if (check.UsePlaceholder)
                {
                    pageData[$"CheckNumber{idx}"] = "VOID";
                    pageData[$"EncodedCheckNumber{idx}"] = "VOID";
                    pageData[$"Date{idx}"] = "VOID";
                    pageData[$"Memo{idx}"] = check.Vin;
                    pageData[$"Amount{idx}"] = "VOID";
                    pageData[$"PayeeName{idx}"] = "VOID";
                    pageData[$"AmountLonghand{idx}"] = $"**** INSERT COPY OF CHECK # {check.CheckNumber} ****";
                    continue;
                }
                pageData[$"Memo{idx}"] = check.Vin;
                pageData[$"Amount{idx}"] = feeinfo.DmvTotal.ToString("C");
                pageData[$"PayeeName{idx}"] = feeinfo.DMVPayToName;
                pageData[$"CheckNumber{idx}"] = string.Format("{0:000000}", check.CheckNumber);
                pageData[$"EncodedCheckNumber{idx}"] = EncodeForE13Bfont(check, e13bWrapperCharacter);
                pageData[$"Date{idx}"] = strCheckDate;
                pageData[$"Memo{idx}"] = check.Vin;
                pageData[$"Amount{idx}"] = check.CheckAmount.ToString("C");
                pageData[$"PayeeName{idx}"] = check.PayeeName;
                pageData[$"AmountLonghand{idx}"] = check.CheckAmountLonghand;
            }
            result.Add(pageData);
        }

        return result;
    }

    private static string EncodeForE13Bfont(CheckMergeData check, string sWrapperCharForCheckNumber)
    {
        sWrapperCharForCheckNumber ??= ";";
        return $"{sWrapperCharForCheckNumber}{string.Format("{0:000000}", check.CheckNumber)}{sWrapperCharForCheckNumber}";
    }

    public static int[,] GenerateCheckMatrix(int checkCount)
    {
        // Calculate the width (Y) and height of the matrix
        int checksPerPage = 3;
        int height = (checkCount + (checksPerPage - 1)) / checksPerPage;

        // Initialize the matrix
        int[,] matrix = new int[height, checksPerPage];

        // Populate the matrix
        int currentNumber = 1;
        for (int col = 0; col < checksPerPage; col++)
        {
            for (int row = 0; row < height; row++)
            {
                if (currentNumber > checkCount) break;
                matrix[row, col] = currentNumber++;
            }
        }

        return matrix;
    }

    public async Task<IActionResult> GetBulkPrintFile(Guid? id)
    {
        try
        {
            var bulkPdfFile = _context.BulkPrintFiles.FirstOrDefault(x => x.BulkPrintFileId == id);

            if (bulkPdfFile == null)
                return BadRequest("File not found.");

            // TBD: return file from storage
            var filedata = await StorageHelpers.GetBlobDataAsync(_configuration.GetValue<string>("BulkPrint:BlobConnectionString"),
                                                                _configuration.GetValue<string>("BulkPrint:BlobEndpointUrl"),
                                                                _configuration.GetValue<string>("BulkPrint:BlobContainerName"),
                                                                $"{bulkPdfFile.BulkPrintFileId}/{bulkPdfFile.Filename}");

            return ReturnFileWithDisposition(bulkPdfFile.Filename, filedata);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in GetBulkPrintFile");
            return BadRequest(ex.Message);
        }
    }
    
    private IActionResult ReturnFileWithDisposition(string filename, byte[] data, System.Net.Mime.ContentDisposition cd = null)
    {
        string contentType = FileHelpers.GetContentType(filename);

        cd ??= new System.Net.Mime.ContentDisposition
        {
            FileName = filename
        };

        Response.Headers.Add("Content-Disposition", cd.ToString());
        return File(data, contentType);
    }

    [HttpPost]
    public async Task<IActionResult> GetApplicationsForMerge([FromForm] string appType, [FromForm] string appState, [FromForm] bool? includeCheckMerge)
    {
        if (appType == null || appState == null)
            return BadRequest("AppType and state required");

        try
        {
            var pdfTemplates = _context.PdfTemplates
                                            .Where(x => x.AppType == appType && x.AppState == appState)
                                            .OrderByVendorSortOrder()
                                            .ToList();
            var model = new ApplicationsForMergeViewModel()
            {
                AppType = appType,
                State = appState,
                IncludeCheckMerge = includeCheckMerge ?? false,
                PdfTemplates = pdfTemplates
            };
            return PartialView("_ApplicationsPrinting", model);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in GetApplicationsForMerge");
            return JsonError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetInfoForCheckMerge([FromForm] string appType, [FromForm] string appState, [FromForm] List<Guid> requestIds)
    {
        if (appType == null || appState == null)
            return BadRequest("AppType and state required");

        try
        {
            var userInfo = await GetCurrentUserAsync();
            var fees = await DataHelpers.GetApplicationFees(userInfo.VendorId, null, appType, appState, null, null);
            if (fees.Count == 0)
            {
                throw new Exception($"Fee information not found for {appType} {appState}");
            }
            if (fees.Count > 1)
            {
                throw new Exception($"Unexpected error: multiple fees found for {appType} {appState}.");
            }
            var fee = fees.FirstOrDefault();

            var checks = await _context.RequestDisbursements
                                    .Where(x => requestIds.Contains(x.RequestId))
                                    .Where(x => (x.Voided ?? false) == false)
                                    .Select(x => new CheckDetailViewModel()
                                    {
                                        RequestId = x.RequestId,
                                        RequestNo = x.Request.Id.ToString(),
                                        VIN = x.Request.Vin,
                                        Amount = x.Amount,
                                        Cleared = x.Cleared ?? false,
                                        ExistingCheckNumber = $"{x.CheckNumber}",
                                        PayToName = x.PayToName
                                    })
                                    .OrderBy(x => x.VIN)
                                    .ToListAsync();
            var model = new CheckMergeViewModel()
            {
                AppType = appType,
                State = appState,
                DMVFee = fee.DmvTotal,
                PayToName = fee.DMVPayToName,
                CheckDetails = checks
            };
            return PartialView("_ChecksPrinting", model);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in GetInfoForCheckMerge");
            return JsonError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetAttachmentsFromRequestIds([FromForm] List<Guid> requestIds)
    {
        if (requestIds == null || requestIds.Count == 0)
            return BadRequest("No requests were selected.");

        try
        {
            var requestAttachments = _context.RequestAttachments
                .Where(x => x.RequestId != null && requestIds.Contains(x.RequestId.Value))
                .Include(x => x.Request)
                .Include(x => x.AttachmentType)
                .Select(x => new
                {
                    x.RequestId,
                    x.AttachmentId,
                    x.AttachmentTypeId,
                    x.Request.AppType,
                    x.Request.State,
                    x.Request.Vin,
                    x.AttachmentType
                })
                .ToList();

            var appStates = requestAttachments.Select(x => x.State).Distinct().ToList();
            var appTypes = requestAttachments.Select(x => x.AppType).Distinct().ToList();

            if (appStates.Count() == 0 || appTypes.Count() == 0)
                return JsonError("The related States and App Types are incorrect. Please, contact the system administrator.");

            if ((appStates.Count() > 1 || appTypes.Count() > 1) || appStates.Count() != appTypes.Count())
                return JsonError("You can only perform a bulk print for requests with same State and App Type.");

            List<MdpAttachmentTypes> result = new();

            if (requestAttachments.Count > 0)
            {
                foreach (var item in requestIds)
                {
                    var xt = requestAttachments
                                    .Where(x => x.RequestId == item
                                                && x.AttachmentType != null
                                                && x.AttachmentType.HardcopyOnly == false
                                                && x.AttachmentType.Name != "Extra")
                                    .Select(x => x.AttachmentType)
                                    .ToList();
                    result.AddRange(xt);
                }
            }
            // Lookup MdpAppTypeAttachments for the AppType and State
            var internalAttachments = _context.MdpAppTypeAttachmentTypes
                                                .Include(x => x.AttachmentType)
                                                .Include(x => x.AppTypeState)
                                                .ThenInclude(x => x.AppType)
                                                .Where(x => x.AppTypeState.AppState == appStates.FirstOrDefault())
                                                .Where(x => x.AppTypeState.AppType.AppType == appTypes.FirstOrDefault())
                                                .Where(x => x.AttachmentType.InternalFromClient || x.AttachmentType.InternalFromVendor)
                                                .Select(x => x.AttachmentType)
                                                .ToList();
            // Add the internal attachments to the list
            result.AddRange(internalAttachments);

            result = result.Distinct().ToList();

            if (result.Count == 0)
                return JsonError("No attachments were found.");

            RequestAttachmentsForPrintingViewModel viewModel = new RequestAttachmentsForPrintingViewModel()
            {
                State = appStates.FirstOrDefault(),
                AppType = appTypes.FirstOrDefault(),
                AttachmentTypes = result
            };

            return PartialView("_AttachmentsPrinting", viewModel);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in GetAttachmentsFromRequestIds");
            return JsonError(ex);
        }
    }

    #endregion endpoints for bulk print user operations 

    #region datatable data retrieval for BulkPrint Applications

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_BulkPrintApplications() => await BulkPrintApplications("ToDo");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_BulkPrintApplications() => await BulkPrintApplications("ToDoWVClearing");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLC_BulkPrintApplications() => await BulkPrintApplications("ToDoLC");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_BulkPrintApplications() => await BulkPrintApplications("ToDoLI");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_BulkPrintApplications() => await BulkPrintApplications("ToDoOther");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_BulkPrintApplications() => await BulkPrintApplications("ToDoTC");

    private async Task<IActionResult> BulkPrintApplications(string vendorQueueName)
    {
        var apptypes = await QueueTypes_Vendor(vendorQueueName);

        return await GetBulkPdfFiles(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<BulkPrintFile> files =
                                            from i in _context.BulkPrintFiles
                                                join u in _context.Users on i.UploadedBy equals u.UserId
                                            where i.IsApplication == true && apptypes.Contains(i.AppType)
                                            select new BulkPrintFile()
                                            {
                                                Id = i.Id,
                                                Filename = i.Filename,
                                                AppType = i.AppType,
                                                AppTypeState = i.AppTypeState,
                                                DateRequested = i.DateRequested,
                                                DateCompleted = i.DateCompleted,
                                                BulkPrintFileId = i.BulkPrintFileId,
                                                RequestCount = i.RequestCount,
                                                IsApplication = i.IsApplication,
                                                IsCheckMerge = i.IsCheckMerge,
                                                UploadedBy = i.UploadedBy,
                                                CreatedBy = u.DisplayName
                                            };

            var result = (from f in files
                          select new BulkPrintFile()
                          {
                              Id = f.Id,
                              Filename = f.Filename,
                              AppType = f.AppType,
                              AppTypeState = f.AppTypeState,
                              DateRequested = f.DateRequested,
                              DateCompleted = f.DateCompleted,
                              BulkPrintFileId = f.BulkPrintFileId,
                              RequestCount = f.RequestCount,
                              IsApplication = f.IsApplication,
                              IsCheckMerge = f.IsCheckMerge,
                              UploadedBy = f.UploadedBy,
                              CreatedBy = f.CreatedBy
                          });

            return result;
        });
    }

    #endregion datatable data retrieval for BulkPrint Applications

    #region datatable data retrieval for BulkPrint Checks

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_BulkPrintChecks() => await BulkPrintChecks("ToDo");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_BulkPrintChecks() => await BulkPrintChecks("ToDoWVClearing");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_BulkPrintChecks() => await BulkPrintChecks("ToDoOther");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_BulkPrintChecks() => await BulkPrintChecks("ToDoLI");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLC_BulkPrintChecks() => await BulkPrintChecks("ToDoLC");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_BulkPrintChecks() => await BulkPrintChecks("ToDoTC");

    [HttpPost]
    public async Task<IActionResult> BulkPrintChecks(string vendorQueueName)
    {
        var apptypes = await QueueTypes_Vendor(vendorQueueName);
        return await GetBulkPdfFiles(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<BulkPrintFile> files =
                                            from i in _context.BulkPrintFiles
                                            join u in _context.Users on i.UploadedBy equals u.UserId
                                            where i.IsCheckMerge == true && apptypes.Contains(i.AppType)
                                            select new BulkPrintFile()
                                            {
                                                Id = i.Id,
                                                Filename = i.Filename,
                                                AppType = i.AppType,
                                                AppTypeState = i.AppTypeState,
                                                DateRequested = i.DateRequested,
                                                DateCompleted = i.DateCompleted,
                                                BulkPrintFileId = i.BulkPrintFileId,
                                                RequestCount = i.RequestCount,
                                                IsApplication = i.IsApplication,
                                                IsCheckMerge = i.IsCheckMerge,
                                                UploadedBy = i.UploadedBy,
                                                CreatedBy = u.DisplayName
                                            };

            var result = (from f in files
                          select new BulkPrintFile()
                          {
                              Id = f.Id,
                              Filename = f.Filename,
                              AppType = f.AppType,
                              AppTypeState = f.AppTypeState,
                              DateRequested = f.DateRequested,
                              DateCompleted = f.DateCompleted,
                              BulkPrintFileId = f.BulkPrintFileId,
                              RequestCount = f.RequestCount,
                              IsApplication = f.IsApplication,
                              IsCheckMerge = f.IsCheckMerge,
                              UploadedBy = f.UploadedBy,
                              CreatedBy = f.CreatedBy
                          });

            return result;
        });
    }

    #endregion datatable data retrieval for BulkPrint Checks

    #region DataTable data retrieval
    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_BulkPrintFiles() => await BulkPrintFiles("ToDo");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_BulkPrintFiles() => await BulkPrintFiles("ToDoWVClearing");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_BulkPrintFiles() => await BulkPrintFiles("ToDoOther");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_BulkPrintFiles() => await BulkPrintFiles("ToDoLI");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLC_BulkPrintFiles() => await BulkPrintFiles("ToDoLC");

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_BulkPrintFiles() => await BulkPrintFiles("ToDoTC");

    [HttpPost]
    public async Task<IActionResult> BulkPrintFiles(string vendorQueueName)
    {
        var apptypes = await QueueTypes_Vendor(vendorQueueName);
        return await GetBulkPdfFiles(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<BulkPrintFile> files =
                                            from i in _context.BulkPrintFiles
                                            where (i.UploadedBy == userId) &&
                                                    ((i.IsApplication ?? false) == false && (i.IsCheckMerge ?? false) == false)
                                                    && apptypes.Contains(i.AppType)
                                            select i;

            var result = (from f in files
                          select new BulkPrintFile()
                          {
                              Id = f.Id,
                              Filename = f.Filename,
                              AppType = f.AppType,
                              AppTypeState = f.AppTypeState,
                              DateRequested = f.DateRequested,
                              DateCompleted = f.DateCompleted,
                              BulkPrintFileId = f.BulkPrintFileId,
                              RequestCount = f.RequestCount
                          });

            return result;
        });
    }
    #endregion

    #region data access helpers
    protected async Task<IActionResult> GetBulkPdfFiles(QueryRecords qr, bool? active = null, bool? filterOnCurrentUser = null)
    {
        try
        {
            if (CurrentUserIdAndGroups(out var vendorId, out var groupId, out var userId))
            {
                if (vendorId != null || groupId != null)
                {
                    DatatableFormData dfd = GetData();

                    IQueryable<BulkPrintFile> bulkPdfs = qr(vendorId, groupId, userId);
                    var data = GetBulkPrintFilesFiltered(bulkPdfs, dfd, out var recordsTotal, out var filters);
                    return Json(new { draw = dfd.draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data, columnFilters = filters });
                }
            }
            return EmptyDataTablesQueryResult();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public List<BulkPrintFile> GetBulkPrintFilesFiltered(IQueryable<BulkPrintFile> bulkPrintFiles, DatatableFormData dfd, out int TotalCount, out object filters)
    {
        foreach (DatatableColumn col in dfd.columns)
        {
            bulkPrintFiles = Where(bulkPrintFiles, col);
        }

        if (dfd.draw == 1 || dfd.skip == 0)
        {
            var oDropdowns = new Dictionary<string, object>();
            try
            {
                oDropdowns["appType"] = bulkPrintFiles.Select(s => s.AppType).Distinct().ToArray().OrderBy(x => x).ToList();
            }
            catch (Exception ex) { };
            try
            {
                oDropdowns["appTypeState"] = bulkPrintFiles.Select(s => s.AppTypeState).Distinct().ToArray().OrderBy(x => x).ToList();
            }
            catch (Exception ex) { };

            filters = oDropdowns;
        }
        else
        {
            filters = null;
        }

        IOrderedQueryable<BulkPrintFile> orderedResults = null;
        for (int i = 0; i < dfd.sort.Count; i++)
        {
            DatatableSort s = dfd.sort[i];
            if (orderedResults == null)
            {
                orderedResults = OrderBy(bulkPrintFiles, s);
            }
            else
            {
                orderedResults = ThenBy(orderedResults, s);
            }
        }
        orderedResults ??= bulkPrintFiles.OrderByDescending(r => r.DateRequested);

        TotalCount = bulkPrintFiles.Count();

        var result = orderedResults.Skip(dfd.start).Take(dfd.length).ToList();
        return result;
    }
    private static IOrderedQueryable<BulkPrintFile> ThenBy(IOrderedQueryable<BulkPrintFile> result, DatatableSort s)
    {
        Expression<Func<BulkPrintFile, object>> selector = GetSelector(s);

        if (s.descending)
        {
            return result.ThenByDescending(selector);
        }
        else
        {
            return result.ThenBy(selector);
        }
    }
    private static Dictionary<string, Expression<Func<BulkPrintFile, object>>> s_selectors = null;

    /// <summary>
    /// s_selectors is used for EntityFramework query building
    /// 
    /// Certain functions are required to execute on the client, 
    /// and as a result can greatly impact performance.
    /// The EF now throws an exception in these cases.
    /// I have changed the VendorChatSort to be done in the 
    /// SQL view vw_RequestStatus_v22
    ///
    /// There may be other cases 
    /// 
    /// </summary>
    private static void InitExpressions()
    {
        s_selectors = new Dictionary<string, Expression<Func<BulkPrintFile, object>>>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "apptype", r => r.AppType },
            { "apptypestate", r => r.AppTypeState},
            { "datecompleted", r => r.DateCompleted },
            { "daterequested", r => r.DateRequested },
            { "filename", r => r.Filename },
            { "uploadedby", r => r.UploadedBy },
            { "bulkprintfileid", r=> r.BulkPrintFileId },
            { "requestcount", r=> r.RequestCount },
            { "id", r => r.Id },
            { "", r => r.DateRequested } // Default field for sort
        };
    }
    private static IOrderedQueryable<BulkPrintFile> OrderBy(IQueryable<BulkPrintFile> result, DatatableSort s)
    {
        Expression<Func<BulkPrintFile, object>> selector = GetSelector(s);

        if (s.descending)
        {
            return result.OrderByDescending(selector);
        }
        else
        {
            return result.OrderBy(selector);
        }
    }
    private static Expression<Func<BulkPrintFile, object>> GetSelector(DatatableSort s)
    {
        Expression<Func<BulkPrintFile, object>> selector = null;
        string colkey = s.columnName.ToLower();

        if (colkey != null && s_selectors.ContainsKey(colkey))
        {
            selector = s_selectors[colkey];
        }
        selector ??= s_selectors[""]; // default
        return selector;
    }

    private IQueryable<BulkPrintFile> Where(IQueryable<BulkPrintFile> result, DatatableColumn col)
    {
        string val = col.searchValue;
        if (string.IsNullOrWhiteSpace(val))
            return result;

        switch (col.name)
        {
            case "apptype":
                result = result.Where(x => x.AppType == col.data);
                break;
            case "state":
                result = result.Where(x => x.AppTypeState == col.data);
                break;
            default:
                System.Diagnostics.Debug.WriteLine("Unhandled filter");
                break;
        }

        return result;
    }
    #endregion data access helpers

}