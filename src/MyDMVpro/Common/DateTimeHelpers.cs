using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Common
{
    public static class DateTimeHelpers
    {
        private static TimeZoneInfo s_EasternTimeZone;
        static DateTimeHelpers()
        {
            s_EasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        public static DateTime ServerDate()
        {
            var timeUtc = DateTime.UtcNow;
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, s_EasternTimeZone);
            return easternTime.Date;
        }
        public static DateTime ServerDateTime()
        {
            var timeUtc = DateTime.UtcNow;
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, s_EasternTimeZone);
            return easternTime;
        }
        public static string EasternDateTime(DateTime? dt, string format)
        {
            if (dt == null) return "";
            if (dt.Value.TimeOfDay == TimeSpan.Zero)
            {
                // Dates with no time are not converted to timezone
                return dt.Value.ToString(format);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(dt.Value, s_EasternTimeZone).ToString(format);
        }
        public static DateTime? ConvertToEasternDateTime(DateTime? dt)
        {
            if (dt == null) return null;
            if (dt.Value.TimeOfDay == TimeSpan.Zero)
            {
                // Dates with no time are not converted to timezone
                return dt.Value;
            }
            return TimeZoneInfo.ConvertTimeFromUtc(dt.Value, s_EasternTimeZone);
        }
        public static string FormatTimespan(DateTime dt)
        {
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            DateTime now = DateTime.UtcNow;
            TimeSpan ts = now - dt;
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

            if (delta < 2 * MINUTE)
                return "a minute ago";

            if (delta < 45 * MINUTE)
                return ts.Minutes + " minutes ago";

            if (delta < 90 * MINUTE)
                return "an hour ago";

            if (delta < 24 * HOUR)
                return ts.Hours + " hours ago";

            if (delta < 48 * HOUR)
            {
                if (now.Day > dt.Day)
                    return "yesterday";
                return "2 days ago";
            }

            if (delta < 30 * DAY)
            {
                return ts.Days + " days ago";
            }

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }
    }
}
