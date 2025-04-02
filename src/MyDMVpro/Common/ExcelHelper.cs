using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using MyDMVpro.Models;
using DocumentFormat.OpenXml;
using System.Text;

namespace MyDMVpro.Common
{
    public class ExcelHelper : IDisposable 
    {
        private MaggardDMVContext _context;
        private static Dictionary<string, Type> ColumnTypes = new Dictionary<string, Type>()
        {
            { "Alternate Phone", typeof(string) },
            { "Alternate Extension", typeof(string)},
            { "City", typeof(string) },
            { "Country", typeof(string) },
            { "E-mail", typeof(string) },
            { "Address 1", typeof(string) },
            { "Address 2", typeof(string) },
            { "Address 3", typeof(string) },
            { "Phone", typeof(string) },
            { "ZIP Code", typeof(string) },
            { "State", typeof(string) },
            { "Extension", typeof(string) },
            { "ID/Status", typeof(string) },
            { "Company", typeof(string) },
            { "Web Site", typeof(string) },
            { "Create Date", typeof(DateTime) },
            { "Record Creator", typeof(string) },
            { "Abstract", typeof(string) },
            { "Affirmation", typeof(string) },
            { "Amt pd", typeof(string) },
            { "Auctioneer", typeof(string) },
            { "Balance Due", typeof(string) },
            { "Bal Pd Date", typeof(DateTime) },
            { "-Billed-", typeof(string) },
            { "Borrower Address", typeof(string) },
            { "Borrower City", typeof(string) },
            { "Borrower Last Reg", typeof(string) },
            { "Borrower Name", typeof(string) },
            { "Borrower FName", typeof(string) },
            { "Borrower LName", typeof(string) },
            { "Borrower State", typeof(string) },
            { "Borrower Zip", typeof(string) },
            { "Certify Actual Mileage", typeof(string) },
            { "Certify Mech Limits", typeof(string) },
            { "Certify Odometer Discrepancy", typeof(string) },
            { "Client check", typeof(string) },
            { "Client Id Number", typeof(string) },
            { "Client Ref", typeof(string) },
            { "CoBorrower Name", typeof(string) },
            { "CoBorrower FName", typeof(string) },
            { "CoBorrower LName", typeof(string) },
            { "Courier", typeof(string) },
            { "Date Authorized", typeof(DateTime) },
            { "Date Pd", typeof(DateTime) },
            { "Date Received", typeof(DateTime) },
            { "Date Title Issued", typeof(DateTime) },
            { "DMV Disb", typeof(string) },
            { "DMV District Office Notified", typeof(string) },
            { "DMV Notification Date", typeof(DateTime) },
            { "Doors", typeof(string) },
            { "Draw", typeof(string) },
            { "ETA", typeof(string) },
            { "Finish Date", typeof(DateTime) },
            { "Leasing Company Name", typeof(string) },
            { "Legal Sale Date", typeof(DateTime) },
            { "Lien Holder Address", typeof(string) },
            { "Lien Holder City", typeof(string) },
            { "Lienholder Corp Code", typeof(string) },
            { "Lien Holder County", typeof(string) },
            { "Lien Holder L A", typeof(string) },
            { "Lien Holder L C", typeof(string) },
            { "Lien Holder Legal County", typeof(string) },
            { "Lien Holder Legal Zip", typeof(string) },
            { "Lien Holder L S", typeof(string) },
            { "Lien Holder Name", typeof(string) },
            { "Lien Holder Phone", typeof(string) },
            { "Lien Holder State", typeof(string) },
            { "Lienholder Tax ID No", typeof(string) },
            { "Lien Holder Zip", typeof(string) },
            { "Lien Renewed", typeof(string) },
            { "MIA CK", typeof(string) },
            { "Other Notified Date", typeof(DateTime) },
            { "Other Notified Name", typeof(string) },
            { "Paid in Full", typeof(string) },
            { "PA Lien Expiration Date", typeof(DateTime) },
            { "PartneringProcess", typeof(string) },
            { "Pa Title Number", typeof(string) },
            { "Person Submitting Form", typeof(string) },
            { "Police Agency Notified", typeof(string) },
            { "Police Notification Date", typeof(DateTime) },
            { "Postage", typeof(string) },
            { "Problem App", typeof(string) },
            { "Purchase Price", typeof(string) },
            { "Record No", typeof(string) },
            { "Refund", typeof(string) },
            { "Rejected", typeof(string) },
            { "Repo Date", typeof(DateTime) },
            { "Sales Tax", typeof(string) },
            { "Security Contract Date", typeof(DateTime) },
            { "Service Code", typeof(string) },
            { "Service Company Name", typeof(string) },
            { "Service Fee", typeof(string) },
            { "State Titled", typeof(string) },
            { "DMV Submit Date", typeof(DateTime) },
            { "Time Received", typeof(DateTime) },
            { "Total Amt Due", typeof(string) },
            { "Tracking", typeof(string) },
            { "VA Title Number", typeof(string) },
            { "Vehicle Body Type", typeof(string) },
            { "Vehicle Color", typeof(string) },
            {  "Vehicle Cylinders", typeof(string) },
            { "Vehicle Has Been Wrecked", typeof(string) },
            { "Vehicle Has Not Wrecked", typeof(string) },
            { "Vehicle Make", typeof(string) },
            { "Vehicle Max Gross Weight", typeof(string) },
            { "Vehicle Mileage Brand", typeof(string) },
            { "Vehicle Odometer", typeof(string) },
            { "Vehicle Odometer Numbers", typeof(string) },
            { "Vehicle Other", typeof(string) },
            { "Vehicle Owner Notification Date", typeof(DateTime) },
            { "Vehicle Power", typeof(string) },
            { "Vehicle Unladen Weight", typeof(string) },
            { "Vehicle Vin", typeof(string) },
            { "Vehicle Year", typeof(string) },
            { "Z-Received Title", typeof(string) },
            { "Department", typeof(string) },
            { "Edit Date", typeof(DateTime) },
            { "Last Edited By", typeof(string) },
            { "Fax Phone", typeof(string) },
            { "Fax Extension", typeof(string) },
            { "First Name", typeof(string) },
            { "Contact", typeof(string) },
            { "Home City", typeof(string) },
            { "Home Country", typeof(string) },
            { "Home Address 1", typeof(string) },
            { "Home Address 2", typeof(string) },
            { "Home Address 3", typeof(string) },
            { "Home Phone", typeof(string) },
            { "Home ZIP Code", typeof(string) },
            { "Home State", typeof(string) },
            { "Home Extension", typeof(string) },
            { "Messenger ID", typeof(string) },
            { "Private Contact", typeof(string) },
            { "Title", typeof(string) },
            { "Last Attempt", typeof(string) },
            { "Last E-mail", typeof(string) },
            { "Letter Date", typeof(DateTime) },
            { "Last Meeting", typeof(string) },
            { "Last Name", typeof(string) },
            { "Last Reach", typeof(string) },
            { "Last Results", typeof(string) },
            { "Record Manager", typeof(string) },
            { "Middle Name", typeof(string) },
            { "Mobile Phone", typeof(string) },
            { "Mobile Extension", typeof(string) },
            { "Name Prefix", typeof(string) },
            { "Name Suffix", typeof(string) },
            { "Pager Phone", typeof(string) },
            { "Pager Extension", typeof(string) },
            { "Personal E-mail", typeof(string) },
            { "Referred By", typeof(string) },
            { "Salutation", typeof(string) },
            { "Spouse", typeof(string) },
        };
        public ExcelHelper(MaggardDMVContext context)
        {
            _context = context;
        }

