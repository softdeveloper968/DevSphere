using Humanizer;
using Microsoft.EntityFrameworkCore;
using MyDMVpro.Controllers;
using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDMVpro.Common
{
    public class VinHelper
    {
        class DigitInfo
        {
            public char Digit { get; set; }
            public int Value { get; set; }
            public char[] FixUps { get; set; }
        }
        static DigitInfo[] _dis = new DigitInfo[]
        {
            new DigitInfo { Digit = 'A', Value = 1, FixUps = new char[] { '4' } },
            new DigitInfo { Digit = 'B', Value = 2, FixUps = new char[] { '8' }  },
            new DigitInfo { Digit = 'C', Value = 3 },
            new DigitInfo { Digit = 'D', Value = 4, FixUps = new char[] { '0' } },
            new DigitInfo { Digit = 'E', Value = 5, FixUps = new char[] { 'F' } },
            new DigitInfo { Digit = 'F', Value = 6, FixUps = new char[] { 'E' } },
            new DigitInfo { Digit = 'G', Value = 7 },
            new DigitInfo { Digit = 'H', Value = 8 },
            new DigitInfo { Digit = 'J', Value = 1 },
            new DigitInfo { Digit = 'K', Value = 2 },
            new DigitInfo { Digit = 'L', Value = 3, FixUps = new char[] { '6' } },
            new DigitInfo { Digit = 'M', Value = 4 },
            new DigitInfo { Digit = 'N', Value = 5 },
            new DigitInfo { Digit = 'P', Value = 7, FixUps = new char[] { 'R' }  },
            new DigitInfo { Digit = 'R', Value = 9, FixUps = new char[] { 'P' }  },
            new DigitInfo { Digit = 'S', Value = 2, FixUps = new char[] { '5' }  },
            new DigitInfo { Digit = 'T', Value = 3 },
            new DigitInfo { Digit = 'U', Value = 4 },
            new DigitInfo { Digit = 'V', Value = 5 },
            new DigitInfo { Digit = 'W', Value = 6 },
            new DigitInfo { Digit = 'X', Value = 7 },
            new DigitInfo { Digit = 'Y', Value = 8 },
            new DigitInfo { Digit = 'Z', Value = 9 },
            new DigitInfo { Digit = '0', Value = 0, FixUps = new char[] { 'D' } },
            new DigitInfo { Digit = '1', Value = 1 },
            new DigitInfo { Digit = '2', Value = 2 },
            new DigitInfo { Digit = '3', Value = 3 },
            new DigitInfo { Digit = '4', Value = 4, FixUps = new char[] { 'A' }  },
            new DigitInfo { Digit = '5', Value = 5, FixUps = new char[] { 'S' }  },
            new DigitInfo { Digit = '6', Value = 6, FixUps = new char[] { 'L' } },
            new DigitInfo { Digit = '7', Value = 7 },
            new DigitInfo { Digit = '8', Value = 8, FixUps = new char[] { 'B' }  },
            new DigitInfo { Digit = '9', Value = 9 },
        };

        static int[] weights = new int[]
        {
            8, /* 1st */
            7, /* 2nd */
            6, /* 3rd */
            5, /* 4th */
            4, /* 5th */
            3, /* 6th */
            2, /* 7th */
            10, /* 8th */
            0, /* 9th (checkdigit) */
            9, /* 10th */
            8, /* 11th */
            7, /* 12th */
            6, /* 13th */
            5, /* 14th */
            4, /* 15th */
            3, /* 16th */
            2  /* 17th */
        };

        static Dictionary<char, int> _digits;
        static Dictionary<char, DigitInfo> _digitInfo;
        static VinHelper()
        {
            _digits = new Dictionary<char, int>();
            _digitInfo = new Dictionary<char, DigitInfo>();
            foreach (DigitInfo di in _dis)
            {
                _digits.Add(di.Digit, di.Value);
                _digitInfo.Add(di.Digit, di);
            }
        }
        public static List<string> GetPotentialVins(string vin)
        {
            string checkvin = vin;
            List<string> potentialMatches = new List<string>();

            for (int i = 0; i < 17; i++)
            {
                string v;
                if (TestReplacingDigit(vin, i, out v))
                {
                    potentialMatches.Add(v);
                }
            }

            return potentialMatches;
        }
        public static bool TestReplacingDigit(string vin, int digit, out string newvin)
        {
            newvin = null;
            char c = vin[digit];
            StringBuilder sb = new StringBuilder(vin);
            if (!_digitInfo.ContainsKey(c) || _digitInfo[c].FixUps == null) return false;
            foreach (char cReplacement in _digitInfo[c].FixUps)
            {
                sb[digit] = cReplacement;
                string testvin = sb.ToString();
                if (IsValidVIN(testvin))
                {
                    newvin = testvin;
                    return true;
                }
            }
            return false;
        }
        public static bool IsValidVIN(string vin)
        {
            if (vin == null)
                return false;

            vin = vin.Trim().ToUpper();

            if (vin.IndexOf('I') != -1 || vin.IndexOf('O') != -1)
                return false;
            if (vin.Length != 17)
                return false;

            bool invalidChars = vin.ToCharArray().Any(c => !_digits.ContainsKey(c));
            if (invalidChars)
            {
                return false;
            }

            try
            {
                char checkDigit = '0';
                int sum = 0;
                for (int i = 0; i < 17; i++)
                {
                    char digit = vin[i];

                    if (i == 8)
                    {
                        checkDigit = digit;
                        continue; // checkdigit
                    }
                    int value = _digits[digit];
                    int weight = weights[i];
                    sum += (value * weight);
                }
                int checkDigitValue = (sum % 11);
                char checkDigitCalc;
                if (checkDigitValue == 10)
                    checkDigitCalc = 'X';
                else
                    checkDigitCalc = Convert.ToChar((int)'0' + checkDigitValue);

                return (checkDigit == checkDigitCalc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
                return false;
            }
        }
#if NO_LONGER_USED
        public async static Task<MyDMVpro.Models.VINProperties> GetVinDetails(string vin)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string url = $"https://vpic.nhtsa.dot.gov/api/vehicles/decodevinvalues/{vin}?format=json";
                    string data = await client.DownloadStringTaskAsync(url);

                    return GetVehicleData(JsonConvert.DeserializeObject(data));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
            }
            return null;
        }
#endif
        ///vehicles/DecodeVINValuesBatch/
#if NO_LONGER_USED
        public async static Task<string> GetVinDetailsJson(string[] vin)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string url = $"https://vpic.nhtsa.dot.gov/apivehicles/DecodeVINValuesBatch/?format=json";

                    string postData = string.Join(";", vin);
                    client.Headers[HttpRequestHeader.ContentType] = "text/html";

                    string data = await client.UploadStringTaskAsync(url, "POST", postData);
                    return data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
            }
            return null;
        }

        public static MyDMVpro.Models.VINProperties GetVehicleData(dynamic data)
        {
            MyDMVpro.Models.VINProperties vp = new MyDMVpro.Models.VINProperties();
            vp.VIN = data.VIN;
            vp.Make = data.Make;
            vp.Model = data.Model;
            vp.ModelYear = data.ModelYear;
            vp.Doors = data.Doors;
            vp.EngineCylinders = data.EngineCylinders;
            return vp;
        }
