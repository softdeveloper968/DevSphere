using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Controllers;
using MyDMVpro.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace MyDMVpro.Common;

public static class ConfigurationHelper
{
    public static IConfiguration Configuration { get; set; }
}
public static class LoggerHelper
{
    public static ILogger Logger { get; set; }
}


public class DataHelpers
{
    private static readonly HttpClient s_httpClient;

    static DataHelpers()
    {
        s_httpClient = new HttpClient(new SocketsHttpHandler()
        {
            // The maximum idle time for a connection in the pool. When there is no request in
            // the provided delay, the connection is released.
            // Default value in .NET 6: 1 minute
            //PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),

            // This property defines maximal connection lifetime in the pool regardless
            // of whether the connection is idle or active. The connection is reestablished
            // periodically to reflect the DNS or other network changes.
            // ⚠️ Default value in .NET 6: never
            //    Set a timeout to reflect the DNS or other network changes
            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
        });

        string sdays;

        sdays = ConfigurationHelper.Configuration["AppSettings:Active_Days_Vendor"] ?? "";
        Active_Days_Vendor = GetAbsIntMax(sdays, 30, 7);

        sdays = ConfigurationHelper.Configuration["AppSettings:Active_Days_LH"] ?? "";
        Active_Days_LH = GetAbsIntMax(sdays, 30, 14); // Default LH active days to 14

        sdays = ConfigurationHelper.Configuration["AppSettings:Archive_Days_LH"] ?? "";
        Archive_Days_LH = GetAbsIntMax(sdays, 30, 14);

        sdays = ConfigurationHelper.Configuration["AppSettings:Archive_Days_Vendor"] ?? "";
        Archive_Days_Vendor = GetAbsIntMax(sdays, 30, 7);

        InitViewDefinitions();
    }

    public static void InitViewDefinitions()
    {
        if (ViewFilterDefaults == null)
        {
            var list = new Dictionary<string, ViewDefinition>();
            try
            {
                var viewDefaults = GetViewDefinitions().Result;
                foreach (var fv in viewDefaults)
                {
                    list.Add(fv.ViewName, fv);
                }
                ViewFilterDefaults = list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error deserializing ViewDefaults\r\n{0}", ex.Message);

            }
        }
    }
    public static async Task<FeaturePermissionTypes> GetFeaturePermission(string featureKey, Guid? userId)
    {
        if (userId == null)
        {
            ThrowUnauthorizedAccessException();
        }

        using SqlConnection conn = new(SqlConnectionString);
        conn.Open();

        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.GetFeaturePermissions";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@featureKey", featureKey);
        // just start by getting the user's permissions
        cmd.Parameters.AddWithValue("@userId", userId);

