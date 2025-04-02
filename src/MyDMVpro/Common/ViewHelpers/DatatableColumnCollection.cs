using System.Collections.Generic;

namespace MyDMVpro.Common.ViewHelpers
{
    public class DatatableColumnCollection : List<DatatableColumnInfo>
    {
        public DatatableColumnCollection() : base()
        {
        }
        public DatatableColumnInfo Add(string title)
        {
            return Add(title: title, @class: null, hoverText: null, filterField: null, filterClass: null);
        }
        public DatatableColumnInfo Add(string title, string @class)
        {
            return Add(title, @class, hoverText: null, filterField: null, filterClass: null);
        }
        public DatatableColumnInfo Add(string title, string @class, string hoverText = null, string filterField = null, string filterClass = null)
        {
            DatatableColumnInfo col = new DatatableColumnInfo()
            {
                Title = title,
                Class = @class,
                HoverText = hoverText,
                FilterField = filterField,
                FilterClass = filterClass
            };
            this.Add(col);
            return col;
        }
        public DatatableColumnInfo Add_AppType()
        {
            return Add("Type").SetClass("reqType afilter").SetFilterField("appType");
        }

        public DatatableColumnInfo Add_ChatAppType()
        {
            return Add("Type").SetClass("reqChatAppType afilter").SetFilterField("Type");
        }
        public DatatableColumnInfo Add_SpecialBilling()
        {
            return Add("SB")
                        .SetClass("reqSpecialBilling")
                        .SetFilterField("specialBilling")
                        .SetHoverText("Special Billing");
        }
        public DatatableColumnInfo Add_AppType(bool addHelp)
        {
            if (addHelp) Add_HelpLink();
            return Add("Type").SetClass("reqType afilter").SetFilterField("appType");
        }
        public DatatableColumnInfo Add_AppState()
        {
            return Add("State").SetClass("reqState afilter").SetFilterField("state");
        }
        public DatatableColumnInfo Add_AppStateAllStateFilter()
        {
            return Add("State").SetClass("reqState allStatesFilter");
        }
        public DatatableColumnInfo Add_HelpLink()
        {
            return Add("").SetClass("reqHelpLink").SetDataField("appType").SetExportable(false);
        }

        public DatatableColumnInfo Add_VIN()
        {
            // reqVIN has a custom search, so asearch not needed
            return Add("VIN").SetClass("reqVIN").SetDataField("vin");
        }
        public DatatableColumnInfo Add_SentToDMV()
        {
            return Add("Sent To DMV").SetClass("reqDateToDMV dsearch").SetDataField("dateToDMV");
        }

        public DatatableColumnInfo Add_Select()
        {
            return Add("").SetClass("reqChkbx").SetExportable(false);
        }
        public DatatableColumnInfo Add_Blank()
        {
            return Add("").SetClass("reqBlank").SetExportable(false);
        }
        public DatatableColumnInfo Add_Chat()
        {
            return Add("").SetClass("reqChat norowselect").SetExportable(false);
        }
        public DatatableColumnInfo Add_Links()
        {
            return Add("").SetClass("reqLinks norowselect").SetExportable(false);
        }
        public DatatableColumnInfo Add_Attachments()
        {
            return Add("").SetClass("reqAtt norowselect").SetExportable(false);
        }
        public DatatableColumnInfo Add_AutoIMS()
        {
            return Add("").SetClass("reqAutoIMS").SetExportable(false);
        }
        public DatatableColumnInfo Add_DupeCheck()
        {
            return Add("").SetClass("reqDupeCheck").SetExportable(false);
        }
        public DatatableColumnInfo Add_RepoDate()
        {
            return Add("Repo Date").SetClass("reqRepoDate emptyornot datefmt").SetDataField("repoDate");
        }
        public DatatableColumnInfo Add_Group()
        {
            return Add("Group").SetClass("reqGroup afilter").SetFilterField("groupName");
        }
        public DatatableColumnInfo Add_RT_LH()
        {
            return Add("RT-LH").SetClass("reqRTLH afilter").SetFilterField("rT_LH");
        }
        public DatatableColumnInfo Add_DT_LH()
        {
            return Add("DT-LH").SetClass("reqDTLH afilter").SetFilterField("dT_LH_Name");
        }
        public DatatableColumnInfo Add_LH()
        {
            return Add("LH").SetClass("reqLH afilter").SetFilterField("lienholderName");
        }
        public DatatableColumnInfo Add_SubmittedBy()
        {
            return Add("Submitted By").SetClass("reqSubmittedBy afilter").SetFilterField("submittedBy");
        }
        public DatatableColumnInfo Add_ClientRef()
        {
            return Add("Client Ref").SetClass("reqClientRef asearch").SetDataField("clientRef");
        }
        public DatatableColumnInfo Add_Vendor()
        {
            return Add("Vendor").SetClass("reqVendor").SetDataField("vendorCode");
        }


