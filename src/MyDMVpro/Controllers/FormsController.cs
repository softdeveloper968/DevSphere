using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Models;
using System.ComponentModel;

namespace MyDMVpro.Controllers;

public enum ProcessStageIDs
{
    Pending = 1,
    Incoming = 2,
    Signing = 3,
    Print = 4,
    ReadyToBeProcessed = 4, /* new name for Print queue */
    ReadyToProcess = 4,
    SendToDMV = 5,
    ReadyForPacking = 5, /* new name for SendToDMV */
    ReceiveFromDMV = 6,
    Receiving = 6, /* new name for ReceiveFromDMV */
    ShipToLienholder = 7,
    SendToVendor = 8,
    TitlePending = 9,
    Invoicing = 10,
    InTransit = 11,
    NotReadyForProcessing = 12,
    NotReadyToProcess = 12,
    InProcessing = 13,
    ReadyForPrinting = 14,
    Completed = 15,
    WVRejections = 16,
    WVSendQueue = 9, /* Mapped to Title Pending for now.  It may become its own queue */
    Cancelled = 98,
    Hold = 99,
    MQ_Review = 60,
    MQ_NotReadyToAccept = 61,
    MQ_FollowUpReview = 62,
    MQ_AcceptedIncoming = 63,
    MQ_AcceptedNotReadyToProcess = 64,
    MQ_Accepted_FollowUpReview = 65,
    MQ_ReadyToProcess = 66,
    MQ_CouldNotProcess = 67,
    MQ_ProcessedReconcile = 68,
    MQ_Completed = 69,
    MQ_ReturnedTemporarily = 70,
    WorkingList = 110
}

/*
 * Order of queues for RT/DT
 * 
 * Incoming
 * Sign
 * NotReadyForProcessing
 * ReadyToBeProcessed
 * InProcessing
 * ReadyForPrinting
 * ReadyForPackingShipping
 * TitlePending
 * InTransit
 * Receiving
 * ShipToLienholder
 * 
 * */

public enum RequestStatusIDs
{
    Pending = 0,
    Active = 1,
    Complete = 2,
    Hold = 9,
    Cancelled = 10,
    DeletePending = 99,
    Deleted = 100
}

public class FormsController : BaseController
{
    public FormsController(MaggardDMVContext context, IConfiguration configuration, ILogger<FormsController> logger) : base(context, configuration, logger)
    {
    }
}