        try
        {
            object oResult = await cmd.ExecuteScalarAsync();
            return (FeaturePermissionTypes)Convert.ToInt32(oResult);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("CheckFeaturePermission: {0}", ex);
            return FeaturePermissionTypes.None;
        }
    }

    [DoesNotReturn]
    public static void ThrowUnauthorizedAccessException()
    {
        string message = "User does not have permission to access this feature";
        throw new UnauthorizedAccessException(message);
    }

    public static async Task<bool> CheckFeaturePermission_Read(FeatureKey key, Guid? userId, bool throwIfNoPerm = true)
    {
        return await CheckFeaturePermission(key, userId, true, false, false, false, false, throwIfNoPerm);
    }
    public static async Task<bool> CheckFeaturePermission(FeatureKey key, Guid? userId, bool read = false, bool create = false, bool write = false, bool delete = false, bool execute = false, bool throwIfNoPerm = true)
    {
        string featureKey = Enum.GetName(typeof(FeatureKey), key);

        if (!(read || write || create || delete || execute))
        {
            throw new ApplicationException($"At least one permission must be requested for '{featureKey}'");
        }

        try
        {

            using SqlConnection conn = new(SqlConnectionString);

            await conn.OpenAsync();

            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "dbo.CheckFeaturePermission_v2";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@featureKey", featureKey);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@read", read);
            cmd.Parameters.AddWithValue("@create", create);
            cmd.Parameters.AddWithValue("@write", write);
            cmd.Parameters.AddWithValue("@delete", delete);
            cmd.Parameters.AddWithValue("@execute", execute);

            await cmd.ExecuteNonQueryAsync();

            return true;
        }
        catch (Exception ex)
        {
            if (throwIfNoPerm)
            {
                throw new ApplicationException($"Access denied to feature '{featureKey}'");
            }
            return false;
        }
    }

    public static async Task<DropDownListCollection> GetRequestStatusDropDowns(string sql, string dropdownParam)
    {
        var results = new DropDownListCollection();

        using SqlConnection conn = new(SqlConnectionString);

        await conn.OpenAsync();

        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;
        cmd.Parameters.AddWithValue("@dropdownParam", dropdownParam);

        var reader = await cmd.ExecuteReaderAsync();
        while (!reader.IsClosed)
        {
            try
            {
                DataTable dtDropdown = new DataTable();
                dtDropdown.Load(reader);
                if (dtDropdown.Rows.Count > 0)
                {
                    var ddl = new List<string>();
                    if (dtDropdown.Columns.Count == 1)
                    {
                        var fieldName = dtDropdown.Columns[0].ColumnName;
                        foreach (DataRow row in dtDropdown.Rows)
                        {
                            var val = row[0];
                            if (val != null && val is not DBNull)
                            {
                                if (val is DateTime?)
                                {
                                    DateTime? dt = val as DateTime?;
                                    ddl.Add(dt.Value.ToString("yyyy-MM-dd"));
                                }
                                else
                                {
                                    ddl.Add(val.ToString());
                                }
                            }
                        }
                        if (results.ContainsKey(fieldName))
                        {
                            var strings = results[fieldName];
                            strings.AddRange(ddl);
                            strings = strings.Distinct().OrderBy(s => s).ToList();
                            results[fieldName] = strings;
                        }
                        else
                        {
                            results.Add(fieldName, ddl);
                        }
                    }
                    else if (dtDropdown.Columns.Count > 1)
                    {
                        string fieldName = null;
                        foreach (DataRow row in dtDropdown.Rows)
                        {
                            var val = row[0];
                            if (val != null && val is not DBNull)
                            {
                                if (val is DateTime?)
                                {
                                    DateTime? dt = val as DateTime?;
                                    ddl.Add(dt.Value.ToString("yyyy-MM-dd"));
                                }
                                else
                                {
                                    ddl.Add(val.ToString());
                                }
                            }
                            fieldName ??= row[1]?.ToString();
                        }
                        if (results.ContainsKey(fieldName))
                        {
                            var strings = results[fieldName];
                            strings.AddRange(ddl);
                            strings = strings.Distinct().OrderBy(s => s).ToList();
                            results[fieldName] = strings;
                        }
                        else
                        {
                            results.Add(fieldName, ddl);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Expected 2 columns in dropdown result set");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
        }
        return results;
    }

    public static async Task<List<ApplicationTypes>> GetApplicationTypes(Guid? vendorId, Guid? groupId, string state = null)
    {
        var list = new List<ApplicationTypes>();

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "forms.GetSupportedAppTypes_v2";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@vendorId", EmptyToDbNull(vendorId));
            cmd.Parameters.AddWithValue("@groupId", EmptyToDbNull(groupId));

            StringBuilder sb = new();
            using var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
#if true
                    ApplicationTypes apt = new()
                    {
                        AppType = reader.GetStringSafe(0),
                        Description = reader.GetStringSafe(1),
                        Title = reader.GetStringSafe(1),
                        AppStates = new List<string>((reader.GetStringSafe(2).Split(new char[] { ',' })))
                    };
                    if (state != null)
                    {
                        string states = reader.GetStringSafe(2);
                        if (!states.Contains(state))
                        {
                            continue;
                        }
                    }
                    list.Add(apt);
#else
                        sb.Append(reader.GetString(0));
#endif
                }
            }
#if false
            list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ApplicationTypes>>(sb.ToString());
#endif
        }

        return list;
    }

    public static async Task<List<string>> GetVendorStates(Guid? vendorId, Guid? groupId)
    {
        var list = new List<string>();

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "forms.GetAppTypeStates_v2";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@vendorId", EmptyToDbNull(vendorId));
            cmd.Parameters.AddWithValue("@groupId", EmptyToDbNull(groupId));

            using var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    list.Add(reader.GetString(0));
                }
            }
        }

        return list.OrderBy(x => x).ToList();
    }
    public static async Task<ApplicationTypes> GetApplicationType(Guid? vendorId, string appType, string appState)
    {
        ApplicationTypes applicationType = null;

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "forms.GetAppType_v1_from_v2_tables_ver4";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@vendorId", EmptyToDbNull(vendorId));

            cmd.Parameters.AddWithValue("@appType", appType);
            cmd.Parameters.AddWithValue("@appState", appState);

            StringBuilder sb = new();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        string spartial = reader.GetString(0);
                        sb.Append(spartial);
                    }
                }
            }
            if (sb.Length == 0) sb.Append("{}");
            applicationType = Newtonsoft.Json.JsonConvert.DeserializeObject<ApplicationTypes>(sb.ToString());
            int fieldId = 1;
            // Set field id of all fields before passing
            // the view uses in a few places
            applicationType.AppFormSections ??= new List<AppFormSections>();
            applicationType.AppFormProcessFields ??= new List<AppFormProcessFields>();
            foreach (var section in applicationType.AppFormSections)
            {
                if (section.AppFormSectionFields == null)
                {
                    section.AppFormSectionFields = new List<AppFormSectionFields>();
                }
                else
                {
                    foreach (var field in section.AppFormSectionFields)
                    {
                        field.FieldId = fieldId++;
                    }
                }
            }
        }

        return applicationType;
    }
    public static async Task<List<ProcessFields>> GetProcessFieldsForType(Guid? vendorId, string appType, string appState)
    {
        var fields = new List<ProcessFields>();

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "forms.GetProcessFields";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@vendorId", EmptyToDbNull(vendorId));

            cmd.Parameters.AddWithValue("@appType", appType);
            cmd.Parameters.AddWithValue("@appState", appState);

            using var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    try
                    {
                        ProcessFields pf = new()
                        {
                            ProcessFieldId = reader.GetGuid(0),
                            FieldName = reader.GetStringSafe(1),
                            DisplayName = reader.GetStringSafe(2),
                            DotNetType = reader.GetStringSafe(3),
                            SqlFieldName = reader.GetStringSafe(4),
                            SqlType = reader.GetStringSafe(5)
                        };
                        fields.Add(pf);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine("GetProcessFieldsForType: error {0}", ex.Message);
                    }
                }
            }
        }

        return fields;

    }
    public static string GetDefaultFilterValue(string viewName, string fieldName)
    {
        string defaultFilter = "";
        if (ViewFilterDefaults.ContainsKey(viewName))
        {
            var vd = ViewFilterDefaults[viewName];
            foreach (var f in vd.Filter)
            {
                if (string.Compare(f.Field, fieldName, true) == 0)
                {
                    return f.Value;
                }
            }
        }
        return defaultFilter;
    }
    public class ViewColumn
    {
        public string Title { get; set; }
        public string Classes { get; set; }
    }
    public class FieldFilter
    {
        public string Field { get; set; }
        public string Value { get; set; }
    }
    public class DataTableColumnSortInfo
    {
        public string Class { get; set; }
        public bool Descending { get; set; }
    }
    public class ViewDefinition
    {
        public string ViewName { get; set; }
        public List<FieldFilter> Filter { get; set; }
        public List<ViewColumn> Columns { get; set; }
        public List<DataTableColumnSortInfo> Sort { get; set; }
    }
    public class UserViewDefaults : List<ViewDefinition>
    {
        public UserViewDefaults() { }
    }
    private static int GetAbsIntMax(string sval, int maxValue, int defaultValue)
    {
        if (!Int32.TryParse(sval, out int ival))
            ival = defaultValue;
        ival = Math.Min(Math.Abs(ival), maxValue);
        return ival;
    }
    public static void AddColumnFilters(string viewName, MyDMVpro.Models.FormsViewModels.RequestViewModel list)
    {
        AddColumnFilters(viewName, list.Columns);
    }
    public static void AddColumnFilters(string viewName, MyDMVpro.Common.ViewHelpers.DatatableColumnCollection columns)
    {
        if (DataHelpers.ViewFilterDefaults.ContainsKey(viewName))
        {
            DataHelpers.ViewDefinition filter = DataHelpers.ViewFilterDefaults[viewName];
            foreach (var f in filter.Filter)
            {
                columns.Where(c => c.Title == f.Field).FirstOrDefault().DefaultFilterValue = f.Value;
            }
        }
        else
        {
            System.Diagnostics.Trace.WriteLine($"View not found in configuration: {viewName}'");
        }
    }
    public static async Task<List<ViewDefinition>> GetViewDefinitions()
    {
        List<ViewDefinition> viewFilters = new();
        Dictionary<string, string> filterJson = new();

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT ViewName, json FROM ViewDefinitions";
            cmd.CommandType = CommandType.Text;

            using var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    try
                    {
                        string viewName = reader.GetStringSafe(0);
                        string json = reader.GetStringSafe(1);
                        filterJson.Add(viewName, json);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine("GetViewDefinitions: error {0}", ex.Message);
                    }
                }
            }
        }
        foreach (var key in filterJson.Keys)
        {
            string json = filterJson[key];
            try
            {
                var viewFilter = Newtonsoft.Json.JsonConvert.DeserializeObject<ViewDefinition>(json);
                if (viewFilter.ViewName != key)
                {
                    // ignore
                }
                else
                {
                    viewFilters.Add(viewFilter);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("GetViewDefinitions: error {0}", ex.Message);
            }
        }

        return viewFilters;
    }
    public static void AddColumnFilters(string viewName, MyDMVpro.Models.FormsViewModels.VendorRequestViewModel list)
    {
        AddColumnFilters(viewName, list.Columns);
    }
    public static bool UpdateViewColumns(string viewName, MyDMVpro.Common.ViewHelpers.BaseListViewModel list)
    {
        if (DataHelpers.ViewFilterDefaults.ContainsKey(viewName))
        {
            var filter = DataHelpers.ViewFilterDefaults[viewName];
            foreach (var f in filter.Columns)
            {
                list.Columns.Add(f.Title, f.Classes);
            }
            foreach (var sort in filter.Sort)
            {
                list.SortColumns.Add(sort.Class, sort.Descending);
            }
            foreach (var f in filter.Filter)
            {
                try
                {
                    list.Columns.Where(c => c.Title == f.Field).FirstOrDefault().DefaultFilterValue = f.Value;
                }
                catch (Exception ex)
                {
                    // TBD Log error
                }
            }
            return true;
        }
        else
        {
            System.Diagnostics.Trace.WriteLine($"View not found in configuration: {viewName}'");
            return false;
        }
    }
    public static int Active_Days_Vendor { get; private set; }
    public static int Active_Days_LH { get; private set; }
    public static int Archive_Days_LH { get; private set; }
    public static int Archive_Days_Vendor { get; private set; }

    public static Dictionary<string, ViewDefinition> ViewFilterDefaults { get; private set; }

    public static string AutoIMSTriggerUrl { get { return ConfigurationHelper.Configuration["AutoIMS:TriggerUrl"]; } }
    public static string AutoIMSMergeCRUrl { get { return ConfigurationHelper.Configuration["AutoIMS:MergeCRHttpUrl"]; } }
    public static string FormAnalyzerTriggerUrl { get { return ConfigurationHelper.Configuration["FormAnalyzer:TriggerUrl"]; } }
    public static string SqlConnectionString
    {
        get
        {
            return ConfigurationHelper.Configuration.GetConnectionString("DmvConnection");
        }
    }
    public static async Task<bool> IsVendorAgent(string username)
    {
        Guid? vendorId;
        (_, vendorId, _, _, _) = await GetVendorInfoForAgent(username);
        return (vendorId.HasValue && vendorId != Guid.Empty);
    }
    public static bool ResendGroupInvite(string siteUrl, string inviteeEmail, string groupName, Guid inviteId)
    {
        return SendGroupInviteEmail(siteUrl, groupName, inviteId, inviteeEmail);
    }
    public static bool ResendVendorInvite(string siteUrl, string inviteeEmail, string vendorName, Guid inviteId)
    {
        return SendVendorInviteEmail(siteUrl, vendorName, inviteId, inviteeEmail);
    }
    public static async Task<List<Guid>> GetLinkedRequests(Guid requestID)
    {
        List<Guid> requestIDs = new();

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "LinkedRequests_Get";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@RequestID", SqlDbType.UniqueIdentifier).Value = requestID;

            using var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    requestIDs.Add(reader.GetGuid(0));
                }
            }
        }
        return requestIDs;
    }

    public static async Task<List<Guid>> AddRequestLink(Guid? vendorID, Guid? agentID, Guid? requestID, Guid? linkedID, int? linkedReqNo)
    {
        List<Guid> requestIDs = new();
        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "LinkedRequest_Add";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@VendorID", SqlDbType.UniqueIdentifier).Value = vendorID;
            cmd.Parameters.Add("@AgentID", SqlDbType.UniqueIdentifier).Value = agentID;
            cmd.Parameters.Add("@PrimaryRequestID", SqlDbType.UniqueIdentifier).Value = requestID;

            cmd.Parameters.Add("@LinkRequestID", SqlDbType.UniqueIdentifier).Value = EmptyToDbNull(linkedID);
            cmd.Parameters.Add("@LinkReqno", SqlDbType.Int).Value = EmptyToDbNull(linkedReqNo);

            await cmd.ExecuteNonQueryAsync();
        }
        return requestIDs;
    }
    public static async Task<List<Guid>> RemoveRequestLink(Guid? vendorID, Guid? agentID, Guid? requestID, Guid? linkRequestID)
    {
        List<Guid> requestIDs = new();
        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "LinkedRequest_Remove";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@VendorID", SqlDbType.UniqueIdentifier).Value = vendorID;
            cmd.Parameters.Add("@AgentID", SqlDbType.UniqueIdentifier).Value = agentID;
            cmd.Parameters.Add("@PrimaryRequestID", SqlDbType.UniqueIdentifier).Value = requestID;
            cmd.Parameters.Add("@LinkRequestID", SqlDbType.UniqueIdentifier).Value = linkRequestID;
            await cmd.ExecuteNonQueryAsync();
        }
        return requestIDs;
    }

    public static async Task<List<Guid>> GetCRAttachmentsForRequests(List<Guid> requestIDs)
    {
        List<Guid> attachmentIDs = new();
        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "GetCRAttachmentsForRequests";
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", requestIDs);

            using var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    attachmentIDs.Add(reader.GetGuid(0));
                }
            }
        }
        return attachmentIDs;
    }
    public static async Task<bool> SendVendorInvite(string siteUrl, string createdByUser, Guid vendorId, string inviteeEmail, bool makeVendorAdmin)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $@"CreateVendorInvite";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@userName", createdByUser);
        p = cmd.Parameters.AddWithValue("@inviteEmail", inviteeEmail);
        p = cmd.Parameters.AddWithValue("@makeVendorAdmin", makeVendorAdmin);
        p = cmd.Parameters.AddWithValue("@vendorId", vendorId);
        p = cmd.Parameters.Add("@vendorName", SqlDbType.NVarChar, 100);
        p.Direction = ParameterDirection.Output;
        p = cmd.Parameters.Add("@inviteId", SqlDbType.UniqueIdentifier);
        p.Direction = ParameterDirection.Output;

        object oResult = await cmd.ExecuteNonQueryAsync();

        try
        {
            Guid inviteId = (Guid)cmd.Parameters["@inviteId"].Value;
            string vendorName = (string)cmd.Parameters["@vendorName"].Value;
            if (inviteId != Guid.Empty)
            {
                if (SendVendorInviteEmail(siteUrl, vendorName, inviteId, inviteeEmail))
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("SendVendorInvite: {0}", ex);
            return false;
        }
    }
    public static async Task<bool> SendGroupInvite(string siteUrl, string createdByUser, Guid groupId, string inviteeEmail, bool makeGroupAdmin)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $@"CreateGroupInvite";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@userName", createdByUser);
        p = cmd.Parameters.AddWithValue("@inviteEmail", inviteeEmail);
        p = cmd.Parameters.AddWithValue("@makeGroupAdmin", makeGroupAdmin);
        p = cmd.Parameters.AddWithValue("@groupId", groupId);
        p = cmd.Parameters.Add("@groupName", SqlDbType.NVarChar, 100);
        p.Direction = ParameterDirection.Output;
        p = cmd.Parameters.Add("@inviteId", SqlDbType.UniqueIdentifier);
        p.Direction = ParameterDirection.Output;

        object oResult = await cmd.ExecuteNonQueryAsync();

        try
        {
            Guid? inviteId = (cmd.Parameters["@inviteId"].Value) as Guid?;
            string groupName = (string)cmd.Parameters["@groupName"].Value;
            if (inviteId.HasValue)
            {
                if (SendGroupInviteEmail(siteUrl, groupName, inviteId.Value, inviteeEmail))
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("SendGroupInvite: {0}", ex);
            return false;
        }
    }
    public static async Task<string> GetGroupUserIDs(Guid groupId)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $@"GetGroupUsers";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@groupId", groupId);
        //p = cmd.Parameters.AddWithValue("@fmt", "json");

        try
        {
            object oResult = await cmd.ExecuteScalarAsync();

            return (oResult as string);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("GetGroupUsers: {0}", ex);
            return null;
        }
    }
    static bool SendGroupInviteEmail(string siteUrl, string groupName, Guid inviteId, string inviteeEmail)
    {
        try
        {
            return SmtpHelper.SendGroupInvite(siteUrl, null, inviteeEmail, inviteId, groupName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("SendGroupInviteEmailGroupInvite: {0}", ex);
            return false;
        }
    }
    static bool SendVendorInviteEmail(string siteUrl, string vendorName, Guid inviteId, string inviteeEmail)
    {
        try
        {
            return SmtpHelper.SendVendorInvite(siteUrl, null, inviteeEmail, inviteId, vendorName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("SendVendorInviteEmailGroupInvite: {0}", ex);
            return false;
        }
    }

    public static async Task<bool> GetSysAdminInfo(string username)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $@"GetSystemAdminInfoForUser";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@userName", username);
        p = cmd.Parameters.Add("@isSysAdmin", SqlDbType.Bit);
        p.Direction = ParameterDirection.Output;

        object oResult = await cmd.ExecuteNonQueryAsync();

        try
        {
            bool isAdmin = (bool)cmd.Parameters["@isSysAdmin"].Value;

            return isAdmin;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("GetSysAdminInfo: {0}", ex);
            return false;
        }
    }
    public static async Task<(Guid? userId, Guid? groupId, string groupName, bool isGroupAdmin)> GetGroupInfoForUser(string username)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $@"GetGroupInfoForUser";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@userName", username);
        p = cmd.Parameters.Add("@groupId", SqlDbType.UniqueIdentifier, -1);
        p.Direction = ParameterDirection.Output;
        p = cmd.Parameters.Add("@groupName", SqlDbType.NVarChar, 100);
        p.Direction = ParameterDirection.Output;
        p = cmd.Parameters.Add("@isGroupAdmin", SqlDbType.Bit);
        p.Direction = ParameterDirection.Output;
        p = cmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier, -1);
        p.Direction = ParameterDirection.Output;

        try
        {
            object oResult = await cmd.ExecuteNonQueryAsync();
            string name = null;
            Guid? id = null;
            Guid? userId = null;
            bool isAdmin = false;

            object o;
            o = cmd.Parameters["@groupName"].Value;
            if (o is DBNull)
                name = null;
            else
                name = (string)o;
            o = cmd.Parameters["@groupId"].Value;
            if (o is DBNull)
                id = null;
            else
                id = (Guid)o;
            o = cmd.Parameters["@userId"].Value;
            if (o is DBNull)
                userId = null;
            else
                userId = (Guid)o;

            o = cmd.Parameters["@isGroupAdmin"].Value;
            if (o is DBNull)
                isAdmin = false;
            else
                isAdmin = (bool)o;

            return (userId, groupId: id, groupName: name, isGroupAdmin: isAdmin);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("GetGroupInfoForUser: {0}", ex);
            return (userId: null, groupId: null, groupName: null, isGroupAdmin: false);
        }
    }
    public static async Task<bool> IsUserRegistered(string username)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT COUNT(*) FROM Users WHERE UserPrincipalName = @username OR NameIdentifierClaim = @username";
        cmd.CommandType = CommandType.Text;

        SqlParameter p;
        p = cmd.Parameters.AddWithValue("@userName", username);

        object oResult = await cmd.ExecuteScalarAsync();
        if (Convert.ToInt32(oResult) == 1)
        {
            return true;
        }
        return false;
    }
    public static async Task<(Guid? agentId, Guid? vendorId, string vendorCode, string vendorName, bool isVendorAdmin)> GetVendorInfoForAgent(string username)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $@"GetVendorInfoForAgent";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@userName", username);
        p = cmd.Parameters.Add("@vendorId", SqlDbType.UniqueIdentifier, -1);
        p.Direction = ParameterDirection.Output;
        p = cmd.Parameters.Add("@agentId", SqlDbType.UniqueIdentifier, -1);
        p.Direction = ParameterDirection.Output;
        p = cmd.Parameters.Add("@vendorCode", SqlDbType.NVarChar, 10);
        p.Direction = ParameterDirection.Output;
        p = cmd.Parameters.Add("@vendorName", SqlDbType.NVarChar, 100);
        p.Direction = ParameterDirection.Output;
        p = cmd.Parameters.Add("@isVendorAdmin", SqlDbType.Bit);
        p.Direction = ParameterDirection.Output;

        object oResult = await cmd.ExecuteNonQueryAsync();

        try
        {
            string name = (string)cmd.Parameters["@vendorName"].Value;
            string code = (string)cmd.Parameters["@vendorCode"].Value;
            Guid id = (Guid)cmd.Parameters["@vendorId"].Value;
            Guid agentId = (Guid)cmd.Parameters["@agentId"].Value;
            bool admin = (bool)cmd.Parameters["@isVendorAdmin"].Value;
            return (agentId, vendorId: id, vendorCode: code, vendorName: name, isVendorAdmin: admin);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("GetVendorInfoForAgent: {0}", ex);
            return (null, null, null, null, false);
        }
    }
    public static async Task<(Guid?, string)> VendorSubmitToFileUploads(BaseMaggardDMVContext dbcontext, string filename, Guid? groupId, Guid? userId, string userPrincipalName, byte[] data, bool submitDirect, DateTime? dateToVendor, bool requireUserId)
    {
        string errorMessage = null;
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = $@"VendorUploadRequestFile";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter p;

            p = cmd.Parameters.Add("@filename", SqlDbType.NVarChar, 256);
            p.Value = filename;

            p = cmd.Parameters.AddWithValue("@groupId", EmptyToDbNull(groupId));
            p = cmd.Parameters.AddWithValue("@userId", EmptyToDbNull(userId));
            p = cmd.Parameters.AddWithValue("@agentName", userPrincipalName);

            p = cmd.Parameters.Add("@fileimage", SqlDbType.Image);
            p.Value = data;

            object oResult = await cmd.ExecuteScalarAsync();
            if (oResult is Guid fileUploadId)
            {
                try
                {
                    await DataHelpers.ProcessRequestFile(dbcontext, (Guid)oResult, submitDirect, dateToVendor, requireUserId);
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error processing worksheet: {ex.Message}";
                }
                return (fileUploadId, errorMessage);
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error uploading worksheet: {ex.Message}";
        }
        return (null, errorMessage);
    }

    public static async Task UpdateFormAnalyzerUploadVin(Guid? userId, Guid? vendorId, Guid? uploadId, string vin)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = $@"FormAnalyzer_UpdateVin";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter p;

            p = cmd.Parameters.AddWithValue("@userId", EmptyToDbNull(userId));
            p = cmd.Parameters.AddWithValue("@vendorId", vendorId.Value);
            p = cmd.Parameters.AddWithValue("@fileUploadId", uploadId.Value);
            p = cmd.Parameters.AddWithValue("@vin", vin);

            int result = cmd.ExecuteNonQuery();
            if (result != -1)
            {
                System.Diagnostics.Trace.TraceWarning($"UploadFormAnalyzerFile sproc returned {result}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"UpdateFormAnalyzerUploadVin: {ex.Message}");
        }
    }

    public static async Task<(Guid?, string)> SubmitToFormAnalyzerUploads(string filename, Guid? userId, string userPrincipalName, Guid? formId, byte[] data, bool? allowDuplicates, DateTime? filedatetime)
    {
        string errorMessage = null;

        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = $@"UploadFormAnalyzerFile";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter p;

            p = cmd.Parameters.Add("@filename", SqlDbType.NVarChar, 256);
            p.Value = filename;

            p = cmd.Parameters.AddWithValue("@userId", EmptyToDbNull(userId));
            p = cmd.Parameters.AddWithValue("@userName", userPrincipalName);
            if (formId != null)
            {
                p = cmd.Parameters.AddWithValue("@formId", formId.Value);
            }
            p = cmd.Parameters.Add("@fileimage", SqlDbType.Image);
            p.Value = data;

            if (filedatetime.HasValue)
            {
                cmd.Parameters.AddWithValue("@filedate", filedatetime.Value);
            }

            SqlParameter pFileUploadId = cmd.Parameters.Add("@fileUploadId", SqlDbType.UniqueIdentifier);
            pFileUploadId.Direction = ParameterDirection.Output;

            if (allowDuplicates != null)
            {
                cmd.Parameters.AddWithValue("@ignoreDupe", allowDuplicates.Value);
            }

            int result = cmd.ExecuteNonQuery();
            if (result == -1)
            {
                Guid? fileUploadId = (Guid?)pFileUploadId.Value;
                try
                {
                    // Trigger analyzer run here
                    if (fileUploadId == null)
                    {
                        System.Diagnostics.Trace.TraceError($"UploadFormAnalyzerFile returned null fileUploadId");
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error processing: {ex.Message}";
                }
                return (fileUploadId, errorMessage);
            }
            else
            {
                System.Diagnostics.Trace.TraceWarning($"UploadFormAnalyzerFile sproc returned {result}");
            }
            return (null, errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error uploading attachment: {ex.Message}";
        }
        return (null, errorMessage);
    }

    public static async Task<(Guid?, string)> FileLibrary_UploadFile(string filename, Guid? userId, byte[] data, bool? allowDuplicates, DateTime? filedatetime, Guid? attachmentTypeId)
    {
        string errorMessage = null;

        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = $@"FileLibrary_UploadFile_v2";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter p;

            p = cmd.Parameters.Add("@filename", SqlDbType.NVarChar, 256);
            p.Value = filename;

            p = cmd.Parameters.AddWithValue("@userId", EmptyToDbNull(userId));
            p = cmd.Parameters.Add("@fileimage", SqlDbType.Image);
            p.Value = data;

            p = cmd.Parameters.Add("@attachmentTypeId", SqlDbType.UniqueIdentifier);
            p.Value = attachmentTypeId;

            if (filedatetime.HasValue)
            {
                cmd.Parameters.AddWithValue("@filedate", filedatetime.Value);
            }

            SqlParameter pAttachmentId = cmd.Parameters.Add("@attachmentId", SqlDbType.UniqueIdentifier);
            pAttachmentId.Direction = ParameterDirection.Output;

            allowDuplicates ??= true; // default to allow
            if (allowDuplicates != null)
            {
                cmd.Parameters.AddWithValue("@ignoreDupe", allowDuplicates.Value);
            }

            int result = cmd.ExecuteNonQuery();
            if (result == -1)
            {
                Guid? attachmentId = (Guid?)pAttachmentId.Value;
                try
                {
                    // Trigger analyzer run here
                    if (attachmentId == null)
                    {
                        System.Diagnostics.Trace.TraceError($"FileLibrary_UploadFile returned null attachmentId");
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error processing: {ex.Message}";
                }
                return (attachmentId, errorMessage);
            }
            else
            {
                System.Diagnostics.Trace.TraceWarning($"FileLibrary_UploadFile sproc returned {result}");
            }
            return (null, errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error uploading attachment: {ex.Message}";
        }
        return (null, errorMessage);
    }
    public static async Task<(Guid?, string)> SubmitToFileUploads(BaseMaggardDMVContext dbcontext, string filename, Guid? userId, Guid? groupId, string userPrincipalName, byte[] data, bool requireUserId)
    {
        string errorMessage = null;

        try
        {
            string excelJson = DataHelpers.GetJsonFromExcelFile(data, groupId);

            // Add vin lookups here
            excelJson = await PerformVinLookups(dbcontext, excelJson);

            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = $@"UploadRequestFile";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter p;

            p = cmd.Parameters.Add("@filename", SqlDbType.NVarChar, 256);
            p.Value = filename;

            p = cmd.Parameters.AddWithValue("@userId", EmptyToDbNull(userId));
            p = cmd.Parameters.AddWithValue("@userName", userPrincipalName);

            p = cmd.Parameters.Add("@fileimage", SqlDbType.Image);
            p.Value = data;

            object oResult = await cmd.ExecuteScalarAsync();
            if (oResult is Guid fileUploadId)
            {
                try
                {
                    await DataHelpers.ProcessRequestFile(dbcontext, fileUploadId, submitDirect: false, dateToVendor: null, requireUserId: requireUserId);
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error processing worksheet: {ex.Message}";
                }
                return (fileUploadId, errorMessage);
            }
            return (null, errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error uploading worksheet: {ex.Message}";
        }
        return (null, errorMessage);
    }

    public static async Task ProcessRequestFile(BaseMaggardDMVContext dbcontext, Guid fileUploadId, bool submitDirect = false, DateTime? dateToVendor = null, bool requireUserId = false)
    {
        byte[] data = null;
        Guid? userId = null;
        Guid? agentId = null;
        Guid? groupId = null;

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = $@"GetRequestFileWithGroup";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter p;

            p = cmd.Parameters.AddWithValue("@fileUploadId", fileUploadId);
            p.Direction = ParameterDirection.Input;
            p = cmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier);
            p.Direction = ParameterDirection.Output;
            p = cmd.Parameters.Add("@groupId", SqlDbType.UniqueIdentifier);
            p.Direction = ParameterDirection.Output;
            p = cmd.Parameters.Add("@image", SqlDbType.VarBinary, -1);
            p.Direction = ParameterDirection.Output;
            p = cmd.Parameters.Add("@uploadByAgent", SqlDbType.UniqueIdentifier);
            p.Direction = ParameterDirection.Output;

            object oResult = await cmd.ExecuteNonQueryAsync();
            userId = (Guid)cmd.Parameters["@userId"].Value as Guid?;
            groupId = (Guid)cmd.Parameters["@groupId"].Value as Guid?;
            agentId = cmd.Parameters["@uploadByAgent"].Value as Guid?;
            data = (byte[])cmd.Parameters["@image"].Value;
        }
        if (userId != null)
        {
            await ProcessExcelFile(dbcontext, fileUploadId, data, userId.Value, groupId: groupId, agentUploaded: agentId, submitDirect: submitDirect, requireUserId: requireUserId, dateToVendor: dateToVendor);
        }
    }
    public static async Task<byte[]> GetPdfFormTemplate(Guid vendorId, string formId)
    {
        byte[] data = null;

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = $@"GetFormTemplate";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter p;

            p = cmd.Parameters.AddWithValue("@vendorId", vendorId);
            p.Direction = ParameterDirection.Input;
            p = cmd.Parameters.AddWithValue("@formId", formId);
            p.Direction = ParameterDirection.Input;
            p = cmd.Parameters.Add("@image", SqlDbType.VarBinary, -1);
            p.Direction = ParameterDirection.Output;

            object oResult = await cmd.ExecuteNonQueryAsync();
            data = (byte[])cmd.Parameters["@image"].Value;
        }
        return data;
    }
    public static async Task<string> GetImportMapping(Guid? groupId)
    {
        string jmapping = null;
        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = $@"SELECT @jMapping = jMapping FROM ImportMapping WHERE GroupId = @groupId";
            cmd.CommandType = CommandType.Text;

            SqlParameter p;

            p = cmd.Parameters.AddWithValue("@groupId", groupId);
            p.Direction = ParameterDirection.Input;

            p = cmd.Parameters.Add("@jMapping", SqlDbType.NVarChar, -1);
            p.Direction = ParameterDirection.Output;

            object oResult = await cmd.ExecuteNonQueryAsync();
            jmapping = (string)cmd.Parameters["@jMapping"].Value;
        }
        return jmapping;
    }
    public static async Task ProcessExcelFile(BaseMaggardDMVContext dbcontext, Guid fileUploadId, byte[] data, Guid userId, Guid? groupId, Guid? agentUploaded, bool submitDirect, DateTime? dateToVendor, bool requireUserId)
    {
        string json = GetJsonFromExcelFile(data, groupId);
    
        // Add vin lookups here
        json = await PerformVinLookups(dbcontext, json);

        await SaveToRequests(fileUploadId, userId, groupId, json, agentUploaded, submitDirect, dateToVendor, requireUserId);
    }

    public static async Task<string> PerformVinLookups(BaseMaggardDMVContext dbcontext, string json)
    {
        var rows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
        bool updated = false;
        var enumerator = rows;
        foreach (var row in rows)
        {
            if (row.ContainsKey("Vehicle Vin"))
            {
                var vin = row["Vehicle Vin"];

                var lookups = await VinHelper.LookupVinDetails(dbcontext, vin);
                if (lookups.vpd != null && lookups.vpd.Count > 0)
                {
                    var vpd = lookups.vpd[0];
                    vpd.InitFieldMappings();
                    foreach (var mapping in vpd.FieldMappings)
                    {
                        if (row.ContainsKey(mapping.Key))
                        {
                            var val = row[mapping.Key];
                            if (string.IsNullOrWhiteSpace(val))
                            {
                                try
                                {
                                    row[mapping.Key] = mapping.Value;
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                                }
                                updated = true;
                            }
                        }
                        else
                        {
                            try
                            {
                                row.Add(mapping.Key, mapping.Value);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.ToString());
                            }
                            updated = true;
                        }
                    }
                }
            }
        }
        if (updated)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(rows);
        }
        return json;
    }