        public DatatableColumnInfo Add_Make()
        {
            return Add("Make").SetClass("reqMake asearch").SetDataField("vehicleMake");
        }
        public DatatableColumnInfo Add_Year()
        {
            return Add("Year").SetClass("reqYear asearch").SetDataField("vehicleYear");
        }
        public DatatableColumnInfo Add_Odometer()
        {
            return Add("Odometer").SetClass("reqOdometer asearch").SetDataField("odometer");
        }
        public DatatableColumnInfo Add_MileageBrand()
        {
            return Add("Brand").SetClass("reqMileageBrand asearch").SetDataField("mileageBrand");
        }
        public DatatableColumnInfo Add_SendToDMV()
        {
            return Add("Sent To DMV").SetClass("reqDateToDMV dsearch").SetDataField("dateToDMV");
        }
        public DatatableColumnInfo Add_TitleIssued()
        {
            return Add("Title Issued").SetClass("reqTitleIssued dsearch").SetDataField("titleIssued");
        }

        public DatatableColumnInfo Add_ETA()
        {
            return Add("ETA").SetClass("reqETA dsearch").SetDataField("eta");
        }
        public DatatableColumnInfo Add_LH_ETA()
        {
            return Add("ETA").SetClass("reqLH_ETA dsearch").SetDataField("lh_eta");
        }
        public DatatableColumnInfo Add_LH_Shipped_to_Vendor()
        {
            return Add("LH Shipped to Vendor").SetClass("reqDateToVendor dsearch").SetDataField("dateToVendor");
        }
        public DatatableColumnInfo Add_From_LH_Courier()
        {
            return Add("From LH Courier").SetClass("reqToVendorCourier asearch").SetDataField("toVendorCourier");
        }
        public DatatableColumnInfo Add_From_LH_Tracking()
        {
            return Add("From LH Tracking").SetClass("reqToVendorTracking asearch").SetDataField("toVendorTracking");
        }
        public DatatableColumnInfo Add_Notes()
        {
            return Add("Notes").SetClass("reqNotes asearch").SetDataField("notes");
        }
        public DatatableColumnInfo Add_Outcome()
        {
            return Add("Outcome").SetClass("outcomecomboboxColumn outcomesearch").SetIsCombobox(true).SetDataField("outcome");
        }
        public DatatableColumnInfo Add_NotesAudit()
        {
            return Add("Notes")
                .SetClass("NotesInputbox asearch")
                .SetExportable(false)
                .SetIsInputBox(true)
                .SetDataField("Notes");
        }

        public DatatableColumnInfo Add_NotesFollowUpAudit()
        {
            return Add("AuditNotes")
                .SetClass("AuditNotesInputbox asearch")
                .SetExportable(false)
                .SetIsInputBox(true)
                .SetDataField("AuditNotes");
        }
        public DatatableColumnInfo Add_Internal()
        {
            return Add("Internal")
                .SetClass("InternalInputbox asearch")
                .SetExportable(false)
                .SetIsInputBox(true)
                .SetDataField("Internal");
        }

