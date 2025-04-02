using MyDMVpro.Controllers;
using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace MyDMVpro.Common
{
    public static class GlobalHelper
    {
        public static bool IsValidEmailAddress(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        public static string ProcessStageName(int? stageId)
        {
            return ProcessStageName((ProcessStageIDs?)stageId);
        }
        public static string ProcessStageName(ProcessStageIDs? stageId)
        {
            if (stageId == null) return "";
            switch (stageId)
            {
                case ProcessStageIDs.Pending: return "Pending";
                case ProcessStageIDs.Incoming: return "Incoming";
                case ProcessStageIDs.Signing: return "Signing";
                case ProcessStageIDs.ReadyToBeProcessed: return "Ready for Processing";
                case ProcessStageIDs.ReadyForPacking: return "Ready for Packing/Shipping";
                case ProcessStageIDs.ReceiveFromDMV: return "Receive From DMV";
                case ProcessStageIDs.ShipToLienholder: return "Ship to Lienholder";
                case ProcessStageIDs.SendToVendor: return "Ship To Vendor";
                case ProcessStageIDs.TitlePending: return "Title Pending";
                case ProcessStageIDs.Invoicing: return "Invoicing";
                case ProcessStageIDs.InTransit: return "In-Transit";
                case ProcessStageIDs.NotReadyForProcessing: return "Not Ready for Processing";
                case ProcessStageIDs.InProcessing: return "In Processing";
                case ProcessStageIDs.ReadyForPrinting: return "Ready for Printing";
                case ProcessStageIDs.MQ_Review: return "MQ_Review";
                case ProcessStageIDs.MQ_NotReadyToAccept: return "MQ_NotReadyToAccept";
                case ProcessStageIDs.MQ_FollowUpReview: return "MQ_FollowUpReview";
                case ProcessStageIDs.MQ_AcceptedIncoming: return "MQ_AcceptedIncoming";
                case ProcessStageIDs.MQ_AcceptedNotReadyToProcess: return "MQ_AcceptedNotReadyToProcess";
                case ProcessStageIDs.MQ_Accepted_FollowUpReview: return "MQ_Accepted_FollowUpReview";
                case ProcessStageIDs.MQ_ReadyToProcess: return "MQ_ReadyToProcess";
                case ProcessStageIDs.MQ_CouldNotProcess: return "MQ_CouldNotProcess";
                case ProcessStageIDs.MQ_ProcessedReconcile: return "MQ_ProcessedReconcile";
                case ProcessStageIDs.MQ_Completed: return "MQ_Completed";
                case ProcessStageIDs.MQ_ReturnedTemporarily: return "MQ_ReturnedTemporarily";
            }
            return "unknown";
        }
        // This is intended for the "title" of the stage, not the name of the stage
        // Name of stage is for the listview, title is for the header of the list
        public static string ProcessStageTitle(ProcessStageIDs? stageId)
        {
            if (stageId == null) return "";
            switch (stageId)
            {
                case ProcessStageIDs.Pending: return "Pending";
                case ProcessStageIDs.Incoming: return "Incoming";
                case ProcessStageIDs.Signing: return "Signing";
                case ProcessStageIDs.ReadyToBeProcessed: return "Ready to be Processed";
                case ProcessStageIDs.ReadyForPacking: return "Ready for Packing & Shipping";
                case ProcessStageIDs.ReceiveFromDMV: return "Receive From DMV";
                case ProcessStageIDs.ShipToLienholder: return "Ship to Lienholder";
                case ProcessStageIDs.SendToVendor: return "Ship To Vendor";
                case ProcessStageIDs.TitlePending: return "Title Pending";
                case ProcessStageIDs.Invoicing: return "Invoicing";
                case ProcessStageIDs.InTransit: return "In-Transit";
                case ProcessStageIDs.NotReadyForProcessing: return "Not Ready for Processing";
                case ProcessStageIDs.InProcessing: return "In Processing";
                case ProcessStageIDs.ReadyForPrinting: return "Ready for Printing";
                case ProcessStageIDs.MQ_Review: return "MQ_Review";
                case ProcessStageIDs.MQ_NotReadyToAccept: return "MQ_NotReadyToAccept";
                case ProcessStageIDs.MQ_FollowUpReview: return "MQ_FollowUpReview";
                case ProcessStageIDs.MQ_AcceptedIncoming: return "MQ_AcceptedIncoming";
                case ProcessStageIDs.MQ_AcceptedNotReadyToProcess: return "MQ_AcceptedNotReadyToProcess";
                case ProcessStageIDs.MQ_Accepted_FollowUpReview: return "MQ_Accepted_FollowUpReview";
                case ProcessStageIDs.MQ_ReadyToProcess: return "MQ_ReadyToProcess";
                case ProcessStageIDs.MQ_CouldNotProcess: return "MQ_CouldNotProcess";
                case ProcessStageIDs.MQ_ProcessedReconcile: return "MQ_ProcessedReconcile";
                case ProcessStageIDs.MQ_Completed: return "MQ_Completed";
                case ProcessStageIDs.MQ_ReturnedTemporarily: return "MQ_ReturnedTemporarily";
            }
            return "unknown";
        }
        public static List<string> GetColumnsToShow(string name)
        {
            switch (name.ToLower())
            {
                case "eta":
                    return new List<string> { "Stage", "Status", "Type", "State", "VIN", "R#", "Make", "Year", "Group", "RT-LH", "DT-LH", "ETA", "Sent To DMV", "Outcome", "Notes", "Internal", "Assign User" };
                case "pcode":
                    return new List<string> { "Stage", "Status", "Type", "State", "VIN", "R#", "Make", "Year", "Group", "RT-LH", "DT-LH", "ETA", "Sent To DMV", "Outcome", "Notes", "Internal", "Assign User" };
                case "hold":
                    return new List<string> { "Type", "State", "VIN", "R#", "Group", "LH Shipped to Vendor", "Outcome", "Notes", "Internal", "Assign User" };
                case "incoming":
                    return new List<string> { "Type", "State", "VIN", "R#", "Repo Date", "Odometer", "Group", "Outcome", "Notes", "Internal", "Assign User" };
                case "followup":
                    return new List<string> { "VIN", "R#", "Created", "Due Date", "Title", "Outcome", "Notes", "Internal", "Assign User" };
                default:
                    return new List<string>();
            }
        }

        public static string GetDocumentType(string key)
        {
            return key.ToLower() switch
            {
                "mv82" => "NY - MV82",
                "mv900" => "NY - MV900",
                "mv103" => "NY - MV103",
                "extra" => "Extra Important Document",
                "dtf" => "NY - Tax Document",
                "dl" => "NY - ID - Registrant",
                "insurance" => "NY - Insurance Card",
                "poa" => "NY - POA",
                "dr123" => "NY - State Reciprocity Form",
                "cov" => "NY - Title - PDF",
                "cot" => "NY - Title - PDF",
                "lease" => "NY - Bill of Sale or Lease Agreement",
                "rejected" => "NY - Rejected",
                _ => "Unknown", // Handle cases where the key is not recognized
            };
        }

        public static string GetConvertedPowerType(string powerTypeValue)
        {
            var powerTypeMappings = new Dictionary<string, string>
            {
                { "Bio Diesel", "B" },
                { "Diesel", "D" },
                { "Diesel Hybrid", "DH" },
                { "Electric", "L" },
                { "Flex Fuel", "F" },
                { "Gasoline", "G" },
                { "Hydrogen Fuel Cell", "H" },
                { "Plug-in Hybrid", "I" },
                { "Natural Gas", "N" },
                { "Propane", "P" },
                { "Gas/Electric Hybrid", "Y" }
            };
            // Return the mapped value if it exists, otherwise return the original value
            return powerTypeMappings.ContainsKey(powerTypeValue) ? powerTypeMappings[powerTypeValue] : powerTypeValue;
        }
    }
}