#if false
    public static async Task<string> ValidateExcelFile(Guid fileUploadId, byte[] data, Guid userId, Guid? groupId)
    {
        string json = GetJsonFromExcelFile(data, groupId);
        return ValidateExcelFileJson(json);
    }
    public static string ValidateExcelFileJson(string json)
    {
        return "{}";
    }
#endif
    public static string GetJsonFromExcelFile(byte[] data, Guid? groupId)
    {
        using MemoryStream ms = new(data);
        string json = ExcelHelper.GetRangeJson(ms, groupId);
        return json;
    }
    public static async Task SaveToRequests(Guid fileUploadId, Guid userId, Guid? groupId, string jsonArray, Guid? uploadByAgent, bool submitDirect = false, DateTime? dateToVendor = null, bool requireUserId = false)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"ProcessFileUploadJson_v2";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@fileUploadId", fileUploadId);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.Add("@groupId", SqlDbType.UniqueIdentifier).Value = groupId;
        cmd.Parameters.AddWithValue("@jsonArray", jsonArray);
        cmd.Parameters.AddWithValue("@uploadByAgent", uploadByAgent);
        cmd.Parameters.AddWithValue("@submitDirect", submitDirect);
        cmd.Parameters.AddWithValue("@requireUserId", requireUserId);
        if (dateToVendor != null)
        {
            cmd.Parameters.AddWithValue("@dateToVendor", dateToVendor);
        }

        await cmd.ExecuteNonQueryAsync();
    }
    private static List<jsonRequestObject> GetRequestObjects(SqlCommand cmd)
    {
        List<MyDMVpro.Models.jsonRequestObject> list = new();

        using (var reader = cmd.ExecuteReaderAsync().Result)
        {
            while (reader.ReadAsync().Result)
            {
                jsonRequestObject r = GetRequestObject(reader);
                if (r != null)
                {
                    list.Add(r);
                }
            }
        }
        return list;
    }
    private static jsonRequestObject GetRequestObject(SqlDataReader reader)
    {
        try
        {
            jsonRequestObject r = new()
            {
                RequestId = reader["RequestId"] as Guid?,
                GroupId = reader["GroupId"] as Guid?,
                UserId = reader["UserId"] as Guid?,
                VendorId = reader["VendorId"] as Guid?,
                jRequest = JsonConvert.DeserializeObject<dynamic>(reader["jRequest"].ToString()),
                Signed = reader["Signed"] as bool?,
                DateSigned = reader["DateSigned"] as DateTime?,
                Lienholder = reader["Lienholder"] as string,
                HasActiveChat = Convert.ToBoolean(reader["HasActiveChat"]),
                HasNewChat = Convert.ToBoolean(reader["HasNewChat"]),
                WaitingForVendorReply = Convert.ToBoolean(reader["WaitingForVendorReply"]),
                WaitingForUserReply = Convert.ToBoolean(reader["WaitingForUserReply"]),
                SubmittedBy = reader["SubmittedBy"] as string,
            };
            try
            {
                r.HasAttachments = (Convert.ToInt32(reader["AttachmentCount"]) > 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("GetRequestObject(HasAttachments): {0}", ex);
            }
            try
            {
                if (reader["AttachmentStatus"] == DBNull.Value)
                {
                    r.AttachmentStatus = AttachmentStatusEnum.NoConditionsRequired; // no conditions required
                }
                else
                {
                    r.AttachmentStatus = Convert.ToInt32(reader["AttachmentStatus"]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("GetRequestObject(AttachmentStatus): {0}", ex);
            }

            try
            {
                r.FileUploadId = reader["FileUploadId"] as Guid?;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("GetRequestObject(FileUploadId): {0}", ex);
            }
            try
            {
                r.CheckVIN = !VinHelper.IsValidVIN(JRequest.GetVIN(r.jRequest));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("GetRequestObject(CheckVIN): {0}", ex);
            }
            if (r.DateSigned.HasValue)
                r.DateSigned = r.DateSigned.Value.Date; // remove timestamp
            return r;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("GetRequestObject: {0}", ex);
        }
        return null;
    }

    private static string GetDateOnly(string dateTimeStr)
    {
        return DateToJsonString(GetDateFromString(dateTimeStr));
    }
    private static string DateToJsonString(DateTime? dt)
    {
        if (dt.HasValue)
        {
            return dt.Value.ToString("yyyy-MM-dd");
        }
        return "";
    }
    private static DateTime? GetDateFromString(string dateString)
    {
        if (DateTime.TryParse(dateString, out DateTime dt))
        {
            return dt.Date;
        }
        return null;
    }

    public static async Task<MyDMVpro.Models.jsonRequestObject> GetRequest(Guid requestId, string userName)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"GetRequest";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@requestId", requestId);
        p = cmd.Parameters.AddWithValue("@userName", userName);

        List<jsonRequestObject> list = GetRequestObjects(cmd);
        if (list.Count == 0) return null;
        return list[0];
    }
    public static async Task<List<Guid>> GetBatchRequestIds(Guid batchId)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"GetVendorRequests";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@batchId", batchId);

        List<Guid> idList = new();

        using (var reader = cmd.ExecuteReaderAsync().Result)
        {
            while (reader.ReadAsync().Result)
            {
                Guid? g = reader["RequestId"] as Guid?;
                if (g.HasValue)
                {
                    idList.Add(g.Value);
                }
            }
        }
        return idList;
    }
    public static async Task<List<MyDMVpro.Models.jsonRequestObject>> GetAllVendorRequests(string userName, bool? active)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"GetAllVendorRequests";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@agentName", userName);
        p = cmd.Parameters.AddWithValue("@active", EmptyToDbNull(active));
        return GetRequestObjects(cmd);
    }

    public static async Task PurgeLienholderData(Guid? groupId, Guid? userId)
    {
        if (groupId == null || userId == null)
            return;

        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"ClearLienholderData";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;
        p = cmd.Parameters.AddWithValue("@groupId", groupId.Value);
        p = cmd.Parameters.AddWithValue("@userId", userId.Value);

        await cmd.ExecuteNonQueryAsync();
    }
