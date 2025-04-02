using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    public partial class InvoiceController : BaseController
    {
        public InvoiceController(MaggardDMVContext context, IConfiguration configuration, ILogger<InvoiceController> logger) : base(context, configuration, logger)
        {
        }

        public delegate IQueryable<InvoiceStatus> QueryInvoices(Guid? vendorId, Guid? groupId, Guid? userId);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private IActionResult GetInvoices(QueryInvoices qr, bool? active = null, bool? filterOnCurrentUser = null)
        {
            try
            {
                if (CurrentUserIdAndGroups(out Guid? vendorId, out Guid? groupId, out Guid? userId))
                {
                    if (vendorId != null || groupId != null)
                    {
                        DatatableFormData dfd = GetData();

                        IQueryable<InvoiceStatus> invoiceStatus = qr(vendorId, groupId, filterOnCurrentUser == true ? userId : null);
                        var data = GetInvoicesFiltered(invoiceStatus, dfd, out int recordsTotal, out object filters);
                        return Json(new { dfd.draw, recordsFiltered = recordsTotal, recordsTotal, data, columnFilters = filters });
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
        public List<InvoiceStatus> GetInvoicesFiltered(IQueryable<InvoiceStatus> invoiceStatus, DatatableFormData dfd, out int TotalCount, out object filters)
        {
            List<string> colCheck = new List<string>();

            foreach (DatatableColumn col in dfd.columns)
            {
                colCheck.Add(col.data.ToLower());
                invoiceStatus = Where(invoiceStatus, col);
            }

            // search by contains
            invoiceStatus = Search(invoiceStatus, dfd.columns, dfd.globalSearch);

            if (dfd.draw == 1 || dfd.skip == 0)
            {
                var o = new Dictionary<string, object>();

                //try
                //{
                //    if (colCheck.Contains("invoiceno")) {
                //        o["invoiceNo"] = invoiceStatus.Select(r => r.InvoiceNo).Distinct().ToArray().OrderBy(x => x);
                //    }
                //}
                //catch (Exception ex) { 
                //    System.Diagnostics.Trace.WriteLine(ex.Message);
                //};
                //try
                //{
                //    if (colCheck.Contains("invoicedate"))
                //    {
                //        o["invoiceDate"] = invoiceStatus.Select(r => r.InvoiceDate).Distinct().ToArray().OrderBy(x => x).ToList();
                //    }
                //}
                //catch (Exception ex)
                //{
                //    System.Diagnostics.Trace.WriteLine(ex.Message);
                //};
                //try
                //{
                //    if (colCheck.Contains("invoicedatepaid"))
                //    {
                //        o["invoiceDatePaid"] = invoiceStatus.Select(r => r.InvoiceDatePaid).Distinct().ToArray().OrderBy(x => x).ToList();
                //    }
                //}
                //catch (Exception ex)
                //{
                //    System.Diagnostics.Trace.WriteLine(ex.Message);
                //};
                try
                {
                    if (colCheck.Contains("groupname"))
                    {
                        o["groupName"] = invoiceStatus.Select(r => r.GroupName).Distinct().ToArray().OrderBy(x => x).ToList();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                };
                try
                {
                    if (colCheck.Contains("billtoname"))
                    {
                        o["billToName"] = invoiceStatus.Select(r => r.BillToName).Distinct().ToArray().OrderBy(x => x).ToList();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                };
                try
                {
                    if (colCheck.Contains("createdby"))
                    {
                        o["createdBy"] = invoiceStatus.Select(r => r.CreatedBy).Distinct().ToArray().OrderBy(x => x).ToList();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                };
                filters = o;
            }
            else
            {
                filters = null;
            }

            IOrderedQueryable<InvoiceStatus> orderedResults = null;
            for (int i = 0; i < dfd.sort.Count; i++)
            {
                DatatableSort s = dfd.sort[i];
                if (orderedResults == null)
                {
                    orderedResults = OrderBy(invoiceStatus, s);
                }
                else
                {
                    orderedResults = ThenBy(orderedResults, s);
                }
            }
            TotalCount = orderedResults.Count();

            var result = orderedResults.Skip(dfd.start).Take(dfd.length).ToList();
            return result;
        }
        public IQueryable<InvoiceStatus> Search(IQueryable<InvoiceStatus> result, List<DatatableColumn> cols, string searchVal)
        {
            var predicate = PredicateBuilder.False<InvoiceStatus>();
            if (string.IsNullOrWhiteSpace(searchVal))
                return result;

            bool isDate = false;
            DateTime dtCompare = DateTime.MinValue;
            isDate = DateTime.TryParse(searchVal, out dtCompare);

            bool isBool = false;
            isBool = Boolean.TryParse(searchVal, out bool bCompare);

            bool isInt = false;
            isInt = Int32.TryParse(searchVal, out int iCompare);

            bool isGuid = false;
            isGuid = Guid.TryParse(searchVal, out Guid gCompare);
            bool isDecimal = Decimal.TryParse(searchVal, out decimal decValue);

            foreach (DatatableColumn col in cols)
            {
                if (col.searchable)
                {
                    switch (col.data.ToLower())
                    {
                        case "vendorid": predicate = predicate.Or(p => isGuid && p.VendorId.Equals(gCompare)); break;
                        case "invoiceno": predicate = predicate.Or(p => p.InvoiceNo.Contains(searchVal)); break;
                        case "invoicedate": predicate = predicate.Or(p => isDate && p.InvoiceDate.Equals(dtCompare)); break;
                        case "invoicedatepaid": predicate = predicate.Or(p => isDate && p.InvoiceDatePaid.Equals(dtCompare)); break;
                        case "groupname": predicate = predicate.Or(p => p.GroupName.Equals(searchVal)); break;
                        case "billtoname": predicate = predicate.Or(p => p.BillToName.Equals(searchVal)); break;
                        case "invoiceamount": predicate = predicate.Or(p => isDecimal && p.InvoiceAmount.Equals(decValue)); break;
                        case "invoicenote": predicate = predicate.Or(p => p.InvoiceNote.Contains(searchVal)); break;
                    }
                }
            }
            return result.Where(predicate);
        }
        private static readonly string[] s_dateColumns = new string[] { "invoicedate", "invoicedatepaid" };
        protected string[] GetDateColumns()
        {
            return s_dateColumns;
        }
        private IQueryable<InvoiceStatus> Where(IQueryable<InvoiceStatus> result, DatatableColumn col)
        {
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

            if (IsDateRange(col.data.ToLower(), val, out DateRange range))
            {
                switch (col.data.ToLower())
                {
                    case "invoicedate": result = result.Where(r => r.InvoiceDate >= range.FromDate && r.InvoiceDate <= range.ToDate); break;
                    case "invoicedatepaid": result = result.Where(r => r.InvoiceDatePaid >= range.FromDate && r.InvoiceDatePaid <= range.ToDate); break;
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
                        case "invoiceno": result = result.Where(p => p.InvoiceNo.Contains(singleVal)); break;
                        case "invoicedate": result = result.Where(p => IsDate(singleVal) && p.InvoiceDate == Convert.ToDateTime(singleVal)); break;
                        case "invoicedatepaid": result = result.Where(r => IsDate(singleVal) && r.InvoiceDatePaid == Convert.ToDateTime(singleVal)); break;
                        case "groupname": result = result.Where(r => r.GroupName == singleVal); break;
                        case "billtoname": result = result.Where(r => r.BillToName == singleVal); break;
                        case "invoiceamount": result = result.Where(r => IsDecimal(singleVal) && r.InvoiceAmount == Convert.ToDecimal(singleVal)); break;
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
        /* 
         * 
NotesAbbrev
PA_TitleNo
PA_LRBOL
         * */
        private IOrderedQueryable<InvoiceStatus> OrderBy(IQueryable<InvoiceStatus> result, DatatableSort s)
        {
            IOrderedQueryable<InvoiceStatus> newResult = null;

            if (s.descending)
            {
                newResult = s.columnName.ToLower() switch
                {
                    "invoiceno" => result.OrderByDescending(r => r.InvoiceNo),
                    "invoicedate" => result.OrderByDescending(r => r.InvoiceDate),
                    "groupname" => result.OrderByDescending(r => r.GroupName),
                    "billtoname" => result.OrderByDescending(r => r.BillToName),
                    "invoiceamount" => result.OrderByDescending(r => r.InvoiceAmount),
                    "invoicedatepaid" => result.OrderByDescending(r => r.InvoiceDatePaid),
                    _ => result.OrderByDescending(i => i.InvoiceDate),// log error
                };
            }
            else
            {
                newResult = s.columnName.ToLower() switch
                {
                    "invoiceno" => result.OrderBy(r => r.InvoiceNo),
                    "invoicedate" => result.OrderBy(r => r.InvoiceDate),
                    "groupname" => result.OrderBy(r => r.GroupName),
                    "billtoname" => result.OrderBy(r => r.BillToName),
                    "invoiceamount" => result.OrderBy(r => r.InvoiceAmount),
                    "invoicedatepaid" => result.OrderBy(r => r.InvoiceDatePaid),
                    _ => result.OrderBy(r => r.InvoiceDate),// log unhandled error
                };
            }
            return newResult;
        }
        private IOrderedQueryable<InvoiceStatus> ThenBy(IOrderedQueryable<InvoiceStatus> result, DatatableSort s)
        {
            IOrderedQueryable<InvoiceStatus> newResult = result;

            if (s.descending)
            {
                switch (s.columnName.ToLower())
                {
                    case "invoiceno": newResult = result.ThenByDescending(r => r.InvoiceNo); break;
                    case "invoicedate": newResult = result.ThenByDescending(r => r.InvoiceDate); break;
                    case "groupname": newResult = result.ThenByDescending(r => r.GroupName); break;
                    case "billtoname": newResult = result.ThenByDescending(r => r.BillToName); break;
                    case "invoiceamount": newResult = result.ThenByDescending(r => r.InvoiceAmount); break;
                    case "invoicedatepaid": newResult = result.ThenByDescending(r => r.InvoiceDatePaid); break;
                    default:
                        // log unhandled error
                        break;
                }
            }
            else
            {
                switch (s.columnName.ToLower())
                {
                    case "invoiceno": newResult = result.ThenBy(r => r.InvoiceNo); break;
                    case "invoicedate": newResult = result.ThenBy(r => r.InvoiceDate); break;
                    case "groupname": newResult = result.ThenBy(r => r.GroupName); break;
                    case "billtoname": newResult = result.ThenBy(r => r.BillToName); break;
                    case "invoiceamount": newResult = result.ThenBy(r => r.InvoiceAmount); break;
                    case "invoicedatepaid": newResult = result.ThenBy(r => r.InvoiceDatePaid); break;
                    default:
                        // log unhandled error
                        break;
                }
            }
            return newResult;
        }

        [HttpPost]
        public IActionResult VendorInvoices()
        {
            return GetInvoices(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<InvoiceStatus> invoiceStatus = _context.InvoiceStatus
                                                                    .AsNoTracking()
                                                                    .Where(i => i.VendorId == vendorId);

                return invoiceStatus;
            });
        }

        [HttpPost]
        public async Task<IActionResult> VendorUnpaidInvoices()
        {
            return GetInvoices(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<InvoiceStatus> invoiceStatus =
                        _context.InvoiceStatus
                                    .AsNoTracking()
                                    .Where(i => vendorId != null && i.VendorId == vendorId && i.InvoiceDatePaid == null)
                                    .AsQueryable();

                return invoiceStatus;
            });
        }

        [HttpPost]
        public IActionResult VendorPaidInvoices()
        {
            return GetInvoices(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<InvoiceStatus> invoiceStatus =
                                            from i in _context.InvoiceStatus
                                            where (vendorId != null && i.VendorId == vendorId &&
                                            i.InvoiceDatePaid != null)
                                            select i;
                return invoiceStatus;
            });
        }

        [HttpPost]
        public async Task<IActionResult> ClientUnpaidInvoices()
        {
            return GetInvoices(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<InvoiceStatus> invoiceStatus =
                        _context.InvoiceStatus
                                    .AsNoTracking()
                                    .Where(i => groupId != null && i.GroupId == groupId && i.InvoiceDatePaid == null)
                                    .AsQueryable();

                return invoiceStatus;
            });
        }

        [HttpPost]
        public IActionResult ClientPaidInvoices()
        {
            return GetInvoices(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<InvoiceStatus> invoiceStatus =
                                            from i in _context.InvoiceStatus
                                            where (groupId != null && i.GroupId == groupId &&
                                            i.InvoiceDatePaid != null)
                                            select i;
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
                List<Guid> list = new();
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
            System.Net.Mime.ContentDisposition cd = new()
            {
                FileName = filename
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            byte[] file = System.Text.UTF8Encoding.UTF8.GetBytes(fileContent);
            return File(file, "text/csv");
        }
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
        protected override bool IsDateColumn(string colName)
        {
            switch (colName)
            {
                case "invoicedate": break;
                case "invoicedatepaid": break;
                default:
                    return false;
            }
            return true;
        }
    }
}
