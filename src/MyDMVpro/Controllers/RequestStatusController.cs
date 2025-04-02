using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyDMVpro.Common;
using MyDMVpro.Common.Extensions;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using MyDMVpro.Models.DocumentsReceived_NoRequest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

//[Route("RequestStatus")]
public class RequestStatusController : SharedBaseController
{
    RequestStatusDbContext _context { get; set; }

    public RequestStatusController(RequestStatusDbContext context, IConfiguration configuration) : base(configuration, null, context)
    {
        _context = context;
    }

    static RequestStatusController()
    {
        InitExpressions();
    }

    #region Master page support

    [HttpPost]
    public async Task<IActionResult> Master()
    {
        //QueryRequests(Guid ? vendorId, Guid ? groupId, Guid ? userId, DatatableFormData dfd)
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) => FilterByUser(vendorId, groupId, userId),
                        active: true,
                        filterOnCurrentUser: false);
    }

    [HttpPost]
    public async Task<IActionResult> Communication()
    {
        var requests = await GetRequests(
            async (vendorId, groupId, userId, dfd) =>
            {
                return FilterByUser(vendorId, groupId, userId)
                    .WhereIsInRequiredStages()
                    .WhereAttachmentsNotMet();
            }, true);

        return requests;
    }

    #endregion Master page support

    #region Multi-page support

    [HttpPost("/RequestStatus/LinkSearchVin/{vin?}")]
    public async Task<IActionResult> LinkSearchVin(string vin)
    {
        if (string.IsNullOrEmpty(vin))
        {
            return EmptyDataTablesQueryResult();
        }
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                                {
                                    var result = FilterByUser(vendorId, groupId, userId);
                                    if (!string.IsNullOrWhiteSpace(vin))
                                    {
                                        result = result.Where(r => r.Vin == vin);
                                    }
                                    else
                                    {
                                        result = result.Where(r => false);
                                    }
                                    return result;
                                },
                        active: true,
                        filterOnCurrentUser: false);
    }

    [HttpPost("/RequestStatus/LinkSearchVin/{vin}/{reqno}")]
    public async Task<IActionResult> LinkSearchVin(string vin, int reqno)
    {
        if (reqno == 0)
        {
            return await LinkSearchVin(vin);
        }
        List<Guid> existingLinked = new();
        var existing = await _context.RequestStatus.Where(r => r.RequestNo == reqno)
                                        .Select(r => new { r.GroupId, r.RequestId })
                                        .SingleOrDefaultAsync();
        if (existing != null)
        {
            existingLinked = await DataHelpers.GetLinkedRequests(existing.RequestId);
        }
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var result = FilterByUser(vendorId, groupId, userId);
                            if (!string.IsNullOrWhiteSpace(vin))
                            {
                                // reqno is the one we want to exclude from the search, as we are attaching to that request
                                if (existingLinked.Count > 0)
                                {
                                    result = result.Where(r => (r.Vin == vin || r.Last6Vin == vin) && !existingLinked.Contains(r.RequestId) && r.RequestNo != reqno && (r.GroupId == existing.GroupId));
                                }
                                else
                                {
                                    result = result.Where(r => (r.Vin == vin || r.Last6Vin == vin) && r.RequestNo != reqno && (r.GroupId == existing.GroupId));
                                }
                            }
                            else
                            {
                                result = result.Where(r => false);
                            }
                            return result;
                        },
                        active: true,
                        filterOnCurrentUser: false);
    }

    [HttpPost("/RequestStatus/LinkSearchReqno/{reqno}")]
    public async Task<IActionResult> LinkSearchReqno(int reqno)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var result = FilterByUser(vendorId, groupId, userId);
                            result = result.Where(r => r.RequestNo == reqno);
                            return result;
                        },
                        active: true,
                        filterOnCurrentUser: false);
    }

    #endregion Multi-page support


    #region FormAnalyzer support

    [HttpPost]
    public async Task<IActionResult> FormAnalyzerMatch()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            // verify filter by vin has been set
                            var col = dfd.columns.Where(c => c.data == "vin").FirstOrDefault();
                            if (col != null && string.IsNullOrEmpty(col.searchValue))
                            {
                                col.searchValue = "XXXXXXXXXXXXXXXXX";// should return 0 records, but need to simplify this to just return 0 records without filtering
                            }
                            return FilterByUser(vendorId, groupId, userId);
                        },
                        active: true,
                        filterOnCurrentUser: false);
    }

    #endregion FormAnalyzer support

    #region FileLibrary support

    [HttpPost]
    public async Task<IActionResult> FileLibraryMatch()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            // verify filter by vin has been set
                            var col = dfd.columns.Where(c => c.data == "vin").FirstOrDefault();
                            if (col != null && string.IsNullOrEmpty(col.searchValue))
                            {
                                col.searchValue = "XXXXXXXXXXXXXXXXX";// should return 0 records, but need to simplify this to just return 0 records without filtering
                            }
                            return FilterByUser(vendorId, groupId, userId);
                        },
                        active: true,
                        filterOnCurrentUser: false);
    }

    #endregion FileLibrary support


    #region General support methods

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private async Task<IActionResult> GetRequests(Func<Guid?, Guid?, Guid?, DatatableFormData, Task<IQueryable<RequestStatus>>> qr, bool? active = null, bool filterOnCurrentUser = false)
    {
        try
        {
            await _context.Database.OpenConnectionAsync();
            CurrentUserInfo userinfo = new();
            if (await CurrentUserIdAndGroupsAsync(userinfo))
            {
                var vendorId = userinfo.VendorId;
                var groupId = userinfo.GroupId;
                var userId = userinfo.UserId;
                if (vendorId != null || groupId != null)
                {
                    DatatableFormData dfd = GetData();

                    if (dfd.notesLength.HasValue && dfd.notesLength > 0)
                    {
                        SetMaxNotesLength(dfd.notesLength.Value);
                    }
                    IQueryable<RequestStatus> requestStatus = await qr(vendorId, groupId, filterOnCurrentUser == true ? userId : null, dfd);
                    if (!dfd.includeArchived)
                    {
                        if (vendorId != null)
                        {
                            requestStatus = requestStatus.WhereNotInArchive(0 - DataHelpers.Archive_Days_Vendor);
                        }
                        else
                        {
                            requestStatus = requestStatus.WhereNotInArchive(0 - DataHelpers.Archive_Days_LH);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(dfd.jsonFilter))
                    {
                        requestStatus = ProcessJsonFilter(requestStatus, dfd.jsonFilter);
                    }

                    var resultsContainer = new GetRequestFilterOutput();
                    var data = await GetRequestsFiltered(requestStatus, dfd, (vendorId != null), resultsContainer);
                    if (dfd.downloadCSV)
                    {
                        if (string.IsNullOrWhiteSpace(dfd.downloadFileName)) dfd.downloadFileName = "file.csv";
                        List<CsvColumn> columns = JsonConvert.DeserializeObject<List<CsvColumn>>(dfd.jsonColumns);
                        return ReturnCsvFile(dfd.downloadFileName, columns, data);
                    }
                    return Json(new
                    {
                        status = "success",
                        dfd.draw,
                        recordsFiltered = resultsContainer.TotalCount,
                        recordsTotal = resultsContainer.TotalCount,
                        data,
                        columnFilters = resultsContainer.Filters
                    });
                }
            }
            return EmptyDataTablesQueryResult();
        }
        catch (Exception ex)
        {
            LogError(ex, "GetRequests");
            throw;
        }
        finally
        {
            _context.Database.CloseConnection();
        }
    }

    public class JsonFilterItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public IQueryable<RequestStatus> ProcessJsonFilter(IQueryable<RequestStatus> requestStatus, string json)
    {
        List<JsonFilterItem> filterItems = JsonConvert.DeserializeObject<List<JsonFilterItem>>(json);

        foreach (var obj in filterItems)
        {
            if (IsDateRange(obj.Name.ToLower(), obj.Value, out DateRange range))
            {
                requestStatus = requestStatus.Where(GetDateRangeExpression(obj.Name, range));
            }
            else
            {
                requestStatus = requestStatus.Where(GetIsEqualExpression(obj.Name, obj.Value));
            }
        }
        return requestStatus;
    }

    public class GetRequestFilterOutput
    {
        public int TotalCount { get; set; }
        public object Filters { get; set; }
    }
    public static bool ColumnInView(List<DatatableColumn> columns, string columnName)
    {
        foreach (DatatableColumn col in columns)
        {
            if (string.Equals(col.data, columnName, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }
        return false;
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
    static string CoalesceToEmptyString(string arg)
    {
        return arg ?? "";
    }
    private static List<object> GetDropdownValues(IQueryable<RequestStatus> requestStatus, string colName)
    {
        try
        {
            Expression<Func<RequestStatus, object>> selector = null;
            selector = CoalesceExpression(colName, null);

            return requestStatus.Select(selector)
                .Distinct()
                .ToArray()
                .OrderBy(x => x)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            return new List<object>();
        }
    }
    static readonly string[] s_filterColumns = new string[] {
        "dateToVendor",
        "appType",
        "groupName",
        "auction",
        "code",
        "courier",
        "vehicleYear",
        "vehicleMake",
        "state",
        "dmvCourier",
        "lienholderName",
        "dT_LH_Name",
        "rT_LH",
        "submittedBy",
        "toVendorCourier",
        "vendorCode",
        "elt",
        "eta",
        "dateFromDmv",
        "datePrinted",
        "dateReceived",
        "dateShipped",
        "dateSigned",
        "dateTitleIssued",
        "dateToDmv",
        "processStageId",
        "statusId",
        "lienExpDate",
        "invoiceDate",
        "invoiceDatePaid",
        //"checkNumber", // changed to asearch instead of afilter
        "rejectionDate",
        "lI_DateToDmv",
        "lI_DateFromDmv",
        //"lI_CheckNumber", // changed to asearch instead of afilter
        "lI_ToDmvCourier",
        "lI_ToDmvTracking"
#if USE_LAST_MODIFIED_FILTER
        ,"lastModified"
#endif
        ,"lastModifiedBy",
        "reG_LH_Name",
        "reG_Reg_Name"
    };
    public async Task<List<RequestStatus>> GetRequestsFiltered(IQueryable<RequestStatus> requestStatus, DatatableFormData dfd, bool isVendor, GetRequestFilterOutput outputResults)
    {
        try
        {
            foreach (DatatableColumn col in dfd.columns)
            {
                requestStatus = Where(requestStatus, col);
            }

            // search by contains
            requestStatus = Search(requestStatus, dfd.columns, dfd.globalSearch);

            if (!dfd.downloadCSV && (dfd.draw == 1 || dfd.skip == 0))
            {
                var oDropdowns = new Dictionary<string, object>();
                if (_configuration.GetValue<bool>("AppSettings:PopulateDropdownsOnLoad", false))
                {
                    if (_configuration.GetValue<bool>("AppSettings:UseDropdownProc", false))
                    {
                        outputResults.Filters = await QueryDropdownsWithStoredProcedure(requestStatus, dfd);
                    }
                    else
                    {
                        QueryDropdowns(requestStatus, oDropdowns, dfd);
                        outputResults.Filters = oDropdowns;
                    }
                }
            }
            else
            {
                outputResults.Filters = null;
            }

            IOrderedQueryable<RequestStatus> orderedResults = null;
            for (int i = 0; i < dfd.sort.Count; i++)
            {
                DatatableSort s = dfd.sort[i];
                if (isVendor)
                {
                    if (s.columnName.ToLower() == "chatsort")
                    {
                        s.columnName = "vendorchatsort";
                    }
                }
                if (orderedResults == null)
                {
                    orderedResults = OrderBy(requestStatus, s);
                }
                else
                {
                    orderedResults = ThenBy(orderedResults, s);
                }
            }
            if (orderedResults == null)
            {
                orderedResults = requestStatus.OrderBy(r => r.Last6Vin);
            }
            //orderedResults ??= requestStatus.OrderBy(r => r.Last6Vin);
            outputResults.TotalCount = orderedResults.Count();

            var result = orderedResults.Skip(dfd.start);
            if (dfd.length != 0)
            {
                result = result.Take(dfd.length);
            }
            return result.ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            throw;
        }
    }
    public async Task<DropDownListCollection> QueryDropdownsWithStoredProcedure(IQueryable<RequestStatus> requestStatus, DatatableFormData dfd)
    {
        // take the generated SELECT STATEMENT and insert an INSERT INTO #Request 
        var sqlQuery = requestStatus.Select(r => new { r.RequestNo }).Distinct().ToQueryString();
        return await GetDropdowns(sqlQuery, dfd);
    }
    public async Task<DropDownListCollection> GetDropdowns(string sqlSelect, DatatableFormData dfd)
    {
        bool useTempTable = ConfigurationHelper.Configuration.GetValue<bool>("AppSettings:GetDropdownsSproc_UseTempTable", false);
        string sprocName;

        if (useTempTable)
        {
            sprocName = ConfigurationHelper.Configuration.GetValue<string>("AppSettings:GetDropdownsSprocName_TempTable")
                                    ?? "dbo.usp_Requests_GetDropDowns_tempTable";
        }
        else
        {
            sprocName = ConfigurationHelper.Configuration.GetValue<string>("AppSettings:GetDropdownsSprocName_udt")
                                    ?? "dbo.usp_Requests_GetDropDowns_udt";
        }

        StringBuilder sb = new(sqlSelect);
        StringBuilder sb1 = new();

        if (useTempTable)
        {
            sb1.AppendLine("CREATE TABLE #Request(id int);");
            sb1.AppendLine("INSERT INTO #Request(id)");
            sb1.Append("SELECT");
        }
        else
        {
            sb1.AppendLine("DECLARE @utRequestId dbo.udtId;");
            sb1.AppendLine("INSERT INTO @utRequestId(id)");
            sb1.Append("SELECT");
        }

        sb.Replace("SELECT", sb1.ToString());
        sb.AppendLine("");
        sb.AppendLine("DECLARE @jsonParams nvarchar(max);");

        string dropdownlist = "";
        var dropdowns = GetDropdownNames(dfd);

        GetRequestStatusV2Params param = new GetRequestStatusV2Params()
        {
            DropDowns = dropdowns
        };
        string dropdownParam = JsonConvert.SerializeObject(param);
        sb.AppendLine($"SET @jsonParams = @dropdownParam;");

        if (useTempTable)
        {
            sb.AppendLine($"EXEC {sprocName} @jsonParams");
        }
        else
        {
            sb.AppendLine($"EXEC {sprocName} @jsonParams, @utRequestId");
        }

        var result = await DataHelpers.GetRequestStatusDropDowns(sb.ToString(), dropdownParam);

        var keys = result.Keys;
        for (int i = 0; i < keys.Count; i++)
        {
            string originalkey = keys.ElementAt(i);
            string key = JsonNameToViewName(originalkey);
            if (originalkey != null && key != originalkey)
            {
                List<string> values = result[originalkey];
                result.Remove(originalkey);
                result.Add(key, values);
            }
        }

        return result;
    }
    //public static bool ChangeKey<TKey, TValue>(this IDictionary<TKey, TValue> dict,
    //                                   TKey oldKey, TKey newKey)
    //{
    //    TValue value;
    //    if (!dict.Remove(oldKey, out value))
    //        return false;

    //    dict[newKey] = value;  // or dict.Add(newKey, value) depending on ur comfort
    //    return true;
    //}
    public List<string> GetDropdownNames(DatatableFormData dfd)
    {
        List<string> result = new();
        foreach (string colname in s_filterColumns)
        {
            try
            {
                if (ColumnInView(dfd.columns, colname) && (dfd.FilterList != null && dfd.FilterList.Contains(colname)))
                {
                    result.Add(ViewNameToJsonName(colname));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
        }
        return result;
    }
    private string ViewNameToJsonName(string colname)
    {
        switch (colname.ToLower())
        {
            case "auction": return "auctioneer";
            case "rt_lh": return "rT-LH";
            case "dt_lh_name": return "dT-LH-Name";
            case "vehicleyear": return "Vehicle Year";
            case "vehiclemake": return "Vehicle Make";
            case "lienholdername": return "Lien Holder Name";
            case "reg_lh_name": return "reG_LH_Name";
            case "reg_reg_name": return "reG_Reg_Name";
            default: return colname;
        }
    }

    private string JsonNameToViewName(string colname)
    {
        switch (colname.ToLower())
        {
            case "auctioneer": return "auction";
            case "rt-lh": return "rT_LH";
            case "dt-lh-name": return "dT_LH_Name";
            case "vehicle year": return "vehicleYear";
            case "vehicle make": return "vehicleMake";
            case "lien holder name": return "lienholderName";
            case "reg_lh_name": return "reG_LH_Name";
            case "reg_reg_name": return "reG_Reg_Name";
            default: return colname;
        }
    }
    public void QueryDropdowns(IQueryable<RequestStatus> requestStatus, Dictionary<string, object> ddValues, DatatableFormData dfd)
    {
        DateTime start = DateTime.Now;

        foreach (string colname in s_filterColumns)
        {
            try
            {
                if (ColumnInView(dfd.columns, colname) && (dfd.FilterList != null && dfd.FilterList.Contains(colname)))
                {
                    ddValues[colname] = GetDropdownValues(requestStatus, colname);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
        }
        DateTime end = DateTime.Now;
        System.Diagnostics.Trace.WriteLine($"GetDropdownValues took {(end - start).TotalMilliseconds}ms");
    }
    public IQueryable<RequestStatus> Search(IQueryable<RequestStatus> result, List<DatatableColumn> cols, string searchVal)
    {
        if (string.IsNullOrWhiteSpace(searchVal))
            return result;

        var predicate = PredicateBuilder.False<RequestStatus>();
        bool isDate = DateTime.TryParse(searchVal, out DateTime dtCompare);
        bool isBool = Boolean.TryParse(searchVal, out bool bCompare);
        bool isInt = Int32.TryParse(searchVal, out int iCompare);
        bool isGuid = Guid.TryParse(searchVal, out Guid gCompare);

        foreach (DatatableColumn col in cols)
        {
            if (col.searchable)
            {
                switch (col.data)
                {
                    case "apptype": predicate = predicate.Or(p => p.AppType.Equals(searchVal)); break;
                    case "attachmentcount": predicate = predicate.Or(p => isInt && p.AttachmentCount.Equals(iCompare)); break;
                    case "attachmentstatus": predicate = predicate.Or(p => isInt && p.AttachmentStatus.Equals(iCompare)); break;
                    case "auction": predicate = predicate.Or(p => p.Auction.Equals(searchVal)); break;
                    case "chatsort": predicate = predicate.Or(p => isInt && p.ChatSort.Equals(iCompare)); break;
                    case "clientref": predicate = predicate.Or(p => p.ClientRef.Contains(searchVal)); break;
                    case "courier": predicate = predicate.Or(p => p.Courier.Contains(searchVal)); break;
                    case "courierid": predicate = predicate.Or(p => isGuid && p.CourierId.Equals(gCompare)); break;
                    case "datefromdmv": predicate = predicate.Or(p => isDate && p.DateFromDmv.Equals(dtCompare)); break;
                    case "dateprinted": predicate = predicate.Or(p => isDate && p.DatePrinted.Equals(dtCompare)); break;
                    case "dateshipped": predicate = predicate.Or(p => isDate && p.DateShipped.Equals(dtCompare)); break;
                    case "datesigned": predicate = predicate.Or(p => isDate && p.DateSigned.Equals(dtCompare)); break;
                    case "datetitleissued": predicate = predicate.Or(p => isDate && p.DateTitleIssued.Equals(dtCompare)); break;
                    case "datetodmv": predicate = predicate.Or(p => isDate && p.DateToDmv.Equals(dtCompare)); break;
                    case "datetovendor": predicate = predicate.Or(p => isDate && p.DateToVendor.Equals(dtCompare)); break;
                    case "directtovendor": predicate = predicate.Or(p => isBool && p.DirectToVendor.Equals(bCompare)); break;
                    case "dmvcourier": predicate = predicate.Or(p => p.DmvCourier.Contains(searchVal)); break;
                    case "dmvcourierid": predicate = predicate.Or(p => isGuid && p.DmvCourierId.Equals(gCompare)); break;
                    case "dmvtrackingnumber": predicate = predicate.Or(p => p.DmvTrackingNumber.Contains(searchVal)); break;
                    case "todmvtrackingnumber": predicate = predicate.Or(p => p.ToDmvTrackingNumber.Contains(searchVal)); break;
                    case "elt": predicate = predicate.Or(p => p.ELT.Contains(searchVal)); break;
                    case "eta": predicate = predicate.Or(p => p.Eta.Equals(searchVal)); break;
                    case "lh_eta": predicate = predicate.Or(p => p.lh_eta.Equals(searchVal)); break;
                    case "groupname": predicate = predicate.Or(p => p.GroupName.Contains(searchVal)); break;
                    case "hasactivechat": predicate = predicate.Or(p => isBool && p.HasActiveChat.Equals(bCompare)); break;
                    case "hasnewchat": predicate = predicate.Or(p => isBool && p.HasNewChat.Equals(bCompare)); break;
                    case "lienholdername": predicate = predicate.Or(p => p.LienholderName.Equals(searchVal)); break;
                    case "dt_lh_name": predicate = predicate.Or(p => p.DT_LH_Name.Equals(searchVal)); break;
                    case "rt_lh": predicate = predicate.Or(p => p.RT_LH.Equals(searchVal)); break;
                    case "lienexpdate": predicate = predicate.Or(p => p.LienExpDate.Equals(dtCompare)); break;
                    case "notesabbrev": predicate = predicate.Or(p => p.NotesAbbrev.Contains(searchVal)); break;
                    case "odometer": predicate = predicate.Or(p => p.Odometer.Equals(searchVal)); break;
                    case "processstageid": predicate = predicate.Or(p => isInt && p.ProcessStageId.Equals(iCompare)); break;
                    case "processstagename": predicate = predicate.Or(p => p.ProcessStageName.Equals(searchVal)); break;
                    case "processingday": predicate = GetProcessingDayPredicate(searchVal, predicate); break;
                    case "requestno": predicate = predicate.Or(p => isInt && p.RequestNo.Equals(iCompare)); break;
                    case "reasoncancelled": predicate = predicate.Or(p => p.ReasonCancelled.Contains(searchVal)); break;
                    case "state": predicate = predicate.Or(p => p.State.Contains(searchVal)); break;
                    case "statusid": predicate = predicate.Or(p => isInt && p.StatusId.Equals(iCompare)); break;
                    case "submittedby": predicate = predicate.Or(p => p.SubmittedBy.Equals(searchVal)); break;
                    case "tovendorcourier": predicate = predicate.Or(p => p.ToVendorCourier.Equals(searchVal)); break;
                    case "tovendortracking": predicate = predicate.Or(p => p.ToVendorTracking.Equals(searchVal)); break;
                    case "trackingnumber": predicate = predicate.Or(p => p.TrackingNumber.Contains(searchVal)); break;
                    case "vehiclemake": predicate = predicate.Or(p => p.VehicleMake.Equals(searchVal)); break;
                    case "vehicleyear": predicate = predicate.Or(p => p.VehicleYear.Equals(searchVal)); break;
                    case "vendorcode": predicate = predicate.Or(p => p.VendorCode.Equals(searchVal)); break;
                    case "vendorid": predicate = predicate.Or(p => isGuid && p.VendorId.Equals(gCompare)); break;
                    case "vin":
                        {
                            if (searchVal.IndexOfAny(new char[] { '*', '?' }) > -1)
                            {
                                // replace ? and * with _ and %
                                searchVal = searchVal.Replace("*", "%").Replace("?", "_");
                                // already contains LIKE operators
                                predicate = predicate.Or(p => EF.Functions.Like(p.Vin, searchVal));
                            }
                            else if (searchVal.Length == 6)
                                predicate = predicate.Or(p => p.Last6Vin.Equals(searchVal));
                            else if (searchVal.Length == 17)
                                predicate = predicate.Or(p => p.Vin.Equals(searchVal));
                            else if (searchVal.Length > 2)
                            {
                                string likeSearch = $"%{searchVal}%";
                                predicate = predicate.Or(p => EF.Functions.Like(p.Vin, searchVal));
                            }
                        }
                        break;
                    case "waitingforuserreply": predicate = predicate.Or(p => isBool && p.WaitingForUserReply.Equals(bCompare)); break;
                    case "waitingforvendorreply": predicate = predicate.Or(p => isBool && p.WaitingForVendorReply.Equals(bCompare)); break;
                    /* INVOICE DETAILS */
                    case "invoiceno": predicate = predicate.Or(p => p.InvoiceNo == searchVal); break;
                    case "invoicedate": predicate = predicate.Or(p => isDate && p.InvoiceDate.Equals(dtCompare)); break;
                    case "invoicedatepaid": predicate = predicate.Or(p => isDate && p.InvoiceDatePaid.Equals(dtCompare)); break;
                    case "lastmodified": predicate = predicate.Or(p => isDate && p.LastModified.Value.Date.Equals(dtCompare)); break;
                    case "lastmodifiedby": predicate = predicate.Or(p => p.LastModifiedBy.Equals(searchVal)); break;
                    case "reg_reg_name": predicate = predicate.Or(p => p.REG_Reg_Name.Equals(searchVal)); break;
                    case "reg_lh_name": predicate = predicate.Or(p => p.REG_LH_Name.Equals(searchVal)); break;
                        //case "servicefee": predicate = predicate.Or(p => p.ServiceFee == searchVal); break;
                        //case "dmvfee": predicate = predicate.Or(p => p.DmvFee == searchVal); break;
                        //case "otherfee": predicate = predicate.Or(p => p.OtherFee== searchVal); break;
                        //case "otherdesc": predicate = predicate.Or(p => p.OtherDesc == searchVal); break;
                        //case "totaldue": predicate = predicate.Or(p => p.TotalDue == searchVal); break;
                }
            }
        }
        return result.Where(predicate);
    }
    private Expression<Func<RequestStatus, bool>> GetProcessingDayPredicate(string? searchVal, Expression<Func<RequestStatus, bool>> predicate = null)
    {
        predicate ??= PredicateBuilder.False<RequestStatus>();

        if (searchVal != null)
        {
            searchVal = searchVal.TrimStart('^').TrimEnd('$');

            if (int.TryParse(searchVal, out int val))
            {
                // The searchVal has been successfully converted to a ProcessingDayEnum type.
                // You can now use the 'val' variable in your code.
                // For example, you can add a condition to the predicate based on the converted value:
                predicate = predicate.Or(rs => (rs.ProcessingDay & val) == val);
            }
        }
        // return the modified predicate
        return predicate;
    }
    protected override bool IsDateColumn(string colName)
    {
        switch (colName)
        {
            case "eta": break;
            case "datefromdmv": break;
            case "dateprinted": break;
            case "dateshipped": break;
            case "datereceived": break;
            case "datesigned": break;
            case "datetodmv": break;
            case "datetovendor": break;
            case "datetitleissued": break;
            case "lienexpdate": break;
            case "invoicedate": break;
            case "invoicedatepaid": break;
            case "lastmodified": break;
            default:
                return false;
        }
        return true;
    }

    private IQueryable<RequestStatus> Where(IQueryable<RequestStatus> result, DatatableColumn col)
    {
        string colName = col.data.ToLower();
        string val = col.searchValue;
        if (string.IsNullOrWhiteSpace(val))
            return result;

        bool useLike = false;

        if (col.searchRegex)
        {
            // can't handle regex with SQL, so handle simple ones
            if (val.StartsWith("^") && val.EndsWith("$"))
            {
                if (val.Length == 2)
                    val = "";
                else
                    val = val[1..^1];
                col.searchValue = val;
                col.searchRegex = false;
            }
            else if (val.StartsWith("^"))
            {
                useLike = true;
                val += "%";
            }
            else if (val.EndsWith("$"))
            {
                useLike = true;
                val = "%" + val;
            }
        }
        if (useLike)
        {
            throw new InvalidOperationException($"Like expression used: {val}");
        }

        if (IsDateRange(colName.ToLower(), val, out DateRange range))
        {
            result = result.Where(GetDateRangeExpression(colName, range));
        }
        else
        {
            List<string> multipleValues = GetMultipleValues(val);
            if (multipleValues.Count == 1)
            {
                string singleVal = multipleValues[0];
                const string c_empty = "(empty)";
                const string c_notempty = "(not empty)";
                bool matchNull = false;
                bool matchNotNull = false;
                if (singleVal.Equals(c_empty, StringComparison.InvariantCultureIgnoreCase))
                {
                    singleVal = "";
                    matchNull = true;
                }
                else if (singleVal.Equals(c_notempty, StringComparison.InvariantCultureIgnoreCase))
                {
                    singleVal = "";
                    matchNotNull = true;
                }

                if (matchNotNull)
                {
                    result = result.Where(GetSelectorIsNotNullOrBlank(col.data));
                }
                else if (matchNull)
                {
                    result = result.Where(GetSelectorIsNullOrBlank(col.data));
                }
                else
                {
                    string colname = col.data;
                    if (col.data == "vin")
                    {
                        if (singleVal.IndexOfAny(new char[] { '*', '?' }) > -1)
                        {
                            // replace ? and * with _ and %
                            singleVal = singleVal.Replace("*", "%").Replace("?", "_");
                            if (!singleVal.StartsWith("%") && !singleVal.EndsWith("%"))
                            {
                                singleVal = $"%{singleVal}%";
                            }
                            // already contains LIKE operators
                            result = result.Where(p => EF.Functions.Like(p.Vin, singleVal));
                        }
                        else if (singleVal.Length == 6)
                            result = result.Where(p => p.Last6Vin.Equals(singleVal));
                        else if (singleVal.Length == 17)
                            result = result.Where(p => p.Vin.Equals(singleVal));
                        else if (singleVal.Length > 2)
                        {
                            string likeSearch = $"%{singleVal}%";
                            result = result.Where(p => EF.Functions.Like(p.Vin, likeSearch));
                        }
                    }
                    else if (col.data == "notesabbrev")
                    {
                        singleVal = EscapeLikePattern(singleVal); // escape
                        string likeSearch;
                        // USE SetNotesFilter stored procedure before query to 
                        // handle filtering
                        if (singleVal.IndexOfAny(new char[] { '*', '?' }) > -1)
                        {
                            bool prependPercent = true;
                            bool appendPercent = true;
                            if (singleVal.StartsWith("*") || singleVal.StartsWith("?"))
                            {
                                prependPercent = false;
                            }
                            if (singleVal.EndsWith("*") || singleVal.EndsWith("?"))
                            {
                                appendPercent = false;
                            }
                            // replace ? and * with _ and %
                            likeSearch = singleVal.Replace("*", "%").Replace("?", "_");
                            if (prependPercent) likeSearch = "%" + likeSearch;
                            if (appendPercent) likeSearch += "%";
                        }
                        else
                        {
                            likeSearch = $"%{EscapeLikePattern(singleVal)}%";
                        }
                        SetNotesFilter(likeSearch);
                    }
                    else if (col.data == "reasoncancelled")
                    {
                        singleVal = EscapeLikePattern(singleVal); // escape
                        string likeSearch = $"%{singleVal}%";
                        result = result.Where(p => EF.Functions.Like(p.ReasonCancelled, likeSearch));
                    }
                    else if (col.data == "processingday")
                    {
                        if (!string.IsNullOrEmpty(singleVal))
                        {
                            var predicate = GetProcessingDayPredicate(singleVal);
                            result = result.Where(predicate);
                        }
                    }
                    else
                    {
                        result = result.Where(GetIsEqualExpression(colname, singleVal));
                    }
                }
            }
            else
            {
                string firstVal = multipleValues[0];
                switch (colName)
                {
                    case "apptype": result = result.Where(r => multipleValues.Contains(r.AppType)); break;
                    //case "attachmentcount": result = result.Where(r => r.AttachmentCount == Convert.ToInt32(singleVal)); break;
                    case "attachmentstatus": result = result.Where(r => multipleValues.ToIntList().Contains(r.AttachmentStatus)); break;
                    case "auction": result = result.Where(r => multipleValues.Contains(r.Auction)); break;
                    case "borrowername": result = result.Where(r => multipleValues.Contains(r.BorrowerName)); break;
                    case "chatsort": result = result.Where(r => multipleValues.ToIntList().Contains(r.ChatSort)); break;
                    case "clientref": result = result.Where(r => multipleValues.Contains(r.ClientRef)); break;
                    case "code": result = result.Where(r => multipleValues.Contains(r.Code)); break;
                    case "courier": result = result.Where(r => multipleValues.Contains(r.Courier)); break;
                    case "courierid": result = result.Where(r => multipleValues.ToIntList().Contains(r.CourierId)); break;
                    case "datefromdmv": result = result.Where(r => multipleValues.ToDateList().Contains(r.DateFromDmv)); break;
                    case "dateprinted": result = result.Where(r => multipleValues.ToDateList().Contains(r.DatePrinted)); break;
                    case "dateshipped": result = result.Where(r => multipleValues.ToDateList().Contains(r.DateShipped)); break;
                    case "datereceived": result = result.Where(r => multipleValues.ToDateList().Contains(r.DateReceived)); break;
                    case "datesigned": result = result.Where(r => multipleValues.ToDateList().Contains(r.DateSigned)); break;
                    case "datetodmv": result = result.Where(r => multipleValues.ToDateList().Contains(r.DateToDmv)); break;
                    case "datetovendor": result = result.Where(r => multipleValues.ToDateList().Contains(r.DateToVendor)); break;
                    case "datetitleissued": result = result.Where(r => multipleValues.ToDateList().Contains(r.DateTitleIssued)); break;
                    case "directtovendor": result = result.Where(r => multipleValues.ToBoolList().Contains(r.DirectToVendor)); break;
                    case "dmvcourier": result = result.Where(r => multipleValues.Contains(r.DmvCourier)); break;
                    case "dmvcourierid": result = result.Where(r => multipleValues.ToIntList().Contains(r.DmvCourierId)); break;
                    case "dmvtrackingnumber": result = result.Where(r => multipleValues.Contains(r.DmvTrackingNumber)); break;
                    case "todmvtrackingnumber": result = result.Where(r => multipleValues.Contains(r.ToDmvTrackingNumber)); break;
                    case "elt": result = result.Where(r => multipleValues.Contains(r.ELT)); break;
                    case "eta": result = result.Where(r => multipleValues.ToDateList().Contains(r.Eta)); break;
                    case "lh_eta": result = result.Where(r => multipleValues.ToDateList().Contains(r.lh_eta)); break;
                    case "groupname": result = result.Where(r => multipleValues.Contains(r.GroupName)); break;
                    case "hasactivechat": result = result.Where(r => multipleValues.ToBoolList().Contains(r.HasActiveChat)); break;
                    case "hasnewchat": result = result.Where(r => multipleValues.ToBoolList().Contains(r.HasNewChat)); break;
                    case "lienholdername": result = result.Where(r => multipleValues.Contains(r.LienholderName)); break;
                    case "dt_lh_name": result = result.Where(r => multipleValues.Contains(r.DT_LH_Name)); break;
                    case "rt_lh": result = result.Where(r => multipleValues.Contains(r.RT_LH)); break;
                    case "lienexpdate": result = result.Where(r => multipleValues.ToDateList().Contains(r.LienExpDate)); break;
                    case "notesabbrev": result = result.Where(p => EF.Functions.Like(p.NotesAbbrev, EscapeLikePattern(firstVal))); break;
                    case "odometer": result = result.Where(r => multipleValues.Contains(r.Odometer)); break;
                    case "pa_titleno": result = result.Where(r => multipleValues.Contains(r.PA_TitleNo)); break;
                    case "pa_lrbol": result = result.Where(r => multipleValues.Contains(r.PA_LRBOL)); break;
                    case "processstageid": result = result.Where(r => multipleValues.ToIntList().Contains(r.ProcessStageId)); break;
                    case "processstagename": result = result.Where(r => multipleValues.Contains(r.ProcessStageName)); break;
                    case "reasoncancelled": result = result.Where(p => EF.Functions.Like(p.ReasonCancelled, EscapeLikePattern(firstVal))); break;
                    case "requestno": result = result.Where(r => multipleValues.ToIntList().Contains(r.RequestNo)); break;
                    case "state": result = result.Where(r => multipleValues.Contains(r.State)); break;
                    case "statusid": result = result.Where(r => multipleValues.ToIntList().Contains(r.StatusId)); break;
                    case "submittedby": result = result.Where(r => multipleValues.Contains(r.SubmittedBy)); break;
                    case "tovendorcourier": result = result.Where(r => multipleValues.Contains(r.ToVendorCourier)); break;
                    case "tovendortracking": result = result.Where(r => multipleValues.Contains(r.ToVendorTracking)); break;
                    case "trackingnumber": result = result.Where(r => multipleValues.Contains(r.TrackingNumber)); break;
                    case "vehiclemake": result = result.Where(r => multipleValues.Contains(r.VehicleMake)); break;
                    case "vehicleyear": result = result.Where(r => multipleValues.Contains(r.VehicleYear)); break;
                    case "vendorcode": result = result.Where(r => multipleValues.Contains(r.VendorCode)); break;
                    case "vendorid": result = result.Where(r => multipleValues.ToGuidList().Contains(r.VendorId)); break;
                    case "vin":
                        if (firstVal.Length == 6)
                        {
                            result = result.Where(r => multipleValues.Contains(r.Last6Vin));
                        }
                        else
                        {
                            result = result.Where(r => multipleValues.Contains(r.Vin));
                        }
                        break;
                    case "waitingforuserreply": result = result.Where(r => multipleValues.ToBoolList().Contains(r.WaitingForUserReply)); break;
                    case "waitingforvendorreply": result = result.Where(r => multipleValues.ToBoolList().Contains(r.WaitingForVendorReply)); break;
                    case "invoicedate": result = result.Where(r => multipleValues.ToDateList().Contains(r.InvoiceDate)); break;
                    case "invoicedatepaid": result = result.Where(r => multipleValues.ToDateList().Contains(r.InvoiceDatePaid)); break;
                    case "lastmodified": result = result.Where(r => r.LastModified.HasValue && multipleValues.ToDateList(true).Contains(r.LastModified.Value.Date)); break;
                    case "lastmodifiedby": result = result.Where(r => multipleValues.Contains(r.LastModifiedBy)); break;
                    case "reg_reg_name": result = result.Where(r => multipleValues.Contains(r.REG_Reg_Name)); break;
                    case "reg_lh_name": result = result.Where(r => multipleValues.Contains(r.REG_LH_Name)); break;
                    default:
                        System.Diagnostics.Trace.WriteLine("Unhandled sort");
                        break;
                }
            }
        }
        return result;
    }

    private static string EscapeLikePattern(string val)
    {
        // REPLACE(REPLACE(REPLACE(@myString, '[', '[[]'), '_', '[_]'), '%', '[%]')
        return val.Replace("[", "[[]").Replace("_", "[_]").Replace("%", "[%]");
    }
    /* 
     * 
        NotesAbbrev
        PA_TitleNo
        PA_LRBOL
     * */
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
    private static readonly List<int> s_vendorChatSort = new(new int[] { 0, 1, 3, 2 });

    private static Dictionary<string, Expression<Func<RequestStatus, object>>> s_selectors = null;

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
        s_selectors = new Dictionary<string, Expression<Func<RequestStatus, object>>>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "apptype", r => r.AppType },
            { "attachmentcount", r => r.AttachmentCount },
            { "attachmentstatus", r => r.AttachmentStatus },
            { "auction", r => r.Auction },
            { "chatsort", r => r.ChatSort },
            { "vendorchatsort", r => r.VendorChatSort },
            { "clientref", r => r.ClientRef },
            { "code", r => r.Code },
            { "courier", r => r.Courier },
            { "courierid", r => r.CourierId },
            { "datefromdmv", r => r.DateFromDmv },
            { "dateprinted", r => r.DatePrinted },
            { "datereceived", r => r.DateReceived },
            { "dateshipped", r => r.DateShipped },
            { "datesigned", r => r.DateSigned },
            { "datetodmv", r => r.DateToDmv },
            { "datetovendor", r => r.DateToVendor },
            { "datetitleissued", r => r.DateTitleIssued },
            { "directtovendor", r => r.DirectToVendor },
            { "dmvcourierid", r => r.DmvCourierId },
            { "dmvcourier", r => r.DmvCourier },
            { "dmvtrackingnumber", r => r.DmvTrackingNumber },
            { "todmvtrackingnumber", r => r.ToDmvTrackingNumber },
            { "elt", r => r.ELT },
            { "eta", r => r.Eta },
            { "lh_eta", r => r.lh_eta },
            { "groupname", r => r.GroupName },
            { "hasactivechat", r => r.HasActiveChat },
            { "hasnewchat", r => r.HasNewChat },
            { "lienholdername", r => r.LienholderName },
            { "dt_lh_name", r => r.DT_LH_Name },
            { "rt_lh", r => r.RT_LH },
            { "lienexpdate", r => r.LienExpDate },
            // Odometer is held as a string, and attempting to convert to an integer is
            // problematic since it may throw an exception
            // Disabling the conversion to integer until we can find a way to get
            // the EF to use TRY_CONVERT to do the numeric conversion
            // (the below does not work, because IsNumeric returns true for things that won't convert to Int32)
            //
            // => EF.Functions.IsNumeric(r.Odometer) ? Convert.ToInt32(r.Odometer) : 0);
            //
            { "odometer", r => r.Odometer },

            { "pa_titleno", r => r.PA_TitleNo },
            { "pa_lrbol", r => r.PA_LRBOL },
            { "processstageid", r => r.ProcessStageId },
            { "processstagename", r => r.ProcessStageName },
            { "requestno", r => r.RequestNo },
            { "specialbilling", r => r.SpecialBilling },
            { "state", r => r.State },
            { "statusid", r => r.StatusId },
            { "submittedby", r => r.SubmittedBy },
            { "tovendorcourier", r => r.ToVendorCourier },
            { "tovendortracking", r => r.ToVendorTracking },
            { "trackingnumber", r => r.TrackingNumber },
            { "vehiclemake", r => r.VehicleMake },
            { "vehicleyear", r => r.VehicleYear },
            { "vendorcode", r => r.VendorCode },
            { "vendorid", r => r.VendorId },
            { "vin", r => r.Vin },
            { "last6vin", r => r.Last6Vin },
            { "waitingforuserreply", r => r.WaitingForUserReply },
            { "waitingforvendorreply", r => r.WaitingForVendorReply },
            { "invoiceno", r => r.InvoiceNo },
            { "invoicedate", r => r.InvoiceDate },
            { "invoicedatepaid", r => r.InvoiceDatePaid },
            { "servicefee", r => r.ServiceFee },
            { "dmvfee", r => r.DmvFee },
            { "otherfee", r => r.OtherFee },
            { "otherdesc", r => r.OtherDesc },
            { "totaldue", r => r.TotalDue },
            { "checkNumber", r => r.CheckNumber },
            { "li_datetodmv", r => r.LI_DateToDmv },
            { "li_datefromdmv", r => r.LI_DateFromDmv },
            { "li_checknumber", r => r.LI_CheckNumber },
            { "li_todmvcourier", r => r.LI_ToDmvCourier },
            { "li_todmvtracking", r => r.LI_ToDmvTracking },
            { "rejectionDate", r => r.RejectionDate },
            { "lastmodified", r => r.LastModified },
            { "lastmodifiedby", r => r.LastModifiedBy },
            { "repodate", r => r.RepoDate },
            { "lastchatdate", r => r.LastChatDate },
            { "reg_reg_name", r => r.REG_Reg_Name },
            { "reg_lh_name", r => r.REG_LH_Name },
            { "", r => r.Vin } // Default field for sort
        };
    }
    private static IOrderedQueryable<RequestStatus> OrderBy(IQueryable<RequestStatus> result, DatatableSort s)
    {
        Expression<Func<RequestStatus, object>> selector = GetSelector(s);

        if (s.descending)
        {
            return result.OrderByDescending(selector);
        }
        else
        {
            return result.OrderBy(selector);
        }
    }
    private static Expression<Func<RequestStatus, object>> GetSelector(DatatableSort s)
    {
        Expression<Func<RequestStatus, object>> selector = null;
        string colkey = s.columnName.ToLower();
        return GetSelector(colkey);
    }
    private static Expression<Func<RequestStatus, object>> GetSelector(string colkey)
    {
        Expression<Func<RequestStatus, object>> selector = null;

        if (colkey != null && s_selectors.ContainsKey(colkey))
        {
            selector = s_selectors[colkey];
        }
        selector ??= s_selectors[""]; // default
        return selector;
    }
    private static Expression<Func<RequestStatus, object>> CoalesceExpression(string colName, object value)
    {
        ParameterExpression argParam = Expression.Parameter(typeof(RequestStatus), "r");
        Expression property = Expression.Property(argParam, colName);

        Expression eValue = null;
        if (value != null)
        {
            value = ConvertToType(property.Type, value);
            eValue = Expression.Constant(value);
            if (property.Type.Name == "Nullable`1")
            {
                // Convert to nullable
                eValue = Expression.Convert(eValue, property.Type);
            }
            if (eValue.Type.IsValueType)
                eValue = Expression.Convert(eValue, typeof(object));
        }

        Expression expression = property;
        if (property.Type.Name == "Nullable`1")
        {
            if (Nullable.GetUnderlyingType(property.Type) == typeof(DateTime))
            {
                Expression e2 = Expression.Property(property, "Value");
                Expression e3 = Expression.Property(e2, "Date");
                expression = Expression.Convert(e3, property.Type);
            }
        }
        else
        {
            if (property.Type == typeof(DateTime))
            {
                Expression e2 = Expression.Property(property, "Date");
                expression = Expression.Convert(e2, property.Type);
            }
        }

        if (value != null)
        {
            expression = Expression.Coalesce(expression, eValue);
        }
        if (expression.Type.IsValueType)
        {
            expression = Expression.Convert(expression, typeof(object));
        }
        var lambda = Expression.Lambda<Func<RequestStatus, object>>(expression, argParam);
        return lambda;
    }
    private static Expression<Func<RequestStatus, bool>> GetIsEqualExpression(string colName, object value)
    {
        ParameterExpression argParam = Expression.Parameter(typeof(RequestStatus), "r");

        Expression property = Expression.Property(argParam, colName);

        Expression eValue;
        value = ConvertToType(property.Type, value);
        if (property.Type.Name == "Nullable`1")
        {
            eValue = Expression.Constant(value);
            // Convert to nullable
            eValue = Expression.Convert(eValue, property.Type);
        }
        else
        {
            eValue = Expression.Constant(value, property.Type);
        }

        Expression expression = property;
        if (property.Type.Name == "Nullable`1")
        {
            if (Nullable.GetUnderlyingType(property.Type) == typeof(DateTime))
            {
                Expression e2 = Expression.Property(property, "Value");
                Expression e3 = Expression.Property(e2, "Date");
                expression = Expression.Convert(e3, property.Type);
            }
        }
        else
        {
            if (property.Type == typeof(DateTime))
            {
                Expression e2 = Expression.Property(property, "Date");
                expression = Expression.Convert(e2, property.Type);
            }
        }

        expression = Expression.Equal(expression, eValue);
        var lambda = Expression.Lambda<Func<RequestStatus, bool>>(expression, argParam);
        return lambda;
    }
    private static Expression<Func<RequestStatus, bool>> GetSelectorIsNullOrBlank(string colName)
    {
        return GetIsNullOrNotNullExpression(colName, true);
    }
    private static Expression<Func<RequestStatus, bool>> GetSelectorIsNotNullOrBlank(string colName)
    {
        return GetIsNullOrNotNullExpression(colName, false);
    }
    private static object ConvertToType(Type type, object value)
    {
        if (value == null)
        {
            return null;
        }
        if (value.GetType() == type)
        {
            return value;
        }

        if (value.GetType() == typeof(string))
        {
            return ConvertToType(type, value as string);
        }
        return value;
    }
    private static object ConvertToType(Type type, string value)
    {
        Type ut = type;
        if (type.Name == "Nullable`1")
        {
            ut = Nullable.GetUnderlyingType(ut);
        }
        if (ut == typeof(DateTime))
        {
            if (DateTime.TryParse(value, out DateTime result))
            {
                return result;
            }
            return null;
        }
        else if (ut == typeof(Decimal))
        {
            if (Decimal.TryParse(value, out decimal result))
            {
                return result;
            }
            return null;
        }
        else if (ut == typeof(bool))
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            return null;
        }
        else if (ut == typeof(int))
        {
            if (Int32.TryParse(value, out int result))
            {
                return result;
            }
            return null;
        }
        else if (ut == typeof(short))
        {
            if (short.TryParse(value, out short result))
            {
                return result;
            }
            return null;
        }
        else if (ut == typeof(Guid))
        {
            if (Guid.TryParse(value, out Guid g))
            {
                return g;
            }
            return null;
        }
        return null;
    }
    private static Expression<Func<RequestStatus, bool>> GetIsNullOrNotNullExpression(string colName, bool isNullCheck)
    {
        ParameterExpression argParam = Expression.Parameter(typeof(RequestStatus), "r");
        Expression property = Expression.Property(argParam, colName);

        if (property.Type.Name == "Nullable`1")
        {
            var nullValue = Expression.Constant(null);
            Expression expression = Expression.Equal(property, nullValue);
            if (!isNullCheck) expression = Expression.Not(expression);
            var lambda = Expression.Lambda<Func<RequestStatus, bool>>(expression, argParam);
            return lambda;
        }
        else
        {
            var blank = Expression.Constant("");
            var nullValue = Expression.Constant(null);

            Expression e1;
            Expression e2;
            if (isNullCheck)
            {
                e1 = Expression.Equal(property, nullValue);
                e2 = Expression.Equal(property, blank);
                var expression = Expression.OrElse(e1, e2);
                var lambda = Expression.Lambda<Func<RequestStatus, bool>>(expression, argParam);
                return lambda;
            }
            else
            {
                e1 = Expression.NotEqual(property, nullValue);
                e2 = Expression.NotEqual(property, blank);
                var expression = Expression.AndAlso(e1, e2);
                var lambda = Expression.Lambda<Func<RequestStatus, bool>>(expression, argParam);
                return lambda;
            }
        }
    }
    private static Expression<Func<RequestStatus, bool>> GetDateRangeExpression(string colName, DateRange range)
    {
        ParameterExpression argParam = Expression.Parameter(typeof(RequestStatus), "r");

        Expression property = Expression.Property(argParam, colName);

        var eFromDate = Expression.Constant(range.FromDate);
        var eFromConverted = Expression.Convert(eFromDate, property.Type);
        var eToDate = Expression.Constant(range.ToDate);
        var eToConverted = Expression.Convert(eToDate, property.Type);

        Expression e1 = Expression.GreaterThanOrEqual(property, eFromConverted);
        Expression e2 = Expression.LessThanOrEqual(property, eToConverted);

        var expression = Expression.AndAlso(e1, e2);
        var lambda = Expression.Lambda<Func<RequestStatus, bool>>(expression, argParam);
        return lambda;
    }
    private static IOrderedQueryable<RequestStatus> ThenBy(IOrderedQueryable<RequestStatus> result, DatatableSort s)
    {
        Expression<Func<RequestStatus, object>> selector = GetSelector(s);

        if (s.descending)
        {
            return result.ThenByDescending(selector);
        }
        else
        {
            return result.ThenBy(selector);
        }
    }
    #endregion

    #region Holds page queries

    [HttpPost]
    public async Task<IActionResult> Vendor_Holds_Holds()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                        .WhereStatus(RequestStatusIDs.Hold);
                        },
                        active: true,
                        filterOnCurrentUser: false);
    }

    #endregion Holds page queries

    #region Vendor Cancelled page queries

    [HttpPost]
    public async Task<IActionResult> Vendor_Cancelled_Cancelled()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                    .WhereStatus(RequestStatusIDs.Cancelled);
                        },
                        active: true,
                        filterOnCurrentUser: false);
    }

    #endregion Vendor Cancelled page queries

    #region LH data queries

    [HttpPost]
    public async Task<IActionResult> Active()
    {
        return await GetRequests(ActiveQuery, active: true, filterOnCurrentUser: false);
    }

    private async Task<IQueryable<RequestStatus>> ActiveQuery(Guid? vendorId, Guid? groupId, Guid? userId, DatatableFormData dfd)
    {
        DateTime cutoffDate = GetActiveDaysCutoffDate(vendorId);
        var types = await RequestAppTypes();
        IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                    .WherePALC(false)
                                                    .WhereAppType(types)
                                                    .WhereIsActiveOrWithinCutoff(cutoffDate);
        return requestStatus;
    }

    public static DateTime GetActiveDaysCutoffDate(Guid? vendorId)
    {
        DateTime cutoffDate = ServerDate();
        if (vendorId == null)
            cutoffDate = cutoffDate.AddDays(0 - DataHelpers.Active_Days_LH);
        else
            cutoffDate = cutoffDate.AddDays(0 - DataHelpers.Active_Days_Vendor);
        return cutoffDate;
    }
    private async Task<IQueryable<RequestStatus>> ActiveAbstractQuery(Guid? vendorId, Guid? groupId, Guid? userId, DatatableFormData dfd)
    {
        DateTime cutoffDate = GetActiveDaysCutoffDate(vendorId);
        var apptypes = await AbstractDocTypes();

        IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                    .WherePALC(false)
                                                    .WhereAppType(apptypes)
                                                    .WhereIsActiveOrWithinCutoff(cutoffDate);
        return requestStatus;
    }

    [HttpPost]
    public async Task<IActionResult> MyActive()
    {
        return await GetRequests(MyActiveQuery, active: true, filterOnCurrentUser: true);
    }
    private async Task<IQueryable<RequestStatus>> MyActiveQuery(Guid? vendorId, Guid? groupId, Guid? userId, DatatableFormData dfd)
    {
        DateTime cutoffDate = GetActiveDaysCutoffDate(vendorId);

        IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                    .WhereIsActiveHoldOrWithinCutoff(cutoffDate);
        return requestStatus;
    }
    [HttpPost]
    public async Task<IActionResult> MyIDR()
    {
        return await GetRequests(ActiveAbstractQuery, active: true, filterOnCurrentUser: true);
    }

    private async Task<List<string>> AbstractDocTypes()
    {
        return await QueueTypes_LH("Abstracts");
    }

    private async Task<List<string>> RequestAppTypes()
    {
        return await QueueTypes_LH("Requests");
    }

    private async Task<List<string>> QueueTypes_LH(string queueName)
    {
        List<string> abstractTypes = null;

        abstractTypes = await _context.MdpAppTypes
                                    .Where(at => at.QueueName == queueName)
                                    .Select(a => a.AppType).ToListAsync();

        return abstractTypes;
    }

    private async Task<List<string>> QueueTypes_Vendor(string vendorQueueName)
    {
        List<string> queueTypes = null;

        queueTypes = await _context.MdpAppTypes
                                    .Where(at => at.VendorQueueName == vendorQueueName)
                                    .Select(a => a.AppType).ToListAsync();

        return queueTypes;
    }