#if NO_LONGER_USED
    public static async Task<List<MyDMVpro.Models.jsonRequestObject>> GetActiveChatRequests(string userName)
    {
        using (SqlConnection conn = new SqlConnection(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = $@"GetActiveChatRequests";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter p;

            p = cmd.Parameters.AddWithValue("@userName", userName);

            return GetRequestObjects(cmd);
        }
    }
#endif
    public static async Task<List<MyDMVpro.Models.jsonRequestObject>> GetVendorActiveChatRequests(string userName)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"GetVendorActiveChatRequests";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@agentName", userName);

        return GetRequestObjects(cmd);
    }
    public static async Task<List<MyDMVpro.Models.jsonRequestObject>> GetAllVendorRequests(string userName)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"GetAllVendorRequests";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@agentName", userName);

        return GetRequestObjects(cmd);
    }
    public static async Task<List<MyDMVpro.Models.jsonRequestObject>> GetVendorRequests(string userName, bool printed, bool sentToVendor, bool sentToDmv, bool receivedFromDmv, bool shipped, bool? directToVendor = null)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"GetVendorRequests";
        cmd.CommandType = CommandType.StoredProcedure;

        SqlParameter p;

        p = cmd.Parameters.AddWithValue("@agentName", userName);
        p = cmd.Parameters.AddWithValue("@printed", printed);
        p = cmd.Parameters.AddWithValue("@senttovendor", sentToVendor);
        p = cmd.Parameters.AddWithValue("@senttodmv", sentToDmv);
        p = cmd.Parameters.AddWithValue("@receivedfromdmv", receivedFromDmv);
        p = cmd.Parameters.AddWithValue("@shipped", shipped);
        if (directToVendor.HasValue)
            p = cmd.Parameters.AddWithValue("@directToVendor", directToVendor.Value);
        return GetRequestObjects(cmd);
    }
    public static async Task<Guid> CreateSigningBatch(string userName, Guid[] ids)
    {
        using MaggardDMVContext context = new();
        // Query for the Blog named ADO.NET Blog
        var user = context.Users
                        .Where(u => u.UserPrincipalName == userName || u.NameIdentifierClaim == userName)
                        .FirstOrDefault();

        Signings signing = new()
        {
            UserId = user.UserId
        };
        foreach (Guid g in ids)
        {
            RequestSigning rs = new()
            {
                RequestId = g,
            };
            signing.RequestSigning.Add(rs);
        }
        var result = await context.Signings.AddAsync(signing);
        await context.SaveChangesAsync(user.UserId);
        return result.Entity.SigningId;
    }
    public static async Task<List<string>> GetBatchStates(List<Guid> ids)
    {
        List<string> states = new();
        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "GetBatchStates";
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", ids);

            using SqlDataReader reader = cmd.ExecuteReaderAsync().Result;
            while (reader.ReadAsync().Result)
            {
                states.Add(reader["State"] as string);
            }
        }
        return states.OrderBy(x => x).ToList();
    }
    public static async Task<string> FileLibrary_DeleteUnlinkedAttachments(Guid? userId, List<Guid> attachmentIds, ILogger logger = null)
    {
        if (attachmentIds == null || attachmentIds.Count == 0)
            return "Attachment id required";

        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "dbo.FileLibrary_DeleteUnlinkedAttachments";
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@AttachmentIDs", attachmentIds);

            cmd.Parameters.AddWithValue("@userId", userId.Value);

            await cmd.ExecuteScalarAsync();
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogError("{Message}", ex.Message);
            return "Error removing 1 or more attachments";
        }
    }
    public static async Task<(Guid, string)> CreatePendingSignBatch(Guid? groupId, Guid? userId, Guid? vendorId, Guid[] ids, byte[] pdf)
    {
        if (ids == null || ids.Length == 0)
            return (Guid.Empty, null);

        using MaggardDMVContext context = new();
        try
        {
            // Make sure calling user is owner of each request
            bool allMatchCurrentUser = context.Requests.Where(r => ids.Contains(r.RequestId)).All(r => (groupId != null && r.GroupId == groupId.Value) || (vendorId != null && r.VendorId == vendorId.Value));
            if (!allMatchCurrentUser)
                return (Guid.Empty, null);

            Guid firstReqId = ids[0];
            Requests req = context.Requests.Where(r => r.RequestId == firstReqId).FirstOrDefault();

            // The same vendor must be used for a single batch
            PendingSignBatch batch = new()
            {
                BatchId = Guid.NewGuid(),
                PdfImage = pdf,
                VendorId = req.VendorId,
                GroupId = req.GroupId.Value // use groupId from Request since a vendor won't necessarily have a groupId
            };

            foreach (Guid g in ids)
            {
                PendingSignRequest psr = new()
                {
                    RequestId = g
                };
                batch.PendingSignRequest.Add(psr);
            }
            var result = context.PendingSignBatch.Add(batch);
            await context.SaveChangesAsync(userId);
            return (batch.BatchId, null);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Error generating PDFs: {ex.Message}";
            System.Diagnostics.Trace.TraceError(errorMessage);
            return (Guid.Empty, errorMessage);
        }
    }
    public static async Task<(Guid, string)> CreatePendingSignBatch(string userName, Guid[] ids, byte[] pdf)
    {
        using MaggardDMVContext context = new();
        try
        {
            var user = context.Users
                            .Where(u => u.UserPrincipalName == userName || u.NameIdentifierClaim == userName)
                            .FirstOrDefault();
            //TODO: Make sure calling user is owner of each request
            PendingSignBatch batch = new()
            {
                BatchId = Guid.NewGuid(),
                PdfImage = pdf,
            };

            foreach (Guid g in ids)
            {
                PendingSignRequest psr = new()
                {
                    RequestId = g
                };
                batch.PendingSignRequest.Add(psr);
            }
            var result = context.PendingSignBatch.Add(batch);
            await context.SaveChangesAsync(user.UserId);
            return (batch.BatchId, null);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Error generating PDFs: {ex.Message}";
            System.Diagnostics.Trace.TraceError(errorMessage);
            return (Guid.Empty, errorMessage);
        }
    }

    public static async Task<int> TriggerFormAnalyzer(List<Guid> fileUploadIds, ILogger logger)
    {
        List<Task<HttpResponseMessage>> tasks = new();

        foreach (Guid g in fileUploadIds)
        {
            tasks.Add(HttpTriggerAnalyzerAsync(g, logger));
        }
        await Task.WhenAll(tasks);

        LogHttpResponseMessages(tasks, logger);

        return 1;
    }
    public static void LogHttpResponseMessages(List<Task<HttpResponseMessage>> tasks, ILogger logger)
    {
        foreach (var t in tasks)
        {
            var result = t.Result;
            logger?.LogInformation("HttpFormAnalyzer:  {StatusCode} - {ReasonPhrase}", result.StatusCode, result.ReasonPhrase);
        }
    }
    public static async Task<int> TriggerFormAnalyzer(Guid fileUploadId, ILogger logger)
    {
        List<Task> listOfTasks = new()
        {
            HttpTriggerAnalyzer(fileUploadId, logger)
        };

        await Task.WhenAll(listOfTasks);

        return 1;
    }
    public static async Task<int> TriggerAutoImsDownload(Guid groupId, List<Guid> ids, ILogger logger, bool force)
    {
        List<Task> listOfTasks = new();
        foreach (var id in ids)
        {
            listOfTasks.Add(HttpTriggerDownload(id, groupId, logger, force));
        }
        await Task.WhenAll(listOfTasks);

        return 1;
    }
    public static async Task<int> TriggerAutoImsDownload(string userName, List<Guid> ids, ILogger logger)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "dbo.ValidateAutoImsQueryList";
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", ids);

            cmd.Parameters.AddWithValue("@username", userName);

            SqlParameter pGroup = cmd.Parameters.Add("@groupId", SqlDbType.UniqueIdentifier);
            pGroup.Direction = ParameterDirection.Output;

            List<Guid> data = new();
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        Guid r = reader.GetGuid(0);
                        data.Add(r);
                    }
                }
            }

            Guid groupId = (Guid)pGroup.Value;
            return await TriggerAutoImsDownload(groupId, data, logger, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in TriggerAutoImsDownload");
            return 0;
        }
    }

    public static async Task<Stream> GetMergedCR(List<Guid> requestids)
    {
        string rootUrl = AutoIMSMergeCRUrl;
        if (string.IsNullOrEmpty(rootUrl))
            rootUrl = "";
        if (!rootUrl.Contains('?'))
        {
            rootUrl += "?";
        }

        string url = $"{rootUrl}";

        var mergeReqeust = new { key = "123", pages = "1", attachmentIDs = requestids.ToArray(), email = "jim.cantwell@sorairo.us" };

        HttpResponseMessage response = await s_httpClient.PostAsJsonAsync(url, mergeReqeust);
        response.EnsureSuccessStatusCode();
        Stream pdfStream = await response.Content.ReadAsStreamAsync();
        return pdfStream;
    }

    //private static IActionResult ReturnPdfFile(string filename, byte[] file, DateTime lastModified, bool displayInline)
    //{
    //    System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
    //    {
    //        FileName = filename,
    //        Inline = displayInline
    //    };
    //    Response.Headers.Add("Content-Disposition", cd.ToString());
    //    Response.Headers.Add("X-Content-Type-Options", "nosniff");
    //    string lastmod = lastModified.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
    //    Response.Headers.Add("Last-Modified", lastmod);

    //    return File(file, "application/pdf");
    //}
    //private IActionResult ReturnPdfFile(string filename, Stream fileStream, DateTime lastModified, bool displayInline)
    //{
    //    System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
    //    {
    //        FileName = filename,
    //        Inline = displayInline
    //    };
    //    Response.Headers.Add("Content-Disposition", cd.ToString());
    //    Response.Headers.Add("X-Content-Type-Options", "nosniff");
    //    string lastmod = lastModified.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
    //    Response.Headers.Add("Last-Modified", lastmod);
    //    fileStream.Seek(0, 0);
    //    //return File(fileStream, "application/pdf", filename, lastModified.ToUniversalTime(), null, true);
    //    return File(fileStream, "application/pdf");
    //}


    public static async Task HttpTriggerAnalyzer(Guid fileUploadId, ILogger logger)
    {
        string url = GetFormAnalyzerTriggerUrl(fileUploadId);
        logger.LogInformation("Triggering analyzer using {url}", url);
        var result = await s_httpClient.GetAsync(url);
        if (result.IsSuccessStatusCode)
        {
            logger?.LogInformation("HttpTriggerAnalyzer error: {FileUploadId} {StatusCode} - {ReasonPhrase}", fileUploadId, result.StatusCode, result.ReasonPhrase);
        }
        else
        {
            logger?.LogError("HttpTriggerAnalyzer: {FileUploadId} {StatusCode} - {ReasonPhrase}", fileUploadId, result.StatusCode, result.ReasonPhrase);
        }
        return;
    }

    private static string GetFormAnalyzerTriggerUrl(Guid fileUploadId)
    {
        string rootUrl = FormAnalyzerTriggerUrl;
        if (string.IsNullOrEmpty(rootUrl))
            rootUrl = "";

        if (!rootUrl.Contains('?'))
        {
            rootUrl += "?";
        }
        if (!rootUrl.EndsWith("&"))
        {
            rootUrl += "&";
        }
        string url = $"{rootUrl}uploadId={fileUploadId}";
        return url;
    }
    public static Task<HttpResponseMessage> HttpTriggerAnalyzerAsync(Guid fileUploadId, ILogger logger)
    {
        string url = GetFormAnalyzerTriggerUrl(fileUploadId);
        logger.LogInformation("Triggering analyzer using {url}", url);
        return s_httpClient.GetAsync(url);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    public static Task<HttpResponseMessage> HttpTriggerDownload(Guid requestId, Guid groupId, ILogger logger, bool force = false)
    {
        string rootUrl = AutoIMSTriggerUrl;
        if (string.IsNullOrEmpty(rootUrl))
            rootUrl = "";
        if (!rootUrl.Contains('?'))
            rootUrl += "?";
        string url = $"{rootUrl}&requestId={requestId}&groupId={groupId}";
        if (force)
        {
            url += "&force=1";
        }
        return s_httpClient.GetAsync(url);
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    public static async Task<int> TriggerAutoImsDownloadForRequests(Guid groupId, List<Guid> requestIds, ILogger logger, bool force)
    {
        List<Task<HttpResponseMessage>> tasks = new();

        foreach (Guid r in requestIds)
        {
            tasks.Add(HttpTriggerDownload(r, groupId, logger, true));
        }
        await Task.WhenAll(tasks);

        LogHttpResponseMessages(tasks, logger);

        return 1;
    }

    public static async Task TriggerAutoImsDownloadForFileUpload(Guid fileUploadId, ILogger logger)
    {
        string rootUrl = AutoIMSTriggerUrl;
        if (string.IsNullOrEmpty(rootUrl))
            rootUrl = "";
        if (!rootUrl.Contains('?'))
            rootUrl += "?";
        string url = $"{rootUrl}&fileUploadId={fileUploadId}";

        var result = await s_httpClient.GetAsync(url);
        if (result.IsSuccessStatusCode)
        {
            logger?.LogInformation("TriggerAutoImsDownloadForFileUpload error: {fileUploadId} {StatusCode} - {ReasonPhrase}", fileUploadId, result.StatusCode, result.ReasonPhrase);
        }
        else
        {
            logger?.LogError("TriggerAutoImsDownloadForFileUpload: {fileUploadId} {StatusCode} - {ReasonPhrase}", fileUploadId, result.StatusCode, result.ReasonPhrase);
        }

        return;
    }

    public static async Task<string> ExportInvoices(Guid vendorId, Guid userId, Guid? groupId, List<Guid> ids)
    {
        return await ExportInvoiceData(vendorId, userId, groupId, ids);
    }
    public static async Task<string> ExportRequests(Guid vendorId, Guid userId, Guid? groupId, List<Guid> ids)
    {
        return await ExportRequestData(vendorId, userId, groupId, ids);
    }
    public static async Task<string> UniversalExportRequests(Guid vendorId, Guid userId, Guid? groupId, List<Guid> ids)
    {
        var expdef = await DataHelpers.GetExportDefinition(vendorId, false);
        return await UniversalExportRequestData(vendorId, userId, groupId, ids, expdef);
    }

    public static async Task<int> MarkAsNotReadyForProcessing(Guid userId, string note, string remark, string code, string chat, List<Guid> ids)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(SqlConnectionString))
            {
                await conn.OpenAsync();
                SqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = "MarkAsNotReadyForProcessing_v2";
                cmd.CommandType = CommandType.StoredProcedure;

                AddRequestIDsParameter(cmd, "@RequestIDs", ids);

                cmd.Parameters.AddWithValue("@agentId", userId);

                cmd.Parameters.AddWithValue("@note", EmptyToDbNull(note));
                cmd.Parameters.AddWithValue("@remark", EmptyToDbNull(remark));
                cmd.Parameters.AddWithValue("@code", EmptyToDbNull(code));
                cmd.Parameters.AddWithValue("@chat", EmptyToDbNull(chat));

                return await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            throw;
        }
    }
    public static object EmptyToDbNull(bool? bval)
    {
        if (bval == null) return DBNull.Value;
        return bval;
    }
    public static object EmptyToDbNull(int? ival)
    {
        if (ival == null) return DBNull.Value;
        return ival;
    }
    public static object EmptyToDbNull(Guid? guid)
    {
        if (guid == null) return DBNull.Value;
        return guid;
    }
    public static object EmptyToDbNull(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return DBNull.Value;
        return s;
    }
    public static object EmptyToDbNull(DateTime? dt)
    {
        if (dt == null) return DBNull.Value;
        return dt;
    }
    public static async Task<int> MarkAsReceivedByLH(Guid userId, DateTime date, string note, string remark, string code, List<Guid> ids)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "MarkAsReceivedByLH";
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", ids);

            cmd.Parameters.AddWithValue("@agentId", userId);
            cmd.Parameters.AddWithValue("@date", date.Date);

            cmd.Parameters.AddWithValue("@note", EmptyToDbNull(note));
            cmd.Parameters.AddWithValue("@remark", EmptyToDbNull(remark));
            cmd.Parameters.AddWithValue("@code", EmptyToDbNull(code));

            return await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            throw new ApplicationException(ex.Message);
        }
    }
    public static async Task<int> MarkAsDirectToBilling(Guid userId, DateTime date, string note, string remark, string code, List<Guid> ids)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "MarkAsDirectToBilling_v2";
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", ids);

            cmd.Parameters.AddWithValue("@agentId", userId);
            cmd.Parameters.AddWithValue("@date", date.Date);

            cmd.Parameters.AddWithValue("@note", EmptyToDbNull(note));
            cmd.Parameters.AddWithValue("@remark", EmptyToDbNull(remark));
            cmd.Parameters.AddWithValue("@code", EmptyToDbNull(code));

            return await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            throw new ApplicationException(ex.Message);
        }
    }
    #region LH updates
    public static async Task<int> MarkAsDirectToVendor(string userName, DateTime date, List<Guid> ids)
    {
        return await SetRequestDateField(@"MarkAsDirectToVendor", userName, date, ids);
    }
    public static async Task<int> MarkAsShippedToVendor(string agentName, DateTime date, List<Guid> ids)
    {
        return await SetRequestDateField(@"MarkAsShippedToVendor", agentName, date, ids);
    }
    #endregion

    public static async Task<int> MarkAsFinished(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MarkAsFinished_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> MarkAsShipped(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MarkAsShipped_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> RemovePending(string agentName, DateTime date, List<Guid> ids)
    {
        return await SetRequestDateField(@"RemovePendingRequests", agentName, date, ids);
    }

    public static async Task<int> MarkAsDmvShipped(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MarkAsDmvShipped_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> MarkAsPrinted(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MarkAsPrinted_v2", agentName, date, ids, code, note, remark, chat);
    }

    public static async Task<int> MoveNotReadyToReadyForProcessing(string agentName, DateTime? date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MoveNotReadyToReadyForProcessing_v2", agentName, date, ids, code, note, remark, chat);
    }

    public static async Task<int> MarkAsReceivedFromLH(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MarkAsReceivedFromLH_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> MarkAsTitleIssued(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MarkAsTitleIssued_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> MarkAsTitleIssuedInTransit(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MarkAsTitleIssuedInTransit_v2", agentName, date, ids, code, note, remark, chat);
    }

    public static async Task<int> MoveRequestsToWorkingList(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MoveRequestsToWorkingList_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> MarkAsReceivedFromDmv(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"MarkAsReceivedFromDmv_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> LIRequested(string agentName, DateTime date, int? checkNumber, string tracking, List<Guid> ids, bool? autoIncrement, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithCheckAndTracking(@"LIRequested", agentName, date, checkNumber, tracking, ids, autoIncrement);
    }
    public static async Task<int> LCRequested(string agentName, DateTime date, int? checkNumber, string tracking, List<Guid> ids, bool? autoIncrement)
    {
        return await SetRequestDateFieldWithCheckAndTracking(@"LCRequested", agentName, date, checkNumber, tracking, ids, autoIncrement);
    }
    public static async Task<int> LCReceivedFromDmv(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"LCReceivedFromDmv_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> LIReceivedFromDmv(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"LIReceivedFromDmv_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> LCReceivedFromLH(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"LCReceivedFromLH_v2", agentName, date, ids, code, note, remark, chat);
    }
    public static async Task<int> LCRejected(string agentName, DateTime date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        return await SetRequestDateFieldWithNotes(@"LCRejected_v2", agentName, date, ids, code, note, remark, chat);
    }

    private static void AddOrderedRequestParameter(SqlCommand cmd, List<Guid> ids)
    {
        var dataTable = new DataTable
        {
            TableName = "dbo.OrderedRequestIDsUDT"
        };
        dataTable.Columns.Add("order", typeof(int));
        dataTable.Columns.Add("RequestId", typeof(Guid));
        int i = 0;
        foreach (Guid g in ids)
        {
            dataTable.Rows.Add(i++, g);
        }
        SqlParameter pIds = cmd.Parameters.Add("@RequestIDs", SqlDbType.Structured);
        pIds.TypeName = "dbo.OrderedRequestIDsUDT";
        pIds.Value = dataTable;
    }
    private static void AddRequestIDsParameter(SqlCommand cmd, string paramName, List<Guid> ids)
    {
        var dataTable = new DataTable
        {
            TableName = "dbo.RequestIDsUDT"
        };
        dataTable.Columns.Add("RequestId", typeof(Guid));
        foreach (Guid g in ids)
        {
            dataTable.Rows.Add(g);
        }
        SqlParameter pIds = cmd.Parameters.Add(paramName, SqlDbType.Structured);
        pIds.TypeName = "dbo.RequestIDsUDT";
        pIds.Value = dataTable;
    }

    public static async Task<int> SetRequestDateFieldWithCheckAndTracking(string storedProcName, string agentName, DateTime date, int? checkNumber, string trackingNumber, List<Guid> ids, bool? autoIncrement)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = storedProcName;
            cmd.CommandType = CommandType.StoredProcedure;

            AddOrderedRequestParameter(cmd, ids);
            cmd.Parameters.AddWithValue("@agentname", agentName);
            cmd.Parameters.AddWithValue("@date", date.Date);
            if (checkNumber != null)
                cmd.Parameters.AddWithValue("@checkNumber", checkNumber.Value);
            if (autoIncrement != null)
                cmd.Parameters.AddWithValue("@autoIncrement", autoIncrement.Value);
            cmd.Parameters.AddWithValue("@trackingNumber", trackingNumber);

            return await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            throw new ApplicationException(ex.Message);
        }
    }
    public static async Task<int> SetRequestDateField(string storedProcName, string agentName, DateTime? date, List<Guid> ids)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = storedProcName;
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", ids);

            cmd.Parameters.AddWithValue("@agentname", agentName);
            cmd.Parameters.AddWithValue("@date", EmptyToDbNull(date?.Date));

            return await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            throw;
        }
    }

    public static async Task<int> SetRequestDateFieldWithNotes(string storedProcName, string agentName, DateTime? date, List<Guid> ids, string code, string note, string remark, string chat)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = storedProcName;
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", ids);

            cmd.Parameters.AddWithValue("@agentname", agentName);
            cmd.Parameters.AddWithValue("@date", EmptyToDbNull(date?.Date));
            cmd.Parameters.AddWithValue("@code", EmptyToDbNull(code));
            cmd.Parameters.AddWithValue("@note", EmptyToDbNull(note));
            cmd.Parameters.AddWithValue("@chat", EmptyToDbNull(chat));
            cmd.Parameters.AddWithValue("@remark", EmptyToDbNull(remark));

            return await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            throw;
        }
    }
    public static async Task<List<Guid>> GenerateMultipleShippingReports(string userName, DateTime date, List<Guid> ids)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"CreateMultipleShipments_v2";
        cmd.CommandType = CommandType.StoredProcedure;

        AddOrderedRequestParameter(cmd, ids);

        cmd.Parameters.AddWithValue("@agentname", userName);
        cmd.Parameters.AddWithValue("@date", date.Date);

        List<Guid> shipmentIds = new();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    shipmentIds.Add(reader.GetGuid(0));
                }
            }
        }
        return shipmentIds;
    }
    public static async Task<Guid?> GenerateShippingReport(string userName, DateTime date, string courierName, string trackingNumber, List<Guid> ids)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"CreateShipment_v2";
        cmd.CommandType = CommandType.StoredProcedure;

        AddOrderedRequestParameter(cmd, ids);

        cmd.Parameters.AddWithValue("@agentname", userName);
        cmd.Parameters.AddWithValue("@date", date.Date);
        if (courierName != null)
        {
            cmd.Parameters.AddWithValue("@courierName", courierName);
        }
        if (trackingNumber != null)
        {
            cmd.Parameters.AddWithValue("@trackingNumber", trackingNumber);
        }
        SqlParameter pout = cmd.Parameters.Add("@shipmentId", SqlDbType.UniqueIdentifier);
        pout.Direction = ParameterDirection.Output;
        _ = await cmd.ExecuteNonQueryAsync();

        if (pout.Value is DBNull)
            return null;
        return (pout.Value as Guid?);
    }
    public static async Task<int> UpdateShipToVendorDetails(string userName, DateTime date, string courierName, string trackingNumber, List<Guid> ids)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"UpdateShipToVendorInfo";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@RequestIDs", ids);

        cmd.Parameters.AddWithValue("@username", userName);
        cmd.Parameters.AddWithValue("@date", date.Date);
        cmd.Parameters.AddWithValue("@courierName", courierName);
        cmd.Parameters.AddWithValue("@trackingNumber", trackingNumber);

        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<int> MoveItems(string agentName, int stage, string code, string reasonCancelled, string note, string remark, string chat, List<Guid> ids)
    {
        //List<string> availableStages = new List<string>(new string[] { "HOLD", "INCOMING", "SIGN", "PRINT", "SHIPTODMV", "TITLEPENDING", "RECEIVING" });
        //if (!availableStages.Contains(stage))
        //{
        //    return -1;
        //}

        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"MoveRequests_v5";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@RequestIDs", ids);

        cmd.Parameters.AddWithValue("@agentname", agentName);
        cmd.Parameters.AddWithValue("@code", EmptyToDbNull(code));
        cmd.Parameters.AddWithValue("@reasonCancelled", EmptyToDbNull(reasonCancelled));
        cmd.Parameters.AddWithValue("@stage", EmptyToDbNull(stage));
        cmd.Parameters.AddWithValue("@note", EmptyToDbNull(note));
        cmd.Parameters.AddWithValue("@remark", EmptyToDbNull(remark));
        cmd.Parameters.AddWithValue("@chat", EmptyToDbNull(chat));

        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<int> MarkItemsAsShipped(
        string agentName,
        string vin,
        string trackingNumber,
        DateTime dateShipped,
        string shippedNote,
        bool isShipped,
        List<Guid> documentReceivedIds)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        // Update the stored procedure name if required
        cmd.CommandText = "MarkItemsAsShipped_v1";
        cmd.CommandType = CommandType.StoredProcedure;

        // Add parameters
        AddRequestIDsParameter(cmd, "@DocumentReceivedIDs", documentReceivedIds);
        cmd.Parameters.AddWithValue("@agentName", agentName);
        cmd.Parameters.AddWithValue("@vin", vin);
        cmd.Parameters.AddWithValue("@trackingNumber", EmptyToDbNull(trackingNumber));
        cmd.Parameters.AddWithValue("@dateShipped", dateShipped);
        cmd.Parameters.AddWithValue("@shippedNote", EmptyToDbNull(shippedNote));
        cmd.Parameters.AddWithValue("@isShipped", isShipped);

        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<int> MoveItemsToPendingList(string agentName, int stage, string code, string reasonCancelled, string note, string remark, string chat, List<Guid> ids, DateTime? eta)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"MoveRequestsToPending";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@RequestIDs", ids);

        cmd.Parameters.AddWithValue("@agentname", agentName);
        cmd.Parameters.AddWithValue("@code", EmptyToDbNull(code));
        cmd.Parameters.AddWithValue("@reasonCancelled", EmptyToDbNull(reasonCancelled));
        cmd.Parameters.AddWithValue("@stage", EmptyToDbNull(stage));
        cmd.Parameters.AddWithValue("@note", EmptyToDbNull(note));
        cmd.Parameters.AddWithValue("@remark", EmptyToDbNull(remark));
        cmd.Parameters.AddWithValue("@chat", EmptyToDbNull(chat));
        cmd.Parameters.AddWithValue("@etaDate", EmptyToDbNull(eta));

        return await cmd.ExecuteNonQueryAsync();
    }
    public static async Task<int> HoldItems(string agentName, string stage, string code, string reasonCancelled, List<Guid> ids)
    {
        //List<string> availableStages = new List<string>(new string[] { "HOLD", "INCOMING", "SIGN", "PRINT", "SHIPTODMV", "TITLEPENDING", "RECEIVING" });
        //if (!availableStages.Contains(stage))
        //{
        //    return -1;
        //}

        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"MoveRequests_v3";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@RequestIDs", ids);

        cmd.Parameters.AddWithValue("@agentname", agentName);
        cmd.Parameters.AddWithValue("@code", EmptyToDbNull(code));
        cmd.Parameters.AddWithValue("@reasonCancelled", EmptyToDbNull(reasonCancelled));
        cmd.Parameters.AddWithValue("@stage", stage);

        return await cmd.ExecuteNonQueryAsync();
    }
    public static async Task<int> DeleteShipment(Guid shipmentId, string agentName, bool moveToShipToLHQueue)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"DeleteShipment";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@shipmentId", shipmentId);
        cmd.Parameters.AddWithValue("@agentname", agentName);
        cmd.Parameters.AddWithValue("@markAsNotShipped", moveToShipToLHQueue);

        return await cmd.ExecuteNonQueryAsync();
    }
    public static async Task<int> EditShipment(Guid shipmentId, string agentName, DateTime? date, string courierName, string trackingNumber)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"EditShipment";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@shipmentId", shipmentId);
        cmd.Parameters.AddWithValue("@agentname", agentName);
        if (date != null)
        {
            cmd.Parameters.AddWithValue("@date", date.Value.Date);
        }
        if (courierName != null)
        {
            cmd.Parameters.AddWithValue("@courierName", courierName);
        }
        if (trackingNumber != null)
        {
            cmd.Parameters.AddWithValue("@trackingNumber", trackingNumber);
        }
        return await cmd.ExecuteNonQueryAsync();
    }
    public static async Task<int> UpdateShippingDetails(string agentName, DateTime date,
        string courierName, string trackingNumber, List<Guid> ids)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"UpdateShippingInfo";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@RequestIDs", ids);

        cmd.Parameters.AddWithValue("@agentname", agentName);
        cmd.Parameters.AddWithValue("@date", date.Date);
        cmd.Parameters.AddWithValue("@courierName", courierName);
        cmd.Parameters.AddWithValue("@trackingNumber", trackingNumber);

        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<int> UpdateDmvShippingDetails(string agentName,
            DateTime date,
            string courierName,
            string trackingNumber,
            string toDmvTrackingNumber,
            string eta, List<Guid> ids)
    {
        DateTime? etaDate = null;
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"UpdateDmvShippingInfo_v3";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@RequestIDs", ids);

        if (!string.IsNullOrWhiteSpace(eta))
        {
            if (DateTime.TryParse(eta, out DateTime dt))
            {
                etaDate = dt;
            }
        }
        cmd.Parameters.AddWithValue("@agentname", agentName);
        cmd.Parameters.AddWithValue("@date", date.Date);
        cmd.Parameters.AddWithValue("@eta", EmptyToDbNull(etaDate));

        cmd.Parameters.AddWithValue("@courierName", EmptyToDbNull(courierName));
        cmd.Parameters.AddWithValue("@trackingNumber", trackingNumber);
        cmd.Parameters.AddWithValue("@toDmvTrackingNumber", toDmvTrackingNumber);

        return await cmd.ExecuteNonQueryAsync();
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private static async Task<string> ExportInvoiceData(Guid vendorId, Guid userId, Guid? groupId, List<Guid> ids)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"ExportInvoiceData";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@InvoiceIDs", ids);

        cmd.Parameters.AddWithValue("@vendorId", vendorId);
        cmd.Parameters.AddWithValue("@userId", userId);

        StringBuilder sb = new();
        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    string s = reader.GetString(0);
                    sb.Append(s);
                }
            }
        }

        string data = sb.ToString();
        JArray jarray = JArray.Parse(data);
        return BuildCSVfromJArray(jarray);
    }
    private static async Task<string> ExportRequestData(Guid vendorId, Guid userId, Guid? groupId, List<Guid> ids)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = @"ExportRequestData";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@RequestIDs", ids);

        cmd.Parameters.AddWithValue("@vendorId", vendorId);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@groupId", groupId);


        List<string> data = new();
        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    string s = reader.GetString(0);
                    data.Add(s);
                }
            }
        }

        return BuildCSV(data);
    }
    private static async Task<ProfileCategories> GetGroupProfileDefinitions(Guid vendorId, string profileCategoryName = null)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        using SqlCommand jsonCmd = conn.CreateCommand();
        StringBuilder sb = new();
        jsonCmd.CommandText = @"GetGroupProfileDefinitions";
        jsonCmd.CommandType = CommandType.StoredProcedure;
        jsonCmd.Parameters.Add(new SqlParameter("@vendorId", vendorId));
        if (!string.IsNullOrWhiteSpace(profileCategoryName))
        {
            jsonCmd.Parameters.Add(new SqlParameter("@profileCategoryName", profileCategoryName));
        }

        using (SqlDataReader reader = await jsonCmd.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    string s = reader.GetString(0);
                    sb.Append(s);
                }
            }
        }
        string json = sb.ToString();
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ProfileCategories>(json);
    }

    private static async Task<ExportDefinition> GetGroupProfileDefinition(Guid vendorId, string profileCategoryName)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        using SqlCommand jsonCmd = conn.CreateCommand();
        StringBuilder sb = new();
        jsonCmd.CommandText = @"GetGroupProfileDefinition";
        jsonCmd.CommandType = CommandType.StoredProcedure;
        jsonCmd.Parameters.Add(new SqlParameter("@vendorId", vendorId));
        jsonCmd.Parameters.Add(new SqlParameter("@profileCategoryName", profileCategoryName));

        using (SqlDataReader reader = await jsonCmd.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    string s = reader.GetString(0);
                    sb.Append(s);
                }
            }
        }
        string json = sb.ToString();
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExportDefinition>(json);
    }
    private static async Task<ExportDefinition> GetExportDefinition(Guid vendorId, bool profilesOnly)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        using SqlCommand jsonCmd = conn.CreateCommand();
        StringBuilder sb = new();
        jsonCmd.CommandText = @"GetExportDefinition_v3";
        jsonCmd.CommandType = CommandType.StoredProcedure;
        jsonCmd.Parameters.Add(new SqlParameter("@vendorId", vendorId));
        jsonCmd.Parameters.Add(new SqlParameter("@profilesOnly", profilesOnly));

        using (SqlDataReader reader = await jsonCmd.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    string s = reader.GetString(0);
                    sb.Append(s);
                }
            }
        }
        string json = sb.ToString();
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExportDefinition>(json);
    }
    private static async Task<string> UniversalExportRequestData(Guid vendorId, Guid userId, Guid? groupId, List<Guid> ids, ExportDefinition exportdef)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        List<string> data = new();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"ExportRequestData_v2";
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", ids);

            cmd.Parameters.AddWithValue("@vendorId", vendorId);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@groupId", groupId);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    string s = reader.GetString(0);
                    data.Add(s);
                }
            }
        }
        return BuildCSV_v2(exportdef, data);
    }
    public static async Task UniversalExportBulkProfileSave(UserInfo user, Guid? groupId, string json)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        List<string> data = new();
        using SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = @"VendorBulkEditProfiles";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@json", json);
        cmd.Parameters.AddWithValue("@groupId", groupId);
        cmd.Parameters.AddWithValue("@userId", user.UserId);
        await cmd.ExecuteNonQueryAsync();
    }
    public static async Task UniversalExportBulkSave(UserInfo user, string json, ProcessStageIDs? sendToStage)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        List<string> data = new();
        using SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = @"VendorBulkEditRequests_v2";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@json", json);
        cmd.Parameters.AddWithValue("@userId", user.UserId);

        cmd.Parameters.AddWithValue("@sendToStage", EmptyToDbNull((int?)sendToStage));

        using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                string s = reader.GetString(0);
                data.Add(s);
            }
        }
    }
    public static async Task UniversalExportBulkSave(UserInfo user, string json)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        using SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = @"VendorBulkEditRequests";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@json", json);
        cmd.Parameters.AddWithValue("@userId", user.UserId);
        await cmd.ExecuteReaderAsync();
    }
    internal class UniversalBulkEdit
    {
        public List<Dictionary<string, string>> ListData { get; set; } = new();
        public List<Dictionary<string, object>> EditorFields { get; set; } = new();
        public List<Dictionary<string, object>> ListColumns { get; set; } = new();
    }
    private static string MakeHtmlId(string name)
    {
        return (name?.Replace(" ", "_") ?? "");
    }
    private static string MakeHtmlName(string name)
    {
        return name.Replace(" ", "_");
    }
    public static string[] s_readOnlyFields = //new string[]
    {
        "R#",
        "Group",
        "Submitted By",
        "AppType",
        "AppTypeState",
        "Vehicle Vin",
        "VIN",
        "RequestId",
        "UserId",
        "GroupId",
        "VendorCode"
    };
    public static string[] s_noSearchFields = //new string[]
    {
        "R#"
    };
    public static string[] s_hiddenFields =
    {
        "RequestId",
        "App State",
        "UserId",
        "GroupId",
        "VendorId",
        "VendorCode",
        "IsOnBehalfOf"
        //,"Today's Date"
    };

    internal static async Task<List<string>> GetFieldsForAppType(Guid vendorId, string appType)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();

        List<string> data = new();
        string json;

        using SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = @"GetFieldsForAppType";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@vendorId", vendorId);
        cmd.Parameters.AddWithValue("@appType", appType);

        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    string s = reader.GetString(0);
                    data.Add(s);
                }
            }
        }
        return data;
    }
    internal static async Task<List<Dictionary<string, string>>> GetUniversalBulkEditData(UserInfo user, List<Guid> ids)
    {
        var result = new List<Dictionary<string, string>>();

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();

            List<string> data = new();
            string json;

            using SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"GetUniversalEditData_v3";
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", ids);

            cmd.Parameters.AddWithValue("@vendorId", user.VendorId);

            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        string s = reader.GetString(0);
                        data.Add(s);
                    }
                }
            }
            json = "[" + string.Join(',', data) + "]";
            var rowData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
            foreach (var row in rowData.ToList())
            {
                foreach (var col in row.Keys.ToList())
                {
                    var dataVal = row[col];
                    if (dataVal != null && dataVal.EndsWith("T00:00:00"))
                    {
                        dataVal = dataVal[..^9];
                        row[col] = dataVal;
                    }
                }
                result.Add(row);
            }
        }

        return result;
    }
    internal static async Task<UniversalBulkEdit> UniversalRequestBulkEdit(UserInfo user, List<Guid> ids, bool profileOnly)
    {
        if (!user.VendorId.HasValue || user.VendorId == Guid.Empty)
            throw new Exception("User is not associated with a vendor.");

        var result = new UniversalBulkEdit();

        var expdef = await DataHelpers.GetExportDefinition(user.VendorId.Value, profileOnly);

        foreach (var col in expdef.Export)
        {
            if (col.Exclude)
                continue;

            var listColumn = new Dictionary<string, object>()
                            {
                                { "data", col.ExcelName },
                                { "title", col.ExcelName },
                                { "requiredType", col.RequiredType },
                                { "defaultContent", null }
                            };

            var editorField = new Dictionary<string, object>()
            {
                { "label", col.ExcelName },
                { "name", col.ExcelName },
                { "data", col.ExcelName },
                { "id", MakeHtmlId(col.ExcelName) }
            };
            if (col.FldType == "date")
            {
                listColumn.Add("render", "datatable_render_date");

                editorField.Add("wireFormat", "YYYY-MM-DD");
                editorField.Add("displayFormat", "MM-DD-YYYY");
                //editorField.Add("keyInput", "true");
                editorField.Add("type", "datetime");
            }
            else
            {
                if (s_readOnlyFields.Contains(col.ExcelName))
                {
                    editorField.Add("type", "readonly");
                }
            }
            if (!s_hiddenFields.Contains(col.ExcelName))
            {
                result.EditorFields.Add(editorField);
            }
            else
            {
                editorField.Add("type", "readonly");
                result.EditorFields.Add(editorField);
            }
            if (!s_readOnlyFields.Contains(col.ExcelName) && !s_hiddenFields.Contains(col.ExcelName))
            {
                listColumn.Add("className", "editable");
            }
            if (s_hiddenFields.Contains(col.ExcelName))
            {
                listColumn.Add("visible", false);
            }
            if (s_hiddenFields.Contains(col.ExcelName) || s_noSearchFields.Contains(col.ExcelName))
            {
                listColumn.Add("searchable", false);
            }
            result.ListColumns.Add(listColumn);
        }
        result.ListData = await GetUniversalBulkEditData(user, ids);

        return result;
    }

    internal static async Task<UniversalBulkEdit> UniversalProfileBulkEdit(UserInfo user, bool useNewProfiles = false, string categoryName = null)
    {
        if (!user.VendorId.HasValue || user.VendorId == Guid.Empty)
            throw new Exception("User is not associated with a vendor.");
        var result = new UniversalBulkEdit();
        ExportDefinition expdef = null;
        if (useNewProfiles)
        {
            expdef = await DataHelpers.GetGroupProfileDefinition(user.VendorId.Value, categoryName);
        }
        else
        {
            expdef = await DataHelpers.GetExportDefinition(user.VendorId.Value, true);
        }
        foreach (var col in expdef.Export)
        {
            if (col.Exclude)
                continue;
            var listColumn = new Dictionary<string, object>()
                            {
                                { "data", col.ExcelName },
                                { "title", col.ExcelName },
                                { "defaultContent", null }
                            };
            var editorField = new Dictionary<string, object>()
            {
                { "label", col.ExcelName },
                { "name", col.ExcelName },
                { "data", col.ExcelName },
                { "id", MakeHtmlId(col.ExcelName) }
            };
            if (col.FldType == "date")
            {
                listColumn.Add("render", "datatable_render_date");
                editorField.Add("wireFormat", "YYYY-MM-DD");
                editorField.Add("displayFormat", "MM-DD-YYYY");
                editorField.Add("type", "datetime");
            }
            else
            {
                if (s_readOnlyFields.Contains(col.ExcelName))
                {
                    editorField.Add("type", "readonly");
                }
                else
                {
                    editorField.Add("type", "text");
                }
            }
            if (!s_readOnlyFields.Contains(col.ExcelName) && !s_hiddenFields.Contains(col.ExcelName))
            {
                listColumn.Add("className", "editable");
            }
            if (s_hiddenFields.Contains(col.ExcelName))
            {
                listColumn.Add("visible", false);
            }
            if (s_hiddenFields.Contains(col.ExcelName) || s_noSearchFields.Contains(col.ExcelName))
            {
                listColumn.Add("searchable", false);
            }
            result.EditorFields.Add(editorField);
            result.ListColumns.Add(listColumn);
        }
        //result.ListData = await GetUniversalBulkEditData(user, ids);
        return result;
    }
    internal static async Task<ProfileCategories> UniversalGroupProfiles(UserInfo user)
    {
        if (!user.VendorId.HasValue || user.VendorId == Guid.Empty)
            throw new Exception("User is not associated with a vendor.");
        var result = await DataHelpers.GetGroupProfileDefinitions(user.VendorId.Value);
        foreach (var profile in result.Categories)
        {
            foreach (var col in profile.Columns)
            {
                var listColumn = new Dictionary<string, object>()
                            {
                                { "data", new OptionsFunction($"getfn_datatable_data_dictionary_handler('fields', '{col.ExcelName}')") },
                                //{ "data", $"fields[\"{col.ExcelName}\"]" },
                                { "title", col.Label ?? col.ExcelName },
                                { "defaultContent", null }
                            };
                var editorField = new Dictionary<string, object>()
                {
                    { "label", col.Label ?? col.ExcelName },
                    { "name", col.ExcelName },
                    { "data", new OptionsFunction($"getfn_datatable_data_dictionary_handler('fields', '{col.ExcelName}')") },
                    //{ "data", $"function (data, type, row, meta) {{ return datatable_data_handler(data, type, row, meta, 'fields', '{col.ExcelName}'); }}
                    { "id", MakeHtmlId(col.ExcelName) }
                };
                if (col.FldType == "date")
                {
                    listColumn.Add("render", "datatable_render_date");
                    editorField.Add("wireFormat", "YYYY-MM-DD");
                    editorField.Add("displayFormat", "MM-DD-YYYY");
                    editorField.Add("type", "datetime");
                }
                else
                {
                    if (s_readOnlyFields.Contains(col.ExcelName))
                    {
                        editorField.Add("type", "readonly");
                    }
                    else
                    {
                        editorField.Add("type", "text");
                    }
                }
                if (!s_readOnlyFields.Contains(col.ExcelName) && !s_hiddenFields.Contains(col.ExcelName))
                {
                    listColumn.Add("className", "editable");
                }
                if (s_hiddenFields.Contains(col.ExcelName))
                {
                    listColumn.Add("visible", false);
                }
                if (s_hiddenFields.Contains(col.ExcelName) || s_noSearchFields.Contains(col.ExcelName))
                {
                    listColumn.Add("searchable", false);
                }
                profile.EditorFields.Add(editorField);
                profile.ListColumns.Add(listColumn);
            }
        }
        return result;
    }
    private static bool SkipColumn(string colname, HashSet<string> skipFields = null)
    {
        if (colname.StartsWith("__"))
            return true;
        if (skipFields != null && skipFields.Contains(colname))
        {
            return true;
        }
        return false;
    }
    private static void AddWithoutDupes(List<string> columns, Dictionary<string, string> data, HashSet<string> skipFields = null)
    {
        foreach (string key in data.Keys)
        {
            if (SkipColumn(key, skipFields)) continue;

            if (!columns.Contains(key))
                columns.Add(key);
        }
    }
    internal static string BuildCSVfromJArray(JArray jsonRows)
    {
        List<string> columns = new();
        List<Dictionary<string, string>> rows = new();

        foreach (var jsonRow in jsonRows)
        {
            var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonRow.ToString());
            rows.Add(values);
            AddWithoutDupes(columns, values);
        }
        return BuildCSVfromDictionary(columns, rows);
    }
    private static string BuildCSV_v2(ExportDefinition orderDef, List<string> jsonRows)
    {
        return BuildCSV_v2(orderDef, jsonRows, "csv");
    }
    private static string BuildCSV_v2(ExportDefinition orderDef, List<string> jsonRows, string exportType = "csv")
    {
        List<string> columns = new();
        List<Dictionary<string, string>> rows = new();
        HashSet<string> skipFields = new();

        var exports = orderDef.Export;
        for (int i = 0; i < exports.Count; i++)
        {
            var export = exports[i];
            if (export.Exclude)
            {
                skipFields.Add(export.ExcelName);
            }
            else
            {
                columns.Add(export.ExcelName);
            }
        }
        foreach (string jsonRow in jsonRows)
        {
            var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonRow);
            foreach (var field in orderDef.Export)
            {
                if (!values.ContainsKey(field.ExcelName) && (!string.IsNullOrEmpty(field.AltSource) && values.ContainsKey(field.AltSource)))
                {
                    // populate destination  column with source value
                    values.Add(field.ExcelName, values[field.AltSource]);
                }
            }
            rows.Add(values);
            if (!orderDef.ExcludeUnlisted)
            {
                AddWithoutDupes(columns, values, skipFields);
            }
        }
        if (exportType == "csv")
        {
            return BuildCSVfromDictionary(columns, rows, orderDef);
        }
        else
        {
            return BuildJsonfromDictionary(columns, rows, orderDef);
        }
    }
    private static string BuildCSV(List<string> jsonRows)
    {
        List<string> columns = new();
        List<Dictionary<string, string>> rows = new();

        foreach (string jsonRow in jsonRows)
        {
            var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonRow);
            rows.Add(values);
            AddWithoutDupes(columns, values);
        }
        return BuildCSVfromDictionary(columns, rows);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private static string BuildCSVfromDictionary(List<string> columns, List<Dictionary<string, string>> rows, ExportDefinition exportDef = null)
    {
        DataTable dt = new();
        foreach (string colName in columns)
        {
            dt.Columns.Add(colName);
        }
        foreach (Dictionary<string, string> row in rows)
        {
            DataRow dr = dt.NewRow();
            foreach (string colname in columns)
            {
                if (row.ContainsKey(colname))
                {
                    string val = row[colname];
                    //TODO: Handle dates or other special formatting requirements more generally
                    // handle converting yyyy-MM-ddT00:00:00 to yyyy-MM-dd or yyyy-MM-ddT00:00:00
                    if (val != null && val.Length == "yyyy-MM-ddT00:00:00".Length)
                    {
                        if (val.EndsWith("T00:00:00"))
                        {
                            val = val[..10];
                        }
                        else if (val[10] == 'T')
                        {
                            if (DateTime.TryParseExact(val, "yyyy-MM-ddTHH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                            {
                                val = date.ToString("yyyy-MM-dd HH:mm:ss");
                            }
                        }
                    }
                    dr[colname] = val;
                }
            }
            dt.Rows.Add(dr);
        }

        StringBuilder sb = new();

        for (int i = 0; i < dt.Columns.Count; i++)
        {
            DataColumn col = dt.Columns[i];
            if (i > 0)
                sb.Append(',');
            sb.Append(col.ColumnName);
        }
        sb.AppendLine();

        foreach (DataRow row in dt.Rows)
        {
            IEnumerable<string> fields = row.ItemArray.Select(field => QuoteField(field.ToString()));

            sb.AppendLine(string.Join(",", fields));
        }

        return sb.ToString();
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private static string BuildJsonfromDictionary(List<string> columns, List<Dictionary<string, string>> rows, ExportDefinition exportDef = null)
    {
        foreach (Dictionary<string, string> row in rows)
        {
            Dictionary<string, string> newRow = new();
            foreach (string colname in columns)
            {
                if (row.ContainsKey(colname))
                {
                    //string val = row[colname];
                }
            }
        }
        return null;
    }

    public static string QuoteField(string fieldValue)
    {
        //TODO: wrap fields with comma
        fieldValue = fieldValue.Replace("\"", "\"\""); // double quotes up
        if (fieldValue.Contains(','))
        {
            fieldValue = $@"""{fieldValue}""";
        }
        return fieldValue;
    }
    public static async Task<string> GetFiltersForGroup(Guid? vendorId, Guid? groupId, bool? active)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"GetFiltersForGroup";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("@vendorId", SqlDbType.UniqueIdentifier).Value = EmptyToDbNull(vendorId);

        cmd.Parameters.Add("@groupId", SqlDbType.UniqueIdentifier).Value = EmptyToDbNull(groupId);

        if (active != null)
            cmd.Parameters.Add("@active", SqlDbType.Bit).Value = active.Value ? 1 : 0;
        else
            cmd.Parameters.Add("@active", SqlDbType.Bit).Value = DBNull.Value;

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }
    private class ResultData
    {
        public SqlParameter pVendorId;
        public SqlParameter pVendorName;
        public SqlParameter pUserId;
        public SqlParameter pGroupId;
        public SqlParameter pGroupName;
        public SqlParameter pIsVendorAdmin;
        public SqlParameter pIsGroupAdmin;
        public SqlParameter pIsSysAdmin;
    }
    private static SqlCommand CreateUserCommand(SqlConnection conn, string userSID, ResultData result)
    {
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = $@"GetUserInfoForClaims_v3";
        cmd.CommandType = CommandType.StoredProcedure;

        // User info
        cmd.Parameters.AddWithValue("@userName", userSID);
        result.pUserId = cmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier);
        result.pUserId.Direction = ParameterDirection.Output;
        // Vendor info
        result.pVendorId = cmd.Parameters.Add("@vendorId", SqlDbType.UniqueIdentifier);
        result.pVendorId.Direction = ParameterDirection.Output;
        result.pVendorName = cmd.Parameters.Add("@vendorName", SqlDbType.NVarChar, 100);
        result.pVendorName.Direction = ParameterDirection.Output;
        result.pIsVendorAdmin = cmd.Parameters.Add("@isVendorAdmin", SqlDbType.Bit);
        result.pIsVendorAdmin.Direction = ParameterDirection.Output;
        // Group Info
        result.pGroupId = cmd.Parameters.Add("@groupId", SqlDbType.UniqueIdentifier);
        result.pGroupId.Direction = ParameterDirection.Output;
        result.pGroupName = cmd.Parameters.Add("@groupName", SqlDbType.NVarChar, 100);
        result.pGroupName.Direction = ParameterDirection.Output;
        result.pIsGroupAdmin = cmd.Parameters.Add("@isGroupAdmin", SqlDbType.Bit);
        result.pIsGroupAdmin.Direction = ParameterDirection.Output;
        // SysAdmin INFO
        result.pIsSysAdmin = cmd.Parameters.Add("@isSysAdmin", SqlDbType.Bit);
        result.pIsSysAdmin.Direction = ParameterDirection.Output;

        return cmd;
    }
    private static ClaimValues GetClaimsFromResult(ResultData result)
    {
        ClaimValues values = new();
        if ((result.pUserId.Value is not DBNull) && (result.pUserId.Value != null))
        {
            values.Add(MyDmvProClaims.UserId, result.pUserId.Value?.ToString());
            // VENDOR INFO
            if ((result.pVendorId.Value is not DBNull) && (result.pVendorId.Value != null))
            {
                values.Add(MyDmvProClaims.VendorId, result.pVendorId.Value.ToString());
            }
            if ((result.pVendorName.Value is not DBNull) && (result.pVendorName.Value != null))
            {
                values.Add(MyDmvProClaims.VendorName, result.pVendorName.Value.ToString());
            }
            if ((result.pIsVendorAdmin.Value is not DBNull) && (result.pIsVendorAdmin.Value != null))
            {
                if ((bool)result.pIsVendorAdmin.Value == true)
                {
                    values.Add(MyDmvProClaims.IsVendorAdmin, "true");
                }
            }
            // GROUP INFO
            if ((result.pGroupId.Value is not DBNull) && (result.pGroupId.Value != null))
            {
                values.Add(MyDmvProClaims.GroupId, result.pGroupId.Value.ToString());
            }
            if ((result.pGroupName.Value is not DBNull) && (result.pGroupName.Value != null))
            {
                values.Add(MyDmvProClaims.GroupName, result.pGroupName.Value.ToString());
            }
            if (result.pIsGroupAdmin.Value is not DBNull && (result.pIsGroupAdmin.Value != null))
            {
                if ((bool)result.pIsGroupAdmin.Value == true)
                {
                    values.Add(MyDmvProClaims.IsGroupAdmin, "true");
                }
            }
            // SYS ADMIN INFO
            if ((result.pIsSysAdmin.Value is not DBNull) && (result.pIsSysAdmin.Value != null))
            {
                if ((bool)result.pIsSysAdmin.Value == true)
                {
                    values.Add(MyDmvProClaims.IsSysAdmin, "true");
                }
            }
        }
        return values;
    }
    public static ClaimValues GetClaimsForUser(string userSID)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            conn.Open();
            ResultData resultData = new();
            SqlCommand cmd = CreateUserCommand(conn, userSID, resultData);
