using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyDMVpro.Common.Extensions;
using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyDMVpro.Common.ViewHelpers
{
    public class FilterHelper<T>
    {
        protected readonly MaggardDMVContext _context;
        protected readonly IConfiguration _configuration;
        protected Controller _controller = null;
        protected Prefilter _prefilter = null;

        private int _maxcolumns = 50;
        public const int STATUS_ID_DELETE_PENDING = 99;
        public const int STATUS_ID_DELETED = 100;
        public const int STATUS_ID_HOLD = 9;

        public delegate IQueryable<T> Prefilter(IQueryable<T> recordtype, DatatableFormData dfd);

        public FilterHelper()
        {
        }
        public FilterHelper(MaggardDMVContext context, IConfiguration configuration, Controller controller, Prefilter prefilter)
        {
            _context = context;
            _configuration = configuration;
            _controller = controller;
            _prefilter = prefilter;
        }
        public FilterHelper(MaggardDMVContext context, IConfiguration configuration, Controller controller) : this(context, configuration, controller, null)
        {
        }

        protected Controller GetController()
        {
            return _controller;
        }
        protected int MaxColumns
        {
            get { return _maxcolumns; }
            set { _maxcolumns = value; }
        }

        public delegate IQueryable<T> Query(Guid? vendorId, Guid? groupId, Guid? userId);
        public IQueryable<T> Where(IQueryable<T> result, DatatableColumn col)
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

            string colName = col.data.ToLower();
            if (IsDateRange(colName, val, out var range))
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
                        result = result.Where(GetSelectorIsNotNullOrBlank(colName));
                    }
                    else if (matchNull)
                    {
                        result = result.Where(GetSelectorIsNullOrBlank(colName));
                    }
                    else
                    {
                        bool supportsLast6Vin = false;
                        if (colName == "vin" && singleVal.Length == 6 &&
                            typeof(T).GetProperty("last6vin", BindingFlags.IgnoreCase) != null)
                        {
                            supportsLast6Vin = true;
                        }

                        if (supportsLast6Vin && colName == "vin" && singleVal.Length == 6)
                        {
                            colName = "last6vin";
                            result = result.Where(GetIsEqualExpression(colName, singleVal, false));
                        }
                        else
                        {
                            result = result.Where(GetIsEqualExpression(colName, singleVal, col.searchable));
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine("Not yet implemented");
                }
            }
            return result;
        }
        public IQueryable<T> Where(IQueryable<T> result, Expression<Func<T, object>> selector, string value)
        {
            return result;// Default unchanged
        }
        public async Task<IActionResult> GetRecords(Query q, bool? active = null, bool? filterOnCurrentUser = null)
        {
            try
            {
                CurrentUserInfo userinfo = new();
                if (await CurrentUserIdAndGroupsAsync(userinfo))
                {
                    DatatableFormData dfd = GetData();

                    IQueryable<T> rows = q(userinfo.VendorId, userinfo.GroupId, filterOnCurrentUser == true ? userinfo.UserId : null);
                    var result = await GetTypeFiltered(rows, dfd);
                    return Json(new { status = "success", draw = dfd.draw, recordsFiltered = result.totalCount, recordsTotal = result.totalCount, data = result.data, columnFilters = result.filters });
                }
                return EmptyDataTablesQueryResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
                throw;
            }
        }

        protected IActionResult EmptyDataTablesQueryResult()
        {
            return Json(new { draw = 0, recordsFiltered = 0, recordsTotal = 0 });
        }
        protected IActionResult Json(object odata)
        {
            Controller c = GetController();
            if (c != null)
            {
                return c.Json(odata);
            }
            return null;
        }
        public HttpRequest GetRequest()
        {
            return _controller?.Request;
        }
        public DatatableFormData GetData()
        {
            DatatableFormData dfd = new DatatableFormData();
            HttpRequest request = GetRequest();

            if (request.HasFormContentType)
            {
                IFormCollection form = request.Form;

                var draw = form["draw"].FirstOrDefault();
                var start = form["start"].FirstOrDefault();
                var length = form["length"].FirstOrDefault();

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
                    var coldata = form[$"columns[{i}][data]"].FirstOrDefault();
                    if (coldata == null)
                        break;

                    DatatableColumn col = new DatatableColumn()
                    {
                        data = coldata.ToLower(),
                        name = form[$"columns[{i}][name]"].FirstOrDefault(),
                        orderable = (form[$"columns[{i}][orderable]"].FirstOrDefault() ?? "") == "true",
                        searchable = (form[$"columns[{i}][searchable]"].FirstOrDefault() ?? "") == "true",
                        searchRegex = (form[$"columns[{i}][search][regex]"].FirstOrDefault() ?? "false") == "true",
                        searchValue = form[$"columns[{i}][search][value]"].FirstOrDefault()
                    };
                    if (string.IsNullOrWhiteSpace(col.name))
                    {
                        System.Diagnostics.Trace.WriteLine($"Column data: {col.data}");
                        cols.Add(col);
                    }
                    else
                    {
                        cols.Add(col);
                    }
                }
                dfd.columns = cols;

                List<DatatableSort> sort = new List<DatatableSort>();
                for (int i = 0; i < MaxColumns; i++)
                {
                    var sortcol = form[$"order[{i}][column]"].FirstOrDefault();
                    if (sortcol == null)
                        break;
                    if (!string.IsNullOrWhiteSpace(sortcol))
                    {
                        string sDirection = form[$"order[{i}][dir]"].FirstOrDefault() ?? "";
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
                
                if (bool.TryParse(form["allGroupChats"], out bool allGroupChatsBool))
                {
                    dfd.allGroupChats = allGroupChatsBool;
                }

                dfd.pageSize = length != null ? Convert.ToInt32(length) : 0;
                dfd.skip = start != null ? Convert.ToInt32(start) : 0;
                dfd.globalSearch = form["search[value]"].FirstOrDefault();

                dfd.includeArchived = (form["includeArchived"].FirstOrDefault() == "true");
                dfd.includeCleared = (form["includeCleared"].FirstOrDefault() == "true");

                var customFilters = (form["dropdownFilters"].FirstOrDefault() ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries);
                dfd.FilterList = new List<string>(customFilters);
            }

            return dfd;
        }
        public IQueryable<T> Search(IQueryable<T> result, List<DatatableColumn> cols, string searchVal)
        {
            if (string.IsNullOrWhiteSpace(searchVal))
                return result;

            var predicate = PredicateBuilder.False<T>();

            foreach (DatatableColumn col in cols)
            {
                if (col.searchable)
                {
                    var selector = GetSelector(col.data);

                    predicate = Search(predicate, selector, searchVal);
                }
            }
            return result.Where(predicate);
        }
        protected Expression<Func<T, bool>> Search(Expression<Func<T, bool>> predicate, Expression property, string searchVal)
        {
            // NYI
            return predicate;
        }
        public Expression<Func<T, bool>> GetContainsExpression(string propertyName, string propertyValue)
        {
            var parameterExpression = Expression.Parameter(typeof(T), "type");
            var propertyExpression = Expression.Property(parameterExpression, propertyName);
            MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var someValue = Expression.Constant(propertyValue);
            var containsMethodExpression = Expression.Call(propertyExpression, method, someValue);
            var lambdaExpression = Expression.Lambda<Func<T, bool>>(containsMethodExpression, parameterExpression);
            return lambdaExpression;
        }

        public class FilteredResult
        {
            public List<T> data { get; set; }
            public int totalCount { get; set; }
            public object filters { get; set; }
        }
        public async Task<FilteredResult> GetTypeFiltered(IQueryable<T> recordtype, DatatableFormData dfd)
        {
            object filters;

            if (_prefilter != null)
            {
                recordtype = _prefilter(recordtype, dfd);
            }

            foreach (DatatableColumn col in dfd.columns)
            {
                recordtype = Where(recordtype, col);
            }

            // search by contains
            recordtype = Search(recordtype, dfd.columns, dfd.globalSearch);

            if (dfd.draw == 1 || dfd.skip == 0)
            {
                var o = new Dictionary<string, object>();
                foreach (var colName in dfd.FilterList)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(colName))
                        {
                            var filter = recordtype.Select(GetSelector(colName, true)).Distinct().ToArray().OrderBy(x => x).ToList();
                            o[colName] = filter;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.ToString());
                    }
                }
                filters = o;
            }
            else
            {
                filters = null;
            }

            IOrderedQueryable<T> orderedResults = null;
            for (int i = 0; i < dfd.sort.Count; i++)
            {
                DatatableSort s = dfd.sort[i];
                if (orderedResults == null)
                {
                    orderedResults = OrderBy(recordtype, s);
                }
                else
                {
                    orderedResults = ThenBy(orderedResults, s);
                }
            }


           
            List<T> dataset;
            int recordCount;

            if (dfd.length == 0)
            {
                if (orderedResults == null)
                {
                    dataset = await recordtype.ToListAsync();
                }
                else
                {
                    dataset = await orderedResults.ToListAsync();
                }

                recordCount = dataset.Count();
            }
            else
            {
                orderedResults ??= recordtype.OrderBy(r => 1);
                dataset = await orderedResults.Skip(dfd.start).Take(dfd.length).ToListAsync();
                recordCount = await orderedResults.CountAsync();
            }
          
            var result = new FilteredResult()
            {
                data = dataset,
                filters = filters,
                totalCount = recordCount
            };
            return result;
        }
        public Expression<Func<T, object>> GetSelector(DatatableSort s)
        {
            return GetSelector(s.columnName);
        }

        public Expression<Func<T, object>> GetSelector(string colname, bool datePartOnly = false)
        {
            try
            {
                Expression<Func<T, object>> selector = null;

                string colkey = colname.ToLower();

                if (string.IsNullOrWhiteSpace(colname)) return null;

                ParameterExpression argParam = Expression.Parameter(typeof(T), "r");
                MemberExpression property = Expression.Property(argParam, colname);
                Expression expression = property;
                if (expression.Type.IsValueType)
                {
                    if (datePartOnly && expression.Type == typeof(DateTime?))
                    {
                        MemberExpression valueExpression = Expression.Property(expression, "Value");
                        MemberExpression dateExpression = Expression.Property(valueExpression, "Date");
                        expression = Expression.Convert(dateExpression, typeof(object));
                    }
                    else
                    {
                        expression = Expression.Convert(expression, typeof(object));
                    }
                }
                var lambda = Expression.Lambda<Func<T, object>>(expression, argParam);
                return lambda;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                //throw;
                return null;
            }
        }

        public IOrderedQueryable<T> OrderBy(IQueryable<T> result, DatatableSort s)
        {
            IOrderedQueryable<T> newResult = null;
            Expression<Func<T, object>> selector = GetSelector(s);
            if (selector != null)
            {
                if (s.descending)
                {
                    newResult = result.OrderByDescending(selector);
                }
                else
                {
                    newResult = result.OrderBy(selector);
                }
            }
            else
            {
                // TODO: Review - what should we do here?  throw an error if selector is not found?
                if (s.descending)
                {
                    newResult = result.OrderByDescending(r => 1);
                }
                else
                {
                    newResult = result.OrderBy(r => 1);
                }
            }
            return newResult;
        }
        public IOrderedQueryable<T> ThenBy(IOrderedQueryable<T> result, DatatableSort s)
        {
            IOrderedQueryable<T> newResult = result;
            Expression<Func<T, object>> selector = GetSelector(s);

            if (s.descending)
            {
                if (s.descending)
                {
                    newResult = result.ThenByDescending(selector);
                }
                else
                {
                    newResult = result.ThenBy(selector);
                }
            }
            else
            {
                if (s.descending)
                {
                    newResult = result.ThenByDescending(r => 1);
                }
                else
                {
                    newResult = result.ThenBy(r => 1);
                }
            }
            return newResult;
        }
        protected virtual List<string> GetDateColumns()
        {
            return new List<string>();
        }

        private const string c_filterValueDelimiter = ";";

        protected List<string> GetMultipleValues(string val)
        {
            if (string.IsNullOrEmpty(val))
                return new List<string>();

            string[] values = val.Split(c_filterValueDelimiter, StringSplitOptions.None);
            return new List<string>(values);
        }
        protected int? SafeConvertInt(string s)
        {
            if (Int32.TryParse(s, out var val))
                return val;
            return null;
        }
        protected Expression GetProperty(string propname)
        {
            ParameterExpression argParam = Expression.Parameter(typeof(T), "r");
            Expression property = Expression.Property(argParam, propname);
            return property;
        }
        protected bool IsDateRange(string colName, string val, out DateRange range)
        {
            range = null;

            Expression e = GetProperty(colName);

            if (e == null || (e.Type != typeof(DateTime) && e.Type != typeof(DateTime?)))
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
        protected async Task<bool> CurrentUserIdAndGroupsAsync(CurrentUserInfo userinfo)
        {
            userinfo.VendorId = null;
            userinfo.GroupId = null;
            userinfo.UserId = null;

            UserInfo ui = await GetCurrentUserAsync(_controller.User, true);

            if (ui == null || ui.UserId == null)
                return false;
            userinfo.UserId = ui.UserId;

            if (ui.IsVendorAgent)
            {
                userinfo.VendorId = ui.VendorId;
            }
            if (ui.IsGroupMember)
            {
                userinfo.GroupId = ui.GroupId;
            }
            return true;
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


        protected string GetUserSID()
        {
            return _controller?.User?.GetUserSID();
        }
        protected ClaimsPrincipal User
        {
            get
            {
                return _controller?.User;
            }
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
        public UserInfo GetCurrentUser(bool forceDbLookup = true)
        {
            try
            {
                UserInfo ui = UserInfo.GetCurrentUser(User, forceDbLookup);
                return ui;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static async Task<UserInfo> GetCurrentUserAsync(ClaimsPrincipal user, bool forceDbLookup = true)
        {
            if (!user.Identity.IsAuthenticated)
                return null;

            UserInfo ui = new UserInfo();
            ClaimValues cv;
            if (forceDbLookup)
            {
                cv = await DataHelpers.GetClaimsForUserAsync(user.GetUserSID());
                ClaimsHelper.GetAuthenticationClaims(user, cv);
            }
            else
            {
                cv = ClaimsHelper.GetUserClaimsForDB(user);
            }
            ui.Init(cv);
            if (ui.NameIdentifierClaim == null)
                return null;
            return ui;
        }
        protected bool CurrentUserIsMemberOfGroupOrVendor(Guid? groupId, Guid? vendorId, out Guid? userId)
        {
            userId = null;
            UserInfo user = GetCurrentUser();
            if (user != null)
                userId = user.UserId;
            return UserIsMemberOfGroupOrVendor(user, groupId, vendorId);
        }
        protected bool CurrentUserIsMemberOfGroupOrVendor(Guid? groupId, Guid? vendorId)
        {
            UserInfo user = GetCurrentUser();
            return UserIsMemberOfGroupOrVendor(user, groupId, vendorId);
        }
        protected bool CurrentUserHasPermission(Guid? requestId)
        {
            if (requestId == null)
                return false;
            UserInfo user = GetCurrentUser();
            return UserHasRequestPermission(user, requestId.Value);
        }
        protected bool UserHasRequestPermission(UserInfo user, Guid? requestId)
        {
            if (requestId == null)
                return false;
            // TODO: check group and vendor guid for requestid
            var request = _context.Requests.FirstOrDefaultAsync(r => r.RequestId == requestId).Result;
            if (request == null)
                return false;
            return UserIsMemberOfGroupOrVendor(user, request.GroupId, request.VendorId);
        }
        protected bool UserIsMemberOfGroupOrVendor(UserInfo user, Guid? groupId, Guid? vendorId)
        {
            return ((user.GroupId != null && user.GroupId == groupId.Value)
                || (user.VendorId != null && user.VendorId == vendorId.Value));
        }
        protected bool UserIsMemberOfGroupOrVendor(Users user, Guid? groupId, Guid? vendorId)
        {
            if (user.Active)
            {
                if (user.UserGroups != null)
                {
                    if (user.UserGroups.Any(ug => ug.GroupId == groupId))
                    {
                        return true;
                    }
                }
                if (user.VendorAgent != null)
                {
                    if (user.VendorAgent.Any(va => va.VendorId == vendorId))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private Expression<Func<T, object>> CoalesceExpression(string colName, object value)
        {
            ParameterExpression argParam = Expression.Parameter(typeof(T), "r");
            Expression property = Expression.Property(argParam, colName);

            Expression eValue = null;
            if (value != null)
            {
                if (property.Type.Name == "Nullable`1")
                {
                    value = ConvertToType(property.Type, value);
                    eValue = Expression.Constant(value);
                    // Convert to nullable
                    eValue = Expression.Convert(eValue, property.Type);
                }
                else
                {
                    eValue = Expression.Constant(value);
                }
                if (eValue.Type.IsValueType)
                    eValue = Expression.Convert(eValue, typeof(object));
            }

            Expression expression = property;
            if (value != null)
            {
                expression = Expression.Coalesce(property, eValue);
            }
            if (expression.Type.IsValueType)
            {
                expression = Expression.Convert(expression, typeof(object));
            }
            var lambda = Expression.Lambda<Func<T, object>>(expression, argParam);
            return lambda;
        }
        private Expression<Func<T, bool>> GetIsEqualExpression(string colName, object value, bool searchable)
        {
            ParameterExpression argParam = Expression.Parameter(typeof(T), "r");

            Expression property = Expression.Property(argParam, colName);

            if (searchable && property.Type == typeof(string))
            {
                return GetContainsExpression(colName, value as string);
            }
            else
            {
                Expression eValue = null;
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
                var lambda = Expression.Lambda<Func<T, bool>>(expression, argParam);
                return lambda;
            }
        }
        private Expression<Func<T, bool>> GetSelectorIsNullOrBlank(string colName)
        {
            return GetIsNullOrNotNullExpression(colName, true);
        }
        private Expression<Func<T, bool>> GetSelectorIsNotNullOrBlank(string colName)
        {
            return GetIsNullOrNotNullExpression(colName, false);
        }
        private object ConvertToType(Type type, object value)
        {
            if (value == null)
            {
                return null;
            }
            if (value.GetType() == typeof(string))
            {
                return ConvertToType(type, value as string);
            }
            return value;
        }
        private object ConvertToType(Type type, string value)
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
        private Expression<Func<T, bool>> GetIsNullOrNotNullExpression(string colName, bool isNullCheck)
        {
            ParameterExpression argParam = Expression.Parameter(typeof(T), "r");
            Expression property = Expression.Property(argParam, colName);

            if (property.Type.Name == "Nullable`1")
            {
                var nullValue = Expression.Constant(null);
                Expression expression = Expression.Equal(property, nullValue);
                if (!isNullCheck) expression = Expression.Not(expression);
                var lambda = Expression.Lambda<Func<T, bool>>(expression, argParam);
                return lambda;
            }
            else
            {
                var blank = Expression.Constant("");
                var nullValue = Expression.Constant(null);

                Expression e1 = null;
                Expression e2 = Expression.Equal(property, nullValue);
                if (isNullCheck)
                {
                    e1 = Expression.Equal(property, nullValue);
                    e2 = Expression.Equal(property, blank);
                    var expression = Expression.OrElse(e1, e2);
                    var lambda = Expression.Lambda<Func<T, bool>>(expression, argParam);
                    return lambda;
                }
                else
                {
                    e1 = Expression.NotEqual(property, nullValue);
                    e2 = Expression.NotEqual(property, blank);
                    var expression = Expression.AndAlso(e1, e2);
                    var lambda = Expression.Lambda<Func<T, bool>>(expression, argParam);
                    return lambda;
                }
            }
        }
        private Expression<Func<T, bool>> GetDateRangeExpression(string colName, DateRange range)
        {
            ParameterExpression argParam = Expression.Parameter(typeof(T), "r");

            Expression property = Expression.Property(argParam, colName);

            var eFromDate = Expression.Constant(range.FromDate);
            var eFromConverted = Expression.Convert(eFromDate, property.Type);
            var eToDate = Expression.Constant(range.ToDate);
            var eToConverted = Expression.Convert(eToDate, property.Type);

            Expression e1 = Expression.GreaterThanOrEqual(property, eFromConverted);
            Expression e2 = Expression.LessThanOrEqual(property, eToConverted);

            var expression = Expression.AndAlso(e1, e2);
            var lambda = Expression.Lambda<Func<T, bool>>(expression, argParam);
            return lambda;
        }
    }
}
