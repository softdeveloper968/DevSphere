using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Common.Extensions
{
    public static class FormatExtensions
    {
        private static TimeZoneInfo s_EasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        public static string EasternTime(this DateTime datetime)
        {
            try
            {
                string format = "MMM d, yyyy h:mm tt";
                return datetime.EasternTime(format);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public static string EasternTime(this DateTime datetime, string format)
        {
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(datetime, s_EasternTimeZone);
            return easternTime.ToString(format);
        }
    }
}
