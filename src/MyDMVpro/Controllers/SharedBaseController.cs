using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.Extensions;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

public class SharedBaseController : Controller
{
    protected readonly IConfiguration _configuration;
    protected readonly ILogger _logger;
    protected readonly DbContext _rawExecContext;

    protected int _maxcolumns = 50;
    public const int STATUS_ID_DELETE_PENDING = (int)RequestStatusIDs.DeletePending;
    public const int STATUS_ID_DELETED = (int)RequestStatusIDs.Deleted;
    public const int STATUS_ID_HOLD = (int)RequestStatusIDs.Hold;

    protected int MaxColumns
    {
        get { return _maxcolumns; }
        set { _maxcolumns = value; }
    }


    public SharedBaseController(IConfiguration configuration, ILogger logger, DbContext context)
    {
        _configuration = configuration;
        _logger = logger;
        _rawExecContext = context;
    }
    public SharedBaseController(IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = null;
        _rawExecContext = null;
    }
    protected static DateTime ServerDate()
    {
        return DateTimeHelpers.ServerDate();
    }
    protected DateTime ServerDateTime()
    {
        return DateTimeHelpers.ServerDateTime();
    }
    protected IActionResult EmptyDataTablesQueryResult()
    {
        return Json(new { draw = 0, recordsFiltered = 0, recordsTotal = 0 });
    }
    protected bool CurrentUserIdAndGroups(out Guid? vendorId, out Guid? groupId, out Guid? userId)
    {
        vendorId = null;
        groupId = null;
        userId = null;

        UserInfo ui = GetCurrentUser();

        if (ui == null || ui.UserId == null)
            return false;
        userId = ui.UserId;

        if (ui.IsVendorAgent)
        {
            vendorId = ui.VendorId;
        }
        if (ui.IsGroupMember)
        {
            groupId = ui.GroupId;
        }
        return true;
    }
    protected async Task<bool> CurrentUserIdAndGroupsAsync(CurrentUserInfo userInfo)
    {
        userInfo.VendorId = null;
        userInfo.GroupId = null;
        userInfo.UserId = null;

        UserInfo ui = await GetCurrentUserAsync();

        if (ui == null || ui.UserId == null)
            return false;
        userInfo.UserId = ui.UserId;

        if (ui.IsVendorAgent)
        {
            userInfo.VendorId = ui.VendorId;
        }
        if (ui.IsGroupMember)
        {
            userInfo.GroupId = ui.GroupId;
        }
        return true;
    }

    private string _cachedSid = null;
    protected string GetUserSID()
    {
        if (_cachedSid == null)
        {
            _cachedSid = this.User.GetUserSID();
        }
        else
        {
            System.Diagnostics.Debug.Assert(_cachedSid == this.User.GetUserSID());
        }
        return _cachedSid;
    }
    protected string GetUser_Email()
    {
        string email = null;
        Claim claim = this.User.FindFirst(ClaimTypes.Email);
        if (claim != null)
            email = claim.Value;
        return email;
    }
    protected string GetUser_Name()
    {
        string username = null;
        Claim claim = this.User.FindFirst("name");
        if (claim != null)
            username = claim.Value;
        if (username == null)
        {
            claim = this.User.FindFirst(ClaimTypes.Name);
            if (claim != null)
                username = claim.Value;
        }
        return username;
    }
    public async Task<UserInfo> GetCurrentUserAsync(bool forceDbLookup = true)
    {
        try
        {
            UserInfo ui = await UserInfo.GetCurrentUserAsync(this, forceDbLookup);
            return ui;
        }
        catch (Exception)
        {
            return null;
        }
    }
    public UserInfo GetCurrentUser(bool forceDbLookup = true)
    {
        try
        {
            UserInfo ui = UserInfo.GetCurrentUser(this, forceDbLookup);
            return ui;
        }
        catch (Exception)
        {
            return null;
        }
    }
    protected async Task<(bool isMember, UserInfo user)> CurrentUserIsMemberOfGroupOrVendorAsync(Guid? groupId, Guid? vendorId)
    {
        Guid? userId;

        UserInfo user = await GetCurrentUserAsync();
        if (user == null)
            return (false, null);

        bool isMember = UserIsMemberOfGroupOrVendor(user, groupId, vendorId);
        return (isMember, user);
    }
    //protected bool CurrentUserIsMemberOfGroupOrVendor(Guid? groupId, Guid? vendorId, out Guid? userId, out UserInfo user)
    //{
    //    userId = null;
    //    user = GetCurrentUser();
    //    if (user != null)
    //    {
    //        userId = user.UserId;
    //    }
    //    return UserIsMemberOfGroupOrVendor(user, groupId, vendorId);
    //}
    //protected bool CurrentUserIsMemberOfGroupOrVendor(Guid? groupId, Guid? vendorId)
    //{
    //    UserInfo user = GetCurrentUser();
    //    return UserIsMemberOfGroupOrVendor(user, groupId, vendorId);
    //}
    protected bool UserIsMemberOfGroupOrVendor(UserInfo user, Guid? groupId, Guid? vendorId)
    {
        return ((user.GroupId != null && user.GroupId == groupId.Value)
            || (user.VendorId != null && user.VendorId == vendorId.Value));
    }
    protected string GetUserNameOrSID()
    {
        string sid = GetUserSID();
        if (sid != null)
            return sid;
        return HttpContext.User.Identity.Name;
    }