        public DatatableColumnInfo Add_AssignedUser()
        {
            return Add("Assigned User").SetClass("assignedusercomboboxColumn usersearch").SetIsCombobox(true).SetDataField("assignedUser");
        }
        public DatatableColumnInfo Add_Code()
        {
            return Add("Code").SetClass("reqCode afilter").SetFilterField("code");
        }
        public DatatableColumnInfo Add_CodeCount()
        {
            return Add("").SetClass("reqCodeCount").SetFilterField("codeCount");
        }
        public DatatableColumnInfo Add_LHShippedToVendor()
        {
            return Add("LH Shipped to Vendor").SetClass("reqDateToVendor dfilter").SetFilterField("dateToVendor");
        }

        public DatatableColumnInfo Add_FromLHCourier()
        {
            return Add("From LH Courier").SetClass("reqToVendorCourier asearch");
        }
        public DatatableColumnInfo Add_FromLHTracking()
        {
            return Add("From LH Tracking").SetClass("reqToVendorTracking asearch");
        }
        public DatatableColumnInfo Add_DateFromLH()
        {
            return Add("Date from LH").SetClass("reqDateFromLH dsearch").SetDataField("dateReceived");
        }
        public DatatableColumnInfo Add_DateReceivedFromLH()
        {
            return Add("Date Received").SetClass("reqDateFromLH dsearch").SetDataField("dateReceived");
        }
        public DatatableColumnInfo Add_PrintDate()
        {
            return Add("Print Date").SetClass("reqPrinted dsearch").SetDataField("datePrinted");
        }
        public DatatableColumnInfo Add_FinishDate()
        {
            return Add("Finish Date").SetClass("reqFinished dsearch").SetDataField("dateShipped");
        }
        public DatatableColumnInfo Add_ShippedToAuction()
        {
            return Add("Shipped to Auction", "reqFinished dsearch").SetDataField("dateShipped");
        }
        public DatatableColumnInfo Add_Courier()
        {
            return Add("Courier").SetClass("reqCourier asearch").SetDataField("courier");
        }
        public DatatableColumnInfo Add_Tracking()
        {
            return Add("Tracking").SetClass("reqTracking asearch").SetDataField("trackingNumber");
        }

        public DatatableColumnInfo Add_Auction()
        {
            return Add("Auction").SetClass("reqAuctioneer afilter").SetFilterField("auction");
        }
        public DatatableColumnInfo Add_FromDMVTracking()
        {
            return Add("From DMV Tracking").SetClass("reqDmvTracking asearch").SetDataField("dmvTrackingNumber");
        }
        public DatatableColumnInfo Add_ReceivedFromDMV()
        {
            return Add("Received From DMV").SetClass("reqDateFromDMV dsearch").SetDataField("dateFromDmv");
        }
        public DatatableColumnInfo Add_DateToDMV()
        {
            return Add("Date to DMV", "reqDateToDMV dsearch").SetDataField("dateToDmv");
        }
        public DatatableColumnInfo Add_DateToVendor()
        {
            return Add("Date to Vendor").SetClass("reqDateToVendor dsearch").SetDataField("dateToVendor");
        }
        public DatatableColumnInfo Add_VendorToDMV()
        {
            return Add("Vendor to DMV").SetClass("reqVendorToDMV dsearch");
        }
        public DatatableColumnInfo Add_LIReceived()
        {
            return Add("LI Received").SetClass("reqLIDateFromDMV dsearch").SetDataField("lI_DateFromDmv");
        }

        public DatatableColumnInfo Add_LIRequested()
        {
            return Add("LI Requested").SetClass("reqLIDateToDMV dsearch").SetDataField("lI_DateToDmv");
        }
        public DatatableColumnInfo Add_LICheckNumber()
        {
            return Add("LI Check #").SetClass("reqLICheckNumber asearch").SetHoverText("LC Only");
        }
        public DatatableColumnInfo Add_LITrackingToDMV()
        {
            return Add("LI Tracking to DMV").SetClass("reqLItoDMVTracking").SetHoverText("LC Only");
        }

