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

namespace MyDMVpro.Controllers
{
    public class RequestChatsController : BaseController
    {
        public RequestChatsController(MaggardDMVContext context, IConfiguration configuration, ILogger<RequestChatsController> logger) : base(context, configuration, logger)
        {
        }
       
        [HttpPost]
        public IActionResult Master()
        {
            return GetRequests(delegate (Guid? vendorId, Guid? groupId)
            {
                IQueryable<RequestChats> requestChats = _context.RequestChats
                                                                    .AsNoTracking()
                                                                    .Where(r => (vendorId != null && r.VendorId == vendorId) || (groupId != null && r.GroupId == groupId));
                return requestChats;
            });
        }
        public delegate IQueryable<RequestChats> QueryRequests(Guid? vendorId, Guid? groupId);

        private IActionResult GetRequests(QueryRequests qr)
        {
            try
            {
                Guid? vendorId, groupId;
                if (CurrentUserIdAndGroups(out vendorId, out groupId))
                {
                    if (vendorId != null || groupId != null)
                    {
                        DatatableFormData dfd = GetData();
                        int recordsTotal = 0;

                        IQueryable<RequestChats> requestChats = qr(vendorId, groupId);
                        object filters;
                        var data = GetRequestsFiltered(requestChats, dfd, out recordsTotal, out filters);
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
        public object GetRequests(MaggardDMVContext context, Guid? vendorId, Guid? groupId, bool? active)
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();

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

            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            // Sort Column Direction (asc ,desc)
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            // Search Value from (Search box)  
            var searchValue = Request.Form["search[value]"].FirstOrDefault();

            //Paging Size (10,20,50,100)  
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int recordsTotal = 0;

            //Paging    
            DatatableFormData dfd = new DatatableFormData()
            {
                columns = cols,
                sort = sort,
                globalSearch = searchValue,
                skip = skip,
                pageSize = pageSize,
            };

            object filters;
            var data = GetRequestsFiltered(context, dfd, vendorId.Value, out recordsTotal, out filters);

            //Returning Json Data  
            return new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data, columnFilters = filters };
        }
#if false
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

            return dfd;
        }
#endif
        public static List<RequestChats> GetRequestsFiltered(MaggardDMVContext context, DatatableFormData dfd, Guid vendorId, out int TotalCount, out object filters)
        {
            IQueryable<RequestChats> requestChats = context.RequestChats.Where(r => r.VendorId == vendorId);
            return GetRequestsFiltered(requestChats, dfd, out TotalCount, out filters);
        }
        public static List<RequestChats> GetRequestsFiltered(IQueryable<RequestChats> requestChats, DatatableFormData dfd, out int TotalCount, out object filters)
        {
            foreach (DatatableColumn col in dfd.columns)
            {
                requestChats = Where(requestChats, col);
            }

            // search by contains
            requestChats = Search(requestChats, dfd.columns, dfd.globalSearch);

            IOrderedQueryable<RequestChats> orderedResults = null;
            for (int i = 0; i < dfd.sort.Count; i++)
            {
                DatatableSort s = dfd.sort[i];
                if (orderedResults == null)
                {
                    orderedResults = OrderBy(requestChats, s);
                }
                else
                {
                    orderedResults = ThenBy(orderedResults, s);
                }
            }
            if (orderedResults == null)
                orderedResults = requestChats.OrderBy(r => r.Vin);
            TotalCount = orderedResults.Count();

            if (dfd.draw == 1)
            {
                //dynamic o = new JObject();
                //o.userName = JArray.FromObject(requestChats.Select(r => r.UserName).Distinct<string>().ToArray());
                //o.userOrg = JArray.FromObject(requestChats.Select(r => r.UserOrg).Distinct<string>().ToArray());
                //o.dateSent = JArray.FromObject(requestChats.Select(r => r.DateSent).Distinct().OrderBy(x => x));

                //filters = o;
                filters = null;
            }
            else
            {
                filters = null;
            }

            var result = orderedResults.Skip(dfd.start).Take(dfd.length).ToList();
            return result;
        }

        public static IQueryable<RequestChats> Search(IQueryable<RequestChats> result, List<DatatableColumn> cols, string searchVal)
        {
            var predicate = PredicateBuilder.False<RequestChats>();
            if (string.IsNullOrWhiteSpace(searchVal))
                return result;

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
            Guid.TryParse(searchVal, out gCompare);

            foreach (DatatableColumn col in cols)
            {
                if (col.searchable)
                {
                    switch (col.data)
                    {
                        case "vin":
                            predicate = predicate.Or(p => p.Vin.Contains(searchVal));
                            break;
                        case "vendorid":
                            predicate = predicate.Or(p => isGuid && p.VendorId.Equals(gCompare)); break;
                        case "message":
                            predicate = predicate.Or(p => p.Message.Contains(searchVal));
                            break;
                    }
                }
            }
            return result.Where(predicate);
        }
        private static IQueryable<RequestChats> Where(IQueryable<RequestChats> result, DatatableColumn col)
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
            else
            {
                switch (col.data.ToLower())
                {
                    case "vin": result = result.Where(r => r.Vin == val); break;
                    case "vendorid":
                        result = result.Where(r => r.VendorId == new Guid(val)); break;
                }
            }
            return result;
        }
        private static IOrderedQueryable<RequestChats> OrderBy(IQueryable<RequestChats> result, DatatableSort s)
        {
            IOrderedQueryable<RequestChats> newResult = null;
            if (s.descending)
            {
                switch (s.columnName.ToLower())
                {
                    case "vin": newResult = result.OrderByDescending(r => r.Vin); break;
                    case "vendorid":
                        newResult = result.OrderByDescending(r => r.VendorId); break;
                }
            }
            else
            {
                switch (s.columnName.ToLower())
                {
                    case "vin": newResult = result.OrderBy(r => r.Vin); break;
                    case "vendorid":
                        newResult = result.OrderBy(r => r.VendorId); break;
                }
            }
            return newResult;
        }
        private static IOrderedQueryable<RequestChats> ThenBy(IOrderedQueryable<RequestChats> result, DatatableSort s)
        {
            if (s.descending)
            {
                switch (s.columnName.ToLower())
                {
                    case "vin": result = result.ThenByDescending(r => r.Vin); break;
                    case "vendorid":
                        result = result.ThenByDescending(r => r.VendorId); break;
                }
            }
            else
            {
                switch (s.columnName.ToLower())
                {
                    case "vin": result = result.ThenBy(r => r.Vin); break;
                    case "vendorid":
                        result = result.ThenBy(r => r.VendorId); break;
                }
            }
            return result;
        }

        private bool CurrentUserIdAndGroups(out Guid? vendorId, out Guid? groupId)
        {
            vendorId = null;
            groupId = null;

            UserInfo ui = GetCurrentUser();

            if (ui == null)
                return false;
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
    }
}