    //protected bool IsVendorAdmin(Guid? vendorId)
    //{
    //    UserInfo ui = GetCurrentUser();
    //    return ui.IsVendorAdmin;
    //    //return IsVendorAdmin(vendorId, ui.UserId);
    //}
    public DatatableFormData GetData()
    {
        DatatableFormData dfd = new DatatableFormData();

        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Request.Form["start"].FirstOrDefault();
        var length = Request.Form["length"].FirstOrDefault();

        try
        {
            dfd.draw = Convert.ToInt32(draw);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("draw error: {0}", ex);
            dfd.draw = 1;
        }
        try
        {
            dfd.start = Convert.ToInt32(start);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("start error: {0}", ex);
            dfd.start = 1;
        }
        try
        {
            dfd.length = Convert.ToInt32(length);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("length error: {0}", ex);
            dfd.length = 10;
        }


        List<DatatableColumn> cols = new List<DatatableColumn>();
        for (int i = 0; i < MaxColumns; i++)
        {
            var coldata = Request.Form[$"columns[{i}][data]"].FirstOrDefault();
            if (coldata == null)
                break;

            DatatableColumn col = new DatatableColumn()
            {
                data = coldata.ToLower(),
                name = Request.Form[$"columns[{i}][name]"].FirstOrDefault(),
                orderable = (Request.Form[$"columns[{i}][orderable]"].FirstOrDefault() ?? "") == "true",
                searchable = (Request.Form[$"columns[{i}][searchable]"].FirstOrDefault() ?? "") == "true",
                searchRegex = (Request.Form[$"columns[{i}][search][regex]"].FirstOrDefault() ?? "false") == "true",
                searchValue = Request.Form[$"columns[{i}][search][value]"].FirstOrDefault()
            };
            cols.Add(col);
        }
        dfd.columns = cols;

        List<DatatableSort> sort = new List<DatatableSort>();
        for (int i = 0; i < MaxColumns; i++)
        {
            var sortcol = Request.Form[$"order[{i}][column]"].FirstOrDefault();
            if (sortcol == null)
                break;
            if (!string.IsNullOrWhiteSpace(sortcol))
            {
                string sDirection = Request.Form[$"order[{i}][dir]"].FirstOrDefault() ?? "";
                DatatableSort s = new DatatableSort()
                {
                    column = Convert.ToInt32(sortcol ?? "0"),
                    descending = !sDirection.StartsWith("asc", StringComparison.InvariantCultureIgnoreCase)
                };
                s.columnName = cols[s.column].data;
                sort.Add(s);
            }
        }
        dfd.sort = sort;

        dfd.pageSize = length != null ? Convert.ToInt32(length) : 0;
        dfd.skip = start != null ? Convert.ToInt32(start) : 0;
        dfd.globalSearch = Request.Form["search[value]"].FirstOrDefault();

        dfd.includeArchived = (Request.Form["includeArchived"].FirstOrDefault() == "true");
        dfd.notesLength = 0;
        string sNotesLength = (Request.Form["notesLength"].FirstOrDefault());
        if (!string.IsNullOrWhiteSpace(sNotesLength))
        {
            if (Int32.TryParse(sNotesLength, out int len))
            {
                if (len > 0)
                {
                    dfd.notesLength = len;
                }
            }
        }
        dfd.paymentType = 0;
        string iPaymentType =(Request.Form["paymentType"].FirstOrDefault());
        if (!string.IsNullOrWhiteSpace(iPaymentType))
        {
            if (Int32.TryParse(iPaymentType, out int type))
            {
                if (type > 0)
                {
                    dfd.paymentType = type;
                }
            }
        }

        string iPaymentDate = (Request.Form["paymentDate"].FirstOrDefault());

        if (!string.IsNullOrWhiteSpace(iPaymentDate))
        {
            if (DateTime.TryParse(iPaymentDate, out DateTime parsedDate))
            {
                if (parsedDate > DateTime.MinValue)
                {
                    dfd.PaymentDate = parsedDate;
                }
            }
        } 

        string stateTypes = Request.Form["stateType"];
        if (stateTypes == "" || stateTypes == null)
        {
            dfd.stateType = null;
        }
        else
        {
            var customFilters = stateTypes.Split(",");
            dfd.stateType = new List<string>(customFilters);
        }

        string appTypes = Request.Form["appType"];

        if (appTypes != null && appTypes != "")
        {
            var customFilters = appTypes.Split(",");
            dfd.appType = new List<string>(customFilters);
        }

        string iscredit = Request.Form["isCredit"];
        if (iscredit != null && iscredit != "")
        {
            dfd.isCredit = Convert.ToBoolean(iscredit);
        }

        dfd.jsonFilter = Request.Form["jsonFilter"].FirstOrDefault();
        dfd.jsonColumns = Request.Form["jsonColumns"].FirstOrDefault();
        dfd.downloadCSV = (Request.Form["downloadCSV"].FirstOrDefault() == "true");
        dfd.downloadFileName = Request.Form["downloadFileName"].FirstOrDefault();

        var dropdownFilters = Request.Form["dropdownFilters"].FirstOrDefault() ?? "";
        if (dropdownFilters == "")
        {
            dfd.FilterList = null;
        }
        else
        {
            var customFilters = dropdownFilters.Split(",");
            dfd.FilterList = new List<string>(customFilters);
        }

        return dfd;
    }
    protected void OpenDatabaseConnection()
    {
        _rawExecContext.Database.OpenConnection();
    }
    protected async Task OpenDatabaseConnectionAsync()
    {
        await _rawExecContext.Database.OpenConnectionAsync();
    }