        public DatatableColumnInfo Add_EditIcon()
        {
            return Add("").SetClass("reqEditRequest norowselect").SetExportable(false);
        }
        public DatatableColumnInfo Add_Stage()
        {
            return Add("Stage").SetClass("reqStage stageFilter").SetFilterField("processStageId");
        }
        public DatatableColumnInfo Add_StageName()
        {
            return Add("Stage").SetClass("reqStageName afilter").SetFilterField("processStageName").SetDataField("processStageName");
        }
        public DatatableColumnInfo Add_Status()
        {
            return Add("Status").SetClass("reqStatus statusFilter").SetFilterField("statusId");
        }
        public DatatableColumnInfo Add_StatusName()
        {
            return Add("Status").SetClass("reqStatusName afilter").SetFilterField("statusName");
        }

        public DatatableColumnInfo Add_ELT()
        {
            return Add("ELT").SetClass("reqELT afilter");
        }
        //list.Columns.Add("ELT", "reqELT dsearch");

        public DatatableColumnInfo Add_LienExpiration()
        {
            return Add("Lien Expiration").SetClass("reqLienExpDate dsearch");
        }

        public DatatableColumnInfo Add_ReceivedFromLH()
        {
            return Add("Received From LH").SetClass("reqDateFromLH dsearch").SetDataField("dateReceived");
        }

        public DatatableColumnInfo Add_NotReadyToAccept()
        {
            return Add("Not Ready To Accept Date").SetClass("reqDateFromNRA dsearch").SetDataField("notReadyToAccept");
        }

        public DatatableColumnInfo Add_AcceptedDate()
        {
            return Add("Accepted Date").SetClass("reqDateFromAD dsearch").SetDataField("acceptedDate");
        }
        public DatatableColumnInfo Add_NotReadyToProcess()
        {
            return Add("Not Ready To Process Date").SetClass("reqDateFromNRP dsearch").SetDataField("notReadyToProcess");
        }

        public DatatableColumnInfo Add_ReturnedDate()
        {
            return Add("Returned Date").SetClass("reqDateFromRD dsearch").SetDataField("returnedDate");
        }

        public DatatableColumnInfo Add_DateSigned()
        {
            return Add("Date Signed").SetClass("reqSigned dsearch");
        }
        public DatatableColumnInfo Add_Modified()
        {
            return Add("Modified").SetClass("reqLastMod dsearch nowrap").SetExportable(false);
        }
        public DatatableColumnInfo Add_ModifiedBy(bool filter)
        {
            return Add("Modified By").SetClass("reqLastModBy afilter").SetFilterField(filter ? "lastModifiedBy" : null).SetExportable(false);
        }
        public DatatableColumnInfo Add_RequestId()
        {
            return Add("RequestId").SetClass("reqId").SetExportable(false);
        }

        public DatatableColumnInfo Add_CheckNumber()
        {
            return Add("Check #").SetClass("reqCheckNumber asearch");
        }
        public DatatableColumnInfo Add_TrackingNumber()
        {
            return Add("Tracking #").SetClass("reqDmvTracking");
        }
        public DatatableColumnInfo Add_DateFromDMV()
        {
            return Add("Date from DMV").SetClass("reqDateFromDMV dsearch").SetDataField("dateFromDmv");
        }
        public DatatableColumnInfo Add_LItoDMV()
        {
            return Add("LI to DMV").SetClass("reqLIDateToDMV dsearch").SetHoverText("LC Only");
        }
        public DatatableColumnInfo Add_RequestDate()
        {
            return Add("Request Date").SetClass("reqDateToVendor dsearch").SetDataField("dateToVendor");
        }

        public DatatableColumnInfo Add_RNo()
        {
            // reqNo has its own search, so asearch not needed
            return Add("R#").SetClass("reqNo").SetDataField("requestNo");
        }

        public DatatableColumnInfo Add_EditStatus()
        {
            return Add("").SetClass("reqEditStatus").SetExportable(false);
        }