#if false
    private async Task<IActionResult> InternalVendorRequestLI(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            string[] apptypes = new string[] { "LI" };

                            return FilterByUser(vendorId, groupId, userId)
                                    .WherePALC(false)
                                    .WhereAppType(apptypes)
                                    .WhereSentToDmv(false)
                                    .WhereShipped(false)
                                    .WhereIsActive()
                                    .WhereStage(stages);
                        }, 
                        true);
    }
    [HttpPost]
    public async Task<IActionResult> VendorRequestLI_NotReady()
    {
        return await InternalVendorRequestLI(ProcessStageIDs.NotReadyForProcessing);
    }
    [HttpPost]
    public async Task<IActionResult> VendorRequestLI_ReadyToBeProcessed()
    {
        return await InternalVendorRequestLI(ProcessStageIDs.ReadyToBeProcessed);
    }
    [HttpPost]
    public async Task<IActionResult> VendorRequestLI_InProcessing()
    {
        return await InternalVendorRequestLI(ProcessStageIDs.InProcessing);
    }
    [HttpPost]
    public async Task<IActionResult> VendorRequestLI_ReadyForPacking()
    {
        return await InternalVendorRequestLI(ProcessStageIDs.ReadyForPacking);
    }
#endif

    [HttpPost]
    public async Task<IActionResult> PALC()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            DateTime cutoffDate = ServerDate();
                            cutoffDate = cutoffDate.AddDays(-120);

                            return FilterByUser(vendorId, groupId, userId)
                                    .WherePALC()
                                    .WhereIsActiveOrWithinCutoffForPALC(cutoffDate);
                        }, true);
    }

    [HttpPost]
    public async Task<IActionResult> Abstracts()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            DateTime cutoffDate = GetActiveDaysCutoffDate(vendorId);
                            bool useCutoffDate = false;

                            useCutoffDate = (vendorId == null); // If called by Group member, return only last 7 days (move # days to external config setting)

                            var apptypes = await AbstractDocTypes();
                            IQueryable<RequestStatus> requestStatus =
                                FilterByUser(vendorId, groupId, userId)
                                    .WherePALC(false)
                                    .WhereAppType(apptypes);

                            if (useCutoffDate)
                            {
                                requestStatus = requestStatus.WhereIsActiveOrWithinCutoff(cutoffDate);
                            }
                            else
                            {
                                requestStatus = requestStatus.WhereIsActiveOrComplete();
                            }
                            return requestStatus;
                        }
                        , active: true);
    }
    [HttpPost]
    public async Task<IActionResult> IDRActive()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            DateTime cutoffDate = GetActiveDaysCutoffDate(vendorId);

                            var apptypes = await AbstractDocTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes)
                                                                        .WhereIsActiveOrWithinCutoff(cutoffDate);
                            return requestStatus;
                        });
    }
    [HttpPost]
    public async Task<IActionResult> IDRCompleted()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await AbstractDocTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes)
                                                                        .WhereIsComplete();

                            return requestStatus;
                        });
    }
    [HttpPost]
    public async Task<IActionResult> LCActive()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            DateTime cutoffDate = GetActiveDaysCutoffDate(vendorId);

                            var apptypes = await AbstractDocTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActiveOrWithinCutoff(cutoffDate)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> Chats()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            DateTime cutoffDate = GetActiveDaysCutoffDate(vendorId);

                            IQueryable<RequestStatus> requestStatus =
                                                FilterByUser(vendorId, groupId, userId)
                                                            .WhereHasActiveChat()
                                                            .Where(r => (r.StatusId == (int)RequestStatusIDs.Active)
                                                                        || (r.StatusId == (int)RequestStatusIDs.Pending)
                                                                        || (r.StatusId == (int)RequestStatusIDs.Hold)
                                                                        || (r.StatusId == (int)RequestStatusIDs.Complete && r.DateShipped > cutoffDate)
                                                                        || r.WaitingForUserReply
                                                                        || r.WaitingForVendorReply);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> UnreadChats()
    {

        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            DateTime cutoffDate = GetActiveDaysCutoffDate(vendorId);

                            IQueryable<RequestStatus> requestStatus =
                                                FilterByUser(vendorId, groupId, userId)
                                                            .WhereHasActiveChat()
                                                            .Where(r => (r.StatusId == (int)RequestStatusIDs.Active)
                                                                        || (r.StatusId == (int)RequestStatusIDs.Pending)
                                                                        || (r.StatusId == (int)RequestStatusIDs.Hold)
                                                                        || (r.StatusId == (int)RequestStatusIDs.Complete && r.DateShipped > cutoffDate)
                                                                        || r.WaitingForUserReply
                                                                        || r.WaitingForVendorReply);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> Completed()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                        .WhereIsComplete();
                            return requestStatus;
                        });
    }
    [HttpPost]
    public async Task<IActionResult> Unbilled()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsBillable();
                            return requestStatus;
                        });
    }
    [HttpPost]
    public async Task<IActionResult> LCCompleted()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await AbstractDocTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsComplete()
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);

                            return requestStatus;
                        });
    }
    [HttpPost]
    public async Task<IActionResult> UserPending()
    {
        return await GetPending(true);
    }
    [HttpPost]
    public async Task<IActionResult> Pending()
    {
        return await GetPending(false);
    }
    private async Task<IActionResult> GetPending(bool filterOnCurrentUser)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStatus(RequestStatusIDs.Pending)
                                                                        .WhereStage(ProcessStageIDs.Pending);
                            return requestStatus;
                        }, active: null, filterOnCurrentUser: filterOnCurrentUser);
    }
    #endregion LH data queries

