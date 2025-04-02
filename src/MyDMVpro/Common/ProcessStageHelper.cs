using System;
using MyDMVpro.Controllers;
using MyDMVpro.Models;

namespace MyDMVpro.Common
{
    public static class ProcessStageHelper
    {
        public class ProcessStageMessageViewModel
        {
            public string Message { get; set; }
            public string Stage { get; set; }
            public string Label { get; set; }
        }

        public static ProcessStageMessageViewModel GetProcessStageMessage(int stageId)
        {
            switch (stageId)
            {
                case (int)ProcessStageIDs.Incoming:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Incoming",
                        Message = "We received your request at <Time> on <Date>. A member of our team will be reviewing your request shortly and confirming if you need to provide any additional documents or details. Please refer to the 'Data or Documents Needed' section below to see if we have identified any items already."
                    };

                case (int)ProcessStageIDs.NotReadyForProcessing:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Not Ready to Process",
                        Message = "This application was received on <date>, but we are still in need of documents or data to process the request. Please refer to the “Data or Documents Needed” section below and provide the requested items. Once we receive these items, our team will get your request processed ASAP! \uD83D\uDE0A"
                    };

                case (int)ProcessStageIDs.ReadyToProcess:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Ready to Process",
                        Message = "This application is ready to process! Our team will be processing this request for you as soon as possible. Check back here soon for an updated ETA!"
                    };