        public Stream GetExcelFileStream(Guid ID, string userName)
        {
            var file = (from tempupload in _context.FileUploads
                        .Where(x => (x.User.UserPrincipalName == userName || x.User.NameIdentifierClaim == userName) && x.FileUploadId == ID)
                        .AsNoTracking()
                        select tempupload).FirstOrDefault();
            if (file != null)
            {
                return new MemoryStream(file.FileImage);
            }
            return null;
        }

        public static string GetRangeJson(Stream stream, Guid? groupId)
        {
            string json;

            // Translation mapping provides column which data is to 
            //    be copied from to the internal column name
            //
            // {
            //   "DestColumn1": "SourceColumn1",
            //   "DestColumn2": "SourceColumn1"
            // }
            //
            {
                Dictionary<string, string> mapping = GetTranslationMapping(groupId);
                Dictionary<string, string> sourceFields = new Dictionary<string, string>();

                // Read the excel spreadsheet using user columns
                DataTable dt = GetRange(stream, new Dictionary<string, string>());
                
                // For each column in the mapping that does not have a blank source name
                //     add a new column and add to the list of fields being copied from other columns
                // 
                foreach (string destColName in mapping.Keys)
                {
                    string fromColName = mapping[destColName];
                    if (fromColName == destColName)
                        continue; // just skip

                    if (!dt.Columns.Contains(destColName))
                    {
                        if (!string.IsNullOrWhiteSpace(fromColName))
                        {
                            // If fromColumn does not exist, then 
                            // there will not be a value to copy
                            // and no need for the column
                            if (dt.Columns.Contains(fromColName))
                            {
                                Type colType = typeof(string);
                                if (ColumnTypes.ContainsKey(destColName))
                                {
                                    colType = ColumnTypes[destColName];
                                }
                                dt.Columns.Add(destColName, colType);
                                sourceFields.Add(destColName, fromColName);
                            }
                        }
                    }
                    else
                    {
                        // The destination column already exists in the DataTable
                        // add to copyFields 
                        sourceFields.Add(destColName, fromColName);
                    }
                }
                List<string> copyList = new List<string>();
                // Now update each row by copying the source column into the destination column
                //
                foreach (DataRow row in dt.Rows)
                {
                    foreach (var toKey in sourceFields.Keys)
                    {
                        string fromColumn = sourceFields[toKey];
                        if (dt.Columns.Contains(fromColumn))
                        {
                            object colval = row[fromColumn].ToString();
                            DataColumn col = dt.Columns[toKey];
                            if (col.DataType == typeof(DateTime))
                            {
                                if (double.TryParse(colval as string, out double dbl))
                                {
                                    DateTime date = DateTime.FromOADate(dbl);
                                    colval = date;
                                }
                                else if (string.IsNullOrWhiteSpace(colval as string))
                                {
                                    colval = DBNull.Value;
                                }
                                else
                                {
                                    DateTime dateTimeValue;
                                    if (DateTime.TryParse(colval as string, out dateTimeValue))
                                    {
                                        colval = dateTimeValue;
                                    }
                                    else
                                    {
                                        colval = DBNull.Value;
                                    }
                                }
                            }
                            if (row[toKey] is DBNull)
                            {
                                row[toKey] = colval;
                            }
                            else
                            {
                                // skip, do not overwrite existing
#if DEBUG
                                System.Diagnostics.Trace.WriteLine($"Column already contains data for this row: {toKey}");
#endif
                            }
                            if (!ColumnTypes.ContainsKey(fromColumn) && !copyList.Contains(fromColumn))
                            {
                                // used to identify that columns being 
                                // removed have had their data used in another column
                                copyList.Add(fromColumn);
                            }
                        }
                        else //if (!dt.Columns.Contains(toKey))
                        {
                            //TODO: Log warning...
                            System.Diagnostics.Trace.WriteLine($"Missing column defined in mapping: {toKey}");
                        }
                    }
                }

                List<string> fieldsToRemoveAfterCopy = new List<string>();
                foreach (DataColumn col in dt.Columns)
                {
                    if (!ColumnTypes.ContainsKey(col.ColumnName) && copyList.Contains(col.ColumnName))
                    {
                        // This is not a known column, and was copied to another column
                        // so will remove after copying data
                        fieldsToRemoveAfterCopy.Add(col.ColumnName);
                    }
                }

#if DEBUG
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("---------------------------------------------");
                    sb.AppendLine("Removing unused columns from import DataTable");
                    foreach (string colName in fieldsToRemoveAfterCopy)
                    {
                        sb.AppendLine($"Removing column: {colName}");
                    }
                    sb.AppendLine("---------------------------------------------");
                    System.Diagnostics.Trace.WriteLine(sb.ToString());
                }
#endif
                foreach (string colName in fieldsToRemoveAfterCopy)
                {
                    dt.Columns.Remove(colName);
                }
                foreach (DataRow row in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        DataColumn col = dt.Columns[i];

                        if (row[i] is DBNull)
                        {
                            // ignore
                        }
                        else
                        {
                            if (col.DataType == typeof(string))
                            {
                                string val = row[i] as string;
                                if (val != null && val == "")
                                {
                                    row[i] = DBNull.Value;
                                    System.Diagnostics.Trace.WriteLine($"Nulling column value: {col.ColumnName}");
                                }
                            }
                        }
                    }
                }
#if DEBUG
                {
                    // DUMP columns imported
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("------------------");
                    sb.AppendLine("Columns imported");
                    foreach (DataColumn col in dt.Columns)
                    {
                        sb.AppendLine(col.ColumnName);
                    }
                    sb.AppendLine("------------------");
                    System.Diagnostics.Trace.WriteLine(sb.ToString());
                }
#endif
                JsonSerializerSettings jsettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
                json = JsonConvert.SerializeObject(dt, Formatting.Indented, jsettings);
            }
            return json;
        }
        public static Dictionary<string, JToken> GetExtendedTranslationMapping(Guid? groupId)
        {
            Dictionary<string, JToken> columns = new Dictionary<string, JToken>();
            try
            {
                string jsonMapping = DataHelpers.GetImportMapping(groupId).Result;

                // Check columns against expected
                JArray mapping = JArray.Parse(jsonMapping);
                foreach (JObject content in mapping.Children<JObject>())
                {
                    foreach (JProperty prop in content.Properties())
                    {
                        if (!columns.ContainsKey(prop.Name))
                        {
                            columns.Add(prop.Name, prop);
                        }
                        else {
                            System.Diagnostics.Trace.TraceError($"TranslateTableToInternalNames: duplicate column {prop.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("TranslateTableToInternalNames(): {0}", ex);
            }
            return columns;
        }
        public static Dictionary<string, string> GetTranslationMapping(Guid? groupId)
        {
            Dictionary<string, string> columns = new Dictionary<string, string>();
            try
            {
                string jsonMapping = DataHelpers.GetImportMapping(groupId).Result;

                // Check columns against expected
                JArray mapping = JArray.Parse(jsonMapping);
                foreach (JObject content in mapping.Children<JObject>())
                {
                    foreach (JProperty prop in content.Properties())
                    {
                        JToken v = prop.Value;
                        if (v.Type == JTokenType.Array)
                        {
                            // Reserved for future use
                        }
                        else
                        {
                            string s = v.Value<string>();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                if (!columns.ContainsKey(prop.Name))
                                {
                                    columns.Add(prop.Name, s);
                                }
                                else
                                {
                                    //TODO: log error here
                                    System.Diagnostics.Trace.TraceError($"TranslateTableToInternalNames(): duplicate column '{s}' maps to '{prop.Name}' ({columns[s]})");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("TranslateTableToInternalNames(): {0}", ex);
            }
            return columns;
        }
        public static DataTable TranslateTableToInternalNames(DataTable dt, Guid? groupId)
        {
            DataTable translatedDt = null;
            try
            {
                string jsonMapping = DataHelpers.GetImportMapping(groupId).Result;

                // Check columns against expected
                Dictionary<string,string> columns = new Dictionary<string, string>();
                JArray mapping = JArray.Parse(jsonMapping);
                foreach (JObject content in mapping.Children<JObject>())
                {
                    foreach (JProperty prop in content.Properties())
                    {
                        columns.Add(prop.Value<string>(), prop.Name);
                    }
                }

                //foreach (DataColumn col in dt.Columns)
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    DataColumn col = dt.Columns[i];
                    string oldcolname = col.ColumnName;
                    string internalcolname = columns[oldcolname];
                    col.ColumnName = internalcolname;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("TranslateTableToInternalNames(): {0}", ex);
            }
            return translatedDt;
        }
        public static DataTable TransformImportTable(DataTable dt, Guid? groupId)
        {
            DataTable translatedDt = null;
            try
            {
                string jsonMapping = DataHelpers.GetImportMapping(groupId).Result;

                // Check columns against expected
                Dictionary<string, string> columns = new Dictionary<string, string>();
                JArray mapping = JArray.Parse(jsonMapping);
                foreach (JObject content in mapping.Children<JObject>())
                {
                    foreach (JProperty prop in content.Properties())
                    {
                        columns.Add(prop.Value<string>(), prop.Name);
                    }
                }

                //foreach (DataColumn col in dt.Columns)
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    DataColumn col = dt.Columns[i];
                    string oldcolname = col.ColumnName;
                    string internalcolname = columns[oldcolname];
                    col.ColumnName = internalcolname;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("TranslateTableToInternalNames(): {0}", ex);
            }
            return translatedDt;
        }
        public static string GetInternalColumnName(string name, Dictionary<string, JToken> mapping)
        {
            foreach (string key in mapping.Keys)
            {
                JToken tok = mapping[key];
                if (tok.Type == JTokenType.String)
                {
                    return tok.Value<string>();
                }
            }
            return null;
        }
        public static string CalcValue(string internalFieldName, Dictionary<string, JToken> mapping, DataRow row)
        {
            if (mapping.ContainsKey(internalFieldName))
            {
                JToken tok = mapping[internalFieldName];
                if (tok.Type == JTokenType.Object)
                {
                    // command
                    if (tok.First.Type == JTokenType.String)
                    {
                        string cmd = tok.First.Value<string>();
                        if (cmd == "concat")
                        {
                            List<string> values = new List<string>();
                            foreach (JToken ctok in tok.Children())
                            {
                                System.Diagnostics.Trace.WriteLine($"{ctok}");
                            }
                        }
                        else if (cmd == "transform")
                        {
                            List<string> values = new List<string>();
                            JToken fieldTok = tok["field"];
                            JToken valuesTok = tok["values"];

                        }
                        // { "concat": { "field":"Customer_First_Name", "string": " ", "field": "Customer_Last_Name" } }
                        // 	"Vehicle Mileage Brand": { "transform": {"field":"Vehicle Mileage Brand", "values": {"A": "Actual", "E": "Exceeds", "NA":"NOT Actual", "X": "Exempt"}}}
                    }
                }
            }
            return null;
        }
        public static string Concat(JToken tok)
        {
            StringBuilder sb = new StringBuilder();
            foreach (JToken subtok in tok.Children())
            {
                object o = subtok;
            }
            return sb.ToString();
        }
        public static string Transform(DataTable dt, JToken fieldTok, JToken valuesTok)
        {
            string fieldName = fieldTok.Value<string>();
            return fieldName;
        }
        public static DataTable GetExtendedRange(Stream stream, Dictionary<string, JToken> mapping)
        {
            DataTable dt = new DataTable();

            using (SpreadsheetDocument spreadSheetDocument = SpreadsheetDocument.Open(stream, false))
            {
                WorkbookPart workbookPart = spreadSheetDocument.WorkbookPart;
                IEnumerable<Sheet> sheets = spreadSheetDocument.WorkbookPart.Workbook.GetFirstChild<Sheets>().Elements<Sheet>();
                string relationshipId = sheets.First().Id.Value;
                WorksheetPart worksheetPart = (WorksheetPart)spreadSheetDocument.WorkbookPart.GetPartById(relationshipId);
                Worksheet workSheet = worksheetPart.Worksheet;
                SheetData sheetData = workSheet.GetFirstChild<SheetData>();
                IEnumerable<Row> rows = sheetData.Descendants<Row>();

                // Get column headers
                foreach (Cell cell in rows.ElementAt(0))
                {
                    string colName = GetCellValue(spreadSheetDocument, cell);

                    string internalColName = GetInternalColumnName(colName, mapping);

                    Type type = null;
                    if (ColumnTypes.ContainsKey(colName))
                        type = ColumnTypes[colName];
                    if (type == null)
                    {
                        // translate
                        if (mapping.ContainsKey(colName))
                        {
                            colName = mapping[colName].Path;
                            if (ColumnTypes.ContainsKey(colName))
                                type = ColumnTypes[colName];
                        }
                    }
                    if (type == null)
                        type = GetCellType(spreadSheetDocument, rows, cell);
                    dt.Columns.Add(colName, type);
                }

                // Add calc columns
                List<string> calcColumns = new List<string>();
                foreach (string key in mapping.Keys)
                {
                    // if column not found, then add
                    if (!dt.Columns.Contains(key))
                    {
                        Type type = typeof(string);
                        if (ColumnTypes.ContainsKey(key))
                        {
                            type = ColumnTypes[key];
                        }
                        if (mapping[key].First.Type == JTokenType.Object)
                        {
                            dt.Columns.Add(key, type);
                            calcColumns.Add(key);
                        }
                    }
                }

                bool firstRow = true;
                bool rowDataFound = false;
                foreach (Row row in rows) //this will also include your header row...
                {
                    if (firstRow) { firstRow = false; continue; }

                    DataRow tempRow = dt.NewRow();
                    bool found = false;
                    for (int i = 0; i < row.Descendants<Cell>().Count(); i++)
                    {
                        Cell cell = row.Descendants<Cell>().ElementAt(i);
                        int? index = GetColumnIndex(cell);
                        if (index.HasValue && index.Value == 1)
                        {
                            found = true;
                        }
                        if (index.HasValue)
                        {
                            string colval = GetCellValue(spreadSheetDocument, cell);
                            if (string.IsNullOrEmpty(colval))
                                continue;
                            rowDataFound = true;
                            DataColumn col = dt.Columns[index.Value];
                            if (col.DataType == typeof(DateTime))
                            {
                                if (double.TryParse(colval, out double dbl))
                                {
                                    DateTime date = DateTime.FromOADate(dbl);
                                    tempRow[index.Value] = date;
                                    continue;
                                }
                            }
                            try
                            {
                                tempRow[index.Value] = colval;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.TraceError("{0}", ex);
                            }
                        }
                    }
                    if (rowDataFound)
                    {
                        UpdateCalculatedValues(tempRow, mapping, calcColumns);
                        dt.Rows.Add(tempRow);
                    }
                }
            }

            return dt;
        }
        public static void UpdateCalculatedValues(DataRow row, Dictionary<string, JToken> mapping, List<string> calcColumns)
        {
            foreach (string colName in calcColumns)
            {
                row[colName] = Calc(mapping[colName], row);
            }
        }
        public static string Calc(JToken tok, DataRow row)
        {
            string cmd = tok.ToString();
            return "";
        }

        public static DataTable GetRange(Stream stream, Dictionary<string, string> mapping)
        {
            DataTable dt = new DataTable();

            using (SpreadsheetDocument spreadSheetDocument = SpreadsheetDocument.Open(stream, false))
            {
                WorkbookPart workbookPart = spreadSheetDocument.WorkbookPart;
                IEnumerable<Sheet> sheets = spreadSheetDocument.WorkbookPart.Workbook.GetFirstChild<Sheets>().Elements<Sheet>();
                string relationshipId = sheets.First().Id.Value;
                WorksheetPart worksheetPart = (WorksheetPart)spreadSheetDocument.WorkbookPart.GetPartById(relationshipId);
                Worksheet workSheet = worksheetPart.Worksheet;
                SheetData sheetData = workSheet.GetFirstChild<SheetData>();
                IEnumerable<Row> rows = sheetData.Descendants<Row>();

                foreach (Cell cell in rows.ElementAt(0))
                {
                    string colName = GetCellValue(spreadSheetDocument, cell);
                    Type type = null;
                    if (ColumnTypes.ContainsKey(colName))
                        type = ColumnTypes[colName];
                    if (type == null)
                    {
                        // translate
                        if (mapping.ContainsKey(colName))
                        {
                            colName = mapping[colName];
                            if (ColumnTypes.ContainsKey(colName))
                                type = ColumnTypes[colName];
                        }
                    }
                    if (type == null)
                        type = GetCellType(spreadSheetDocument, rows, cell);
                    dt.Columns.Add(colName, type);
                }

                bool firstRow = true;
                foreach (Row row in rows) //this will also include your header row...
                {
                    if (firstRow) { firstRow = false; continue; }

                    DataRow tempRow = dt.NewRow();
                    bool found = false;
                    bool rowDataFound = false;
                    for (int i = 0; i < row.Descendants<Cell>().Count(); i++)
                    {
                        Cell cell = row.Descendants<Cell>().ElementAt(i);
                        int? index = GetColumnIndex(cell);
                        if (index.HasValue && index.Value == 1)
                        {
                            found = true;
                        }
                        if (index.HasValue)
                        {
                            string colval = GetCellValue(spreadSheetDocument, cell);
                            if (string.IsNullOrEmpty(colval))
                                continue;
                            rowDataFound = true;
                            DataColumn col = dt.Columns[index.Value];
                            if (col.DataType == typeof(DateTime))
                            {
                                if (double.TryParse(colval, out double dbl))
                                {
                                    DateTime date = DateTime.FromOADate(dbl);
                                    tempRow[index.Value] = date;
                                    continue;
                                }
                                if (string.IsNullOrEmpty(colval))
                                {
                                    // leave as null value
                                    continue;
                                }
                            }
                            else
                            {
                                // Check for carriage return in the text value
                                //
                                // if data contains \n (slash followed by lowercase n) (not an actual carriage return)
                                if (colval.IndexOf(@"\n") >= 0)
                                {
                                    colval = colval.Replace(@"\r\n", " ");
                                    colval = colval.Replace(@"\n", " ");
                                }
                            }
                            try
                            {
                                tempRow[index.Value] = colval;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.TraceError("{0}", ex);
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (rowDataFound)
                        dt.Rows.Add(tempRow);
                }
            }

            return dt;
        }
        public static string GetColumnReference(Cell cell)
        {
            return cell.CellReference.ToString().ToUpper();
        }
        public static int? GetColumnIndex(Cell cell)
        {
            return GetColumnIndex(GetColumnReference(cell));
        }
        public static int? GetColumnIndex(string columnNameOrCellReference)
        {
            int columnIndex = -1;
            int factor = 1;
            for (int pos = columnNameOrCellReference.Length - 1; pos >= 0; pos--) // R to L
            {
                if (Char.IsLetter(columnNameOrCellReference[pos])) // for letters (columnName)
                {
                    columnIndex += factor * ((columnNameOrCellReference[pos] - 'A') + 1);
                    factor *= 26;
                }
            }
            return columnIndex;

        }
        public static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;

            if (cell.CellValue == null)
            {
                return "";// cell.InnerText;
            }

            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                string val = stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
                return val;
            }
            else
            {
                string val = value;
                return val ?? "";
            }
        }
        public static Type GetCellType(SpreadsheetDocument document, IEnumerable<Row> rows, Cell TitleCell)
        {
            int? titleIndex = GetColumnIndex(TitleCell);

            List<Type> typesFound = new List<Type>();
            foreach (Row row in rows)
            {
                if (row.RowIndex == 0)
                    continue;
                for (int i = 0; i < row.Descendants<Cell>().Count(); i++)
                {
                    Cell cell = row.Descendants<Cell>().ElementAt(i);
                    int? index = GetColumnIndex(cell);
                    if (!index.HasValue && index.Value != titleIndex)
                    {
                        continue;
                    }
                    Type t = GetCellType(cell);
                    if (t != null && !typesFound.Contains(t))
                        typesFound.Add(t);
                }
            }
            if (typesFound.Count == 1)
                return typesFound[0];
            return typeof(string);
        }
        private static Type GetCellType(Cell cell)
        {
            DocumentFormat.OpenXml.EnumValue<CellValues> cellType = cell.DataType;

            if (cellType == null || !cellType.HasValue)
                return null;

            Type type;
            switch (cellType.Value)
            {
                case CellValues.Boolean:
                    type = typeof(Boolean);
                    break;
                case CellValues.Date:
                    type = typeof(DateTime);
                    break;
                case CellValues.Number:
                    type = typeof(Decimal);
                    break;
                case CellValues.SharedString:
                    type = typeof(String);
                    break;
                default:
                    type = typeof(String);
                    break;
            }
            return type;
        }
        private static bool FormatAccess(SpreadsheetDocument SSD)
        {
            bool result = false;
            WorkbookPart WBP = SSD.WorkbookPart;
            Workbook WB = WBP.Workbook;
            Sheet S = WB.Descendants<Sheet>().FirstOrDefault();
            WorksheetPart WSP = WBP.GetPartById(S.Id) as WorksheetPart;
            if (WSP != null)
            {
                Worksheet WS = WSP.Worksheet;
                SheetData SD = WS.Descendants<SheetData>().FirstOrDefault();
                if (SD != null)
                {
                    UInt32Value CSIndex = 0U;
                    Cell A1 = SD.Descendants<Cell>()
                        .Where(Cl => Cl.CellReference == "P2").FirstOrDefault();
                    if (A1 != null)
                    {
                        CSIndex = A1.StyleIndex;
                    }
                    WorkbookStylesPart WSlP = WBP.WorkbookStylesPart;
                    Stylesheet Sls = WSlP.Stylesheet;
                    CellFormats CFs = Sls.Descendants<CellFormats>()
                        .FirstOrDefault();
                    CellFormat CF = CFs.Descendants<CellFormat>()
                        .ToList()[(int)CSIndex.Value];
                    UInt32Value NIndex = 0U;
                    if (CF != null)
                    {
                        NIndex = CF.NumberFormatId;
                    }
                    NumberingFormats NFs = Sls.Descendants<NumberingFormats>()
                        .FirstOrDefault();
                    NumberingFormat NF = NFs.Descendants<NumberingFormat>()
                        .Where(X => X.NumberFormatId.Value == NIndex.Value)
                        .FirstOrDefault();
                    //Now we get Format code
                    string FormatCode = NF.FormatCode.Value;
                }
            }
            return result;
        }

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ExcelHelper() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
#endregion
    }
}