#if false
/* no longer used */
    [HttpPost]
    public async Task<IActionResult> PrintQueue()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await RequestAppTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.Print)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }
    //ProcessStageID ProcessStageName
    //1	Pending
    //2	Incoming
    //3	Signing
    //4	Print
    //5	SendToDMV
    //6	ReceiveFromDMV
    //7	SendToLienholder

    [HttpPost]
    public async Task<IActionResult> ShipToDmvQueue()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await RequestAppTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.SendToDMV)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> ReceivingQueue()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await RequestAppTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.ReceiveFromDMV)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> ShipToLHQueue()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await RequestAppTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.ShipToLienholder)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> VendorSign()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await RequestAppTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.Signing)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> VendorPrint()
    {
        // This includes all requests that are in the Print stage,
        // regardless of whether they have a processing stage set
        // this is for backward compatibility with the old version
        return await InternalVendorRequestRT(ProcessStageIDs.ReadyToBeProcessed,
                                        ProcessStageIDs.InProcessing,
                                        ProcessStageIDs.ReadyForPrinting);
    }
    private async Task<IActionResult> InternalVendorRequestRT(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await RequestAppTypes();
                            var result = FilterByUser(vendorId, groupId, userId)
                                        .WhereIsActive()
                                        .WhereStage(stages)
                                        .WherePALC(false)
                                        .WhereAppType(apptypes);
                            return result;
                        });
    }
