using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Common.Extensions
{
    static class ArrayExtensions
    {
        public static List<int?> ToIntList(this List<string> vals)
        {
            List<int?> list = new List<int?>();
            foreach (string s in vals)
            {
                int val;
                if (Int32.TryParse(s, out val))
                {
                    list.Add(val);
                }
            }
            return list;
        }
        public static List<int?> ToIntList(this string[] vals)
        {
            List<int?> list = new List<int?>();
            foreach (string s in vals)
            {
                int val;
                if (Int32.TryParse(s, out val))
                {
                    list.Add(val);
                }
            }
            return list;
        }
        public static List<DateTime?> ToDateList(this List<string> vals)
        {
            return ToDateList(vals, false);
        }
        public static List<DateTime?> ToDateList(this List<string> vals, bool dateOnly)
        {
            List<DateTime?> list = new List<DateTime?>();
            foreach (string s in vals)
            {
                DateTime val;
                if (DateTime.TryParse(s, out val))
                {
                    if (dateOnly)
                    {
                        list.Add(val.Date);
                    }
                    else
                    {
                        list.Add(val);
                    }
                }
            }
            return list;
        }
        public static List<DateTime?> ToDateList(this string[] vals)
        {
            return ToDateList(vals, false);
        }
        public static List<DateTime?> ToDateList(this string[] vals, bool dateOnly)
        {
            List<DateTime?> list = new List<DateTime?>();
            foreach (string s in vals)
            {
                DateTime val;
                if (DateTime.TryParse(s, out val))
                {
                    if (dateOnly)
                    {
                        list.Add(val.Date);
                    }
                    else
                    {
                        list.Add(val);
                    }
                }
            }
            return list;
        }
        public static List<Decimal?> ToDecimalList(this string[] vals)
        {
            List<Decimal?> list = new List<Decimal?>();
            foreach (string s in vals)
            {
                Decimal val;
                if (Decimal.TryParse(s, out val))
                {
                    list.Add(val);
                }
            }
            return list;
        }
        public static List<Decimal?> ToDecimalList(this List<string> vals)
        {
            List<Decimal?> list = new List<Decimal?>();
            foreach (string s in vals)
            {
                Decimal val;
                if (Decimal.TryParse(s, out val))
                {
                    list.Add(val);
                }
            }
            return list;
        }
        public static List<bool?> ToBoolList(this string[] vals)
        {
            List<bool?> list = new List<bool?>();
            foreach (string s in vals)
            {
                bool val;
                if (Boolean.TryParse(s, out val))
                {
                    list.Add(val);
                }
            }
            return list;
        }
        public static List<bool?> ToBoolList(this List<string> vals)
        {
            List<bool?> list = new List<bool?>();
            foreach (string s in vals)
            {
                bool val;
                if (Boolean.TryParse(s, out val))
                {
                    list.Add(val);
                }
            }
            return list;
        }
        public static List<Guid?> ToGuidList(this string[] vals)
        {
            List<Guid?> list = new List<Guid?>();
            foreach (string s in vals)
            {
                Guid val;
                if (Guid.TryParse(s, out val))
                {
                    list.Add(val);
                }
            }
            return list;
        }
        public static List<Guid?> ToGuidList(this List<string> vals)
        {
            List<Guid?> list = new List<Guid?>();
            foreach (string s in vals)
            {
                Guid val;
                if (Guid.TryParse(s, out val))
                {
                    list.Add(val);
                }
            }
            return list;
        }
    }

}
