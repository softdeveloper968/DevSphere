using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MyDMVpro.Common
{
    public static class JRequest
    {
        public static string GetVIN(dynamic jRequest)
        {
            string vin = "";
            if (DoesPropertyExist(jRequest, "VIN"))
            {
                vin = jRequest["VIN"].Value;
            }
            else if (DoesPropertyExist(jRequest, "Vehicle Vin"))
            {
                vin = jRequest["Vehicle Vin"].Value;
            }
            return vin;
        }
        public static string GetAppType(dynamic jRequest)
        {
            const string FieldName_AppType = "AppType";

            if (DoesPropertyExist(jRequest, FieldName_AppType))
            {
                return jRequest[FieldName_AppType].ToString().ToUpper();
            }
            return null;
        }
        public static string GetAppType(JObject jRequest)
        {
            if (jRequest.ContainsKey("AppType"))
            {
                return jRequest["AppType"].ToString().ToUpper();
            }
            return null;
        }
        public static string GetAppTypeState(JObject jRequest)
        {
            const string FieldName_AppTypeState = "AppTypeState";
            string appTypeState = null;
            if (jRequest.ContainsKey("AppTypeState"))
            {
                appTypeState = jRequest["AppTypeState"].ToString();
            }
            else if (jRequest.ContainsKey("State"))
            {
                appTypeState = jRequest["State"].ToString();
            }
            return null;
        }
        public static string GetAppTypeState(dynamic jRequest)
        {
            const string FieldName_AppTypeState = "AppTypeState";
            string appTypeState = null;

            if (DoesPropertyExist(jRequest, "AppTypeState"))
            {
                appTypeState = jRequest["AppTypeState"];
            }
            else if (jRequest.ContainsKey("State"))
            {
                appTypeState = jRequest["State"];
            }
            return appTypeState?.ToString().ToUpper();
        }
        public static bool DoesPropertyExist(dynamic obj, string name)
        {
            if (obj == null) return false;

            if (obj is Newtonsoft.Json.Linq.JObject jObject)
            {
                return jObject.ContainsKey(name);
            }
            if (obj is ExpandoObject expObject)
            {
                var dict = (IDictionary<string, object>)expObject;
                return dict.ContainsKey(name);
            }
            return obj.GetType().GetProperty(name) != null;
        }
    }
}
