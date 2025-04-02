using DataTables;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Commons.Actions.Contexts;
using iText.Html2pdf.Attach;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyDMVpro.Common;
using MyDMVpro.Models;
using MyDMVpro.Models.RegistrationFeeCalculatorViewModels;
using Newtonsoft.Json;
using OfficeOpenXml;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.AssemblyIdentityComparer;

namespace MyDMVpro.Controllers
{
    public class RegistrationCalculatorController : BaseController
    {
        public static IConfiguration Configuration { get; set; }
        public static int ruleIdCounter = 1;

        public RegistrationCalculatorController(MaggardDMVContext context, IConfiguration configuration, ILogger<AttachmentReviewController> logger) : base(context, configuration, logger)
        {
        }
    
        public async Task<IActionResult> Index()
        {
            var allStates = await _context.States.ToListAsync();
            ViewBag.States = allStates;
            return View();
        }

        [HttpPost]
        public IActionResult Import(IFormFile excelFile, string stateCode, string workflowName)
        {
            try
            {
                var calcTables = _context.CalculationTables.Where(x=>x.State == stateCode && x.WorkflowName == workflowName).ToList();

                if (stateCode.IsNullOrEmpty() && workflowName == null) {
                    return Json(new { success = false, message = "Please enter the full information" });

                }
                if (calcTables.Count > 0)
                {
                    return Json(new { success = false, message = "Table name already exists!" });

                }
                if (excelFile == null || excelFile.Length == 0)
                {
                    var CalculationTable = new CalculationTables
                    {
                        State = stateCode,
                        WorkflowName = workflowName,
                        Json = null 
                    };

                    _context.CalculationTables.Add(CalculationTable);
                    _context.SaveChanges();
                
                    return Json(new { success = true, message = "Table created successfully!" });

                }
          
                using var stream = new MemoryStream();
                excelFile.CopyTo(stream);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    ModelState.AddModelError("", "Excel file is empty.");
                    return View();
                }

                var dataTable = new DataTable();
                foreach (var headerCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
                    dataTable.Columns.Add(headerCell.Text.Trim());

                for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var dataRow = dataTable.NewRow();
                    for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        if (col <= dataTable.Columns.Count)
                        {
                            dataRow[col - 1] = worksheet.Cells[row, col].Text.Trim();
                        }
                        else
                        {
                            dataRow[col - 1] = ""; 
                        }
                    }
                    dataTable.Rows.Add(dataRow);
                }

               var jsonData = JsonConvert.SerializeObject(dataTable);

               string ruleObjects = CreateWorkflowJson(jsonData, workflowName);
                
                var CalculationTables = new CalculationTables
                {
                    State = stateCode,
                    WorkflowName = workflowName,
                    Json = ruleObjects
                };

                _context.CalculationTables.Add(CalculationTables);
                _context.SaveChanges();

