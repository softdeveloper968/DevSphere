using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel;
using System.Threading;
using System.Linq;
using System.Linq.Expressions;
using System.Dynamic;

namespace TypeMerger
{
    public class TypeMerger
    {
        public static dynamic Merge(object item1, object item2)
        {
            IDictionary<string, object> result = new ExpandoObject();

            foreach (var property in item1.GetType().GetProperties())
            {
                if (property.CanRead)
                    result[property.Name] = property.GetValue(item1);
            }

            foreach (var property in item2.GetType().GetProperties())
            {
                if (property.CanRead)
                    result[property.Name] = property.GetValue(item2);
            }

            return result;
        }
    }
}