#if false
                cmd.CommandText = $@"GetUserInfoForClaims_v3";
                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter pVendorId, pVendorName, pUserId, pGroupId, pGroupName, pIsVendorAdmin, pIsGroupAdmin, pIsSysAdmin;
                // User info
                cmd.Parameters.AddWithValue("@userName", userSID);
                pUserId = cmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier);
                pUserId.Direction = ParameterDirection.Output;
                // Vendor info
                pVendorId = cmd.Parameters.Add("@vendorId", SqlDbType.UniqueIdentifier);
                pVendorId.Direction = ParameterDirection.Output;
                pVendorName = cmd.Parameters.Add("@vendorName", SqlDbType.NVarChar, 100);
                pVendorName.Direction = ParameterDirection.Output;
                pIsVendorAdmin = cmd.Parameters.Add("@isVendorAdmin", SqlDbType.Bit);
                pIsVendorAdmin.Direction = ParameterDirection.Output;
                // Group Info
                pGroupId = cmd.Parameters.Add("@groupId", SqlDbType.UniqueIdentifier);
                pGroupId.Direction = ParameterDirection.Output;
                pGroupName = cmd.Parameters.Add("@groupName", SqlDbType.NVarChar, 100);
                pGroupName.Direction = ParameterDirection.Output;
                pIsGroupAdmin = cmd.Parameters.Add("@isGroupAdmin", SqlDbType.Bit);
                pIsGroupAdmin.Direction = ParameterDirection.Output;
                // SysAdmin INFO
                pIsSysAdmin = cmd.Parameters.Add("@isSysAdmin", SqlDbType.Bit);
                pIsSysAdmin.Direction = ParameterDirection.Output;
