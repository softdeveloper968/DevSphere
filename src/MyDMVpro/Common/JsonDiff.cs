using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace MyDMVpro.Common
{
    public class DiffDictionary : Dictionary<string, JsonDiff.LeftRightValue>
    {
        public DiffDictionary() { }
    }
    public static class JsonDiff
    {
        public class LeftRightValue
        {
            public LeftRightValue(object left, object right) {  Left = left; Right = right; }
            public object Left { get; set; }
            public object Right { get; set; }
        }
        private static List<string> s_HiddenKeys = new List<string>()
        {
            "requestid", "groupid", "userid", "vendorid"
        };
        private static bool ExcludeKey(string key)
        {
            key= key.ToLower(); 
            return s_HiddenKeys.Contains(key);
        }
        public static DiffDictionary GetDiff(string leftJson, string rightJson)
        {
            try
            {
                if (leftJson == null) leftJson = "{}";
                if (rightJson == null) rightJson = "{}";

                leftJson = leftJson.Trim();
                rightJson = rightJson.Trim();

                if (leftJson.StartsWith("[") && leftJson.EndsWith("]")) // Workaround for column having array instead of single element
                {
                    leftJson = leftJson.Substring(1, leftJson.Length - 2);
                }
                if (rightJson.StartsWith("[") && rightJson.EndsWith("]"))
                {
                    rightJson = rightJson.Substring(1, rightJson.Length - 2);
                }
                Dictionary<string, object> left = JsonConvert.DeserializeObject<Dictionary<string, object>>(leftJson);
                Dictionary<string, object> right = JsonConvert.DeserializeObject<Dictionary<string, object>>(rightJson);

                DiffDictionary result = new DiffDictionary();

                foreach (var key in left.Keys)
                {
                    if (ExcludeKey(key))
                        continue;
                    if (right.ContainsKey(key))
                    {
                        try
                        {
                            var leftObj = left[key];
                            var rightObj = right[key];

                            if (leftObj == null && rightObj == null)
                            {
                                continue;
                            }

                            if (!leftObj.Equals(rightObj))
                            {
                                result.Add(key, new LeftRightValue(leftObj, rightObj));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        result.Add(key, new LeftRightValue(left[key], ""));
                    }
                }
                foreach (var key in right.Keys)
                {
                    if (ExcludeKey(key))
                        continue;
                    var value = right[key];
                    if (value == null) continue;

                    if (!left.ContainsKey(key))
                    {
                        if (value is string str)
                        {
                            if (str.Length == 0) continue;
                        }
                        result.Add(key, new LeftRightValue("", value));
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
