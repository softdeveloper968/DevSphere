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

public static class IQueryableRequestStatusExtension
{
    public static IQueryable<RequestStatus> WhereTitleScanned(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.TitleScanTime != null) == include);
    }
    public static IQueryable<RequestStatus> WherePALC(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => ((r.AppType == "LC" || r.AppType == "LCO") && r.State == "PA") == include);
    }
    public static IQueryable<RequestStatus> WhereAppType(this IQueryable<RequestStatus> requestStatus, List<string> AppTypes, bool include = true)
    {
        return requestStatus.Where(r => AppTypes.Contains(r.AppType) == include);
    }
    public static IQueryable<RequestStatus> WhereAppType(this IQueryable<RequestStatus> requestStatus, string[] AppTypes, bool include = true)
    {
        return requestStatus.Where(r => AppTypes.Contains(r.AppType) == include);
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    public static IQueryable<RequestStatus> WhereIsActiveHoldOrWithinCutoff(this IQueryable<RequestStatus> requestStatus, DateTime cutoffDate, bool include = true)
    {
        return requestStatus.Where(r => (r.StatusId == (int)RequestStatusIDs.Active) ||
                                        (r.StatusId == (int)RequestStatusIDs.Hold) ||
                                        (r.StatusId == (int)RequestStatusIDs.Cancelled) ||
                                        (r.StatusId == (int)RequestStatusIDs.Complete && r.DateShipped > cutoffDate));
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    public static IQueryable<RequestStatus> WhereIsActiveOrWithinCutoff(this IQueryable<RequestStatus> requestStatus, DateTime cutoffDate, bool include = true)
    {
        return requestStatus.Where(r => (r.StatusId == (int)RequestStatusIDs.Active) ||
                    (r.StatusId == (int)RequestStatusIDs.Complete && r.DateShipped > cutoffDate));
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    public static IQueryable<RequestStatus> WhereIsActiveOrWithinCutoffForPALC(this IQueryable<RequestStatus> requestStatus, DateTime cutoffDate, bool include = true)
    {
        return requestStatus.Where(r => (r.StatusId == (int)RequestStatusIDs.Active) ||
                    (r.StatusId == (int)RequestStatusIDs.Complete && r.DateToDmv > cutoffDate));
    }
#if false
    //TBD
    public static IQueryable<RequestStatus> WhereIsActiveOrWithinCutoff(this IQueryable<RequestStatus> requestStatus, int days, bool include = true)
    {
        DateTime cutoffDate = ServerDate();
        cutoffDate = cutoffDate.AddDays(-days);

        return requestStatus.Where(r => (r.StatusId == (int)RequestStatusIDs.Active) ||
                    (r.StatusId == (int)RequestStatusIDs.Complete && r.DateShipped > cutoffDate));
    }
#endif
    public static IQueryable<RequestStatus> WhereIsActiveOrComplete(this IQueryable<RequestStatus> requestStatus)
    {
        return requestStatus.Where(r => r.StatusId == (int)RequestStatusIDs.Active || r.StatusId == (int)RequestStatusIDs.Complete);
    }
    public static IQueryable<RequestStatus> WhereIsActive(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.StatusId == (int)RequestStatusIDs.Active) == include);
    }
    public static IQueryable<RequestStatus> WhereInHoldQueue(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.StatusId == (int)RequestStatusIDs.Hold) == include);
    }
    public static IQueryable<RequestStatus> WhereIsComplete(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.StatusId == (int)RequestStatusIDs.Complete) == include);
    }
    public static IQueryable<RequestStatus> WhereShipped(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.DateShipped != null) == include);
    }
    public static IQueryable<RequestStatus> WhereInvoiced(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.InvoiceId != null) == include);
    }
    public static IQueryable<RequestStatus> WhereStage(this IQueryable<RequestStatus> requestStatus, ProcessStageIDs stage, bool include = true)
    {
        return requestStatus.Where(r => (r.ProcessStageId == (int)stage) == include);
    }
    public static IQueryable<RequestStatus> WhereStage(this IQueryable<RequestStatus> requestStatus, ProcessStageIDs[] stages, bool include = true)
    {
        if (stages == null || stages.Length == 0)
            return requestStatus;

        if (stages.Length == 1)
            return requestStatus.Where(r => (r.ProcessStageId == (int)stages[0]) == include);
        return requestStatus.Where(r => (stages.Contains((ProcessStageIDs)r.ProcessStageId.Value) == include));
    }
    public static IQueryable<RequestStatus> WhereStatus(this IQueryable<RequestStatus> requestStatus, RequestStatusIDs statusId, bool include = true)
    {
        return requestStatus.Where(r => (r.StatusId == (int)statusId) == include);
    }

    public static IQueryable<RequestStatus> WhereHasActiveChat(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.HasActiveChat == true) == include);
    }

    public static IQueryable<RequestStatus> WhereSentToDmv(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.DateToDmv != null) == include);
    }

    public static IQueryable<RequestStatus> WhereStillAtDMV(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.DateToDmv != null && r.DateFromDmv == null) == include);
    }

    public static IQueryable<RequestStatus> WhereIsBillable(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => ((r.InvoiceId == null) &&
                                        (r.StatusId == (int)RequestStatusIDs.Complete
                                            || ((r.AppType == "LC" || r.AppType == "LCO") && r.State == "PA") && r.DateToDmv != null)) == include);
    }

    public static IQueryable<RequestStatus> WhereReceivedFromDMV(this IQueryable<RequestStatus> requestStatus, bool include = true)
    {
        return requestStatus.Where(r => (r.DateFromDmv != null) == include);
    }

    /// <summary>
    /// Used to filter out items that should not be included unless when viewing archive
    /// 
    /// </summary>
    /// <param name="requestStatus"></param>
    /// <param name="days"></param>
    /// <param name="include"></param>
    /// <returns></returns>
    public static IQueryable<RequestStatus> WhereNotInArchive(this IQueryable<RequestStatus> requestStatus, int? days, bool include = true)
    {
        DateTime cutoffDate = DateTimeHelpers.ServerDate();
        if (days == null)
        {
            days = -7;
        }
        else if (days > 0)
        {
            days = 0 - days;
        }
        cutoffDate = cutoffDate.AddDays((double)days);
        // When not yet invoiced
        // When invoiced, but not yet paid
        // When invoice paid, but within the archive days count
        // Is Active status or Hold status

        return requestStatus.Where(r => (
                    r.StatusId == (short)RequestStatusIDs.Active
                    || r.StatusId == (short)RequestStatusIDs.Hold
                    || r.InvoiceDate == null
                    || r.InvoiceDatePaid == null
                    || (r.InvoiceDatePaid >= cutoffDate)) == include);
    }

    public static IQueryable<RequestStatus> WhereIsInRequiredStages(this IQueryable<RequestStatus> requestStatus)
    {
        return requestStatus.Where(r =>
            r.ProcessStageName == "Incoming" ||
            r.ProcessStageName == "Not Ready for Processing" ||
            r.ProcessStageName == "Hold");
    }

    public static IQueryable<RequestStatus> WhereAttachmentsNotMet(this IQueryable<RequestStatus> requestStatus)
    {
        return requestStatus.Where(r => r.AttachmentStatus != (int)AttachmentStatus.HasAllAttachments && r.AttachmentStatus != (int)AttachmentStatus.NoAttachmentsRequired && r.AttachmentStatus != (int)AttachmentStatus.NeedsApproval);
    }

    public enum AttachmentStatus
    {
        HasAllAttachments = 0,        // Represents a status where all required attachments are present
        MissingSomeAttachments = 1,   // Represents a status where some attachments are missing
        MissingAllAttachments = 2,    // Represents a status where all attachments are missing
        NoAttachmentsRequired = 3,    // Represents a status where no attachments are required
        NeedsApproval = 4             // Represents a status where the attachments need approval
    }



}