#endif
            var result = cmd.ExecuteScalar();

            conn.Close();
            return GetClaimsFromResult(resultData);
        }
        catch (Exception ex)
        {
            return new ClaimValues();
        }
    }
    public static async Task<ClaimValues> GetClaimsForUserAsync(string userSID)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();

            ResultData resultData = new();

            SqlCommand cmd = CreateUserCommand(conn, userSID, resultData);

            var result = await cmd.ExecuteScalarAsync();

            await conn.CloseAsync();

            return GetClaimsFromResult(resultData);
        }
        catch (Exception)
        {
            return new ClaimValues();
        }
    }
    public static async Task AssignToUser(UserInfo currentUser, List<Guid> requestIDs, Guid assignToUserId)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = "AssignRequestToUser";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@RequestIDs", requestIDs);

        SqlParameter pUserId = cmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier);
        pUserId.Value = assignToUserId;

        // Admin userid is group admin or the user who items are currently assigned
        SqlParameter pAdminId = cmd.Parameters.Add("@adminUserId", SqlDbType.UniqueIdentifier);
        pAdminId.Value = currentUser.UserId;

        await cmd.ExecuteNonQueryAsync();
    }
    public static async Task FormAnalyzer_AttachWithoutUpdate(UserInfo user, List<Guid> uploadIds)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = "FormAnalyzer_AttachWithoutUpdate";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@UploadIDs", uploadIds);
        cmd.Parameters.AddWithValue("@agentId", user.UserId);
        cmd.Parameters.AddWithValue("@vendorId", user.VendorId);

        await cmd.ExecuteNonQueryAsync();
    }
    public static async Task FormAnalyzer_ProcessTitles(UserInfo user, List<Guid> uploadIds)
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = "FormAnalyzer_ProcessTitles";
        cmd.CommandType = CommandType.StoredProcedure;

        AddRequestIDsParameter(cmd, "@UploadIDs", uploadIds);
        cmd.Parameters.AddWithValue("@agentId", user.UserId);
        cmd.Parameters.AddWithValue("@vendorId", user.VendorId);

        await cmd.ExecuteNonQueryAsync();
    }
    public static async Task<string> PerformSystemCheck()
    {
        using SqlConnection conn = new(SqlConnectionString);
        await conn.OpenAsync();
        SqlCommand cmd = conn.CreateCommand();

        cmd.CommandText = "PerformSystemCheck";
        cmd.CommandType = CommandType.StoredProcedure;

        StringBuilder sb = new();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    sb.AppendLine(reader.GetString(0));
                }
            }
        }
        return sb.ToString();
    }
    public static async Task<int> RollbackRequests(Guid userId, string note, string remark, string code, List<Guid> ids)
    {
        try
        {
            using SqlConnection conn = new(SqlConnectionString);
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "RollbackRequests";
            cmd.CommandType = CommandType.StoredProcedure;

            AddRequestIDsParameter(cmd, "@RequestIDs", ids);

            cmd.Parameters.AddWithValue("@userId", userId);

            if (string.IsNullOrWhiteSpace(note)) note = null;
            if (string.IsNullOrWhiteSpace(remark)) remark = null;
            if (string.IsNullOrWhiteSpace(code)) code = null;

            cmd.Parameters.AddWithValue("@note", (object)note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@remark", (object)remark ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@code", (object)code ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
            throw;
        }
    }
    /// <summary>
    /// Returns json containing profile for use in .js script
    /// </summary>
    /// <param name="vendorId"></param>
    /// <param name=""></param>
    /// <returns></returns>
    public static async Task<string> GetBulkEditProfiles(Guid? vendorId, string appType, string appTypeState)
    {
        string json = "";

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "dbo.GetBulkEditProfiles";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@vendorId", EmptyToDbNull(vendorId));
            cmd.Parameters.AddWithValue("@appType", EmptyToDbNull(appType));
            cmd.Parameters.AddWithValue("@appTypeState", EmptyToDbNull(appTypeState));

            using var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    json += reader.GetString(0);
                }
            }
        }

        return json;
    }
    public class AppFeeInfo
    {
        public string AppType { get; set; }
        public string AppState { get; set; }
        public Guid? AppTypeStateId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? VendorId { get; set; }
        public decimal? DMVFee { get; set; }
        public string DMVPayToName { get; set; }
        public decimal DmvTotal => DMVFee.GetValueOrDefault() + MiscFee1.GetValueOrDefault();
        public decimal? MiscFee1 { get; set; }
        public string MiscDescription { get; set; }
        public string LienholderName { get; set; }
        public decimal? ServiceFee { get; set; }
        public decimal? MailingFee { get; set; }
        public bool? SecondaryInvoice { get; set; }
    }
    /// <summary>
    /// Returns json containing fees for an app type
    /// </summary>
    /// <param name="vendorId"></param>
    /// <param name=""></param>
    /// <returns></returns>
    public static async Task<List<AppFeeInfo>> GetApplicationFees(Guid? vendorId, Guid? groupId, string appType, string appState, Guid? appTypeStateId, string lienholderName)
    {
        string json = "";
        Guid? lienholderId = null;

        List<AppFeeInfo> fees = new();

        using (SqlConnection conn = new(SqlConnectionString))
        {
            await conn.OpenAsync();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "dbo.GetFeesForApplication_v2";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@vendorId", EmptyToDbNull(vendorId));
            cmd.Parameters.AddWithValue("@groupId", EmptyToDbNull(groupId));
            cmd.Parameters.AddWithValue("@appType", EmptyToDbNull(appType));
            cmd.Parameters.AddWithValue("@appState", EmptyToDbNull(appState));
            cmd.Parameters.AddWithValue("@appTypeStateId", EmptyToDbNull(appTypeStateId));
            cmd.Parameters.AddWithValue("@lienholderName", EmptyToDbNull(lienholderName));
            cmd.Parameters.AddWithValue("@lienholderId", EmptyToDbNull(lienholderId));

            using var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    AppFeeInfo fee = new()
                    {
                        AppType = reader.GetString("AppType") as string,
                        AppState = reader.GetString("AppState") as string,
                        AppTypeStateId = reader.GetGuid("AppTypeStateId"),
                        GroupId = reader.GetGuid("GroupId"),
                        VendorId = reader.GetGuid("VendorId"),
                        DMVFee = reader.GetDecimal("DmvDisbursement"),
                        DMVPayToName = reader.GetString("DmvPayToName"),
                        MiscFee1 = reader.GetDecimal("MiscDisbursement1"),
                        MiscDescription = reader.GetString("MiscDescription"),
                        LienholderName = reader.GetString("LienholderName"),
                        ServiceFee = reader.GetDecimal("ServiceFee"),
                        MailingFee = reader.GetDecimal("MailingFee"),
                        SecondaryInvoice = reader.GetBoolean("SecondaryInvoice")
                    };
                    fees.Add(fee);
                }
            }
        }

        return fees;
    }
}