#endif

    #region Vendor and LH data queries
    [HttpPost]
    public async Task<IActionResult> VendorSLA()
    {
        try
        {
            _context.Database.OpenConnection();
            SetMaxNotesLength(16 * 1024);

            IActionResult result = await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId);
                            return requestStatus;
                        });
            return result;
        }
        finally
        {
            _context.Database.CloseConnection();
        }
    }

    [HttpPost]
    public async Task<IActionResult> LinkedRequests()
    {
        return EmptyDataTablesQueryResult();
    }

    [HttpPost("LinkedRequests/{requestId?}")]
    [HttpPost("MyServices_LinkedRequests/{requestId?}")]
    [HttpPost("Vendor_LinkedRequests/{requestId?}")]
    public async Task<IActionResult> LinkedRequests(Guid requestId)
    {
        var user = await GetCurrentUserAsync();

        var linkedRequests = DataHelpers.GetLinkedRequests(requestId).Result;

        IQueryable<RequestStatus> requestStatus = null;
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            requestStatus = FilterByUser(vendorId, groupId, userId);
                            if (linkedRequests.Count == 0)
                            {
                                // shortcut to return no records
                                requestStatus = requestStatus.Where(r => r.RequestId == Guid.Empty);
                            }
                            else
                            {
                                requestStatus = FilterByUser(vendorId, groupId, userId);
                                requestStatus = requestStatus.Where(r => linkedRequests.Contains(r.RequestId));
                            }
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> RequestSearch([FromForm] string vin)
    {
        if (string.IsNullOrEmpty(vin))
            return JsonError("VIN required");

        List<string> vins = null;
        try
        {
            var matches = _context.RequestStatus.AsQueryable();
            vin = vin.Trim();
            if (vin.Length == 17)
            {
                matches = matches.Where(f => f.Vin == vin);
            }
            else if (vin.Length == 6)
            {
                matches = matches.Where(f => f.Last6Vin == vin || f.Vin.StartsWith(vin));
            }
            else if (vin.Length > 6)
            {
                matches = matches.Where(f => f.Vin.StartsWith(vin) || f.Vin.EndsWith(vin));
            }
            else if (vin.Length >= 3)
            {
                // if partialVin = 'ABC', it these formats
                // matches ABC12345678901234
                // matches 12345678901234ABC
                // matches 12345678901ABC123
                matches = matches.Where(f => f.Vin.StartsWith(vin) || f.Vin.EndsWith(vin) || f.Last6Vin.StartsWith(vin));
            }
            else
            {
                vins = new List<string>();
            }
            vins ??= await matches.Select(m => m.Vin).Distinct().ToListAsync();
        }
        catch (Exception ex)
        {
            LogError(ex, "RequestStatusController.RequestSearch");
            return JsonError(ex);
        }

        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                        .Where(r => vins.Contains(r.Vin));
                            return requestStatus;
                        });
    }

    #endregion Vendor and LH data queries