                case (int)ProcessStageIDs.InProcessing:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "In Processing",
                        Message = "This application is currently being processed! Our team is working on it as soon as possible. Check back here soon for an updated ETA!"
                    };

                case (int)ProcessStageIDs.ReadyForPrinting:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Ready for Printing",
                        Message = "This application is ready to be printed! Our team will process this stage shortly. Check back here soon for updates!"
                    };

                case (int)ProcessStageIDs.ReadyForPacking:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Ready for Packing/Shipping",
                        Message = "This application is ready to be packed and shipped! Our team is finalizing the process. Check back here soon for updates!"
                    };

                case (int)ProcessStageIDs.TitlePending:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Title Pending",
                        Message = "This application has been processed by our office and is currently pending at the state. We have set the current ETA for <Client Facing ETA>. If anything changes, we will update the ETA as soon as possible. Check back here for updates!"
                    };


                case (int)ProcessStageIDs.InTransit:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "In Transit",
                        Message = "This application has been processed and your title has been issued. The title will be sent directly to your address by the state. Please let us know when you receive the title by sending us a chat, thank you!"
                    };

                case (int)ProcessStageIDs.Receiving:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Receiving",
                        Message = "This application has been processed and is being returned to our office from the state. We will send the title to <Auction> as soon as it is received. We will update this record with your secure tracking number as soon as the title is shipped (if applicable)."
                    };

                case (int)ProcessStageIDs.ShipToLienholder:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Ship to LH",
                        Message = "This application has been processed by the state and was received by our office on <date> at <time>. We will send the final title or documents to the final destination as soon as they are received. We will update this record with your secure tracking number as soon as they are shipped (if applicable)."
                    };

                case (int)ProcessStageIDs.Completed:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Complete",
                        Message = "This application was completed on <Date> and your title was shipped via <tracking>."
                    };

                case (int)ProcessStageIDs.Hold:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Hold",
                        Message = "This application is currently on hold. Please review the notes and chats from our team for next steps. – we may want to consider adding a hold-specific note here that will explain exactly what’s going on."
                    };

                default:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "",
                        Message = ""
                    };
            }
        }
        private static string ToLocalTime(DateTime? date)
        {
            return date?.ToLocalTime().ToString("hh:mm tt");
        }
        private static string ToLocalDate(DateTime? date)
        {
            return date?.ToLocalTime().ToString("MMMM dd, yyyy");
        }
        public static ProcessStageMessageViewModel GetProcessStageMessageAfterDynamicUpdates(int stageId, Requests requests)
        {

            switch (stageId)
            {
                case (int)ProcessStageIDs.Incoming:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Incoming",
                        Label = "Request Sent to Maggard",
                        Message = $"We received your request at {ToLocalTime(requests.DateToVendor)} on {ToLocalDate(requests.DateToVendor)}. A member of our team will be reviewing your request shortly and confirming if you need to provide any additional documents or details. Please refer to the 'Data or Documents Needed' section below to see if we have identified any items already.",
                    };

                case (int)ProcessStageIDs.NotReadyForProcessing:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Not Ready to Process",
                        Label = "Required Documents and Data Received",
                        Message = $"This application was received on {ToLocalDate(requests.DateToVendor)}, but we are still in need of documents or data to process the request. Please refer to the “Data or Documents Needed” section below and provide the requested items. Once we receive these items, our team will get your request processed ASAP! \uD83D\uDE0A",
                    };

                case (int)ProcessStageIDs.ReadyToProcess:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Ready to Process",
                        Label = "Required Documents and Data Received",
                        Message = "This application is ready to process! Our team will be processing this request for you as soon as possible. Check back here soon for an updated ETA!"
                    };

                case (int)ProcessStageIDs.InProcessing:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "In Processing",
                        Label = "Required Documents and Data Received",
                        Message = "This application is currently being processed! Our team is working on it as soon as possible. Check back here soon for an updated ETA!"
                    };

                case (int)ProcessStageIDs.ReadyForPrinting:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Ready for Printing",
                        Label = "Required Documents and Data Received",
                        Message = "Your application is ready to be processed. The application will receive a processed date and updated ETA as soon as we ship your application to the state."
                    };

                case (int)ProcessStageIDs.ReadyForPacking:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Ready for Packing/Shipping",
                        Label = "Required Documents and Data Received",
                        Message = "Your application is ready to be processed. The application will receive a processed date and updated ETA as soon as we ship your application to the state."
                    };

                case (int)ProcessStageIDs.TitlePending:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Title Pending",
                        Label = "Request processed by Maggard",
                        Message = $"This application has been processed by our office and is currently pending at the state. We have set the current ETA for {ToLocalDate(requests.LH_ETA)}. If anything changes, we will update the ETA as soon as possible. Check back here for updates!",
                    };


                case (int)ProcessStageIDs.InTransit:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "In Transit",
                        Label = "Request Processed by State",
                        Message = $"This application has been processed and the title was issued on {ToLocalDate(requests.DateTitleIssued)}. The title will be sent directly to your address by the state. Please let us know when you receive the title or documents by sending us a chat, thank you!"
                    };

                case (int)ProcessStageIDs.Receiving:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Receiving",
                        Label = "Title or Documents Received from State",
                        Message = "This application has been processed and is being returned to our office from the state. We will update this record with your secure tracking number as soon as the title is shipped (if applicable)."
                    };

                case (int)ProcessStageIDs.ShipToLienholder:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Ship to LH",
                        Label = "Request Completed",
                        Message = $"This application has been processed by the state and was received by our office on {ToLocalDate(requests.DateFromDmv)}. We will send the final title or documents to the final destination as soon as they are received. We will update this record with your secure tracking number as soon as they are shipped (if applicable)."
                    };

                case (int)ProcessStageIDs.Completed:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Complete",
                        Label = "Request Completed",
                        Message = $"This application was completed on {ToLocalDate(requests.DateShipped)} and your title was shipped via {requests.TrackingNumber} - need to confirm the actual field names for both of these."
                    };

                case (int)ProcessStageIDs.Hold:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "Hold",
                        Label = "",
                        Message = "This application is currently on hold. Please review the notes and chats from our team for next steps. – we may want to consider adding a hold specific note here that will explain exactly what’s going on."
                    };

                default:
                    return new ProcessStageMessageViewModel
                    {
                        Stage = "",
                        Message = ""
                    };
            }
        }
    }

}
