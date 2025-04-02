using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

public class DisbursementsController : BaseController
{
    public DisbursementsController(MaggardDMVContext context, IConfiguration configuration, ILogger<DisbursementsController> logger) : base(context, configuration, logger)
    {
    }
    public async Task<IActionResult> PendingDisbursements()
    {
        return GetDisbursements(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<RequestDisbursement> invoiceStatus = _context.RequestDisbursements
                                                            .AsNoTracking()
                                                            .Where(r => (r.Cleared ?? false) == false)
                                                            .Where(r => (r.Voided ?? false) == false)
                                                            .Where(s => vendorId != null && s.Request.VendorId == vendorId);
            return invoiceStatus;
        });
    }
    public async Task<IActionResult> VoidedDisbursements()
    {
        return GetDisbursements(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<RequestDisbursement> invoiceStatus = _context.RequestDisbursements
                                                            .AsNoTracking()
                                                            .Where(r => (r.Voided ?? false) == true)
                                                            .Where(s => vendorId != null && s.Request.VendorId == vendorId);
            return invoiceStatus;
        });
    }
    public async Task<IActionResult> ClearedDisbursements()
    {
        return GetDisbursements(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<RequestDisbursement> invoiceStatus = _context.RequestDisbursements
                                                            .AsNoTracking()
                                                            .Where(r => (r.Cleared ?? false) == true)
                                                            .Where(s => vendorId != null && s.Request.VendorId == vendorId);
            return invoiceStatus;
        });
    }
    public delegate IQueryable<RequestDisbursement> QueryRecords(Guid? vendorId, Guid? groupId, Guid? userId);

    private IActionResult GetDisbursements(QueryRecords qr, bool? active = null, bool? filterOnCurrentUser = null)
    {
        try
        {
            Guid? vendorId, groupId, userId;
            if (CurrentUserIdAndGroups(out vendorId, out groupId, out userId))
            {
                if (vendorId != null || groupId != null)
                {
                    DatatableFormData dfd = GetData();
                    int recordsTotal = 0;

                    IQueryable<RequestDisbursement> disbursements = qr(vendorId, groupId, filterOnCurrentUser == true ? userId : null);
                    object filters;
                    var data = GetDisbursementsFiltered(disbursements, dfd, out recordsTotal, out filters);
                    return Json(new { draw = dfd.draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data, columnFilters = filters });
                }
            }
            return EmptyDataTablesQueryResult();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("{0}", ex);
            throw;
        }
    }
    public object GetDisbursements(MaggardDMVContext context, Guid? vendorId, Guid? groupId, Guid? userId, bool? active)
    {
        DatatableFormData dfd = GetData();

        int recordsTotal = 0;
        object filters = null;
        var data = GetRequestsFiltered(context, dfd, vendorId.Value, out recordsTotal, out filters);
        return new { draw = dfd.draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data, columnFilters = filters };
    }

    public List<RequestDisbursement> GetRequestsFiltered(MaggardDMVContext context, DatatableFormData dfd, Guid vendorId, out int TotalCount, out object filters)
    {
        IQueryable<RequestDisbursement> disbursements = _context.RequestDisbursements.Include(r => r.Request).Where(r => r.Request.VendorId == @vendorId);
        return GetDisbursementsFiltered(disbursements, dfd, out TotalCount, out filters);
    }
    public List<RequestDisbursement> GetDisbursementsFiltered(IQueryable<RequestDisbursement> disbursements, DatatableFormData dfd, out int TotalCount, out object filters)
    {
        foreach (DatatableColumn col in dfd.columns)
        {
            disbursements = Where(disbursements, col);
        }

        // search by contains
        disbursements = Search(disbursements, dfd.columns, dfd.globalSearch);

        if (dfd.draw == 1 || dfd.skip == 0)
        {
            var o = new Dictionary<string, object>();
            //try
            //{
            //    o["dateShipped"] = disbursements.Select(s => s.DateShipped).Distinct().ToArray().OrderBy(x => x).ToList();
            //}
            //catch (Exception ex) { };
            //try
            //{
            //    o["auctionName"] = disbursements.Select(s => s.AuctionName).Distinct().ToArray().OrderBy(x => x).ToList();
            //}
            //catch (Exception ex) { };
            //try
            //{
            //    o["groupName"] = disbursements.Select(s => s.GroupName).Distinct().ToArray().OrderBy(x => x).ToList();
            //}
            //catch (Exception ex) { };

            filters = o;
        }
        else
        {
            filters = null;
        }

        IOrderedQueryable<RequestDisbursement> orderedResults = null;
        for (int i = 0; i < dfd.sort.Count; i++)
        {
            DatatableSort s = dfd.sort[i];
            if (orderedResults == null)
            {
                orderedResults = OrderBy(disbursements, s);
            }
            else
            {
                orderedResults = ThenBy(orderedResults, s);
            }
        }
        if (orderedResults == null)
        {
            orderedResults = disbursements.OrderByDescending(i => i.CreatedDate);
        }
        TotalCount = orderedResults.Count();

        var result = orderedResults.Skip(dfd.start).Take(dfd.length).ToList();
        return result;
    }
    public IQueryable<RequestDisbursement> Search(IQueryable<RequestDisbursement> result, List<DatatableColumn> cols, string searchVal)
    {
        var predicate = PredicateBuilder.False<RequestDisbursement>();
        if (string.IsNullOrWhiteSpace(searchVal))
            return result;

        searchVal = searchVal.Trim();

        bool isDate = false;
        DateTime dtCompare = DateTime.MinValue;
        isDate = DateTime.TryParse(searchVal, out dtCompare);

        bool isBool = false;
        bool bCompare = false;
        isBool = Boolean.TryParse(searchVal, out bCompare);

        bool isInt = false;
        int iCompare = 0;
        isInt = Int32.TryParse(searchVal, out iCompare);

        bool isGuid = false;
        Guid gCompare = Guid.Empty;
        isGuid = Guid.TryParse(searchVal, out gCompare);
        Decimal decValue;
        bool isDecimal = Decimal.TryParse(searchVal, out decValue);

        foreach (DatatableColumn col in cols)
        {
            if (col.searchable)
            {
                //switch (col.data.ToLower())
                //{
                //    case "vendorid": predicate = predicate.Or(p => isGuid && p.VendorId.Equals(gCompare)); break;
                //    case "dateshipped": predicate = predicate.Or(p => isDate && p.DateShipped.Equals(dtCompare)); break;
                //    case "auctionname": predicate = predicate.Or(p => isDate && p.AuctionName.Equals(searchVal)); break;
                //    case "groupname": predicate = predicate.Or(p => isDate && p.GroupName.Equals(searchVal)); break;
                //    case "trackingnumber": predicate = predicate.Or(p => p.TrackingNumber.Equals(searchVal)); break;
                //}
            }
        }
        return result.Where(predicate);
    }
    protected override bool IsDateColumn(string colName)
    {
        switch (colName)
        {
            case "dateshipped": break;
            case "datecreated": break;
            default:
                return false;
        }
        return true;
    }

    private IQueryable<RequestDisbursement> Where(IQueryable<RequestDisbursement> result, DatatableColumn col)
    {
        string val = col.searchValue;
        if (string.IsNullOrWhiteSpace(val))
            return result;

        bool useLike = false;
        DateRange range = null;

        if (col.searchRegex)
        {
            // can't handle regex with SQL, so handle simple ones
            if (val.StartsWith("^") && val.EndsWith("$"))
            {
                if (val.Length == 2)
                    val = "";
                else
                    val = val.Substring(1, val.Length - 2);
                col.searchValue = val;
                col.searchRegex = false;
            }
            else if (val.StartsWith("^"))
            {
                useLike = true;
                val = val + "%";
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

        if (IsDateRange(col.data.ToLower(), val, out range))
        {
            switch (col.data.ToLower())
            {
                //case "dateshipped": result = result.Where(r => r.DateShipped >= range.FromDate && r.DateShipped <= range.ToDate); break;
                //case "datecreated": result = result.Where(r => r.DateCreated >= range.FromDate && r.DateCreated <= range.ToDate); break;
                default:
                    System.Diagnostics.Trace.WriteLine("Unhandled filter");
                    break;
            }
        }
        else
        {
            List<string> multipleValues = GetMultipleValues(val);
            foreach (string singleVal in multipleValues)
            {
                switch (col.data.ToLower())
                {
                    //case "dateshipped": result = result.Where(p => IsDate(singleVal) && p.DateShipped == Convert.ToDateTime(singleVal)); break;
                    //case "auctionname": result = result.Where(r => r.AuctionName == singleVal); break;
                    //case "groupname": result = result.Where(r => r.GroupName == singleVal); break;
                    //case "trackingnumber": result = result.Where(r => r.TrackingNumber == singleVal); break;
                    default:
                        System.Diagnostics.Trace.WriteLine("Unhandled sort");
                        break;
                }
            }
        }
        return result;
    }
    private bool IsDecimal(string dateValue)
    {
        return Decimal.TryParse(dateValue, out Decimal _);
    }
    private bool IsDate(string dateValue)
    {
        return DateTime.TryParse(dateValue, out DateTime _);
    }

    private IOrderedQueryable<RequestDisbursement> OrderBy(IQueryable<RequestDisbursement> result, DatatableSort s)
    {
        IOrderedQueryable<RequestDisbursement> newResult = null;

        if (s.descending)
        {
            switch (s.columnName.ToLower())
            {
                //case "dateshipped": newResult = result.OrderByDescending(r => r.DateShipped); break;
                //case "auctionname": newResult = result.OrderByDescending(r => r.AuctionName); break;
                //case "groupname": newResult = result.OrderByDescending(r => r.GroupName); break;
                //case "numbershipped": newResult = result.OrderByDescending(r => r.NumberShipped); break;
                //case "datecreated": newResult = result.OrderByDescending(r => r.DateCreated); break;
                //case "trackingnumber": newResult = result.OrderByDescending(r => r.TrackingNumber); break;
                default:
                    // log error
                    //                    newResult = result.OrderByDescending(i => i.DateShipped);
                    break;
            }
        }
        else
        {
            switch (s.columnName.ToLower())
            {
                //case "dateshipped": newResult = result.OrderBy(r => r.DateShipped); break;
                //case "auctionname": newResult = result.OrderBy(r => r.AuctionName); break;
                //case "groupname": newResult = result.OrderBy(r => r.GroupName); break;
                //case "numbershipped": newResult = result.OrderBy(r => r.NumberShipped); break;
                //case "datecreated": newResult = result.OrderBy(r => r.DateCreated); break;
                //case "trackingnumber": newResult = result.OrderBy(r => r.TrackingNumber); break;
                default:
                    // log unhandled error
                    //                    newResult = result.OrderBy(r => r.DateShipped);
                    break;
            }
        }
        return newResult;
    }
    private IOrderedQueryable<RequestDisbursement> ThenBy(IOrderedQueryable<RequestDisbursement> result, DatatableSort s)
    {
        IOrderedQueryable<RequestDisbursement> newResult = result;

        if (s.descending)
        {
            switch (s.columnName.ToLower())
            {
                //case "dateshipped": newResult = result.ThenByDescending(r => r.DateShipped); break;
                //case "auctioneer": newResult = result.ThenByDescending(r => r.AuctionName); break;
                //case "groupname": newResult = result.ThenByDescending(r => r.GroupName); break;
                //case "numbershipped": newResult = result.ThenByDescending(r => r.NumberShipped); break;
                //case "datecreated": newResult = result.ThenByDescending(r => r.DateCreated); break;
                //case "trackingnumber": newResult = result.ThenByDescending(r => r.TrackingNumber); break;
                default:
                    // log unhandled error
                    break;
            }
        }
        else
        {
            switch (s.columnName.ToLower())
            {
                //case "dateshipped": newResult = result.ThenBy(r => r.DateShipped); break;
                //case "auctioneer": newResult = result.ThenBy(r => r.AuctionName); break;
                //case "groupname": newResult = result.ThenBy(r => r.GroupName); break;
                //case "numbershipped": newResult = result.ThenBy(r => r.NumberShipped); break;
                //case "datecreated": newResult = result.ThenBy(r => r.DateCreated); break;
                //case "trackingnumber": newResult = result.ThenBy(r => r.TrackingNumber); break;
                default:
                    // log unhandled error
                    break;
            }
        }
        return newResult;
    }
    //List<Guid> groupIDs = new List<Guid>();
    //groupIDs = adminUser.UserGroups.Where(ug => ug.IsGroupAdmin == true).Select(g => g.GroupId).ToList();

    //var users = await _context.Users.Where(u => groupIDs.Contains(u.UserId)).ToListAsync();
    //    return View("ManageGroup", users);
#if false
        private bool CurrentUserIdAndGroups(out Guid? vendorId, out Guid? groupId, out Guid? userId)
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
#endif
    private List<string> QueueTypes(string queueName)
    {
        List<string> abstractTypes = null;

        abstractTypes = _context.MdpAppTypes.Select(x => x.QueueName).Distinct().ToList();

        return abstractTypes;
    }

    [HttpPost]
    public IActionResult VendorDisbursements()
    {
        return GetDisbursements(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
        {
            IQueryable<RequestDisbursement> invoiceStatus = _context.RequestDisbursements
                                                            .Include(r => r.Request)
                                                            .AsNoTracking()
                                                            .Where(s => vendorId != null && s.Request.VendorId == vendorId);
            return invoiceStatus;
        });
    }
    [HttpPost]
    public async Task<IActionResult> Export([FromForm] string[] ids)
    {
        UserInfo ci = await GetCurrentUserAsync();
        if (ci == null || (ci.VendorId == null))
            return NotFound();
        try
        {
            List<Guid> list = new List<Guid>();
            foreach (string id in ids)
            {
                list.Add(new Guid(id));
            }
            string data = await DataHelpers.ExportInvoices(ci.VendorId.Value, ci.UserId.Value, ci.GroupId, list);
            DateTime now = ServerDateTime();
            string filename = $"myDMVpro-invoices-{now:yyyy-MM-dd-HHmmss}.csv";
            return ReturnCsvFile(filename, data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("{0}", ex);
            return NotFound();
        }
    }
    private IActionResult ReturnCsvFile(string filename, string fileContent)
    {
        System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
        {
            FileName = filename
        };
        Response.Headers.Add("Content-Disposition", cd.ToString());
        byte[] file = System.Text.UTF8Encoding.UTF8.GetBytes(fileContent);
        return File(file, "text/csv");
    }
}