[Serializable]
class ExportDefinition
{
    public List<ColumnExportInfo> Export { get; set; }
    public bool ExcludeUnlisted { get; set; }
}

[Serializable]
class ColumnExportInfo
{
    public string InternalName { get; set; }
    public string ExcelName { get; set; }
    public string AltSource { get; set; }
    public bool Exclude { get; set; }
    public bool MultiEditable { get; set; }
    public string FldType { get; set; }
    public string RequiredType { get; set; }
}

[Serializable]
public class ProfileCategory
{
    public ProfileCategory()
    {
        Columns = new();
    }
    public string Name { get; set; }
    public Guid? GroupProfileCategoryID { get; set; }
    public List<ProfileColumnInfo> Columns { get; set; }

    public List<Dictionary<string, string>> ListData { get; set; } = new();
    public List<Dictionary<string, object>> EditorFields { get; set; } = new();
    public List<Dictionary<string, object>> ListColumns { get; set; } = new();
}

[Serializable]
public class ProfileCategories
{
    public ProfileCategories()
    {
        Categories = new();
    }
    [JsonProperty("Profiles")]
    public List<ProfileCategory> Categories { get; set; }
    public ProfileCategory GetProfile(string CategoryName)
    {
        return Categories.Where(l => l.Name == CategoryName).SingleOrDefault();
    }
}