#if false
/* UNUSED */
    //        Customer Name(LH Group)
    //Application Name:  Is this the LH column value? Yes
    //Service Date
    //Service: The example has a service of “NY TA”.  Will just having State and AppType work? yes
    //                State
    //                AppType
    //Vehicle info:
    //                VIN
    //                Year
    //                Make
    //                Client Ref#
    //Remarks: Is this the same as Notes? Or is a separate field needed for invoicing purposes? Yes
    //Service Fee
    //AbstractFee
    //DMV Disbursement
    //Mailing Fee
    //Total Due

    //Invoice # (this should have the ability to mark all records invoiced.)
    //Invoice Date

    //We will also need a 2nd disbursement field and a field next to it for description.
    //Need a record identifier for each record.
    //Need date paid (this should have the ability to mark all records paid.)
    //Need client check number (this should have the ability to mark all records paid.)    

    //private async Task<IActionResult> InternalVendorRequestOther(params ProcessStageIDs[] stages)
    //{
    //    List<string> apptypes = await QueueTypes_Vendor("ToDoOther");
    //    return await GetRequests(
    //                    async (vendorId, groupId, userId, dfd) =>
    //                    {
    //                        return FilterByUser(vendorId, groupId, userId)
    //                                    .WhereAppType(apptypes)
    //                                    .WhereSentToDmv(false)
    //                                    .WhereStage(stages)
    //                                    .WhereShipped(false)
    //                                    .WhereIsActive();
    //                    }, true);
    //}