#endif
        public static async Task<LookupResult> LookupVinDetails(BaseMaggardDMVContext context, string vin)
        {
            vin = vin?.Trim().ToUpper();
            LookupResult result = new()
            {
                VIN = vin,
                ValidChecksum = VinHelper.IsValidVIN(vin)
            };
            if (!string.IsNullOrEmpty(vin) && vin.Length >= 11)
            {
                string partialVin = string.Concat(vin[..8], vin.AsSpan(9, 2));
                result.vpd = await context.VinPartialDetail
                                                    .AsNoTracking()
                                                    .Where(v => v.VinPattern == partialVin)
                                                    .ToListAsync();
                foreach (var vpd in result.vpd)
                {
                    vpd.InitFieldMappings();
                }
                if (result.vpd.Count == 0)
                {
                    var pvins = VinHelper.GetPotentialVins(vin);
                    result.PossibleVins = await GetVinPartialDetails(context, pvins);
                }
            }
            return result;
        }
        public static async Task<List<VinInfo>> GetVinPartialDetails(BaseMaggardDMVContext context, List<string> vins)
        {
            List<VinInfo> list = new List<VinInfo>();

            var l = new List<string>();
            foreach (var pv in vins)
            {
                string s = pv.Substring(0, 8) + pv.Substring(9, 2);

                var vpdList = await context.VinPartialDetail.AsNoTracking()
                                                        .Where(v => v.VinPattern == s)
                                                        .Select(v => new { Year = v.Year, Make = v.Make })
                                                        .Distinct()
                                                        .ToListAsync();
                foreach (var vpd in vpdList)
                {
                    VinInfo vi = new VinInfo();
                    vi.Vin = pv;
                    vi.Desc = $"{pv} - {vpd.Year} {vpd.Make}";
                    list.Add(vi);
                }
            }
            return list;
        }

    }
}