        public DatatableColumnInfo Add_InvNumber()
        {
            return Add("Inv #").SetClass("reqInvNo asearch");
        }
        public DatatableColumnInfo Add_InvDate()
        {
            return Add("Inv Date").SetClass("reqInvDate dsearch");
        }
        public DatatableColumnInfo Add_InvPaid()
        {
            return Add("Inv Paid").SetClass("reqInvPaid dsearch");
        }
        public DatatableColumnInfo Add_SvcFee()
        {
            return Add("Svc Fee").SetClass("reqInvSvcFee currency nowrap");
        }
        public DatatableColumnInfo Add_DmvFee()
        {
            return Add("Dmv Fee ").SetClass("reqInvDmvFee currency nowrap");
        }
        public DatatableColumnInfo Add_OtherFee()
        {
            return Add("Other Fee").SetClass("reqInvOthFee currency nowrap");
        }
        public DatatableColumnInfo Add_OtherDesc()
        {
            return Add("Other Desc").SetClass("reqInvOthDesc");
        }
        public DatatableColumnInfo Add_TotalDue()
        {
            return Add("Total Due").SetClass("reqInvTtlDue currency nowrap");
        }
        public DatatableColumnInfo Add_DateRequested()
        {
            return Add("Date Requested").SetClass("reqDateToDMV dfilter");
        }
        public DatatableColumnInfo Add_DateReceivedFromDMVAtMaggardOffice()
        {
            return Add("Date Received from DMV at Maggard Office", "reqDateFromDMV dsearch");
        }
        public DatatableColumnInfo Add_DateReceived()
        {
            return Add("Date Received").SetClass("reqDateFromDMV dfilter");
        }

        public DatatableColumnInfo Add_DateToLH()
        {
            return Add("Date to LH").SetClass("reqDateToVendor dfilter");
        }
        public DatatableColumnInfo Add_TrackingToDMV()
        {
            return Add("Tracking to DMV").SetClass("reqDmvTracking asearch");
        }
        public DatatableColumnInfo Add_RejectionDate()
        {
            return Add("Rejection Date").SetClass("reqRejection asearch");
        }
        public DatatableColumnInfo Add_LCRequested()
        {
            return Add("LC Requested").SetClass("reqDateToDMV asearch");
        }
        public DatatableColumnInfo Add_LCCheckNumber()
        {
            return Add("LC Check #").SetClass("reqCheckNumber asearch");
        }
        public DatatableColumnInfo Add_LCTrackingToDMV()
        {
            return Add("LC Tracking to DMV").SetClass("reqDmvTracking asearch");
        }
        public DatatableColumnInfo Add_REG_LH_Name()
        {
            return Add("REG-LH-Name").SetClass("reqREG_LH_Name asearch");
        }
        public DatatableColumnInfo Add_REG_Reg_Name()
        {
            return Add("REG-Reg-Name").SetClass("reqREG_Reg_Name asearch");
        }
        public DatatableColumnInfo Add_ProcessingDay()
        {
            return Add("Day(1or2)").SetClass("processingDaysearchcomboboxColumn processingDaysearch").SetIsCombobox(true).SetDataField("processingDay").SetFilterField("processingDay");
        }
        public DatatableColumnInfo Add_AssignedProcessor()
        {
            return Add("Assigned Processor").SetClass("assignedprocessorcomboboxColumn usersearch").SetIsCombobox(true).SetDataField("assignedProcessor");
        }
        public DatatableColumnInfo Add_AssignedShipper()
        {
            return Add("Assigned Shipper").SetClass("assignedshippercomboboxColumn usersearch").SetIsCombobox(true).SetDataField("assignedShipper");
        }
        public DatatableColumnInfo Add_AttachmentType()
        {
            return Add("Attachment Type").SetClass("reqAttachmentType").SetDataField("reqAttachmentType");
        }