                return Json(new { success = true, message = "Import successful!" });
            }
            catch(Exception ex) 
            {
                return Json(new { success = false, message = "An error occurred while processing the file." });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> GetStateSpecificData(string stateCode)
        {
            var workflowNameToExclude = MasterFormulaEnum.MasterFormula.MasterFormulaTable.ToString();
            var getCalculationTables = await _context.CalculationTables
                .Where(x => x.State == stateCode && x.WorkflowName != workflowNameToExclude)
                .ToListAsync();

            //var getCalculationTables = await _context.CalculationTables.Where(x => x.State == stateCode && x.WorkflowName != MasterFormulaEnum.MasterFormula.MasterFormulaTable).ToListAsync();
            List<RuleObjectForJson> tableData = new List<RuleObjectForJson>();
            var states = _context.States.Where(x => x.StateAbbreviation == stateCode).FirstOrDefault();
            List<RegistrationCalculationModels> registrationCalculationModels = new List<RegistrationCalculationModels>();

            foreach (var item in getCalculationTables)
            {
                if (item.Json == null || item.Json == "")
                {
                    var model = new RegistrationCalculationModels
                    {
                        Data = new List<RuleObjectForJson>(),
                        State = states.StateName,
                        WorkflowName = item.WorkflowName,
                        StateCode = item.State,
                        WorkFlowId = item.Id,
                    };

                    registrationCalculationModels.Add(model);

                }
                else
                {
                    var model = new RegistrationCalculationModels
                    {
                        Data = UpdatedBindDataInModel(item.Json),
                        State = states.StateName,
                        WorkflowName = item.WorkflowName,
                        StateCode = item.State,
                        WorkFlowId = item.Id,
                    };

                    registrationCalculationModels.Add(model);
                }
            }

            return Ok(registrationCalculationModels);

        }

        [HttpPost]
        public async Task<IActionResult> ImportExcelForSpecificTable(IFormFile excelFile, string stateCode, string workflowName, int tableId)
        {
            try
            {
                var getCalculationTables = await _context.CalculationTables.Where(x => x.State == stateCode && x.Id == tableId).FirstOrDefaultAsync();

                if (excelFile == null || excelFile.Length == 0)
                {
                    var CalculationTable = new CalculationTables
                    {
                        State = stateCode,
                        WorkflowName = workflowName,
                        Json = null 
                    };

                    _context.CalculationTables.Add(CalculationTable);
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Table created successfully!" });

                }
          
                using var stream = new MemoryStream();
                excelFile.CopyTo(stream);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    ModelState.AddModelError("", "Excel file is empty.");
                    return View();
                }

                var dataTable = new DataTable();
                foreach (var headerCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
                    dataTable.Columns.Add(headerCell.Text.Trim());

                for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var dataRow = dataTable.NewRow();
                    for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        if (col <= dataTable.Columns.Count)
                        {
                            dataRow[col - 1] = worksheet.Cells[row, col].Text.Trim();
                        }
                        else
                        {
                            dataRow[col - 1] = ""; 
                        }
                    }
                    dataTable.Rows.Add(dataRow);
                }

                var jsonData = JsonConvert.SerializeObject(dataTable);

                string ruleObjects = CreateWorkflowJson(jsonData, workflowName);

                getCalculationTables.Json = ruleObjects;

                _context.CalculationTables.Update(getCalculationTables);
                _context.SaveChanges();

                return Json(new { success = true, message = "Import successful!" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while processing the file." });

            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearTableData(string stateCode, string workflowName, int tableId)
        {

            try
            {
                var getCalculationTables = await _context.CalculationTables.Where(x => x.State == stateCode && x.Id == tableId).FirstOrDefaultAsync();
                
                if (getCalculationTables == null)
                {
                    return Json(new { success = false, message = "An error occurred while processing the request." });

                }
                getCalculationTables.Json = null;

                _context.CalculationTables.Update(getCalculationTables);
                _context.SaveChanges();

                return Json(new { success = true, message = "Clear data successfully!" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while processing the request." });

            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDataTable(string stateCode, string workflowName, int tableId)
        {

            try
            {
                var getCalculationTables = await _context.CalculationTables.Where(x => x.State == stateCode && x.Id == tableId).FirstOrDefaultAsync();

                if (getCalculationTables == null)
                {
                    return Json(new { success = false, message = "An error occurred while processing the request." });

                }
                getCalculationTables.Json = null;

                _context.CalculationTables.Remove(getCalculationTables);
                _context.SaveChanges();

                return Json(new { success = true, message = "Clear data successfully!" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while processing the request." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveTableData([FromBody] SaveTableDataRequest request)
        {
            try
            {
                if (request == null || request.Rows == null || !request.Rows.Any())
                {
                    return BadRequest(new { success = false, message = "Invalid data provided." });
                }
           
                var getCalculationTables = await _context.CalculationTables.Where(x => x.State == request.TableStateCode && x.Id == request.TableDataId).FirstOrDefaultAsync();

                if (getCalculationTables == null)
                {
                    return Json(new { success = false, message = "An error occurred while processing the request." });

                }
                string ruleObjects = UpdatedConvertToRulesNew(request.Rows, getCalculationTables.WorkflowName);

                getCalculationTables.Json = ruleObjects;
                
                _context.CalculationTables.Update(getCalculationTables);
                _context.SaveChanges();

                return Ok(new { success = true, message = "Data saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while saving data.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRules([FromBody] RemoveRulesRequest request)
        {
            try
            {
           
                var workflows = _context.CalculationTables.Where(x => x.State == request.StateCode && x.Id == request.WorkflowId).FirstOrDefault();

                if (workflows == null)
                {
                    return Json(new { success = false, message = "An error occurred while processing the request." });
                }
                var parsedWorkflows = JsonConvert.DeserializeObject<List<MyDMVpro.Models.Workflow>>(workflows.Json);

                foreach (var workflow in parsedWorkflows)
                {
                    workflow.Rules = workflow.Rules
                        .Where(rule => !request.RuleNamesToRemove.Contains(rule.RuleName))
                        .ToList();
                }

                parsedWorkflows = parsedWorkflows.Where(workflow => workflow.Rules.Any()).ToList();

                var updatedJson = JsonConvert.SerializeObject(parsedWorkflows, Formatting.Indented);
                workflows.Json = updatedJson;

                _context.SaveChanges();

                return Ok(new { success = true, message = "Data removed Successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while saving data."});
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetMasterFormulaByState(string stateCode)
        {
            try
            {
                var getCalculationTables = await _context.CalculationTables.Where(x => x.State == stateCode && x.WorkflowName == "Master Formula Table").ToListAsync();
                List<RuleObjectForJson> tableData = new List<RuleObjectForJson>();
                var states = _context.States.Where(x => x.StateAbbreviation == stateCode).FirstOrDefault();
                List<RegistrationCalculationModels> registrationCalculationModels = new List<RegistrationCalculationModels>();

                foreach (var item in getCalculationTables)
                {
                    if (item.Json == null || item.Json == "")
                    {
                        var model = new RegistrationCalculationModels
                        {
                            Data = new List<RuleObjectForJson>(),
                            State = states.StateName,
                            WorkflowName = item.WorkflowName,
                            StateCode = item.State,
                            WorkFlowId = item.Id,
                        };

                        registrationCalculationModels.Add(model);

                    }
                    else
                    {
                        var model = new RegistrationCalculationModels
                        {
                            Data = UpdatedBindDataInModel(item.Json),
                            State = states.StateName,
                            WorkflowName = item.WorkflowName,
                            StateCode = item.State,
                            WorkFlowId = item.Id,
                        };

                        registrationCalculationModels.Add(model);
                    }
                }

                return Ok(registrationCalculationModels);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while getting data." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetTablesNameByState(string stateCode)
        {
            if(stateCode.IsNullOrEmpty())
            {
                return Json(new { success = false, message = "An error occurred while processing the request." });
            }
            var tablesName = await _context.CalculationTables.Where(x=>x.State == stateCode && x.WorkflowName != "Master Formula Table").Select(x=>x.WorkflowName).ToListAsync();
            return Json(new { success = true, data = tablesName });
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableCors("AllowOCR")]
        [IgnoreAntiforgeryToken]

        public async Task<IActionResult> CalculateRegistrationFee([FromBody] VehicleInfo calculatorRequest)
        {
            try
            {
           
                var tablesName = await _context.CalculationTables.Where(x => x.State == calculatorRequest.StateCode).ToListAsync();

                var masterFormulaTable = tablesName.Where(x => x.WorkflowName == MasterFormulaEnum.MasterFormula.MasterFormulaTable && x.State == calculatorRequest.StateCode).FirstOrDefault();

                var deserializeData = UpdatedBindDataInModel(masterFormulaTable.Json);

                var result1 = deserializeData
                    .FirstOrDefault(x => x.DataPoint1?.ToUpper() == MasterFormulaEnum.MasterFormula.PlateClass && x?.Condition1.ToLower() == calculatorRequest.PlateClass.ToLower())?
                    .CalculatedValue;

                if(result1 == null && result1.IsNullOrEmpty())
                {
                    return Json(new { success = false, message = "Please enter the value for plate class." });
                }
                string[] tableNames = result1.Split(new string[] { " + ", " - ", " * ", " / " }, StringSplitOptions.RemoveEmptyEntries);

                tableNames = tableNames.Select(t => t.Trim()).ToArray();

                var successResults = new List<object>();
            
                foreach (var item in tableNames)
                {
                    var itemTable = tablesName.Where(x => x.WorkflowName.ToLower() == item.ToLower()).FirstOrDefault();
                    if(itemTable == null)
                    {
                        return Json(new { success = false, message =  "Item table not found. please check your master formula" });
                    }
                    var deserializeItemTableData = UpdatedBindDataInModel(itemTable?.Json);

               
                        var workflows = JsonConvert.DeserializeObject<RulesEngine.Models.Workflow[]>(itemTable.Json);

                        var rulesEngine = new RulesEngine.RulesEngine(workflows, null);

                        foreach (var workflow in workflows)
                        {
                            var existingWorkflow = rulesEngine.GetAllRegisteredWorkflowNames();

                            if (existingWorkflow == null)
                            {
                                rulesEngine.AddWorkflow(workflow);
                            }
                            else
                            {
                                rulesEngine.AddOrUpdateWorkflow(workflow);
                            }
                        }

                        var requiredDataPoints = ExtractDataPoints(deserializeItemTableData);

                        var filteredInput = FilterVehicleInfo(calculatorRequest, requiredDataPoints);

                        var results = await rulesEngine.ExecuteAllRulesAsync(item.ToString(), filteredInput);

                        foreach (var result in results)
                        {
                            if (result.IsSuccess)
                            {
                                var ruleResult = new
                                {
                                    RuleName = result.Rule.RuleName,
                                    ShippingCost = result.Rule.SuccessEvent,
                                    TableName = item,
                                    Formula = result1.ToString(),
                                };

                                successResults.Add(ruleResult);
                            }
                        }
                }
            
                return Json(new { success = true, data = successResults });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message.IsNullOrEmpty()? "Error occurred during process please try again": ex.Message.ToString() });
            }

        }


        [HttpPost]
        public async Task<IActionResult> SaveRegistration([FromBody] SaveRegistrationModel registrationRequest)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                // Check if the VendorId is not null
                if (ui?.VendorId != null)
                {
                    var request = await _context.Requests
                     .Where(r => r.RequestId == new Guid(registrationRequest.RequestId) && r.VendorId == ui.VendorId)
                     .FirstOrDefaultAsync();

                    // If the request is found
                    if (request != null)
                    {
                        var fee = JsonConvert.DeserializeObject<TaxAndRegFeeModel>(request.jFeesData);
                        fee.CalculatedFeeResult = registrationRequest.CalculatedFeeResult;
                        fee.RegItems = registrationRequest.RegItems;
                        request.jFeesData = JsonConvert.SerializeObject(fee);
                        await _context.SaveChangesAsync();
                    }
                    return Ok();
                }
                else
                {
                    return Unauthorized("VendorId is missing or invalid.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        public static List<string> ExtractTableNames(string input)
        {
            try
            {
                string pattern = @"\s*[\+\*\%_]\s*"; // ✅ Corrected pattern

                var x =  Regex.Split(input, pattern, RegexOptions.None)
                            .Where(t => !string.IsNullOrWhiteSpace(t)) // Remove empty entries
                            .Select(t => t.Trim()) // Trim spaces
                            .ToList();
                return x;
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        private List<string> ExtractDataPoints(List<RuleObjectForJson> tableData)
        {
            return tableData
                .SelectMany(d => new[] { d.DataPoint1, d.DataPoint2, d.DataPoint3, d.DataPoint4,d.DataPoint5 }) // Adjust based on your JSON structure
                .Where(dp => !string.IsNullOrEmpty(dp))
                .Distinct()
                .ToList();
        }

        private object FilterVehicleInfo(VehicleInfo vehicle, List<string> requiredDataPoints)
        {
            var filteredProperties = vehicle.GetType()
                .GetProperties()
                .Where(p => requiredDataPoints.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(p => p.Name, p => p.GetValue(vehicle));

            var anonymousObject = ToAnonymousObject(filteredProperties);
            return anonymousObject;
        }
        private object ToAnonymousObject(Dictionary<string, object> dict)
        {
            var dynamicObject = new ExpandoObject() as IDictionary<string, object>;
            foreach (var kvp in dict)
            {
                dynamicObject[kvp.Key] = kvp.Value;
            }
            return dynamicObject;
        }
        public static string GetConditionValue(string dataPoint, string condition)
        {
            var dataPointTypes = new Dictionary<string, Type>
            {
                { "PLATE CLASS", typeof(string) },
                { "JURISDICTION", typeof(string) },
                { "TITLE", typeof(string) },
                { "LIEN", typeof(string) },
                { "PROPULSION", typeof(string) },
                { "CYLINDERS", typeof(string) },
                { "MPG", typeof(string) },
                { "MGW", typeof(string) },
                { "UNLADEN WEIGHT", typeof(string) },
                { "WEIGHT", typeof(int) },
                { "PLATECLASS", typeof(string) },
                { "Jurisdiction", typeof(string) },
                { "Title", typeof(string) },
                { "Lien", typeof(string) },
                { "Propulsion", typeof(string) },
                { "Cylinders", typeof(string) },
                { "mpg", typeof(string) },
                { "mgw", typeof(string) },
                { "UNLADENWEIGHT", typeof(string) },
                { "Weight", typeof(int) },
                { "Year", typeof(string) },
                { "YEAR", typeof(string) },
            };

            if (dataPointTypes.TryGetValue(dataPoint, out var type))
            {
                if (type == typeof(string))
                {
                    var x = Regex.Unescape(condition);
                    return $"\"{condition.ToLower()}\"";
                    //return $"\"{condition}\""; 
                }
                return condition;
            }
            throw new Exception($"Unknown DataPoint: {dataPoint}");
        }
        public static string GetOperatorSymbol(string inputOperator)
        {
            string operatorSymbol = "=";

            if (inputOperator == "EQUAL TO")
            {
                operatorSymbol = "==";
                return operatorSymbol;
            }
            else if (inputOperator == "NOT EQUAL TO")
            {
                operatorSymbol = "!=";
                return operatorSymbol;
            }
            else if (inputOperator == "LESS THAN OR EQUAL TO")
            {
                operatorSymbol = "<=";

                return operatorSymbol;
            }
            else if (inputOperator == "LESS THAN")
            {
                operatorSymbol = "<";

                return operatorSymbol;
            }
            else if (inputOperator == "GREATER THAN")
            {
                operatorSymbol = ">";

                return operatorSymbol;
            }
            else if (inputOperator == "GREATER THAN OR EQUAL TO")
            {
                operatorSymbol = ">=";

                return operatorSymbol;
            }
            else
            {
                return operatorSymbol;
            }


        }
        public static string UpdatedConvertToRulesNew(List<RuleObjectForJson> inputObjects, string workflowName)
        {
            var workflow = new MyDMVpro.Models.Workflow
            {
                WorkflowName = workflowName,
                Rules = new List<MyDMVpro.Models.Rule>()
            };

            foreach (var inputObject in inputObjects)
            {
                var ruleName = $"{workflowName.Replace(" ", "")}{"_"}{ruleIdCounter++}";  // Use the static counter and increment it
                var expressionParts = new List<string>();

                if (!string.IsNullOrEmpty(inputObject?.DataPoint1?.Replace(" ", "")))
                {
                    var value = GetConditionValue(inputObject?.DataPoint1?.Replace(" ", ""), inputObject.Condition1);
                    expressionParts.Add($"input1.{inputObject?.DataPoint1?.Replace(" ", "")} {GetOperatorSymbol(inputObject.Relation1)} {value}");
                }
                if (!string.IsNullOrEmpty(inputObject?.DataPoint2?.Replace(" ", "")))
                {
                    var value = GetConditionValue(inputObject?.DataPoint2?.Replace(" ", ""), inputObject.Condition2);
                    expressionParts.Add($"input1.{inputObject?.DataPoint2?.Replace(" ", "")} {GetOperatorSymbol(inputObject.Relation2)} {value}");
                }
                if (!string.IsNullOrEmpty(inputObject?.DataPoint3?.Replace(" ", "")))
                {
                    var value = GetConditionValue(inputObject?.DataPoint3?.Replace(" ", ""), inputObject.Condition3);
                    expressionParts.Add($"input1.{inputObject?.DataPoint3?.Replace(" ", "")} {GetOperatorSymbol(inputObject.Relation3)} {value}");
                }
                if (!string.IsNullOrEmpty(inputObject?.DataPoint4?.Replace(" ", "")))
                {
                    var value = GetConditionValue(inputObject?.DataPoint4?.Replace(" ", ""), inputObject.Condition4);
                    expressionParts.Add($"input1.{inputObject?.DataPoint4?.Replace(" ", "")} {GetOperatorSymbol(inputObject.Relation4)} {value}");
                }
                if (!string.IsNullOrEmpty(inputObject?.DataPoint5?.Replace(" ", "")))
                {
                    var value = GetConditionValue(inputObject?.DataPoint5?.Replace(" ", ""), inputObject.Condition5);
                    expressionParts.Add($"input1.{inputObject?.DataPoint5?.Replace(" ", "")} {GetOperatorSymbol(inputObject.Relation5)} {value}");
                }

                var expression = string.Join(" && ", expressionParts);

                var rule = new MyDMVpro.Models.Rule
                {
                    RuleName = ruleName,
                    Expression = expression,
                    SuccessEvent = inputObject?.CalculatedValue.ToString(),
                    ErrorMessage = $"No match for rule {inputObject?.Id}"
                };

                workflow.Rules.Add(rule);
            }

            var json = JsonConvert.SerializeObject(new List<MyDMVpro.Models.Workflow> { workflow }, Formatting.Indented);

            return json;
        }
        public static List<RuleObjectForJson> UpdatedBindDataInModel(string json)
        {
            var workflows = JsonConvert.DeserializeObject<List<MyDMVpro.Models.Workflow>>(json);

            var ruleObjects = new List<RuleObjectForJson>();

            foreach (var workflow in workflows)
            {
                foreach (var rule in workflow.Rules)
                {
                    var parsedConditions = ParseExpression(rule.Expression);
                    var ruleObject = new RuleObjectForJson();

                    ruleObject = new RuleObjectForJson
                    {
                        Id = int.Parse(rule.RuleName.Split('_')[1]), // Extract Id from RuleName like "TestPlateFeeTable_1"
                        CalculatedValue = rule.SuccessEvent, // Keep as string or convert to numeric if needed
                        RuleName = rule.RuleName,
                        calculatedValueDescription = rule.Description
                    };

                    // Loop through parsed conditions and assign to ruleObject properties
                    for (int i = 0; i < parsedConditions.Count; i++)
                    {
                        var condition = parsedConditions[i];
                        switch (i)
                        {
                            case 0:
                                ruleObject.DataPoint1 = condition.DataPoint;
                                ruleObject.Relation1 = condition.Relation;
                                ruleObject.Condition1 = condition.Condition;
                                break;
                            case 1:
                                ruleObject.DataPoint2 = condition.DataPoint;
                                ruleObject.Relation2 = condition.Relation;
                                ruleObject.Condition2 = condition.Condition;
                                break;
                            case 2:
                                ruleObject.DataPoint3 = condition.DataPoint;
                                ruleObject.Relation3 = condition.Relation;
                                ruleObject.Condition3 = condition.Condition;
                                break;
                            case 3:
                                ruleObject.DataPoint4 = condition.DataPoint;
                                ruleObject.Relation4 = condition.Relation;
                                ruleObject.Condition4 = condition.Condition;
                                break;
                            case 4:
                                ruleObject.DataPoint5 = condition.DataPoint;
                                ruleObject.Relation5 = condition.Relation;
                                ruleObject.Condition5 = condition.Condition;
                                break;
                            default:
                                Console.WriteLine("Warning: More conditions than supported.");
                                break;
                        }
                    }

                    ruleObjects.Add(ruleObject);
                }
            }

            // Optional: Console output for debugging
            foreach (var ruleObject in ruleObjects)
            {
                Console.WriteLine($"Id: {ruleObject.Id}, RuleName: {ruleObject.RuleName}, " +
                                   $"DataPoint1: {ruleObject.DataPoint1}, Relation1: {ruleObject.Relation1}, " +
                                   $"Condition1: {ruleObject.Condition1}, CalculatedValue: {ruleObject.CalculatedValue}");
            }

            return ruleObjects;
        }
        public static List<(string DataPoint, string Relation, string Condition)> ParseExpression(string expression)
        {

            var regex = new Regex(@"input1\.\s*([\w\s]+)\s*([=<>!]+)\s*\\?\""(.*?)\\?\""", RegexOptions.IgnoreCase);

            //var regex = new Regex(@"input1\.([\w\s]+)\s*([=<>!]+)\s*(?:(\"".*?\"")|(\d+(\.\d+)?))", RegexOptions.IgnoreCase);

            var matches = regex.Matches(expression);

            var parsedConditions = new List<(string DataPoint, string Relation, string Condition)>();

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var dataPoint = match.Groups[1].Value;

                    var relation = match.Groups[2].Value;

                    var condition = match.Groups[3].Value;

                    if (string.IsNullOrEmpty(condition) && match.Groups[4].Success)
                    {
                        condition = match.Groups[4].Value;
                    }

                    dataPoint = dataPoint.Replace(" ", "");

                    if (relation == "==")
                        relation = "EQUAL TO";
                    else if (relation == "!=")
                        relation = "NOT EQUAL TO";
                    else if (relation == ">")
                        relation = "GREATER THAN";
                    else if (relation == ">=")
                        relation = "GREATER THAN OR EQUAL TO ";
                    else if (relation == "<=")
                        relation = "LESS THAN OR EQUAL TO";
                    else if (relation == "<=")
                        relation = "LESS THAN";
                    else
                    {
                        relation = "BETWEEN";
                    }

                    parsedConditions.Add((dataPoint, relation, condition));
                }
            }

            return parsedConditions;
        }
        public string CreateWorkflowJson(string json, string state)
        {
            List<RuleObjectForJson> ruleObjects = JsonConvert.DeserializeObject<List<RuleObjectForJson>>(json);

            var convertedJson = UpdatedConvertToRulesNew(ruleObjects, state);
            return convertedJson;

        }

    }

}