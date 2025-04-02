namespace MyDMVpro.Models.FormsViewModels
{
    public static class FileLibraryPageNames
    {
        public const string Index = "Index";
    }
    public static class VendorPageNames
    {
        public const string ToDo = "ToDo";
        public const string ToDoLC = "ToDoLC";
        public const string ToDoLI = "ToDoLI";
        public const string ToDoTC = "ToDoTC";
        public const string ToDoOther = "ToDoOther";
        public const string Deletes = "Deletes";
        public const string Holds = "Holds";
        public const string Master = "Master";
        public const string Completed = "Completed";
        public const string Accounting = "Accounting";
        public const string Audits = "Audits";
        public const string AuditLookup = "AuditLookup";
        public const string Reports = "Reports";
        public const string Abstracts = "Abstracts";
        public const string NeedToProcess = "NeedToProcess";
        public const string RequestLookup = "RequestLookup";
        public const string FollowUps = "FollowUps";
        public const string FormAnalyzer = "FormAnalyzer";
    }
    public static class EndUserPageNames
    {
        public const string Deletes = "Deletes";
        public const string Holds = "Holds";
        public const string Master = "Master";
        public const string Completed = "Completed";
        public const string Accounting = "Accounting";
        public const string AuditLookup = "AuditLookup";
        public const string Reports = "Reports";
        public const string Abstracts = "Abstracts";
        public const string FileLibrary = "FileLibrary";
    }
    /// <summary>
    /// Ordering of values does not matter
    /// as they are used only within C# code
    /// and referenced by name.
    /// </summary>
    public enum VendorViewType
    {
        UploadPending,

        RT_Incoming,
        RT_Sign,
        RT_NotReadyToProcess,
        RT_ReadyToProcess, /* new name for RT_Print */
        RT_InProcessing,
        RT_ReadyForPrinting,
        RT_ReadyForPacking, /* new name for RT_ShipToDmv */
        RT_WVSendQueue,
        RT_WVRejections,
        RT_TitlePending,
        RT_InTransit,
        RT_Receiving,
        RT_ShipToLH,

        LI_Incoming, /* new name for LI_Request */
        LI_NotReadyToProcess,
        LI_ReadyToProcess,
        LI_InProcessing,
        LI_ReadyForPrinting,
        LI_ReadyForPacking,
        LI_Pending,
        LI_Completed,

        Active,
        Abstracts,

        LCLI_Request,
        LCLI_Pending,

        LC_Request,
        LC_Completed,
        LC_Hold,
        LC_ReadyForPrinting,

        /* Accounting related */
        UnbilledList,
        InvoiceList,
        PaidInvoiceList,
        Accounting,
        Disbursements,
        SLAReport,

        Completed,
        Master,
        Deletes,
        Holds,

        /* ToDoTC */
        TC_Incoming,
        TC_NotReadyToProcess,
        TC_ReadyToProcess,
        TC_InProcessing,
        TC_ReadyForPrinting,
        TC_ReadyForPacking,
        TC_Pending,
        TC_Completed,
        WorkingList,

        /* ToDoOther */
        Other_Incoming,
        Other_NotReadyToProcess,
        Other_ReadyToProcess,
        Other_InProcessing,
        Other_ReadyForPrinting,
        Other_ReadyForPacking,
        Other_Pending,
        Other_Completed,

        ShippingReports,
        FormAnalyzer,
        //TitlesReceivedToday,
        RequestLookup,
        Cancelled,
        LinkedRequests,
        LinkSearch,
        FollowUps,
        RequestFollowUps,
        RequestTags,
        FollowUpCodes,
        RequestCodes,
        BulkAttachmentsForDownload,
        BulkApplicationsForDownload,
        BulkChecksForDownload,

        /* Disbursment related */
        DisbursementVoided,
        DisbursementPending,
        DisbursementCleared,

        Reports_Active,
        Reports_Completed,
        Reports_Holds,
        Reports_IncomingHolds,

        /*Audits related */
        Audits,
        PCodeAudit,
        HoldAudit,
        IncomingAudit,
        FollowupAudit,

        /*Audits related */
        MQ_Review,
        MQ_NotReadyToAccept,
        MQ_FollowUpReview,
        MQ_AcceptedIncoming,
        MQ_AcceptedNotReadyToProcess,
        MQ_Accepted_FollowUpReview,
        MQ_ReadyToProcess,
        MQ_CouldNotProcess,
        MQ_ProcessedReconcile,
        MQ_Completed,
        MQ_ReturnedTemporarily,

        /*Need To Process*/
        NTP_AllIncoming,

        /* Payment Reporting */
        Payment_Reporting,

        /*Documents Received - No Request */
        DocumentsReceived_NoRequest,
        DocumentsShipped_NoRequest
    }
}