        public DatatableColumnInfo Add_HardcopyOrDigital()
        {
            return Add("Hardcopy or Digital").SetClass("reqHardcopyOrDigital").SetDataField("HardcopyOrDigital");
        }

        public DatatableColumnInfo Add_FieldType()
        {
            return Add("Field Type").SetClass("reqFieldType").SetDataField("FieldType");
        }

        public DatatableColumnInfo Add_Issue()
        {
            return Add("Issue").SetClass("reqIssue").SetDataField("Issue");
        }

        public DatatableColumnInfo Add_SuggestedResolution()
        {
            return Add("Suggested Resolution").SetClass("reqSuggestedResolution").SetDataField("SuggestedResolution");
        }

        public DatatableColumnInfo Add_Field()
        {
            return Add("Field").SetClass("reqField").SetDataField("field");
        }

        public DatatableColumnInfo Add_RequestCode()
        {
            return Add("Field").SetClass("reqMissingRequestCode").SetDataField("field");
        }
        public DatatableColumnInfo Add_Resolution()
        {
            return Add("Resolution").SetClass("reqResolution").SetDataField("resolution");
        }
        public DatatableColumnInfo Add_RequestNote()
        {
            return Add("Note").SetClass("reqMissingRequestNote").SetDataField("note");
        }
        public DatatableColumnInfo Add_NewFieldValue()
        {
            return Add("New Field Value").SetClass("reqNewFieldValue").SetDataField("NewFieldValue");
        }

        public DatatableColumnInfo Add_NewFieldNote()
        {
            return Add("New Field Note").SetClass("reqNewFieldNote").SetDataField("NewFieldNote");
        }

        public DatatableColumnInfo Add_PaymentAmount()
        {
            return Add("Payment Amount").SetClass("reqPaymentAmount").SetDataField("Amount");
        }

        public DatatableColumnInfo Add_PaymentRefNumber()
        {
            return Add("Reference Number").SetClass("reqRefNumber").SetDataField("ReferenceNumber");
        }

        public DatatableColumnInfo Add_PaymentRecordNumber()
        {
            return Add("Record Number(R#)").SetClass("reqRecordNumber").SetDataField("PaymentId");
        }

        public DatatableColumnInfo Add_PaymentDate()
        {
            return Add("Payment Date").SetClass("reqPaymentDate dsearch").SetDataField("paymentDate");

        }

        public DatatableColumnInfo Add_PaymentType()
        {
            return Add("Payment Type").SetClass("reqPaymentType").SetDataField("PaymentTypeID");
        }

        public DatatableColumnInfo Add_NoRequest_Notes()
        {
            return Add("Notes").SetClass("reqNoRequestsNotes").SetDataField("notes");
        }

        public DatatableColumnInfo Add_NoRequest_Document()
        {
            return Add("Document Name").SetClass("reqDocumentName").SetDataField("DocumentName");
        }

        public DatatableColumnInfo Add_ShippingNote()
        {
            return Add("Shipped Note").SetClass("reqShippedNote").SetDataField("shippedNote");
        }

        public DatatableColumnInfo Add_DateShipped()
        {
            return Add("Date Shipped").SetClass("reqDateShipped dsearch").SetDataField("dateShipped");
        }

        public DatatableColumnInfo Add_Communication_Chat()
        {
            return Add("").SetClass("reqCommunicationChat norowselect").SetExportable(false);
        }

        public DatatableColumnInfo Add_TagName()
        {
            return Add("Tag Name").SetClass("reqTagName norowselect").SetDataField("tagName");
        }

        public DatatableColumnInfo Add_RequestNumber()
        {
            return Add("Request Number").SetClass("reqRequestNumber norowselect").SetDataField("requestNumber");
        }

    }

    public class DatatableButtonCollection : List<DatatableButtonInfo>
    {
        public DatatableButtonCollection() : base()
        {
        }
        public void Add(string buttonID)
        {
            DatatableButtonInfo col = new DatatableButtonInfo()
            {
                id = buttonID
            };
            this.Add(col);
        }
    }
}
