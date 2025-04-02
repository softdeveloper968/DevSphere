using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyDMVpro.Models;

namespace MyDMVpro.Common.Extensions
{
    public static class EFExtensions
    {
        public static void Update<T>(this BaseMaggardDMVContext context, T entityObject, params string[] properties) where T : class
        {
            context.Set<T>().Attach(entityObject);

            var entry = context.Entry(entityObject);

            foreach (string name in properties)
                entry.Property(name).IsModified = true;
        }
    }
}