    private const string c_filterValueDelimiter = ";";

    protected static List<string> GetMultipleValues(string val)
    {
        if (string.IsNullOrEmpty(val))
            return new List<string>();

        string[] values = val.Split(c_filterValueDelimiter, StringSplitOptions.None);
        return new List<string>(values);
    }
    protected static int? SafeConvertInt(string s)
    {
        if (Int32.TryParse(s, out var val))
            return val;
        return null;
    }
    protected virtual bool IsDateColumn(string colName)
    {
        throw new NotImplementedException("IsDateColumn not implemented");
    }
    protected bool IsDateRange(string colName, string val, out DateRange range)
    {
        range = null;

        if (!IsDateColumn(colName))
            return false;

        string[] sDateRange = val?.Split(";-to-;", StringSplitOptions.None);
        if (sDateRange == null || sDateRange.Length != 2)
        {
            return false;
        }
        try
        {
            DateRange dr = new DateRange();
            string sFromDate = sDateRange[0];
            string sToDate = sDateRange[1];
            if (string.IsNullOrEmpty(sFromDate))
            {
                dr.FromDate = DateTime.MinValue.Date;
            }
            else
            {
                dr.FromDate = Convert.ToDateTime(sFromDate);
            }
            if (string.IsNullOrEmpty(sToDate))
            {
                dr.ToDate = DateTime.MaxValue.Date;
            }
            else
            {
                dr.ToDate = Convert.ToDateTime(sToDate);
            }
            range = dr;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Invalid date range filter: {ex.Message}");
            return false;
        }
    }
    protected bool GetServerDateOrNow(string sDate, out DateTime dt)
    {
        dt = ServerDate();
        if (!string.IsNullOrEmpty(sDate))
        {
            if (!DateTime.TryParse(sDate, out dt))
            {
                return false;
            }
        }
        return true;
    }
    protected JsonResult GetErrorResult(string msg)
    {
        Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
        return Json(new { status = "error", message = msg });
    }
    protected JsonResult JsonSuccess()
    {
        return Json(new { success = true, status = "success" });
    }
    protected JsonResult JsonSuccess(string msg)
    {
        return Json(new { success = true, status = "success", message = msg });
    }
    protected JsonResult JsonSuccess(object data)
    {
        return Json(new { success = true, status = "success", data = data });
    }
    protected JsonResult JsonError(Exception ex)
    {
        return JsonError(null, ex);
    }
    protected JsonResult JsonError(string errorMessage)
    {
        return JsonError(errorMessage, null);
    }
    protected JsonResult JsonError(string msg, Exception ex = null)
    {
        // log exception
        if (ex != null)
        {
            if (ex.InnerException != null && ex.Message.Contains("inner exception"))
            {
                ex = ex.InnerException;
            }
            if (string.IsNullOrWhiteSpace(msg))
            {
                msg = "ERROR";
            }
            msg = $"{msg}: {ex.Message}";
            System.Diagnostics.Trace.WriteLine(ex.ToString());
        }
        msg ??= "ERROR: Unexpected error occurred";

        return Json(new { success = false, status = "error", message = msg });
    }
    protected IActionResult ReturnFile(string mimeType, byte[] data)
    {
        Stream dataStream = new MemoryStream(data);
        return File(dataStream, mimeType);
    }
    protected IActionResult ReturnFileWithContentDisposition(string mimeType, string filename, Stream fileStream, DateTime lastModified, bool displayInline)
    {
        System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition()
        {
            FileName = filename,
            Inline = displayInline
        };
        Response.Headers.Add("Content-Disposition", cd.ToString());
        Response.Headers.Add("X-Content-Type-Options", "nosniff");
        string lastmod = lastModified.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
        Response.Headers.Add("Last-Modified", lastmod);
        fileStream.Seek(0, 0);
        return File(fileStream, mimeType);
    }
    protected IActionResult ReturnPdfFile(string filename, byte[] file, DateTime lastModified, bool displayInline)
    {
        System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
        {
            FileName = filename,
            Inline = displayInline
        };
        Response.Headers.Add("Content-Disposition", cd.ToString());
        Response.Headers.Add("X-Content-Type-Options", "nosniff");
        string lastmod = lastModified.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
        Response.Headers.Add("Last-Modified", lastmod);

        return File(file, "application/pdf");
    }
    protected IActionResult ReturnPdfFile(string filename, Stream fileStream, DateTime lastModified, bool displayInline)
    {
        return ReturnFileWithContentDisposition("application/pdf", filename, fileStream, lastModified, displayInline);
    }
    public class CsvColumn
    {
        public string Title { get; set; }
        public string DataName { get; set; }
    }
    // function to return a CSV file given an array of CsvColumn objects and a list of objects
    // the list of objects should be a list of objects that have properties that match the DataName of the CsvColumn objects
    protected IActionResult ReturnCsvFile(string filename, List<CsvColumn> columns, List<RequestStatus> data)
    {
        System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
        {
            FileName = filename
        };
        Response.Headers.Add("Content-Disposition", cd.ToString());
        string csv = ToCsv(columns, data);
        byte[] file = System.Text.UTF8Encoding.UTF8.GetBytes(csv);
        return File(file, "text/csv");
    }
    // function to take a list of CsvColumn objects and a list of objects and return a CSV string
    protected string ToCsv(List<CsvColumn> columns, List<RequestStatus> data)
    {
        StringBuilder sb = new StringBuilder();
        // write the column headers
        int colIdx = 0;
        foreach (var col in columns)
        {
            if (colIdx++ > 0)
                sb.Append(",");
            sb.Append(col.Title);
        }
        sb.AppendLine();
        // write the data
        foreach (var item in data)
        {
            colIdx = 0;
            foreach (var col in columns)
            {
                if (colIdx++ > 0)
                    sb.Append(",");
                var prop = item.GetType().GetProperty(col.DataName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    var val = prop.GetValue(item);
                    if (val != null)
                    {
                        if (val is DateTime dt)
                        {
                            val = dt.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            val = val.ToString();
                        }
                        // escape any double quotes in the value
                        val = val.ToString().Replace("\"", "\"\"");
                        // if the value contains a comma, double quote it
                        sb.Append($"\"{val}\"");
                    }
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }


    protected IActionResult ReturnCsvText(object jsonObject)
    {
        string jsonPayload = JsonConvert.SerializeObject(jsonObject);
        byte[] file = System.Text.UTF8Encoding.UTF8.GetBytes(jsonPayload);
        return File(file, "text/csv");
    }
    protected IActionResult ReturnCsvFile(string filename, object jsonObject, string[] columns)
    {
        System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
        {
            FileName = filename
        };
        Response.Headers.Add("Content-Disposition", cd.ToString());
        string jsonPayload = JsonConvert.SerializeObject(jsonObject);
        byte[] file = System.Text.UTF8Encoding.UTF8.GetBytes(jsonPayload);
        return File(file, "text/csv");
    }
    protected void SetMaxNotesLength(int maxNotesLength)
    {
        var p = new SqlParameter("@maxNotesLength", maxNotesLength);
        var sql = "exec dbo.SetMaxNotesLength @maxNotesLength";
        var data = _rawExecContext.Database.ExecuteSqlRaw(sql, p);
    }
    protected void SetNotesFilter(string filter)
    {
        OpenDatabaseConnection();
        var p = new SqlParameter("@filter", filter);
        var sql = "exec dbo.SetNotesFilter @filter";
        var data = _rawExecContext.Database.ExecuteSqlRaw(sql, p);
    }

    protected void LogError(Exception ex, string message)
    {
        _logger?.LogError(ex, message);
    }
}

