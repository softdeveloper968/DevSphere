using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyDMVpro.Common;
using MyDMVpro.Common.Extensions;
using MyDMVpro.Common.ViewHelpers;
using MyDMVpro.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers;

public static class IQuerablePdfTemplateExtensions
{
    public static IQueryable<PdfTemplate> WhereClientVisible(this IQueryable<PdfTemplate> pdfTemplate, bool include = true)
    {
        return pdfTemplate.Where(r => (r.ClientVisible == true || r.GenerateForClient == true) == include);
    }
    public static IOrderedQueryable<PdfTemplate> OrderByClientSortOrder(this IQueryable<PdfTemplate> pdfTemplate, bool include = true)
    {
        return pdfTemplate.OrderBy(e => e.ClientSortOrder == null) // false (non-null) first, true (null) last
                            .ThenBy(x => x.ClientSortOrder);
    }
    public static IOrderedQueryable<PdfTemplate> OrderByVendorSortOrder(this IQueryable<PdfTemplate> pdfTemplate, bool include = true)
    {
        return pdfTemplate.OrderBy(e => e.VendorSortOrder == null) // false (non-null) first, true (null) last
                            .ThenBy(x => x.VendorSortOrder);
    }
}