#endif

    #region Vendor Holds page support

    [HttpPost]
    public async Task<IActionResult> Vendor_Holds_DeletePending()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId, true);
                        },
                        true);
    }

    #endregion Vendor Holds page support

    #region Vendor ToDoLI
    private async Task<IActionResult> internalVendor_LI_Incoming(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            string[] apptypes = new string[] { "LI" };
                            return FilterByUser(vendorId, groupId, userId)
                                    .WhereAppType(apptypes)
                                    .WhereStage(stages)
                                    .Where(r => r.DateReceived == null)
                                    .Where(r => r.LI_DateToDmv == null && r.DateToDmv == null)
                                    .WhereIsActive();
                        },
                        true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_Incoming() => await internalVendor_ToDoLI(ProcessStageIDs.Incoming);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_NotReadyToProcess() => await internalVendor_ToDoLI(ProcessStageIDs.NotReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_ReadyToProcess() => await internalVendor_ToDoLI(ProcessStageIDs.ReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_InProcessing() => await internalVendor_ToDoLI(ProcessStageIDs.InProcessing);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_ReadyForPrinting() => await internalVendor_ToDoLI(ProcessStageIDs.ReadyForPrinting);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_ReadyForPacking() => await internalVendor_ToDoLI(ProcessStageIDs.ReadyForPacking);


    private async Task<IActionResult> internalVendor_ToDoLI(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            string[] apptypes = new string[] { "LI" };

                            return FilterByUser(vendorId, groupId, userId)
                                    .WherePALC(false)
                                    .WhereAppType(apptypes)
                                    .WhereSentToDmv(false)
                                    .WhereShipped(false)
                                    .WhereIsActive()
                                    .WhereStage(stages);
                        }, true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_Pending()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            string[] apptypes = new string[] { "LI" };

                            return FilterByUser(vendorId, groupId, userId)
                                    .WherePALC(false)
                                    .WhereAppType(apptypes)
                                    .WhereStillAtDMV()
                                    .WhereShipped(false)
                                    .WhereIsActive();
                        },
                        true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLI_Completed()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            string[] apptypes = new string[] { "LI" };

                            return FilterByUser(vendorId, groupId, userId)
                                    .WhereAppType(apptypes)
                                    .WhereShipped(false)
                                    .WhereReceivedFromDMV();
                        }, true);
    }

    #endregion

    #region Vendor ToDoLC page support
    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLC_Request() => await InternalVendor_LC_Requests();

    private async Task<IActionResult> InternalVendor_LC_Requests()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                    .WherePALC()
                                    .Where(r => (r.LI_DateFromDmv != null || r.AppType == "LCO"))
                                    .WhereSentToDmv(false)
                                    .WhereIsActive();
                        }, true);
    }
    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLC_Completed()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                    .WherePALC()
                                    .WhereSentToDmv()
                                    .WhereShipped(false);
                        },
                        true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLC_Pending()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            string[] apptypes = new string[] { "LC", "LCO" };

                            return FilterByUser(vendorId, groupId, userId)
                                    .WherePALC(false)
                                    .WhereAppType(apptypes)
                                    .WhereStillAtDMV()
                                    .WhereIsActive();
                        }, true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLC_HOLD()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                        .WherePALC()
                                        .WhereInHoldQueue();
                        },
                        true);
    }

    #endregion

    #region Vendor ToDoLC

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLC_LI_Request()
    {
        // LC and LCLI use old naming convention
        // LCLI_Request maps to Incoming for now
        return await InternalVendor_LCLI(ProcessStageIDs.Incoming);
    }

    [HttpPost]
    public async Task<IActionResult> InternalVendor_LCLI(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                        .WherePALC()
                                        .Where(r => r.LI_DateToDmv == null && r.DateToDmv == null && r.AppType != "LCO")
                                        .WhereShipped(false)
                                        .WhereIsActive()
                                        .WhereStage(stages);
                        }, true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoLC_LI_Pending()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                        .WherePALC()
                                        .Where(r => r.LI_DateToDmv != null && r.LI_DateFromDmv == null && r.AppType != "LCO")
                                        .Where(r => r.DateToDmv == null && r.DateFromDmv == null)
                                        .WhereShipped(false)
                                        .WhereIsActive();
                        }, true);
    }

    #endregion

    #region Vendor ToDo support
    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_Receiving()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                            .WhereIsActive()
                                                                            .WhereStage(ProcessStageIDs.ReceiveFromDMV)
                                                                            .WherePALC(false)
                                                                            .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_ShipToLH()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.ShipToLienholder)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_Incoming()
    {
        return await InternalVendorIncoming(ProcessStageIDs.Incoming);
    }
    private async Task<IActionResult> InternalVendorIncoming(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(stages)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_Sign()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.Signing)
                                                                        .WherePALC(false)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_NotReadyToProcess() => await internalVendor_ToDo(ProcessStageIDs.NotReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_ReadyToProcess() => await internalVendor_ToDo(ProcessStageIDs.ReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_ReadyForPrinting() => await internalVendor_ToDo(ProcessStageIDs.ReadyForPrinting);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_InProcessing() => await internalVendor_ToDo(ProcessStageIDs.InProcessing);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_ReadyForPacking()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.ReadyForPacking)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    private async Task<IActionResult> internalVendor_ToDo(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            var result = FilterByUser(vendorId, groupId, userId)
                                        .WhereIsActive()
                                        .WhereStage(stages)
                                        .WherePALC(false)
                                        .WhereAppType(apptypes);
                            return result;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_TitlePending()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.TitlePending)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }


    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_WorkingList()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.WorkingList)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_InTransit()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.InTransit)
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDo_TitlesReceivedToday()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDo");
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.ShipToLienholder)
                                                                        .WherePALC(false)
                                                                        .WhereTitleScanned()
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }
    #endregion Vendor ToDo support

    #region Vendor ToDoWVClearing support
    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_Receiving() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.ReceiveFromDMV);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_ShipToLH() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.ShipToLienholder);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_Incoming() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.Incoming);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_NotReadyToProcess() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.NotReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_ReadyToProcess() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.ReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_ReadyForPrinting() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.ReadyForPrinting);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_InProcessing() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.InProcessing);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_WVRejections() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.WVRejections);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_WVSendQueue() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.WVSendQueue);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_ReadyForPacking() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.ReadyForPacking);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_TitlePending() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.TitlePending);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_InTransit() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.InTransit);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoWVClearing_TitlesReceivedToday() => await InternalVendor_ToDoWVClearing(ProcessStageIDs.ShipToLienholder);

    private async Task<IActionResult> InternalVendor_ToDoWVClearing(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await QueueTypes_Vendor("ToDoWVClearing");
                            var result = FilterByUser(vendorId, groupId, userId)
                                        .WhereIsActive()
                                        .WhereStage(stages)
                                        .WhereAppType(apptypes);

                            if (stages.Contains(ProcessStageIDs.ShipToLienholder))
                            {
                                result = result.WhereTitleScanned();
                            }
                            return result;
                        });
    }

    #endregion Vendor ToDoWVClearing support
    #region Vendor NeedToProcess data endpoints

    [HttpPost]
    public async Task<IActionResult> Vendor_NeedToProcess_All_Incoming() => await AllVendorIncoming(ProcessStageIDs.Incoming);

    [HttpPost]
    public async Task<IActionResult> Vendor_NeedToProcess_ReadyToProcess() => await Vendor_ToDo_ReadyToProcess();

    [HttpPost]
    public async Task<IActionResult> Vendor_NeedToProcess_LI_ReadyToProcess() => await Vendor_ToDoLI_ReadyToProcess();

    [HttpPost]
    public async Task<IActionResult> Vendor_NeedToProcess_TC_ReadyToProcess() => await Vendor_ToDoTC_ReadyToProcess();

    [HttpPost]
    public async Task<IActionResult> Vendor_NeedToProcess_Other_ReadyToProcess() => await Vendor_ToDoOther_ReadyToProcess();

    private async Task<IActionResult> AllVendorIncoming(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(stages)
                                                                        .WherePALC(false);
                            return requestStatus;
                        });
    }
    #endregion Vendor NeedToProcess data endpoints