[Serializable]
public class ProfileColumnInfo
{
    public string InternalName { get; set; }
    public string ExcelName { get; set; }
    public string AltSource { get; set; }
    public bool MultiEditable { get; set; }
    public string FldType { get; set; }
    public string RequiredType { get; set; }
    public string Label { get; set; }
}

static class ReaderExtensions
{
    public static string GetStringSafe(this IDataReader reader, int colIndex)
    {
        return GetStringSafe(reader, colIndex, string.Empty);
    }

    public static string GetStringSafe(this IDataReader reader, int colIndex, string defaultValue)
    {
        if (!reader.IsDBNull(colIndex))
            return reader.GetString(colIndex);
        else
            return defaultValue;
    }
#if NOT_YET_USED
    public static string GetStringSafe(this IDataReader reader, string indexName)
    {
        return GetStringSafe(reader, reader.GetOrdinal(indexName));
    }

    public static string GetStringSafe(this IDataReader reader, string indexName, string defaultValue)
    {
        return GetStringSafe(reader, reader.GetOrdinal(indexName), defaultValue);
    }
#endif
}

public static class DataReaderExtensions_v2
{
    public static Guid? GetGuid(this SqlDataReader reader, string name)
    {
        var ord = reader.GetOrdinal(name);
        var val = reader.GetSqlGuid(ord);
        return val.IsNull ? (Guid?)null : val.Value;
    }
    // generate for string, int, bool, etc.
    public static string GetString(this SqlDataReader reader, string name)
    {
        var ord = reader.GetOrdinal(name);
        var val = reader.GetSqlString(ord);
        return val.IsNull ? (string)null : val.Value;
    }
    public static int? GetInt(this SqlDataReader reader, string name)
    {
        var ord = reader.GetOrdinal(name);
        var val = reader.GetSqlInt32(ord);
        return val.IsNull ? (int?)null : val.Value;
    }
    public static bool? GetBoolean(this SqlDataReader reader, string name)
    {
        var ord = reader.GetOrdinal(name);
        var val = reader.GetSqlBoolean(ord);
        return val.IsNull ? (bool?)null : val.Value;
    }
    public static decimal? GetDecimal(this SqlDataReader reader, string name)
    {
        var ord = reader.GetOrdinal(name);
        var val = reader.GetSqlDecimal(ord);
        return val.IsNull ? (decimal?)null : val.Value;
    }
}