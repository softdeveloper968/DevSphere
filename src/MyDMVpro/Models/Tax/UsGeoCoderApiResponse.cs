using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyDMVpro.Models.Tax
{
    public class USGeocoderTaxApiResponse
    {
        [JsonProperty("usgeocoder")]
        public USGeocoder USGeocoder { get; set; }
    }

    public class USGeocoder
    {
        [JsonProperty("request_status")]
        public USGeocoderRequestStatus RequestStatus { get; set; }

        [JsonProperty("totalcollection_tax_details")]
        public TotalCollectionTaxDetails TotalCollectionTaxDetails { get; set; }
    }

    public class USGeocoderRequestStatus
    {
        [JsonProperty("request_address")]
        public string RequestAddress { get; set; }

        [JsonProperty("request_status_code")]
        public USGeocoderRequestStatusCode RequestStatusCode { get; set; }

        [JsonProperty("request_status_code_description")]
        public USGeocoderRequestStatusCodeDescription RequestStatusCodeDescription { get; set; }

        [JsonProperty("request_status_version")]
        public string RequestStatusVersion { get; set; }
    }

    public class USGeocoderRequestStatusCode
    {
        [JsonProperty("flags")]
        public string Flags { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class USGeocoderRequestStatusCodeDescription
    {
        [JsonProperty("census")]
        public string Census { get; set; }

        [JsonProperty("usps")]
        public string USPS { get; set; }

        [JsonProperty("parcel")]
        public string Parcel { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class TotalCollectionTaxDetails
    {
        [JsonProperty("t_tax_details_status")]
        public string TaxDetailsStatus { get; set; }

        [JsonProperty("t_tax_total_tax")]
        public string TotalTax { get; set; }

        [JsonProperty("t_tax_state_jurisction_name")]
        public string StateJurisdictionName { get; set; }

        [JsonProperty("t_tax_state_tax")]
        public string StateTax { get; set; }

        [JsonProperty("t_tax_county_jurisdiction_name")]
        public string CountyJurisdictionName { get; set; }

        [JsonProperty("t_tax_county_tax")]
        public string CountyTax { get; set; }

        [JsonProperty("t_tax_city_jurisdiction_name")]
        public string CityJurisdictionName { get; set; }

        [JsonProperty("t_tax_city_tax")]
        public string CityTax { get; set; }

        [JsonProperty("t_tax_incorporated_city")]
        public string IncorporatedCity { get; set; }

        [JsonProperty("t_tax_code")]
        public string TaxCode { get; set; }

        [JsonProperty("t_tax_effective_date")]
        public string EffectiveDate { get; set; }

        [JsonProperty("t_tax_usgid")]
        public string USGID { get; set; }

        public List<DistrictTax> CityDistricts { get; set; } = new();

        public List<DistrictTax> SpecialDistricts { get; set; } = new();

        public List<DistrictTax> CountyDistricts { get; set; } = new();

        //public void ProcessDistricts(JObject jsonObject)
        //{
        //    try
        //    {
        //        CityDistricts = new List<DistrictTax>();
        //        SpecialDistricts = new List<DistrictTax>();

        //        for (int i = 1; i <= 3; i++)
        //        {
        //            string cityNameKey = $"t_tax_city_district{i}_name";
        //            string cityTaxKey = $"t_tax_city_district{i}_tax";
        //            string cityAbbrKey = $"t_tax_city_district{i}_abbr";

        //            if (jsonObject.TryGetValue(cityNameKey, out JToken cityName) &&
        //                jsonObject.TryGetValue(cityTaxKey, out JToken cityTax))
        //            {
        //                CityDistricts.Add(new DistrictTax
        //                {
        //                    Name = cityName.ToString(),
        //                    Tax = cityTax.ToString(),
        //                    Abbreviation = jsonObject.TryGetValue(cityAbbrKey, out JToken cityAbbr) ? cityAbbr.ToString() : ""
        //                });
        //            }

        //            string specialNameKey = $"t_tax_special_district{i}_name";
        //            string specialTaxKey = $"t_tax_special_district{i}_tax";
        //            string specialAbbrKey = $"t_tax_special_district{i}_abbr";

        //            if (jsonObject.TryGetValue(specialNameKey, out JToken specialName) &&
        //                jsonObject.TryGetValue(specialTaxKey, out JToken specialTax))
        //            {
        //                SpecialDistricts.Add(new DistrictTax
        //                {
        //                    Name = specialName.ToString(),
        //                    Tax = specialTax.ToString(),
        //                    Abbreviation = jsonObject.TryGetValue(specialAbbrKey, out JToken specialAbbr) ? specialAbbr.ToString() : ""
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error processing districts: {ex.Message}");
        //    }
        //}
        public void ProcessDistricts(JObject jsonObject)
        {
            try
            {
                CityDistricts = new List<DistrictTax>();
                SpecialDistricts = new List<DistrictTax>();
                CountyDistricts = new List<DistrictTax>();

                // Regular expressions to match district keys and extract their indices
                Regex cityRegex = new Regex(@"t_tax_city_district(\d+)_name");
                Regex specialRegex = new Regex(@"t_tax_special_district(\d+)_name");
                Regex countyRegex = new Regex(@"t_tax_county_district(\d+)_name");

                foreach (var property in jsonObject.Properties())
                {
                    string propName = property.Name;

                    Match cityMatch = cityRegex.Match(propName);
                    if (cityMatch.Success)
                    {
                        string index = cityMatch.Groups[1].Value;

                        string cityTaxKey = $"t_tax_city_district{index}_tax";
                        string cityAbbrKey = $"t_tax_city_district{index}_abbr";

                        jsonObject.TryGetValue(cityTaxKey, out JToken cityTax);
                        jsonObject.TryGetValue(cityAbbrKey, out JToken cityAbbr);

                        CityDistricts.Add(new DistrictTax
                        {
                            Name = property.Value.ToString(),
                            Tax = cityTax?.ToString() ?? "",
                            Abbreviation = cityAbbr?.ToString() ?? ""
                        });

                        continue;
                    }

                    Match specialMatch = specialRegex.Match(propName);
                    if (specialMatch.Success)
                    {
                        string index = specialMatch.Groups[1].Value;

                        string specialTaxKey = $"t_tax_special_district{index}_tax";
                        string specialAbbrKey = $"t_tax_special_district{index}_abbr";

                        jsonObject.TryGetValue(specialTaxKey, out JToken specialTax);
                        jsonObject.TryGetValue(specialAbbrKey, out JToken specialAbbr);

                        SpecialDistricts.Add(new DistrictTax
                        {
                            Name = property.Value.ToString(),
                            Tax = specialTax?.ToString() ?? "",
                            Abbreviation = specialAbbr?.ToString() ?? ""
                        });
                        continue;
                    }

                    Match countyMatch = countyRegex.Match(propName);
                    if (countyMatch.Success)
                    {
                        string index = countyMatch.Groups[1].Value;

                        string countyTaxKey = $"t_tax_county_district{index}_tax";
                        string countyAbbrKey = $"t_tax_county_district{index}_abbr";

                        jsonObject.TryGetValue(countyTaxKey, out JToken countyTax);
                        jsonObject.TryGetValue(countyAbbrKey, out JToken countyAbbr);

                        CountyDistricts.Add(new DistrictTax
                        {
                            Name = property.Value.ToString(),
                            Tax = countyTax?.ToString() ?? "",
                            Abbreviation = countyAbbr?.ToString() ?? ""
                        });

                        continue; // Move to the next property
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing districts: {ex.Message}");
            }
        }
    }

    public class DistrictTax
    {
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string Tax { get; set; }
    }

    public class TotalCollectionTaxDetailsDto
    {
        public string TaxDetailsStatus { get; set; }

        public string TotalTax { get; set; }

        public string StateJurisdictionName { get; set; }

        public string StateTax { get; set; }

        public string CountyJurisdictionName { get; set; }

        public string CountyTax { get; set; }

        public string CityJurisdictionName { get; set; }

        public string CityTax { get; set; }

        public string IncorporatedCity { get; set; }

        public string TaxCode { get; set; }

        public string EffectiveDate { get; set; }

        public string USGID { get; set; }

        public List<DistrictTax> CityDistricts { get; set; } = new();

        public List<DistrictTax> SpecialDistricts { get; set; } = new();

        public List<DistrictTax> CountyDistricts { get; set; } = new();

    }

}