#if false // NO LONGER USED - PREP FOR REMOVAL

    [HttpPost]
    public async Task<IActionResult> TitlesReceivedToday()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var apptypes = await RequestAppTypes();
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereIsActive()
                                                                        .WhereStage(ProcessStageIDs.ShipToLienholder)
                                                                        .WherePALC(false)
                                                                        .WhereTitleScanned()
                                                                        .WhereAppType(apptypes);
                            return requestStatus;
                        });
    }
#endif

    #region Vendor Manual Queue

    // stage 1

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_Review()
    {
        return await internal_Vendor_ND_Review();
    }
    public async Task<IActionResult> internal_Vendor_ND_Review()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStage(ProcessStageIDs.MQ_Review);
                            return requestStatus;
                        });
    }

    // stage 2

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_NotReadyToAccept()
    {
        return await internal_Vendor_ND_NotReadyToAccept();
    }
    public async Task<IActionResult> internal_Vendor_ND_NotReadyToAccept()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStage(ProcessStageIDs.MQ_NotReadyToAccept);
                            return requestStatus;
                        });
    }

    // stage 3

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_FollowUpReview()
    {
        return await internal_Vendor_ND_FollowUpReview();
    }
    public async Task<IActionResult> internal_Vendor_ND_FollowUpReview()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStage(ProcessStageIDs.MQ_FollowUpReview);
                            return requestStatus;
                        });
    }

    // stage 4

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_AcceptedIncoming()
    {
        return await internal_Vendor_MQ_AcceptedIncoming();
    }
    public async Task<IActionResult> internal_Vendor_MQ_AcceptedIncoming()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStage(ProcessStageIDs.MQ_AcceptedIncoming);
                            return requestStatus;
                        });
    }

    // stage 5

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_AcceptedNotReadyToProcess()
    {
        return await internal_Vendor_MQ_AcceptedNotReadyToProcess();
    }
    public async Task<IActionResult> internal_Vendor_MQ_AcceptedNotReadyToProcess()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                        .WhereStage(ProcessStageIDs.MQ_AcceptedNotReadyToProcess);
                            return requestStatus;
                        });
    }

    // stage 6

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_Accepted_FollowUpReview()
    {
        return await internal_Vendor_MQ_Accepted_FollowUpReview();
    }
    public async Task<IActionResult> internal_Vendor_MQ_Accepted_FollowUpReview()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStage(ProcessStageIDs.MQ_Accepted_FollowUpReview);
                            return requestStatus;
                        });
    }


    // stage 7

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_ReadyToProcess()
    {
        return await internal_Vendor_MQ_ReadyToProcess();
    }
    public async Task<IActionResult> internal_Vendor_MQ_ReadyToProcess()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStage(ProcessStageIDs.MQ_ReadyToProcess);
                            return requestStatus;
                        });
    }


    // stage 8

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_CouldNotProcess()
    {
        return await internal_Vendor_MQ_CouldNotProcess();
    }
    public async Task<IActionResult> internal_Vendor_MQ_CouldNotProcess()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStage(ProcessStageIDs.MQ_CouldNotProcess);
                            return requestStatus;
                        });
    }

    // stage 9

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_ProcessedReconcile()
    {
        return await internal_Vendor_MQ_ProcessedReconcile();
    }
    public async Task<IActionResult> internal_Vendor_MQ_ProcessedReconcile()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                            .WhereStage(ProcessStageIDs.MQ_ProcessedReconcile);
                            return requestStatus;
                        });
    }

    // stage 10

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_Completed()
    {
        return await internal_Vendor_MQ_Completed();
    }
    public async Task<IActionResult> internal_Vendor_MQ_Completed()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStage(ProcessStageIDs.MQ_Completed);
                            return requestStatus;
                        });
    }

    // stage 11

    [HttpPost]
    public async Task<IActionResult> Vendor_ManualQueue_ReturnedTemporarily()
    {
        return await internal_Vendor_MQ_ReturnedTemporarily();
    }
    public async Task<IActionResult> internal_Vendor_MQ_ReturnedTemporarily()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            IQueryable<RequestStatus> requestStatus = FilterByUser(vendorId, groupId, userId)
                                                                        .WhereStage(ProcessStageIDs.MQ_ReturnedTemporarily);
                            return requestStatus;
                        });
    }


    #endregion

    #region Vendor TC request support
    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_Incoming()
    {
        return await internalVendor_TC(ProcessStageIDs.Incoming);
    }
    private async Task<IActionResult> internalVendor_TC(params ProcessStageIDs[] stages)
    {
        List<string> apptypes = await QueueTypes_Vendor("ToDoTC");
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                    .WhereAppType(apptypes)
                                    .WhereStage(stages)
                                    .WhereSentToDmv(false)
                                    .WhereShipped(false)
                                    .WhereIsActive();
                        }, true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_NotReadyToProcess() => await internalVendor_TC(ProcessStageIDs.NotReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_ReadyToProcess() => await internalVendor_TC(ProcessStageIDs.ReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_InProcessing() => await internalVendor_TC(ProcessStageIDs.InProcessing);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_ReadyForPrinting() => await internalVendor_TC(ProcessStageIDs.ReadyForPrinting);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_ReadyForPacking() => await internalVendor_TC(ProcessStageIDs.ReadyForPacking);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_Pending()
    {
        List<string> apptypes = await QueueTypes_Vendor("ToDoTC");
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                    .WhereAppType(apptypes)
                                    .WhereStillAtDMV()
                                    .WhereShipped(false)
                                    .WhereIsActive();
                        }, true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoTC_Completed()
    {
        List<string> apptypes = await QueueTypes_Vendor("ToDoTC");
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                    .WhereAppType(apptypes)
                                    .WhereShipped(false)
                                    .WhereReceivedFromDMV();
                        }, true);
    }
    #endregion

    #region Vendor Other types support

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_Incoming() => await internalVendor_Other(ProcessStageIDs.Incoming);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_NotReadyToProcess() => await internalVendor_Other(ProcessStageIDs.NotReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_ReadyToProcess() => await internalVendor_Other(ProcessStageIDs.ReadyToProcess);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_ReadyForPrinting() => await internalVendor_Other(ProcessStageIDs.ReadyForPrinting);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_InProcessing() => await internalVendor_Other(ProcessStageIDs.InProcessing);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_ReadyForPacking() => await internalVendor_Other(ProcessStageIDs.ReadyForPacking);

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_Pending()
    {
        List<string> apptypes = await QueueTypes_Vendor("ToDoOther");
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                    .WhereAppType(apptypes)
                                    .WhereStillAtDMV()
                                    .WhereShipped(false)
                                    .WhereIsActive();
                        }, true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_ToDoOther_Completed()
    {
        List<string> apptypes = await QueueTypes_Vendor("ToDoOther");
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                    .WhereAppType(apptypes)
                                    .WhereShipped(false)
                                    .WhereReceivedFromDMV();
                        }, true);
    }
    #endregion

    #region Vendor Other types support

    private async Task<IActionResult> internalVendor_Other(params ProcessStageIDs[] stages)
    {
        List<string> apptypes = await QueueTypes_Vendor("ToDoOther");
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            return FilterByUser(vendorId, groupId, userId)
                                    .WhereAppType(apptypes)
                                    .WhereSentToDmv(false)
                                    .WhereStage(stages)
                                    .WhereShipped(false)
                                    .WhereIsActive();
                        }, true);
    }

    #endregion Vendor Other types support

    #region Vendor Audits support
    [HttpPost]
    public async Task<IActionResult> Vendor_Audits_Audits()
    {
        return await internalVendor_Audit(ProcessStageIDs.TitlePending);
    }
    private async Task<IActionResult> internalVendor_Audit(params ProcessStageIDs[] stages)
    {
        DateTime yesterday = DateTime.Today.AddDays(-1);
        return await GetRequests(
                    async (vendorId, groupId, userId, dfd) =>
                    {
                        var query = FilterByUser(vendorId, groupId, userId);
                        bool hasInProgressETA = query.Any(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.Eta);

                        if (hasInProgressETA)
                        {
                            return query.Where(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.Eta);
                        }
                        else
                        {
                            return query.WhereStatus(RequestStatusIDs.Active)
                                        .WhereStage(stages)
                                        .Where(rs => rs.Eta <= yesterday);
                        }
                    }, true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_Audits_PCodeAudit()
    {
        return await internalVendor_PCodeAudit(RequestStatusIDs.Pending);
    }
    private async Task<IActionResult> internalVendor_PCodeAudit(params RequestStatusIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var query = FilterByUser(vendorId, groupId, userId);
                            bool hasInProgressPCode = query.Any(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.PCode);
                            if (hasInProgressPCode)
                            {
                                return query.Where(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.PCode);
                            }
                            else
                            {
                                return query.WhereStatus(RequestStatusIDs.Pending);
                            }
                        }, true);
    }


    [HttpPost]
    public async Task<IActionResult> Vendor_Audits_HoldAudit()
    {
        return await internalVendor_HoldAudit();
    }
    private async Task<IActionResult> internalVendor_HoldAudit()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var query = FilterByUser(vendorId, groupId, userId);
                            bool hasInProgressHold = query.Any(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.Hold);
                            if (hasInProgressHold)
                            {
                                return query.Where(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.Hold);
                            }
                            else
                            {
                                return query.WhereInHoldQueue();
                            }
                        }, true);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_Audits_IncomingAudit()
    {
        return await internalVendor_IncomingAudit(ProcessStageIDs.NotReadyToProcess);
    }
    private async Task<IActionResult> internalVendor_IncomingAudit(params ProcessStageIDs[] stages)
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var query = FilterByUser(vendorId, groupId, userId);
                            bool hasInProgressIncoming = query.Any(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.Incoming);
                            if (hasInProgressIncoming)
                            {
                                return query.Where(rs => rs.Auditstatus == AuditMessage.InProgress && rs.AuditType == AuditMessage.Incoming);
                            }
                            else
                            {
                                return query.WhereStage(stages);
                            }
                        }, true);
    }
    #endregion Vendor Audits support

    #region Vendor Report support
    [HttpPost]
    public async Task<IActionResult> Vendor_Reports_Active()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var result = FilterByUser(vendorId, groupId, userId);
                            result = result.WhereIsActive();
                            return result;
                        }, active: true, filterOnCurrentUser: false);
    }
    [HttpPost]
    public async Task<IActionResult> Vendor_Reports_Completed()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var result = FilterByUser(vendorId, groupId, userId);
                            result = result.WhereIsComplete();
                            return result;
                        }, active: true, filterOnCurrentUser: false);
    }
    [HttpPost]
    public async Task<IActionResult> Vendor_Reports_Holds()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var result = FilterByUser(vendorId, groupId, userId);
                            result = result.WhereStatus(RequestStatusIDs.Hold);
                            return result;
                        }, active: true, filterOnCurrentUser: false);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor_Reports_IncomingHolds()
    {
        return await GetRequests(
                        async (vendorId, groupId, userId, dfd) =>
                        {
                            var result = FilterByUser(vendorId, groupId, userId);
                            result = result.WhereStatus(RequestStatusIDs.Active);
                            result = result.Where(r => r.ProcessStageId == (int)ProcessStageIDs.Incoming
                                || r.ProcessStageId == (int)ProcessStageIDs.NotReadyForProcessing);
                            return result;
                        }, active: true, filterOnCurrentUser: false);
    }
    #endregion Vendor Report support

    #region "Helper methods"

    public static string GetEndpointUrl(HttpContext context, string view)
    {
        string controller = "RequestStatus";
        // used for evaluation of performance of sprocs vs EF
        //string sprocFlag = context.Request.Query["sproc"];
        //if (sprocFlag != null && sprocFlag == "1")
        //{
        //    controller = "RS2";
        //}
        return $"/{controller}/{view}";
    }

    protected IQueryable<RequestStatus> FilterByUser(Guid? vendorId, Guid? groupId, Guid? userId, bool onlyDeletePending = false)
    {
        IQueryable<RequestStatus> requestStatus = _context.RequestStatus.AsNoTracking().AsQueryable();
        if (onlyDeletePending)
        {
            requestStatus = requestStatus.Where(r => r.StatusId == STATUS_ID_DELETE_PENDING);
        }
        else
        {
            // Default is all but delete pending or deleted
            requestStatus = requestStatus.Where(r => r.StatusId != STATUS_ID_DELETE_PENDING);
        }
        // This could be combined into one, but keeping separate to 
        // allow future updates without clouding the intent of the query
        if (vendorId != null)
        {
            // Vendor only
            return requestStatus.Where(r => r.VendorId == vendorId.Value);
        }
        else if (groupId != null && userId == null)
        {
            return requestStatus.Where(r => r.GroupId == groupId.Value);
        }
        else
        {
            return requestStatus.Where(r => (r.GroupId == groupId.Value)
                    && (
                        // users assigned requests (but not pending requests uploaded by another user)
                        (r.UserId == userId.Value && !(r.FileUploadUserId == userId.Value && r.StatusId == 0))
                            ||
                        // Uploads user made for another user
                        (r.FileUploadUserId == userId.Value && r.StatusId == 0)
                        )
                   );
        }
    }

    #endregion "Helper methods"

}
