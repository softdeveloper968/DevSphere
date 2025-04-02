using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using MyDMVpro.Models.Tax;
using System.Collections.Generic;
using MyDMVpro.Common;
using System.Text.RegularExpressions;
using System.Data;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Cors;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Reflection.Emit;

namespace MyDMVpro.Controllers
{
    [Authorize]
    public class TaxController : MyDMVpro.Controllers.BaseController
    {
        private readonly HttpClient _httpClient;
        public TaxController(MaggardDMVContext context, IConfiguration configuration, ILogger<TaxController> logger) : base(context, configuration, logger)
        {
            _httpClient = new HttpClient();
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch the list of states
                var states = await _context.States.ToListAsync();

                // Fetch the list of jurisdictions
                var jurisdictions = await _context.Jurisdictions.ToListAsync();

                // Create a model to pass both states and jurisdictions to the view
                var viewModel = new StateJurisdictionViewModel
                {
                    States = states,
                    Jurisdictions = jurisdictions
                };

                return View(viewModel);
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine($"An error occurred: {e}");
                return View(null);
            }
        }

        [HttpPost]
        public IActionResult FetchRules(string stateId, int? jurisdictionId)
        {
            try
            {
                // Fetch all items from the Items table
                var items = _context.TaxableItems.ToList();
                if (jurisdictionId != null && jurisdictionId == 0)
                {
                    jurisdictionId = null;
                }
                // Fetch tax rules for the selected state
                var taxRules = _context.TaxRules.Where(rule => rule.StateAbbreviation == stateId && rule.JurisdictionID == jurisdictionId).ToList();
                // Combine item data with tax rule data (if available)
                var itemTaxRuleData = items.Select(item => new
                {
                    ItemID = item.ItemID,
                    ItemName = item.ItemName,
                    // Check if there is a tax rule for this item
                    TaxRule = taxRules.FirstOrDefault(rule => rule.ItemID == item.ItemID),
                }).ToList();

                // Return the partial view with the combined data
                return PartialView("_TaxRulesPartial", itemTaxRuleData);
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine($"An error occurred: {e}");
                return PartialView("_TaxRulesPartial", null);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveTaxRules([FromBody] List<UpdatedRuleDto> allRules)
        {
            try
            {
                UserInfo user = await GetCurrentUserAsync();
                if (allRules is not null && allRules.Any())
                {
                    var stateConfig = await _context.StateConfigurations
                        .FirstOrDefaultAsync(x => x.StateAbbreviation == allRules[0].StateID);

                    if (stateConfig is not null && !stateConfig.EnableTaxRateFetchFromApi)
                    {
                        if (allRules.Any(rule => rule.RequiresTaxRateFetch))
                        {
                            throw new InvalidOperationException("Tax rate fetching from API is disabled for this state, but some rules require it. So please enable the tax rate fetching from API ");
                        }
                    }
                }
                foreach (var rule in allRules)
                {
                    if (rule.JurisdictionID != null && rule.JurisdictionID == 0)
                    {
                        rule.JurisdictionID = null;
                    }
                    var existingRule = _context.TaxRules
                        .FirstOrDefault(r => r.ItemID == rule.ItemID && r.StateAbbreviation == rule.StateID && r.JurisdictionID == rule.JurisdictionID);

                    if (existingRule != null)
                    {
                        // Update the existing rule
                        existingRule.IsApplicable = rule.IsApplicable;
                        existingRule.Taxable = rule.Taxable;
                        existingRule.TaxRate = rule.TaxRate;
                        existingRule.MaxTaxableAmount = rule.MaxValue;
                        existingRule.ModifiedBy = user.UserId;
                        existingRule.ModifiedDate = DateTime.UtcNow;
                        existingRule.JurisdictionID = rule.JurisdictionID;
                        existingRule.RequiresTaxRateFetch = rule.RequiresTaxRateFetch;
                    }
                    else
                    {
                        // Create a new tax rule
                        var newRule = new TaxRule
                        {
                            ItemID = rule.ItemID,
                            StateAbbreviation = rule.StateID,
                            IsApplicable = rule.IsApplicable,
                            Taxable = rule.Taxable,
                            TaxRate = rule.TaxRate,
                            MaxTaxableAmount = rule.MaxValue,
                            CreatedBy = user.UserId,
                            CreatedDate = DateTime.UtcNow,
                            JurisdictionID = rule.JurisdictionID,
                            RequiresTaxRateFetch= rule.RequiresTaxRateFetch
                        };
                        _context.TaxRules.Add(newRule);
                    }
                }
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception e)
            {
                // Log the exception using Trace.WriteLine
                System.Diagnostics.Trace.WriteLine($"An error occurred: {e}");

                // Return a JSON error response with the exception message
                return JsonError(e);
            }
        }

        [HttpPost]
        public IActionResult FetchFormula(string stateId, int? jurisdictionId, string selectedTaxFormula)
        {
            try
            {
                if (jurisdictionId != null && jurisdictionId == 0)
                {
                    jurisdictionId = null;
                }
                var formula = _context.TaxFormulas.FirstOrDefault(rule => rule.StateAbbreviation == stateId && rule.JurisdictionID == jurisdictionId);
                // Return the partial view with the combined data
                var resultFormula = GetSelectedFormula(formula, selectedTaxFormula);

                // Return the formula or an empty string if null
                return Ok(resultFormula ?? string.Empty);
            }
            catch (Exception e)
            {
                // Log the exception using Trace.WriteLine
                System.Diagnostics.Trace.WriteLine($"An error occurred: {e}");

                // Return a JSON error response with the exception message
                return JsonError(e.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveFormula([FromBody] UpdatedFormulaDto formula)
        {
            try
            {
                if (formula == null)
                    return Json(new { success = false, message = "Formula is empty!" });

                if (formula.Formula == null && formula.Id == null)
                    return Json(new { success = false, message = "Formula or Id is empty!" });

                UserInfo user = await GetCurrentUserAsync();

                if (formula.JurisdictionID != null && formula.JurisdictionID == 0)
                {
                    formula.JurisdictionID = null;
                }
                var existingFormula = _context.TaxFormulas
                        .FirstOrDefault(r => r.StateAbbreviation == formula.StateID && r.JurisdictionID == formula.JurisdictionID);

                if (existingFormula != null)
                {
                    UpdateFormulaProperty(existingFormula, formula.Id, formula.Formula);
                    existingFormula.StateAbbreviation = formula.StateID;
                    existingFormula.JurisdictionID = formula.JurisdictionID;
                    existingFormula.ModifiedBy = user.UserId;
                    existingFormula.ModifiedDate = DateTime.UtcNow;
                }
                else
                {
                    var newFormula = new TaxFormula
                    {
                        StateAbbreviation = formula.StateID,
                        JurisdictionID = formula.JurisdictionID,
                        CreatedBy = user.UserId,
                        CreatedDate = DateTime.UtcNow,
                    };
                    UpdateFormulaProperty(newFormula, formula.Id, formula.Formula);
                    _context.TaxFormulas.Add(newFormula);
                }
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception e)
            {
                // Log the exception using Trace.WriteLine
                System.Diagnostics.Trace.WriteLine($"An error occurred: {e}");

                // Return a JSON error response with the exception message
                return JsonError(e.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        [AllowAnonymous]
        [HttpPost]
        [EnableCors("AllowOCR")]
        public async Task<IActionResult> Calculate([FromBody] CalculateRequest calculateRequest)
        {
            try
            {
                if (string.IsNullOrEmpty(calculateRequest.StateId))
                {
                    return BadRequest("Pleaes provide the state code.");
                }
                var stateConfig = await _context.StateConfigurations.FirstOrDefaultAsync(x => x.StateAbbreviation == calculateRequest.StateId);
                var totalTaxFromApi = 0m;
                var taxDetailsFromApi = new TotalCollectionTaxDetailsDto();
                if (stateConfig is not null)
                {
                    if (stateConfig.EnableTaxRateFetchFromApi)
                    {
                        string streetAddress = calculateRequest.StreetAddress;
                        string zipCode = calculateRequest.ZipCode;
                        if (string.IsNullOrEmpty(streetAddress) || string.IsNullOrEmpty(zipCode))
                        {
                            if (string.IsNullOrEmpty(calculateRequest.RequestId))
                            {
                                return BadRequest("Please provide a requestId or both street address and zip code.");
                            }

                            var requestRecord = await _context.Requests
                                .FirstOrDefaultAsync(x => x.RequestId == Guid.Parse(calculateRequest.RequestId));

                            if (requestRecord == null)
                            {
                                return BadRequest("Invalid requestId provided.");
                            }

                            var existingJsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestRecord.JRequest)
                                                   ?? new Dictionary<string, object>();

                            streetAddress ??= existingJsonData.TryGetValue("REG-Reg-Address-Street", out object street) ? street?.ToString() : null;
                            zipCode ??= existingJsonData.TryGetValue("REG-Reg-Address-ZIP", out object zip) ? zip?.ToString() : null;
                        }

                        if (string.IsNullOrEmpty(streetAddress) || string.IsNullOrEmpty(zipCode))
                        {
                            return BadRequest("Could not determine street address and zip code.");
                        }

                        // Fetch tax details
                        var taxResult = await FetchTaxRateFromUSGeoCoder(streetAddress, zipCode) as ObjectResult;

                        if (taxResult != null && taxResult.StatusCode == 200 && taxResult.Value is TotalCollectionTaxDetails taxDetails)
                        {
                            if (stateConfig.IncludeStateTax)
                            {
                                totalTaxFromApi += decimal.TryParse(taxDetails.StateTax?.Replace("%", "").Trim(), out var stateTax) ? stateTax : 0m;
                                taxDetailsFromApi.StateTax = taxDetails.StateTax;
                                taxDetailsFromApi.StateJurisdictionName = taxDetails.StateJurisdictionName;
                            }

                            if (stateConfig.IncludeCountyTax)
                            {
                                totalTaxFromApi += decimal.TryParse(taxDetails.CountyTax?.Replace("%", "").Trim(), out var countyTax) ? countyTax : 0m;
                                taxDetailsFromApi.CountyJurisdictionName = taxDetails.CountyJurisdictionName;
                                taxDetailsFromApi.CountyTax = taxDetails.CountyTax;
                                taxDetailsFromApi.CountyDistricts = taxDetails.CountyDistricts;
                            }

                            if (stateConfig.IncludeCityTax)
                            {
                                totalTaxFromApi += decimal.TryParse(taxDetails.CityTax?.Replace("%", "").Trim(), out var cityTax) ? cityTax : 0m;
                                taxDetailsFromApi.CityJurisdictionName = taxDetails.CityJurisdictionName;
                                taxDetailsFromApi.CityTax = taxDetails.CityTax;
                                taxDetailsFromApi.CityDistricts = taxDetails.CityDistricts;
                            }

                            if (stateConfig.IncludeSpecialDistrictTax)
                            {
                                totalTaxFromApi += taxDetails.SpecialDistricts?.Sum(sd =>
                                    decimal.TryParse(sd.Tax?.Replace("%", "").Trim(), out var districtTax) ? districtTax : 0m) ?? 0m;
                                taxDetailsFromApi.SpecialDistricts = taxDetails.SpecialDistricts;
                            }
                        }
                    }

                }
                var formulaEntity = await _context.TaxFormulas
                    .FirstOrDefaultAsync(x => x.StateAbbreviation == calculateRequest.StateId && x.JurisdictionID == calculateRequest.JurisdictionId);

                var applicableRules = await _context.TaxRules
                    .Where(x => x.StateAbbreviation == calculateRequest.StateId && x.JurisdictionID == calculateRequest.JurisdictionId && x.IsApplicable)
                    .Include(x => x.TaxableItem)
                    .ToListAsync();

                if (formulaEntity == null || !applicableRules.Any())
                {
                    // If formula or applicable rules not found, try fetching with JurisdictionID as null
                    formulaEntity = await _context.TaxFormulas
                        .FirstOrDefaultAsync(x => x.StateAbbreviation == calculateRequest.StateId && x.JurisdictionID == null);

                    applicableRules = await _context.TaxRules
                        .Where(x => x.StateAbbreviation == calculateRequest.StateId && x.JurisdictionID == null && x.IsApplicable)
                        .Include(x => x.TaxableItem)
                        .ToListAsync();

                    if (formulaEntity == null || !applicableRules.Any())
                        return NotFound("Formula or applicable rules not found.");
                }
                // Create a dictionary to hold the results for each formula

                var results = new CalculatedTaxResult();
                var taxItemsList = new List<CalculatedTaxItem>();

                // Step 1: Calculate each taxable item and tax due for each rule
                //foreach (var item in applicableRules)
                //{
                //    var itemValue = calculateRequest.Items.TryGetValue(item.TaxableItem.ItemName, out var value) ? value : 0m;

                //    // Calculate the taxable amount
                //    var taxableValue = item.Taxable.HasValue ? itemValue * item.Taxable.Value : itemValue;

                //    var taxDueValue = item.TaxRate.HasValue ? taxableValue * item.TaxRate.Value : taxableValue;
                //    if (item.MaxTaxableAmount.HasValue && taxDueValue > item.MaxTaxableAmount)
                //        taxDueValue = (decimal)item.MaxTaxableAmount;

                //    // Add the calculated item to the list
                //    taxItemsList.Add(new CalculatedTaxItem
                //    {
                //        ItemName = item.TaxableItem.ItemName,
                //        ItemId = item.TaxableItem.ItemID,
                //        Value = itemValue,
                //        TaxableAmount = taxableValue,
                //        TaxDue = taxDueValue
                //    });
                //}
                var systemTaxItems = await _context.TaxableItems.ToListAsync();
                foreach (var requestItem in calculateRequest.Items)
                {
                    var item = applicableRules.FirstOrDefault(x => x.TaxableItem.ItemName == requestItem.Key);

                    if (item != null)
                    {
                        var itemValue = requestItem.Value;

                        // Calculate the taxable amount
                        var taxableValue = item.Taxable.HasValue ? itemValue * item.Taxable.Value : 0m;

                        var taxDueValue = item.RequiresTaxRateFetch ? taxableValue * totalTaxFromApi: taxableValue * (item.TaxRate ?? 0m);

                        if (item.MaxTaxableAmount.HasValue && taxDueValue > item.MaxTaxableAmount)
                            taxDueValue = (decimal)item.MaxTaxableAmount;

                        // Add the calculated item to the list
                        taxItemsList.Add(new CalculatedTaxItem
                        {
                            ItemName = item.TaxableItem.ItemName,
                            ItemId = item.TaxableItem.ItemID,
                            Value = itemValue,
                            TaxableAmount = taxableValue,
                            TaxDue = taxDueValue
                        });
                    }
                    else
                    {
                        taxItemsList.Add(new CalculatedTaxItem
                        {
                            ItemId = systemTaxItems.FirstOrDefault(x => x.ItemName == requestItem.Key)?.ItemID ?? 0,
                            ItemName = requestItem.Key,
                            Value = 0,
                            TaxableAmount = 0,
                            TaxDue = 0
                        });
                    }
                }


                // Add the tax items to the results
                results.CalculatedTaxItems = taxItemsList;

                // Step 2: Use the calculated items to evaluate the totals using the formula
                foreach (var formulaName in new[] { "totalTaxableAmount", "totalTaxDue", "totalTaxPaidOtherState", "totalLeftOverTitlingState" })
                {
                    var formula = GetSelectedFormula(formulaEntity, formulaName);

                    if (!string.IsNullOrEmpty(formula))
                    {
                        // Replace item names in the formula with their calculated values
                        foreach (var item in taxItemsList)
                        {
                            if (formulaName == "totalTaxableAmount")
                            {
                                formula = formula.Replace(item.ItemName, item.TaxableAmount.ToString());
                            }else
                            {
                                formula = formula.Replace(item.ItemName, item.TaxDue.ToString());
                            }
                        }

                        var regex = new Regex(@"\b[A-Za-z\s]+\b");
                        formula = regex.Replace(formula, match =>
                        {
                            var placeholder = match.Value;

                            // If the placeholder exists in taxItemsList, keep it as-is (already replaced in the first loop)
                            if (taxItemsList.Any(r => r.ItemName == placeholder))
                            {
                                return placeholder;
                            }

                            // Replace all other placeholders with "0"
                            return "0";
                        });

                        // Check if the formula is valid
                        if (!IsValidFormula(formula))
                        {
                            // Assign the error message to the appropriate result property
                            switch (formulaName)
                            {
                                case "totalTaxableAmount":
                                    results.TotalTaxableAmount = "Invalid formula provided!";
                                    break;
                                case "totalTaxDue":
                                    results.TotalTaxDue = "Invalid formula provided!";
                                    break;
                                case "totalTaxPaidOtherState":
                                    results.TotalTaxPaidOtherState = "Invalid formula provided!";
                                    break;
                                case "totalLeftOverTitlingState":
                                    results.TotalLeftOverTitlingState = "Invalid formula provided!";
                                    break;
                            }
                        }
                        else
                        {
                            // Evaluate the formula
                            var result = EvaluateFormula(formula);

                            // Assign the evaluated result to the appropriate property
                            switch (formulaName)
                            {
                                case "totalTaxableAmount":
                                    results.TotalTaxableAmount = result.ToString();
                                    break;
                                case "totalTaxDue":
                                    results.TotalTaxDue = result.ToString();
                                    break;
                                case "totalTaxPaidOtherState":
                                    results.TotalTaxPaidOtherState = result.ToString();
                                    break;
                                case "totalLeftOverTitlingState":
                                    results.TotalLeftOverTitlingState = result.ToString();
                                    break;
                            }
                        }
                    }
                    else
                    {
                        switch (formulaName)
                        {
                            case "totalTaxableAmount":
                                results.TotalTaxableAmount = "";
                                break;
                            case "totalTaxDue":
                                results.TotalTaxDue = "";
                                break;
                            case "totalTaxPaidOtherState":
                                results.TotalTaxPaidOtherState = "";
                                break;
                            case "totalLeftOverTitlingState":
                                results.TotalLeftOverTitlingState = "";
                                break;
                        }
                    }
                }
                results.TotalCollectionTaxDetails = taxDetailsFromApi;
                // Return the results as a JSON object
                return Ok(results);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        //[HttpPost("Tax/Taxes")]
        //public async Task<IActionResult> GetTaxCalculationData([FromBody] List<string> requestIds)
        //{
        //    try
        //    {
        //        if (requestIds == null || !requestIds.Any())
        //        {
        //            return BadRequest("Request IDs cannot be null or empty.");
        //        }
        //        UserInfo user = await GetCurrentUserAsync();

        //        // fetch the items values from the database then calculate the tax and save into database.

        //        foreach (var request in requestIds)
        //        {
        //            var allPricingDetails = new Dictionary<string, decimal>
        //{
        //    { "MSRP", 25000m },
        //    { "Base Price", 22000m },
        //    { "Dealer Rebate", 2000m },
        //    { "Manufacturer Rebate", 1500m },
        //    { "Discount", 1000m },
        //    { "Doc Fees", 200m },
        //    { "Warranties", 500m },
        //    { "Extended Warranties", 300m },
        //    { "Add On's", 700m },
        //    { "Protection Packages", 350m },
        //    { "Theft Protection", 100m },
        //    { "Insurance Packages", 250m },
        //    { "Dealer Inventory Taxes", 150m },
        //    { "Delivery Fees", 100m },
        //    { "Dealer Service Fee", 150m },
        //    { "Trade in Value", 5000m },
        //    { "Taxes Paid to Dealer State", 1200m }
        //};

        //            var state = 1;
        //            int? jur = null;

        //            var applicableRules = await _context.TaxRules
        //               .Where(x => x.StateID == state && x.JurisdictionID == jur).Include(x => x.TaxableItem)
        //               .ToListAsync();

        //            foreach (var item in applicableRules)
        //            {
        //                var itemValue = allPricingDetails.TryGetValue(item.TaxableItem.ItemName, out var value) ? value : 0m;
        //                decimal? finalTax = null;

        //                if (item.IsApplicable)
        //                {
        //                    var taxableValue = item.Taxable.HasValue ? itemValue * item.Taxable.Value : itemValue;
        //                    finalTax = item.TaxRate.HasValue ? taxableValue * item.TaxRate : taxableValue;
        //                }
        //                var existingRecord = _context.TaxCalculationRecords
        //               .FirstOrDefault(r => r.StateID == state && r.JurisdictionID == jur && r.RequestId == new Guid(request) && r.ItemID == item.ItemID);

        //                if (existingRecord != null)
        //                {
        //                    existingRecord.StateID = state;
        //                    existingRecord.JurisdictionID = jur;
        //                    existingRecord.ModifiedBy = user.UserId;
        //                    existingRecord.ModifiedDate = DateTime.UtcNow;
        //                    existingRecord.TaxDue = finalTax;
        //                    existingRecord.ItemID = item.ItemID;
        //                    existingRecord.RequestId = new Guid(request);
        //                    _context.TaxCalculationRecords.Update(existingRecord);
        //                }
        //                else
        //                {
        //                    var newRecord = new TaxCalculationRecord
        //                    {
        //                        StateID = state,
        //                        JurisdictionID = jur,
        //                        CreatedBy = user.UserId,
        //                        CreatedDate = DateTime.UtcNow,
        //                        TaxDue = finalTax,
        //                        ItemID = item.ItemID,
        //                        RequestId = new Guid(request),
        //                    };
        //                    _context.TaxCalculationRecords.Add(newRecord);
        //                }
        //            }
        //        }
        //        _context.SaveChanges();
        //        //    // Fetch tax calculation records for all request IDs
        //        //    var requestIdsGuid = requestIds.Select(rid => Guid.Parse(rid)).ToList();

        //        //    var taxRecords = await _context.TaxCalculationRecords
        //        //        .Where(t => requestIdsGuid.Contains(t.RequestId))
        //        //        .Include(x => x.TaxableItem)
        //        //        .Include(x => x.State)
        //        //        .Include(x => x.Jurisdiction)
        //        //        .ToListAsync();

        //        //    var result = taxRecords
        //        //.GroupBy(t => t.RequestId)
        //        //.Select(group => new
        //        //{
        //        //    RequestId = group.Key.ToString(),
        //        //    RNumber = group.FirstOrDefault()?.RequestId.ToString(),
        //        //    Records = group.Select(r => new
        //        //    {
        //        //        ItemName = r.TaxableItem.ItemName,
        //        //        Tax = r.TaxDue,
        //        //        State = r.State?.StateName,
        //        //        Jurisdiction = r.Jurisdiction?.JurisdictionName,
        //        //    }).ToList(),
        //        //    Total = group.Sum(x => x.TaxDue)
        //        //})
        //        //.ToList();

        //        //    // Return the grouped data as JSON
        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error for debugging purposes
        //        System.Diagnostics.Trace.WriteLine(ex.ToString());

        //        // Return an error response
        //        return StatusCode(500, "An error occurred while fetching tax calculation data.");
        //    }
        //}

        //[HttpPost("Tax/FetchCalculatedTaxes")]
        //public async Task<IActionResult> FetchCalculatedTaxes([FromBody] List<string> requestIds)
        //{
        //    try
        //    {
        //        var requestIdsGuid = requestIds.Select(rid => Guid.Parse(rid)).ToList();

        //        // Get the tax records for the specified request IDs
        //        var taxRecords = await _context.TaxCalculationRecords
        //            .Where(t => requestIdsGuid.Contains(t.RequestId))
        //            .Include(x => x.TaxableItem)
        //            .Include(x => x.State)
        //            .Include(x => x.Jurisdiction)
        //            .ToListAsync();

        //        // Get all request IDs as a list
        //        var allRequestIds = requestIdsGuid.Distinct().ToList();

        //        var result = allRequestIds.Select(requestId =>
        //        {
        //            // Get the tax records for this requestId or an empty list if none found
        //            var records = taxRecords.Where(t => t.RequestId == requestId).ToList();

        //            return new
        //            {
        //                RequestId = requestId.ToString(),
        //                RNumber = records.FirstOrDefault()?.RequestId.ToString() ?? "N/A", // Provide "N/A" if no record is found
        //                Records = records.Select(r => new
        //                {
        //                    ItemName = r.TaxableItem.ItemName,
        //                    Tax = r.TaxDue,
        //                    State = r.State?.StateName,
        //                    Jurisdiction = r.Jurisdiction?.JurisdictionName,
        //                }).ToList(),
        //                Total = records.Sum(x => x.TaxDue) // Sum the tax if records exist, else return 0
        //            };
        //        }).ToList();

        //        // Return the grouped data as JSON
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Trace.WriteLine(ex.ToString());
        //        // Return an error response
        //        return StatusCode(500, "An error occurred while fetching tax calculation data.");
        //    }
        //}

        [HttpGet("Tax/GetFormulas")]
        public IActionResult GetFormulas(string stateId, int? jurisdictionId)
        {
            try
            {
                if (jurisdictionId != null && jurisdictionId == 0)
                {
                    jurisdictionId = null;
                }
                var formula = _context.TaxFormulas.FirstOrDefault(rule => rule.StateAbbreviation == stateId && rule.JurisdictionID == jurisdictionId);
                // Return the formula or an empty string if null
                return Ok(formula ?? null);
            }
            catch (Exception e)
            {
                // Log the exception using Trace.WriteLine
                System.Diagnostics.Trace.WriteLine($"An error occurred: {e}");

                // Return a JSON error response with the exception message
                return JsonError(e.Message);
            }
        }


        [HttpPost("Tax/SaveTax")]
        public async Task<IActionResult> SaveTax([FromBody] SaveCalculatedTax calculateRequest)
        {
            try
            {
                UserInfo ui = await GetCurrentUserAsync();

                // Check if the VendorId is not null
                if (ui?.VendorId != null)
                {
                    var request = await _context.Requests
                     .Where(r => r.RequestId == new Guid(calculateRequest.RequestId) && r.VendorId == ui.VendorId)
                     .FirstOrDefaultAsync();

                    // If the request is found
                    if (request != null)
                    {
                        var fee = JsonConvert.DeserializeObject<TaxAndRegFeeModel>(request.jFeesData);
                        fee.CalculatedTaxResult = calculateRequest.CalculatedTaxResult;

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

        [HttpGet("Tax/TaxRateFromUSGeoCoder")]
        public async Task<IActionResult> FetchTaxRateFromUSGeoCoder([FromQuery] string address, [FromQuery] string zipCode)
        {
            string authKey = _configuration["USGeocoder:AuthKey"];
            string baseUrl = _configuration["USGeocoder:BaseUrl"];

            if (string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(baseUrl))
            {
                return BadRequest("USGeoCoder API configuration is missing.");
            }

            string requestUrl = $"{baseUrl}address={Uri.EscapeDataString(address)}&zipcode={Uri.EscapeDataString(zipCode)}&authkey={authKey}&format=json";

            HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to fetch tax details.");
            }

            string responseData = await response.Content.ReadAsStringAsync();
            var parsedResponse = JsonConvert.DeserializeObject<USGeocoderTaxApiResponse>(responseData);

            if (parsedResponse?.USGeocoder?.TotalCollectionTaxDetails != null)
            {
                var jsonObject = JObject.Parse(responseData);

                JObject totalCollectionTaxDetails = jsonObject["usgeocoder"]?["totalcollection_tax_details"] as JObject;

                if (totalCollectionTaxDetails != null)
                {
                    parsedResponse.USGeocoder.TotalCollectionTaxDetails.ProcessDistricts(totalCollectionTaxDetails);
                }
                else
                {
                    Console.WriteLine("totalcollection_tax_details not found in response");
                }
            }
            if (parsedResponse?.USGeocoder?.TotalCollectionTaxDetails?.TaxDetailsStatus != "Match Found")
            {
                return NotFound("No matching tax details found.");
            }
            return Ok(parsedResponse.USGeocoder.TotalCollectionTaxDetails);

        }

        [HttpGet("Tax/FetchTaxRate")]
        public async Task<IActionResult> FetchTaxRate([FromQuery] string state)
        {
            var stateConfig = await _context.StateConfigurations.FirstOrDefaultAsync(x => x.StateAbbreviation == state);
            if (stateConfig is not null)
            {
                if (stateConfig.EnableTaxRateFetchFromApi)
                {
                    var taxResult = await FetchTaxRateFromUSGeoCoder("PO Box 4320", "10001") as ObjectResult;
                    if (taxResult != null && taxResult.StatusCode == 200 && taxResult.Value is TotalCollectionTaxDetails taxDetails)
                    {
                        var rate = new TotalCollectionTaxDetails();
                        if (stateConfig.IncludeStateTax)
                        {
                            rate.StateTax = taxDetails.StateTax;
                            rate.StateJurisdictionName = taxDetails.StateJurisdictionName;
                        }
                        if (stateConfig.IncludeCityTax)
                        {
                            rate.CityJurisdictionName = taxDetails.CityJurisdictionName;
                            rate.CityTax = taxDetails.CityTax;
                            rate.CityDistricts = taxDetails.CityDistricts;
                        }
                        if (stateConfig.IncludeSpecialDistrictTax)
                        {
                            rate.SpecialDistricts = taxDetails.SpecialDistricts;
                        }
                        if (stateConfig.IncludeCountyTax)
                        {
                            rate.CountyJurisdictionName = taxDetails.CountyJurisdictionName;
                            rate.CountyTax = taxDetails.CountyTax;
                            rate.CountyDistricts = taxDetails.CountyDistricts;
                        }
                        return Ok(rate);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                else
                {
                    var tax = _context.TaxRules.FirstOrDefault(x => x.StateAbbreviation == state);
                    if (tax is not null)
                    {
                        var rate = new TotalCollectionTaxDetails { StateTax = tax.TaxRate.HasValue ? tax.TaxRate.ToString() : "", StateJurisdictionName = state };
                        return Ok(rate);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            else
            {
                return NotFound();
            }
        }


        [HttpGet]
        public IActionResult StateConfig([FromQuery] string stateAbbreviation)
        {
            if (string.IsNullOrEmpty(stateAbbreviation))
                return BadRequest("State abbreviation is required.");

            var stateConfig = _context.StateConfigurations
                .FirstOrDefault(s => s.StateAbbreviation == stateAbbreviation);

            if (stateConfig == null)
                return NotFound("State configuration not found.");

            return Ok(stateConfig);
        }

        // Save State Configuration (Create or Update)
        [HttpPost]
        public IActionResult SaveStateConfig([FromBody] StateConfiguration model)
        {
            if (model == null || string.IsNullOrEmpty(model.StateAbbreviation))
                return BadRequest("Invalid data.");

            var existingConfig = _context.StateConfigurations
                .FirstOrDefault(s => s.StateAbbreviation == model.StateAbbreviation);

            if (existingConfig == null)
            {
                // Create new config
                _context.StateConfigurations.Add(model);
            }
            else
            {
                // Update existing config
                existingConfig.EnableTaxRateFetchFromApi = model.EnableTaxRateFetchFromApi;
                existingConfig.IncludeStateTax = model.IncludeStateTax;
                existingConfig.IncludeCityTax = model.IncludeCityTax;
                existingConfig.IncludeSpecialDistrictTax = model.IncludeSpecialDistrictTax;
                existingConfig.IncludeCountyTax = model.IncludeCountyTax;
                _context.StateConfigurations.Update(existingConfig);
            }

            _context.SaveChanges();
            return Ok(new { message = "State configuration saved successfully." });
        }

        private static bool IsValidFormula(string formula)
        {
            try
            {
                // Remove any spaces from the formula
                formula = formula.Replace(" ", "");

                // Check for division by zero explicitly
                if (formula.Contains("/0"))
                {
                    return false; // Formula is invalid due to potential division by zero
                }

                // Attempt to evaluate the formula using DataTable.Compute
                var dataTable = new DataTable();
                dataTable.Compute(formula, string.Empty);

                return true; // No exception means the formula is valid
            }
            catch (DivideByZeroException)
            {
                // Specifically handle division by zero (though we already check above)
                return false;
            }
            catch (SyntaxErrorException)
            {
                // Handle cases where the formula syntax is incorrect
                return false;
            }
            catch (Exception ex)
            {
                // Handle other unexpected exceptions
                System.Diagnostics.Trace.WriteLine($"Formula validation error: {ex.Message}");
                return false;
            }

        }
        private static string EvaluateFormula(string formula)
        {
            try
            {
                // Remove spaces from the formula
                formula = formula.Replace(" ", "");

                // Check for division by zero in the formula
                if (formula.Contains("/0"))
                {
                    throw new DivideByZeroException("The formula contains a division by zero.");
                }

                // Use DataTable.Compute to evaluate the formula
                var dataTable = new DataTable();
                var result = dataTable.Compute(formula, string.Empty);

                // Convert the result to decimal
                return result.ToString();
            }
            catch (DivideByZeroException ex)
            {
                // Handle division by zero error
                System.Diagnostics.Trace.WriteLine(ex.Message);
                return ""; // Or return another appropriate value
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                System.Diagnostics.Trace.WriteLine($"Error evaluating formula: {ex.Message}");
                return "";
            }

        }
        private void UpdateFormulaProperty(TaxFormula formula, string selectedRadioButton, string formulaValue)
        {
            switch (selectedRadioButton)
            {
                case "totalTaxableAmount":
                    formula.TotalTaxableAmount = formulaValue;
                    break;
                case "totalTaxDue":
                    formula.TotalTaxDue = formulaValue;
                    break;
                case "totalTaxPaidOtherState":
                    formula.TotalTaxPaidOtherState = formulaValue;
                    break;
                case "totalLeftOverTitlingState":
                    formula.TotalLeftOverTitlingState = formulaValue;
                    break;
                default:
                    break;
            }
        }
        public static string GetSelectedFormula(TaxFormula formula, string selectedRadioButton)
        {
            if (formula == null || string.IsNullOrEmpty(selectedRadioButton))
            {
                return null;
            }

            return selectedRadioButton switch
            {
                "totalTaxableAmount" => formula.TotalTaxableAmount,
                "totalTaxDue" => formula.TotalTaxDue,
                "totalTaxPaidOtherState" => formula.TotalTaxPaidOtherState,
                "totalLeftOverTitlingState" => formula.TotalLeftOverTitlingState,
                _ => null
            };
        }
    }
    public class CalculateRequest
    {
        public string StateId { get; set; }
        public int? JurisdictionId { get; set; }
        public string RequestId { get; set; }
        public string StreetAddress { get; set; }
        public string ZipCode { get; set; }
        public Dictionary<string, decimal> Items { get; set; }
    }
}
