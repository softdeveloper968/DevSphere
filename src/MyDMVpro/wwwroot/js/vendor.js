"use strict";

var vendorUnbilled_Table;
var vendorInvoiceBuilder_Table;
var vendorInvoices_Table;
var vendorCurrentInvoiceIds = [];
var vendorUnbilled_Items = [];
var vendorBulkPrint_Items = [];
var vendorBulkMerge_Items = [];
var vendorInvoiceBuilder_Items = [];
var billToInfo_Items = [];
var auditTimerInterval;
var auditTimerIntervals = {};
var etaId = "DT_Audits";
var pcodeId = "DT_PCodeAudit";
var holdId = "DT_HoldAudit";
var incomingId = "DT_IncomingAudit";
var followUpId = "DT_FollowupAudit";
var dataToAttached;
const ProcessStageIDs = {
    Pending: 1,
    Incoming: 2,
    Signing: 3,
    Print: 4,
    ReadyToProcess: 4 /* new name for Print queue */,
    SendToDMV: 5,
    ReadyForPacking: 5 /* new name for SendToDMV */,
    ReceiveFromDMV: 6,
    Receiving: 6 /* new name for ReceiveFromDMV */,
    ShipToLienholder: 7,
    SendToVendor: 8,
    TitlePending: 9,
    Invoicing: 10,
    InTransit: 11,
    NotReadyToProcess: 12,
    InProcessing: 13,
    ReadyForPrinting: 14,
    Completed: 15 /* just for LC/LI/TC/Other */,
    WVRejections: 16 /* just for WV */,
    WVSendQueue: 17 /* just for WV */,
    HOLD: 99 /* actually a status, not a stage */,
    CANCELLED: 98 /* actually a status, not a stage */,
    // Manual Queue
    MQ_Review: 60,
    MQ_NotReadyToAccept: 61,
    MQ_FollowUpReview: 62,
    MQ_AcceptedIncoming: 63,
    MQ_AcceptedNotReadyToProcess: 64,
    MQ_Accepted_FollowUpReview: 65,
    MQ_ReadyToProcess: 66,
    MQ_CouldNotProcess: 67,
    MQ_ProcessedReconcile: 68,
    MQ_Completed: 69,
    MQ_ReturnedTemporarily: 70,
};

function containsItems(ary) {
    return typeof ary !== "undefined" && ary.length > 0;
}
function vendorSetData(data) {
    // putting the data to the pop up div
    $("#PopupDiv").html(data);
    // viewing the div as block
    $("#PopupDiv").css("display", "block");
}
function vendorCloseWindow() {
    $("#PopupDiv").css("display", "none");
}
function lienholderShowNotes(d, tr, row) {
    var formData = new FormData();

    formData.append("id", d.requestId);
    $.ajax({
        url: "/Requests/ViewNotes",
        type: "POST",
        async: true,
        data: formData,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
    }).done(function (data) {
        row.child(data).show();
        var form = row.child().find(".notesForm").first();
        $(form)
            .find(".btn-close")
            .off("click")
            .on("click", function (e) {
                row.child().hide();
                tr.removeClass("shown");
            });
    });
}

function vendorShowNotes(d, tr, row) {
    var formData = new FormData();

    formData.append("id", d.requestId);
    $.ajax({
        url: "/Requests/EditNotes",
        type: "POST",
        async: true,
        data: formData,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
    }).done(function (data) {
        row.child(data).show();
        var form = row.child().find("form").first();
        $(form)
            .find(".btn-close")
            .off("click")
            .on("click", function (e) {
                row.child().hide();
                tr.removeClass("shown");
            });

        $(form)
            .off("submit")
            .on("submit", function (e) {
                e.preventDefault();
                disableFormSubmit(form);
                $.ajax({
                    url: "/Requests/UpdateNotes",
                    type: "post",
                    data: form.serialize(),
                    headers: {
                        RequestVerificationToken: $(
                            '[name="__RequestVerificationToken"]'
                        ).val(),
                    },
                    //success: function (data) {
                    //    if (data.status === 'error') {
                    //        alert(data.message);
                    //    }
                    //    else {
                    //        var list = $("#billToList");
                    //        billToListData = data;
                    //        populateBillToListOptions(data, list);
                    //    }
                    //},
                    success: function (data) {
                        if (data.success === true) {
                            jqToast.success({
                                text: "Notes updated successfully",
                            });
                            vendorShowNotes(d, tr, row);
                            // leave comments visible until close button clicked
                            //row.child().hide();
                            //tr.removeClass('shown');
                            //row.draw();
                        } else {
                            alert(data.message);
                        }
                        enableFormSubmit(form);
                    },
                    error: function (xhr) {
                        processError(xhr);
                        //TBD: where is btn from????
                        //btn.prop("disabled", true);
                        enableFormSubmit(form);
                    },
                });
            });
        tr.addClass("shown");
    });
}

function vendorShowFupNotes(d, tr, row) {
    if (d.notes.length > 20) {
        let $div = $('<div class="fupnote"></div>');
        $div.text(d.notes);
        row.child($div[0]).show();
        tr.addClass("shown");
    }
}
function disableFormSubmit(frm) {
    $(frm).find(":submit").prop("disabled", true);
}
function enableFormSubmit(frm) {
    $(frm).find(":submit").prop("disabled", false);
}
function processError(xhr, container) {
    var text = xhr.responseText || "";
    if ((text || "") === "") {
        vendor_show_alert(container, "Unknown error occurred", -1);
        return;
    }
    try {
        var obj = JSON.parse(text);
        if (obj) {
            vendor_show_alert(container, obj.message, -1);
        }
    } catch (err) {
        vendor_show_alert(container, text, -1);
    }
}
function vendor_show_Print(t) {
    var items = vendorGetSelectedItems(t);
    var html = "No items selected";
    var disabled = true;
    html =
        "Operation will mark the " +
        items.length +
        " selected items as printed";
    if (items.length !== 0) {
        disabled = false;
    }
    var $form = $("#modal-print");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    $form.find("#selectedCount").html(html);
    var today = moment().format("YYYY-MM-DD");
    $form.find("#datePrinted").val(today);
    $form.find("#datePrinted").prop("disabled", disabled);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                tables.push($("#DT_ReadyForPacking"));
                tables.push($("#DT_Receiving"));
                tables.push($("#DT_ReadyForPrinting"));
                var values = [
                    { name: "date", value: $form.find("#datePrinted").val() },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/PrintItems",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function exportItemsToCsv(t) {
    var exportItems = vendorGetSelectedItems(t);
    if (exportItems && exportItems.length !== 0) {
        var $form = $("#csvExportForm");
        $form.html("");
        $.each(exportItems, function (i, id) {
            $form.append(
                '<input type="hidden" name="ids" value="' + id + '" />'
            );
        });
        attachToken($form);
        $form.trigger("submit");
        $form.html("");
    }
}
function universalExportItemsToCsv(t) {
    var exportItems = vendorGetSelectedItems(t);
    if (exportItems && exportItems.length !== 0) {
        var $form = $("#csvUniversalExportForm");
        $form.html("");
        $.each(exportItems, function (i, id) {
            $form.append(
                '<input type="hidden" name="ids" value="' + id + '" />'
            );
        });
        attachToken($form);
        $form.trigger("submit");
        $form.html("");
    }
}
function attachToken(form) {
    var token = $('[name="__RequestVerificationToken"]').val();
    var rvt = form.find('[name="__RequestVerificationToken"]').html();
    if (typeof rvt === "undefined") {
        form.append(
            '<input type="hidden" name="__RequestVerificationToken" value="' +
                token +
                '" />'
        );
    }
}
function exportInvoicesToCsv(t) {
    var exportItems = vendorGetSelectedInvoices(t);
    if (exportItems && exportItems.length !== 0) {
        var form = $("#csvExportInvoicesForm");
        form.html("");
        $.each(exportItems, function (i, id) {
            form.append(
                '<input type="hidden" name="ids" value="' + id + '" />'
            );
        });
        attachToken(form);
        form.trigger("submit");
        form.html("");
    }
}
function vendor_show_MarkFinished(t) {
    var items = vendorGetSelectedItems(t);
    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-markFinished");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as finished";
        $form.find("#selectedCount").html(html);
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");

    $form.find("#date").val(today);
    $form.find("#date").prop("disabled", disabled);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data)) return;

                var values = [
                    { name: "date", value: data.date },
                    { name: "code", value: data.code },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "chat", value: data.chat },
                ];

                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/MarkAsFinished",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_SubmitToChamps(t) {
    // TBD: call champs endpoint
    var items = vendorGetSelectedItems(t);
    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-sendToChamps");
    vendor_init_dialog($form);
    if (items.length !== 0) {
        html =
            "Operation will send the " +
            items.length +
            " selected items to the WV Clearinghouse";
        $form.find("#selectedCount").html(html);
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [
                    $("#DT_ReadyForPacking"),
                    $("#DT_TitlePending"),
                    $("#DT_WVRejections"),
                ];
                vendorInitiateChampRequest_WithTitle_Action($form, t, tables);
            });
    }
    $form.modal("show");
}
function vendorInitiateChampRequest_WithTitle_Action($form, t, refreshList) {
    let items = vendorGetSelectedItemObjects(t);

    var formData = new FormData();

    $(items).each(function (i, v) {
        formData.append("requestIds", v.requestId);
    });

    $.ajax({
        url: "/Vendor/InitiateChampRequest_WithTitle",
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        data: formData,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (data) {
        data = data || unknownErrorObj;
        $form.modal("hide");
        if (data.indexOf('"status":"error"') >= 0) {
            var obj = JSON.parse(data);
            vendor_show_DataTable_error(t, obj.message);
        } else {
            vendor_show_DataTable_success(t, "Submission initiated");
        }
        AjaxReloadList(refreshList, true);
    });
    return false;
}

function CalculateETA(shippedDate, businessDays) {
    let addedDays = 0;
    let etaDate = moment(shippedDate);

    while (addedDays < businessDays) {
        etaDate = etaDate.add(1, "days");

        if (etaDate.day() !== 6 && etaDate.day() !== 0) {
            addedDays++;
        }
    }

    return etaDate.format("YYYY-MM-DD");
}

function vendor_show_DmvShipItems(t) {
    var items = vendorGetSelectedItemObjects(t);
    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-shipDmv");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    console.log(items)
    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as shipped to DMV";
        $form.find("#selectedCount").html(html);
        disabled = false;
    }

    $form.find("#dmvCourierName").val("");
    $form.find("#dmvTrackingNumber").val("");
    $form.find("#toDmvTrackingNumber").val("");
    $form.find("#dmvCourierName").prop("disabled", disabled);
    $form.find("#dmvTrackingNumber").prop("disabled", disabled);
    var today = moment().format("YYYY-MM-DD");
    var eta = moment().add(11, "days");
    var calculatedEta = CalculateETA(today, items[0].etA_BusinessDays)

    $("#dmvEtaMessage").text(`ETA is set for ${items[0].etA_BusinessDays} business days`);

    $form.find("#dmvETA").val(calculatedEta);
    $form.find("#dmvETALabel").val(calculatedEta);
    $form.find("#dmvETA").prop("disabled", disabled);
    $form.find("#dateToDmv").val(today);
    $form.find("#dateToDmv").prop("disabled", disabled);
    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                let dmvTrackingNumber = $form
                    .find("#dmvTrackingNumber")
                    .val()
                    .trim();
                let toDmvTrackingNumber = $form
                    .find("#toDmvTrackingNumber")
                    .val()
                    .trim();

                if (dmvTrackingNumber === "" || toDmvTrackingNumber === "") {
                    vendor_show_alert($form, "Tracking numbers are required");
                    return;
                }
                if (dmvTrackingNumber === toDmvTrackingNumber) {
                    vendor_show_alert(
                        $form,
                        "Tracking numbers cannot be the same"
                    );
                    return;
                }

                var tables = [t, $("#DT_TitlePending"), $("DT_Pending")];
                var values = [
                    { name: "date", value: $form.find("#dateToDmv").val() },
                    {
                        name: "courierName",
                        value: $form.find("#dmvCourierName").val(),
                    },
                    { name: "trackingNumber", value: dmvTrackingNumber },
                    { name: "toDmvTrackingNumber", value: toDmvTrackingNumber },
                    { name: "eta", value: $form.find("#dmvETA").val() },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/DmvShipItems",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_MultiGroup(t, groupNames) {
    var $form = $("#modal-multiGroupShipment");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    var mglist = $form.find("#multiGroupList");
    mglist.html($("<ul/>"));
    $.each(groupNames, function () {
        $("<li/>").text(this).appendTo(mglist);
    });
    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            $form.modal("hide");
            vendor_show_Ship(t, true);
        });
    $form.modal("show");
}
function vendor_show_Ship(t, force) {
    var items = vendorGetSelectedItems(t);
    var html = "No items selected";
    var disabled = true;

    force = force || false;

    /*
     * If requests from different groups are selected
     * display message to confirm
     */
    if (!force) {
        var groups = vendorGetDistinctSelectedItemValues(t, "groupName");
        if (groups.length > 1) {
            vendor_show_MultiGroup(t, groups);
            return;
        }
    }
    var $form = $("#modal-ship");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as shipped";
        $form.find("#selectedCount").html(html);
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");
    $form.find("#dateShipped").val(today);
    $form.find("#dateShipped").prop("disabled", disabled);

    $form.find("#courierName").val("");
    $form.find("#courierName").prop("disabled", disabled);
    $form.find("#trackingNumber").val("");
    $form.find("#trackingNumber").prop("disabled", disabled);
    $form.find("#genReport").prop("checked", true);

    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [
                    { name: "date", value: $form.find("#dateShipped").val() },
                    {
                        name: "courierName",
                        value: $form.find("#courierName").val(),
                    },
                    {
                        name: "trackingNumber",
                        value: $form.find("#trackingNumber").val(),
                    },
                ];
                var action = "/Vendor/ShipItems";
                if ($form.find("#genReport").prop("checked")) {
                    action = "/Vendor/GenerateShippingReport";
                }
                vendor_action_HELPER($form, items, action, tables, values);
            });
    }
    $form.modal("show");
}
function vendor_show_ReceiveFromDmv(t) {
    var disabled = true;
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var $form = $("#modal-ReceiveFromDmv");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as received from the DMV";
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");
    $form.find("#date").val(today);
    $form.find("#date").prop("disabled", disabled);
    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t, $("#DT_ShipToLH"), $("#DT_Completed")];

                var values = [
                    { name: "date", value: $form.find("#date").val() },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/ReceiveFromDmv",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendorGetDistinctSelectedItemValues(t, f) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        let v = this[f];
        if (!a.includes(v)) {
            a.push(v);
        }
    });
    return a;
}

function vendorGetSelectedItems(t) {
    return vendorGetSelectedItemValues(t, "requestId");
}
function vendorGetSelectedShipments(t) {
    return vendorGetSelectedItemValues(t, "shipmentId");
}
function vendorGetSelectedInvoices(t) {
    return vendorGetSelectedItemValues(t, "invoiceId");
}
function vendorGetSelectedItemValues(t, f) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push(this[f]);
    });
    return a;
}
function vendorGetSelectedItemObjects(t) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push(this);
    });
    return a;
}

function vendorGetShowItemObjects(t) {
    var a = [];
    $.each(t.rows({ selected: false }).data(), function () {
        a.push(this);
    });
    return a;
}

function reloadTable(t) {
    if (typeof t !== "undefined") {
        t.ajax.reload();
    }
}
function initCodeNoteRemarkChat(f) {
    f.find("#code").val("");
    f.find("#stage").val("");
    f.find("#codeNotRequired").prop("checked", false);
    f.find("#note").val("");
    f.find("#remark").val("");
    f.find("#chat").val("");
}
function getFieldValue(f, id, uppercase) {
    let s = f.find(id).val() || "";
    if (s !== null) {
        s = s.trim();
        if (uppercase || false) s = s.toUpperCase();
    }
    return s;
}

function getDateFieldValue(f, id, uppercase, isDate) {
    let s = f.find(id).val() || "";
    if (s !== null) {
        s = s.trim();
        if (isDate) {
            let date = new Date(s);
            if (!isNaN(date.getTime())) {
                return date.toISOString().split("T")[0]; // Returns YYYY-MM-DD format
            }
            return "";
        }
        if (uppercase) s = s.toUpperCase();
    }
    return s;
}

function getValidateFields(f) {
    let trakingNumber = getFieldValue(f, "#trackingNumber", true);
    let dateShipped = getFieldValue(f, "#dateShipped");
    let shippedNote = getFieldValue(f, "#shippedNote");

    return {
        trakingNumber: trakingNumber,
        dateShipped: dateShipped,
        shippedNote: shippedNote,
    };
}
function getCodeNoteRemarkChat(f) {
    let code = getFieldValue(f, "#code", true);
    let codeNotRequired = f.find("#codeNotRequired").prop("checked") || false;
    let note = getFieldValue(f, "#note");
    let remark = getFieldValue(f, "#remark");
    let chat = getFieldValue(f, "#chat");
    let date = getDateFieldValue(f, "#dateETA");
    return {
        code: code,
        note: note,
        remark: remark,
        date: date,
        codeNotRequired: codeNotRequired,
        chat: chat,
    };
}
function validFormFieldsForPending(
    f,
    data,
    codeRequired,
    noteRequired,
    remarkRequired,
    EtaRequired
) {
    if (typeof codeRequired === "undefined") codeRequired = true;
    noteRequired = noteRequired || false;
    remarkRequired = remarkRequired || false;
    EtaRequired = EtaRequired || false;

    if (codeRequired) {
        if (!validCodeProvided(data)) {
            vendor_show_alert(f, "Please provide a valid code");
            return false;
        }
    }
    if (noteRequired) {
        if (data.note === null || data.note === "") {
            vendor_show_alert(f, "Public facing note is required", 5000);
            return false;
        }
    }
    if (remarkRequired) {
        if (data.remark === null || data.remark === "") {
            vendor_show_alert(f, "Internal remark is required", 5000);
            return false;
        }
    }
    if (EtaRequired) {
        if (data.date === null || data.date === "") {
            vendor_show_alert(f, "ETA date is required", 5000);
            return false;
        }
    }
    return true;
}

function validShipFormFields(f, data, trakingNumber, dateShipped, shippedNote) {
    trakingNumber = trakingNumber || false;
    dateShipped = dateShipped || false;
    shippedNote = shippedNote || false;

    if (trakingNumber) {
        if (data.trakingNumber === null || data.trakingNumber === "") {
            vendor_show_alert(
                f,
                "Public facing tracking number is required",
                5000
            );
            return false;
        }
    }
    if (dateShipped) {
        if (data.dateShipped === null || data.dateShipped === "") {
            vendor_show_alert(
                f,
                "Public facing date Shipped is required",
                5000
            );
            return false;
        }
    }
    if (shippedNote) {
        if (data.shippedNote === null || data.shippedNote === "") {
            vendor_show_alert(f, "Internal shipped Note is required", 5000);
            return false;
        }
    }
    return true;
}

function validFormFields(f, data, codeRequired, noteRequired, remarkRequired) {
    if (typeof codeRequired === "undefined") codeRequired = true;
    noteRequired = noteRequired || false;
    remarkRequired = remarkRequired || false;

    if (codeRequired) {
        if (!validCodeProvided(data)) {
            vendor_show_alert(f, "Please provide a valid code");
            return false;
        }
    }
    if (noteRequired) {
        if (data.note === null || data.note === "") {
            vendor_show_alert(f, "Public facing note is required", 5000);
            return false;
        }
    }
    if (remarkRequired) {
        if (data.remark === null || data.remark === "") {
            vendor_show_alert(f, "Internal remark is required", 5000);
            return false;
        }
    }
    return true;
}

function validCodeProvided(data) {
    if (data.code === null || data.code === "") {
        if (data.codeNotRequired === true) {
            return true;
        }
        return false;
    }
    return true;
}
function vendor_show_DirectToBilling(t) {
    var items = vendorGetSelectedItems(t);
    var disabled = true;

    var $form = $("#modal-DirectToBilling");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    var html = "No items selected";
    if (items.length !== 0) {
        html =
            "Operation will move the " + items.length + " selected to billing";
        disabled = false;
    }
    $form.find("#selectedCount").html(html);

    if (items.length !== 0) {
        var vins = vendorGetSelectedItemValues(t, "vin");
        vendor_populate_vin_textarea(
            $form.find("#DirectToBilling_VinsToUpdate"),
            vins
        );
        var today = moment().format("YYYY-MM-DD");
        $form.find("#date").val(today);
    }
    $form.find("#date").prop("disabled", disabled);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                let data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data)) return;

                var values = [
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "date", value: data.date },
                    { name: "code", value: data.code },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    { name: "chat", value: data.chat },
                ];
                var tables = [t];

                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/DirectToBilling",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_MarkAsReceivedByLH(t) {
    var items = vendorGetSelectedItems(t);
    var disabled = true;

    var $form = $("#modal-MarkAsReceivedByLH");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    var html = "No items selected";
    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected as received by Lienholder and proceed to billing.";
        disabled = false;
    }
    $form.find("#selectedCount").html(html);

    if (items.length !== 0) {
        var vins = vendorGetSelectedItemValues(t, "vin");
        vendor_populate_vin_textarea(
            $form.find("#MarkAsReceivedByLH_VinsToUpdate"),
            vins
        );
        var today = moment().format("YYYY-MM-DD");
        $form.find("#date").val(today);
    }
    $form.find("#date").prop("disabled", disabled);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                let data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data)) return;

                var values = [
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "date", value: data.date },
                    { name: "code", value: data.code },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    { name: "chat", value: data.chat },
                ];
                var tables = [t];

                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/ReceivedByLH",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_populate_vin_textarea($ctl, vins) {
    var $ta = $('<textarea class="vin form-control"></textarea>');
    var text = "";
    $.each(vins, function () {
        text = text + this + "\r\n";
    });
    $ta.val(text);
    $ta.prop("readonly", true);
    $ctl.empty().append($ta);
}
function vendor_populate_vin_list($ctl, vins) {
    var $select = $('<select class="vin form-control" readonly></select>');
    $.each(vins, function () {
        $select.append("<option>" + htmlEncode(this) + "</option>");
    });
    $ctl.empty().append($select);
}
function copyText(selector) {
    var tbl = document.getElementById(selector);
    var copyText = document.getElementById(selector).innerText;

    navigator.clipboard.writeText(copyText);
}
var vin_rno_header_cnt = 0;
function vendor_set_single_vin_rno_header(form, txt, requestNo, vin) {
    var html = `<span style="float:left">${txt}</span><span style="float:right">R# ${requestNo}&nbsp;/&nbsp;${vin}</span>`;
    //<div style="float:left">Edit Follow-Up&nbsp;</div><div style="width:50%;display:inline-block;text-align:right" id="REQ_VIN_RNO_HEADER"></div>

    form.find("#REQ_VIN_RNO_HEADER").html(html);
    form.find("#REQ_MULTI_VIN_RNO_HEADER").html("");
}
function vendor_set_multi_vin_rno_title_header(form, items, txt) {
    var div = "";
    if (typeof items !== "undefined") {
        //<th class="fa fa-clipboard" onclick="copyText(&quot;${id}&quot;)">&nbsp;</th>
        var id = `vin_rno_header${vin_rno_header_cnt}`;
        div += `<div style="text-align:right;" width="100%"><button class="fa fa-clipboard float-right" title="Copy to clipboard" class="fa fa-clipboard" onclick="copyText(&quot;${id}&quot;)"></button></div>`;
        div += `<table id="${id}" class="vin_rno_header">`;
        div += `<thead><tr><th>R#</th><th>VIN</th><th>Title</th></tr></thead>`;
        $.each(items, function (idx, item) {
            div +=
                '<tr class="vin_rno_detail"><td>' +
                item.RequestNo +
                '</td><td><span class="vin">' +
                htmlEncode(item.Vin) +
                "</span></td><td>" +
                htmlEncode(item.Title) +
                "</td></tr>";
        });
        div += "</table>";
    }
    if (txt) {
        form.find("#REQ_VIN_RNO_HEADER").html(txt);
    } else {
        form.find("#REQ_VIN_RNO_HEADER").html("");
    }
    form.find("#REQ_MULTI_VIN_RNO_HEADER").html(div);
}
function vendor_set_multi_vin_rno_header(form, items, txt) {
    var div = "";
    if (typeof items !== "undefined") {
        //<th class="fa fa-clipboard" onclick="copyText(&quot;${id}&quot;)">&nbsp;</th>
        var id = `vin_rno_header${vin_rno_header_cnt}`;
        div += `<div style="text-align:right;" width="100%"><button class="fa fa-clipboard float-right" title="Copy to clipboard" class="fa fa-clipboard" onclick="copyText(&quot;${id}&quot;)"></button></div>`;
        div += `<table id="${id}" class="vin_rno_header">`;
        div += `<thead><tr><th>R#</th><th>VIN</th></tr></thead>`;
        $.each(items, function (idx, item) {
            if (item.requestNo) {
                div +=
                    '<tr class="vin_rno_detail"><td>' +
                    item.requestNo +
                    '</td><td><span class="vin">' +
                    htmlEncode(item.vin) +
                    "</span></td></tr>";
            }
        });
        div += "</table>";
    }
    if (txt) {
        form.find("#REQ_VIN_RNO_HEADER").html(txt);
    } else {
        form.find("#REQ_VIN_RNO_HEADER").html("");
    }
    form.find("#REQ_MULTI_VIN_RNO_HEADER").html(div);
}
/*
DT_Incoming => DT_NotReadyToProcess OR DT_ReadyToProcess
DT_Sign
DT_NotReadyToProcess => DT_ReadyToProcess
DT_ReadyToProcess => DT_InProcessing
DT_InProcessing => DT_ReadyForPrinting
DT_ReadyForPrinting => DT_ReadyForPacking
DT_ReadyForPacking => DT_TitlePending
DT_TitlePending => DT_InTransit
DT_InTransit => DT_Receiving
DT_Receiving => DT_ShipToLH
DT_ShipToLH

LC/LI/TC/Other have different paths

*/
function vendor_show_MoveToPending(t) {
    vendor_show_MoveToStage(t, "Pending", ProcessStageIDs.TitlePending, [
        $("#DT_TitlePending"),
        $("#DT_Pending"),
    ]);
}
function vendor_show_MoveToReadyForPacking(t) {
    vendor_show_MoveToStage(
        t,
        "Ready for Packing",
        ProcessStageIDs.ReadyForPacking,
        [$("#DT_ReadyForPacking")]
    );
}
function vendor_show_MoveToReadyForPrinting(t) {
    vendor_show_MoveToStage(
        t,
        "Ready for Printing",
        ProcessStageIDs.ReadyForPrinting,
        [$("#DT_ReadyForPrinting")]
    );
}
function vendor_show_MoveToInProcessing(t) {
    vendor_show_MoveToStage(t, "In Processing", ProcessStageIDs.InProcessing, [
        $("#DT_InProcessing"),
    ]);
}
function vendor_show_MoveToStage(t, stageName, stage, refreshlist) {
    var items = vendorGetSelectedItems(t);
    var disabled = true;
    var html = "No items selected";
    if (items.length !== 0) {
        html = "Operation will move the " + items.length + " selected";
        disabled = false;
    }
    var $form = $("#modal-moveToStage");
    initCodeNoteRemarkChat($form);
    $form.find("#moveItemsToStage").html(htmlEncode(stageName));
    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    $form.find("#date").hide();
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();

                var tables = [t];
                let data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data)) return;

                var values = [
                    { name: "stage", value: stage },
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "code", value: data.code },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    { name: "chat", value: data.chat },
                ];
                $.each(refreshlist, function (idx, table) {
                    tables.push(table);
                });
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/MoveItemsWithNotes",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
var vendor_alert_iteration = 0;
const alert_level_error = 0;
const alert_level_warning = 1;
const alert_level_info = 2;
const alert_level_success = 3;
function vendor_show_DataTable_error(t, msg, timeout) {
    vendor_show_DataTable_alert(t, msg, timeout, alert_level_error);
}
function vendor_show_DataTable_warning(t, msg, timeout) {
    vendor_show_DataTable_alert(t, msg, timeout, alert_level_warning);
}
function vendor_show_DataTable_success(t, msg, timeout) {
    vendor_show_DataTable_alert(t, msg, timeout, alert_level_success);
}
function vendor_show_DataTable_alert(t, msg, timeout, msglevel) {
    if (typeof timeout === "undefined") {
        timeout = timeout || 2500;
    }
    let iteration = ++vendor_alert_iteration;
    let ctl = $(t.table().container()).find("div.alert");
    if (ctl.length === 0) {
        alert(msg);
    } else {
        ctl.removeClass("alert-danger alert-info alert-warning alert-success");
        ctl.text("");
        if (msglevel === alert_level_info) {
            ctl.addClass("alert-info");
        } else if (msglevel === alert_level_warning) {
            ctl.addClass("alert-warning");
        } else if (msglevel === alert_level_success) {
            ctl.addClass("alert-success");
        } else {
            ctl.addClass("alert-danger");
        }
        if (timeout === 0) {
            ctl.addClass("alert-dismissable");
            ctl.find("button").remove();
            ctl.append(
                $(
                    '<button type="button" class="close" data-dismiss="alert" aria-hidden="true">×</button>'
                )
            );
        }
        ctl.data("iteration", iteration);
        ctl.text(msg);
        ctl.show(400);
        setTimeout(() => {
            // check against iteration to handle multiple alerts before time elapses
            if (ctl.data("iteration") === iteration) {
                ctl.hide(400);
            }
        }, timeout);
    }
}
function vendor_hide_alert(container) {
    let ctl = $(container).find("div.alert:first");
    if (ctl.length !== 0) {
        ctl.text("");
        ctl.hide();
    }
}
function vendor_show_alert(container, msg, timeout) {
    timeout = timeout || 2500;
    let iteration = ++vendor_alert_iteration;

    if (typeof container === "undefined") {
        alert(msg);
        return;
    }

    let ctl = $(container).find("div.alert:first");
    if (ctl.length === 0) {
        alert(msg);
    } else {
        ctl.addClass("alert-danger");
        ctl.data("iteration", iteration);
        ctl.text(msg);
        ctl.show(400);
        if (timeout >= 0) {
            setTimeout(() => {
                // check against iteration to handle multiple alerts before time elapses
                if (ctl.data("iteration") === iteration) {
                    ctl.hide(400);
                }
            }, timeout);
        }
    }
}
async function manualStages_move_button(t, header, stage) {
    var items = vendorGetSelectedItemObjects(t);
    var apptype = items[0].appType;
    var html = "No items selected";
    var disabled = true;
    if (items.length === 0) {
        return;
    }
    if (!vendor_CheckAppTypesAllSame(t)) {
        vendor_show_DataTable_alert(
            t,
            "Moving multiple application types not supported"
        );
        return;
    }

    disabled = false;
    var $form = $("#modal-move-manualbutton");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    $form.find("#selectedCount").html(`${items.length} items selected`);
    $form.find("#myModal-label").html(header);
    var requestId = items[0].requestId;

    $form.find("#requestId").val(requestId);
    initCodeNoteRemarkChat($form);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();

                let data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data, true, true)) return;

                vendor_init_dialog($form);
                var tables = [t];

                var values = [
                    { name: "code", value: data.code },
                    { name: "stage", value: stage },
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "chat", value: data.chat },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    { name: "reasonCancelled", value: null },
                    { name: "appType", value: apptype },
                ];

                if (stage === ProcessStageIDs.MQ_NotReadyToAccept) {
                    tables.push(
                        $("#DT_MQ_FollowUpReview"),
                        $("#DT_MQ_NotReadyToAccept")
                    );
                } else if (stage === ProcessStageIDs.MQ_FollowUpReview) {
                    tables.push(
                        $("#DT_MQ_NotReadyToAccept"),
                        $("#DT_MQ_AcceptedIncoming"),
                        $("#DT_MQ_FollowUpReview")
                    );
                } else if (stage === ProcessStageIDs.MQ_AcceptedIncoming) {
                    tables.push(
                        $("#DT_MQ_AcceptedNotReadyToProcess"),
                        $("#DT_MQ_ReadyToProcess"),
                        $("#DT_MQ_AcceptedIncoming")
                    );
                } else if (
                    stage === ProcessStageIDs.MQ_AcceptedNotReadyToProcess
                ) {
                    tables.push(
                        $("#DT_MQ_Accepted_FollowUpReview"),
                        $("#DT_MQ_AcceptedNotReadyToProcess")
                    );
                } else if (
                    stage === ProcessStageIDs.MQ_Accepted_FollowUpReview
                ) {
                    tables.push(
                        $("#DT_MQ_ReadyToProcess"),
                        $("#DT_MQ_AcceptedNotReadyToProcess"),
                        $("#DT_MQ_Accepted_FollowUpReview")
                    );
                } else if (stage === ProcessStageIDs.MQ_ReadyToProcess) {
                    tables.push(
                        $("#DT_MQ_ProcessedReconcile"),
                        $("#DT_MQ_CouldNotProcess"),
                        $("#DT_MQ_ReadyToProcess")
                    );
                } else if (stage === ProcessStageIDs.MQ_CouldNotProcess) {
                    tables.push(
                        $("#DT_MQ_ReadyToProcess"),
                        $("#DT_MQ_CouldNotProcess")
                    );
                } else if (stage === ProcessStageIDs.MQ_ProcessedReconcile) {
                    tables.push(
                        $("#DT_MQ_Completed"),
                        $("#DT_MQ_ProcessedReconcile")
                    );
                } else if (stage === ProcessStageIDs.MQ_Completed) {
                    tables.push(
                        $("#DT_MQ_FollowUpReview"),
                        $("#DT_MQ_Completed")
                    );
                } else if (stage === ProcessStageIDs.MQ_ReturnedTemporarily) {
                    tables.push(
                        $("#DT_MQ_NotReadyToAccept"),
                        $("#DT_MQ_FollowUpReview"),
                        $("#DT_MQ_AcceptedIncoming"),
                        $("#DT_MQ_AcceptedNotReadyToProcess"),
                        $("#DT_MQ_Accepted_FollowUpReview"),
                        $("#DT_MQ_ReadyToProcess"),
                        $("#DT_MQ_CouldNotProcess"),
                        $("#DT_MQ_ProcessedReconcile"),
                        $("#DT_MQ_Completed"),
                        $("#DT_MQ_ReturnedTemporarily"),
                        $("#DT_MQ_ReturnedTemporarily")
                    );
                }
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/MoveItemsWithNotes",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}

function vendor_Move_Pending(t, disableRefresh) {
    disableRefresh = disableRefresh || false;
    var items = vendorGetSelectedItemObjects(t);
    var disabled = true;
    var html = "No items selected";
    if (items.length !== 0) {
        html = "Operation will move the " + items.length + " selected";
        disabled = false;
    }
    if (items.length > 1) {
        html = "Move operation can only be done one request at a time";
        disabled = false;
    }
    var requestId = items[0].requestId;
    var vin = items[0].vin;
    var appType = items[0].appType;
    var appState = items[0].appState || "";
    var stageId = ProcessStageIDs.TitlePending;

    for (var idx = 1; idx < items.length; idx++) {
        if (items[idx].appType !== appType) {
            vendor_show_DataTable_alert(
                t,
                "Cannot move items of different app types"
            );
            return;
        }
    }
    var $form = $("#modal-moveTitlePending");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    vendor_set_multi_vin_rno_header($form, items);

    initCodeNoteRemarkChat($form);
    $form.find("#moveToStage").val("Title Pending");
    $form.find("#reasonCancelled").val("");
    $form.find("#reasonCancelledDiv").hide();
    $form.find("#moveToDiv").hide();
    $form.find("#requiredEtaDiv").show();

    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    $form.find("#okButton").off("click");
    var $validationForm = $form.find("form");

    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var data = getCodeNoteRemarkChat($form);
                if (
                    !validFormFieldsForPending(
                        $form,
                        data,
                        true,
                        true,
                        true,
                        true
                    )
                )
                    return;

                var values = [
                    { name: "code", value: data.code },
                    { name: "stage", value: stageId },
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "chat", value: data.chat },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    {
                        name: "reasonCancelled",
                        value: $form.find("#reasonCancelled").val(),
                    },
                    { name: "appType", value: appType },
                    { name: "eta", value: $form.find("#dateETA").val() },
                ];
                var stg = $form.find("#stage").val();

                var tables = [];
                if (!disableRefresh) {
                    tables.push(t);
                    tables.push($("#DT_TitlePending"), $("#DT_Pending"));
                }
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/MoveItemsWithNotesAndEtaToPending",
                    tables,
                    values
                );
            });
    }
    initChatForm($form.find(".chatsEditPanel"), requestId, vin);
    $form.modal("show");
}

function vendor_Mark_as_Associated(t, disableRefresh) {
    disableRefresh = disableRefresh || false;
    var items = vendorGetSelectedItemObjects(t);

    dataToAttached = items;
    var disabled = true;
    var html = "No items selected";
    if (items.length !== 0) {
        html = "Operation will move the " + items.length + " selected";
        disabled = false;
    }
    if (items.length > 1) {
        html = "Move operation can only be done one request at a time";
        disabled = false;
    }
    var requestId = items[0].requestId;
    var groupId = items[0].groupId;
    var vin = items[0].vin;
    var appType = items[0].appType;
    var appState = items[0].appState || "";

    const formData = new FormData();
    formData.append("vin", vin);
    formData.append("groupId", groupId);

    if (!disabled) {
        $.ajax({
            url: "/DocumentsReceivedNoRequest/GetDuplicateRecords",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
            cache: false,
            success: function (response) {
                showDuplicatePopup(response);
            },
            error: function (xhr, status, error) {
                console.error("Error fetching duplicate records:", error);
            },
        });
    }
}

function vendor_Mark_as_Shipped(t, disableRefresh) {
    disableRefresh = disableRefresh || false;
    var items = vendorGetSelectedItemObjects(t);
    var disabled = true;
    var html = "No items selected";
    if (items.length !== 0) {
        html = "Operation will move the " + items.length + " selected";
        disabled = false;
    }
    if (items.length > 1) {
        html = "Move operation can only be done one request at a time";
        disabled = false;
    }
    var requestId = items[0].requestId;
    var documentReceivedID = items[0].documentReceivedID;
    var vin = items[0].vin;
    var appType = items[0].appType;
    var appState = items[0].appState || "";

    var $form = $("#shipModal");
    if (!disabled) {
        $form
            .find("#confirmShip")
            .off("click")
            .on("click", function () {
                var data = getValidateFields($form);
                if (!validShipFormFields($form, data, true, true, true)) return;

                var values = [
                    {
                        name: "trackingNumber",
                        value: $form.find("#trackingNumber").val(),
                    },
                    {
                        name: "dateShipped",
                        value: $form.find("#dateShipped").val(),
                    },
                    {
                        name: "shippedNote",
                        value: $form.find("#shippedNote").val(),
                    },
                    { name: "vin", value: vin },
                    { name: "documentReceivedId", value: documentReceivedID },
                ];

                var tables = [];
                if (!disableRefresh) {
                    tables.push(t);
                    tables.push($("#DocumentsReceived_NoRequest"));
                    tables.push($("#DocumentsShipped_NoRequest"));
                }
                vendor_action_Mark_as_Shipped_HELPER(
                    $form,
                    items,
                    "/Vendor/Documents_Mark_as_Shipped",
                    tables,
                    values
                );
            });
    }
    initChatForm($form.find(".chatsEditPanel"), requestId, vin);
    $form.modal("show");
}

function vendor_Move_Reject(t, disableRefresh) {
    disableRefresh = disableRefresh || false;
    var items = vendorGetSelectedItemObjects(t);
    var disabled = true;
    var html = "No items selected";
    if (items.length !== 0) {
        html = "Operation will move the " + items.length + " selected";
        disabled = false;
    }
    if (items.length > 1) {
        html = "Move operation can only be done one request at a time";
        disabled = false;
    }
    var requestId = items[0].requestId;
    var vin = items[0].vin;
    var appType = items[0].appType;
    var appState = items[0].appState || "";

    for (var idx = 1; idx < items.length; idx++) {
        if (items[idx].appType !== appType) {
            vendor_show_DataTable_alert(
                t,
                "Cannot move items of different app types"
            );
            return;
        }
    }
    var $form = $("#modal-moveTitlePending");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    vendor_set_multi_vin_rno_header($form, items);

    var stages = [];
    stages.push({ id: ProcessStageIDs.HOLD, name: "HOLD" });
    stages.push({
        id: ProcessStageIDs.ReadyToProcess,
        name: "Ready To Process",
    });

    initCodeNoteRemarkChat($form);
    $form.find("#moveToStage").val("HOLD");
    $form.find("#reasonCancelled").val("");
    $form.find("#reasonCancelledDiv").hide();
    $form.find("#dateETA").val("");
    $form.find("#requiredEtaDiv").hide();
    //console.log($form.find("#stage").length);
    var $ddl = $form.find("#stage");

    $ddl.empty();
    stages.forEach(function (stage) {
        var $option = $("<option/>");
        $option.attr("value", stage.id);
        $option.text(stage.name);
        $ddl.append($option);
    });

    $form
        .find("#stage")
        .off("change")
        .on("change", function () {
            if ($form.find("#stage").val() === ProcessStageIDs.CANCELLED) {
                $form.find("#reasonCancelledDiv").show();
            } else {
                $form.find("#reasonCancelledDiv").hide();
            }
            if (
                $form.find("#stage").val() === ProcessStageIDs.NotReadyToProcess
            ) {
                $form.find("#info_notReadyToProcess").show();
                $form.find("#info_other").hide();
            } else {
                $form.find("#info_notReadyToProcess").hide();
                $form.find("#info_other").show();
            }
            if ($form.find("#stage").val() === ProcessStageIDs.TitlePending) {
                $form.find("#requiredEtaDiv").show();
            } else {
                $form.find("#requiredEtaDiv").hide();
            }
        });
    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    $form.find("#okButton").off("click");
    var $validationForm = $form.find("form");

    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data)) return;

                var values = [
                    { name: "code", value: data.code },
                    { name: "stage", value: $form.find("#stage").val() },
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "chat", value: data.chat },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    {
                        name: "reasonCancelled",
                        value: $form.find("#reasonCancelled").val(),
                    },
                    { name: "appType", value: appType },
                ];
                var stg = $form.find("#stage").val();

                var tables = [];
                if (!disableRefresh) {
                    tables.push(t);

                    if (stg === ProcessStageIDs.ShipToLienholder) {
                        tables.push($("#DT_ShipToLH"));
                    } else if (appType === "REG") {
                        tables.push(
                            $("#DT_MQ_Review"),
                            $("#DT_MQ_NotReadyToAccept"),
                            $("#DT_MQ_FollowUpReview"),
                            $("#DT_MQ_AcceptedIncoming"),
                            $("#DT_MQ_AcceptedNotReadyToProcess"),
                            $("#DT_MQ_Accepted_FollowUpReview"),
                            $("#DT_MQ_ReadyToProcess"),
                            $("#DT_MQ_CouldNotProcess"),
                            $("#DT_MQ_ProcessedReconcile"),
                            $("#DT_MQ_Completed"),
                            $("#DT_MQ_ReturnedTemporarily")
                        );
                    } else if (stg === ProcessStageIDs.ReadyForPacking) {
                        tables.push($("#DT_ReadyForPacking"));
                    } else if (stg === ProcessStageIDs.ReadyToProcess) {
                        tables.push($("#DT_ReadyToProcess"));
                    } else if (stg === ProcessStageIDs.NotReadyToProcess) {
                        tables.push($("#DT_NotReadyToProcess"));
                    } else if (stg === ProcessStageIDs.InTransit) {
                        tables.push($("#DT_InTransit"));
                    } else if (stg === ProcessStageIDs.ReadyForPrinting) {
                        tables.push($("#DT_ReadyForPrinting"));
                    } else if (stg === ProcessStageIDs.Receiving) {
                        tables.push($("#DT_Receiving"));
                    } else if (stg === ProcessStageIDs.Incoming) {
                        tables.push($("#DT_Incoming"));
                    } else if (stg === ProcessStageIDs.Signing) {
                        tables.push($("#DT_Sign"));
                    } else if (stg === ProcessStageIDs.TitlePending) {
                        tables.push($("#DT_TitlePending"), $("#DT_Pending"));
                    } else if (stg === ProcessStageIDs.HOLD) {
                    } else if (stg === ProcessStageIDs.CANCELLED) {
                    }
                }
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/MoveItemsWithNotes",
                    tables,
                    values
                );
            });
    }
    initChatForm($form.find(".chatsEditPanel"), requestId, vin);
    $form.modal("show");
}
function vendor_show_Move(t, disableRefresh) {
    disableRefresh = disableRefresh || false;
    var items = vendorGetSelectedItemObjects(t);
    var disabled = true;
    var html = "No items selected";
    if (items.length !== 0) {
        html = "Operation will move the " + items.length + " selected";
        disabled = false;
    }
    if (items.length > 1) {
        html = "Move operation can only be done one request at a time";
        disabled = false;
    }
    var requestId = items[0].requestId;
    var vin = items[0].vin;
    var appType = items[0].appType;
    var appState = items[0].state || "";

    // check if all items are of same app type
    for (var idx = 1; idx < items.length; idx++) {
        if (items[idx].appType !== appType) {
            //alert("Cannot move items of different app types");
            vendor_show_DataTable_alert(
                t,
                "Cannot move items of different app types"
            );
            return;
        }
    }

    var $form = $("#modal-move");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    vendor_set_multi_vin_rno_header($form, items);

    // Get stages for app type
    var stages = [];
    var requestsQueueList = [
        "RT",
        "DT",
        "DT-LH",
        "DT-LS",
        "DT-IV",
        "DTSIF",
        "ORT",
        "RA",
    ];
    var lcQueueList = ["LC", "LCO"];
    var liQueueList = ["LI"];
    var tcQueueList = ["TC"];

    //var otherQueueList = anything not in the other queues
    if (appType === "CRT") {
        stages.push({ id: ProcessStageIDs.HOLD, name: "HOLD" });
        stages.push({ id: ProcessStageIDs.CANCELLED, name: "CANCELLED" });
        stages.push({ id: ProcessStageIDs.Incoming, name: "Incoming" });
        stages.push({ id: ProcessStageIDs.Signing, name: "Signing" });
        stages.push({ id: ProcessStageIDs.Print, name: "Print" });
        stages.push({
            id: ProcessStageIDs.ReadyToProcess,
            name: "Ready for Processing",
        });
        stages.push({
            id: ProcessStageIDs.ReadyForPacking,
            name: "Ready For Packing/Shipping",
        });
        stages.push({ id: ProcessStageIDs.Receiving, name: "Receiving" });
        stages.push({
            id: ProcessStageIDs.ShipToLienholder,
            name: "Ship To Lienholder",
        });
        stages.push({
            id: ProcessStageIDs.SendToVendor,
            name: "Send To Vendor",
        });
        stages.push({
            id: ProcessStageIDs.WVRejections,
            name: "WV Rejections",
        });
        stages.push({ id: ProcessStageIDs.WVSendQueue, name: "WV Send Queue" });
        stages.push({
            id: ProcessStageIDs.TitlePending,
            name: "Title Pending",
        });
        stages.push({ id: ProcessStageIDs.Invoicing, name: "Invoicing" });
        stages.push({ id: ProcessStageIDs.InTransit, name: "In-Transit" });
        stages.push({
            id: ProcessStageIDs.NotReadyToProcess,
            name: "Not Ready for Processing",
        });
        stages.push({
            id: ProcessStageIDs.InProcessing,
            name: "In Processing",
        });
        stages.push({
            id: ProcessStageIDs.ReadyForPrinting,
            name: "Ready for Printing",
        });
    } else if (requestsQueueList.includes(appType)) {
        stages.push({ id: ProcessStageIDs.HOLD, name: "HOLD" });
        stages.push({ id: ProcessStageIDs.CANCELLED, name: "CANCELLED" });
        stages.push({ id: ProcessStageIDs.Incoming, name: "Incoming" });
        stages.push({ id: ProcessStageIDs.Signing, name: "Signing" });
        stages.push({ id: ProcessStageIDs.Print, name: "Print" });
        stages.push({
            id: ProcessStageIDs.ReadyToProcess,
            name: "Ready for Processing",
        });
        stages.push({
            id: ProcessStageIDs.ReadyForPacking,
            name: "Ready For Packing/Shipping",
        });
        stages.push({ id: ProcessStageIDs.Receiving, name: "Receiving" });
        stages.push({
            id: ProcessStageIDs.ShipToLienholder,
            name: "Ship To Lienholder",
        });
        stages.push({
            id: ProcessStageIDs.SendToVendor,
            name: "Send To Vendor",
        });
        stages.push({
            id: ProcessStageIDs.TitlePending,
            name: "Title Pending",
        });
        stages.push({ id: ProcessStageIDs.Invoicing, name: "Invoicing" });
        stages.push({ id: ProcessStageIDs.InTransit, name: "In-Transit" });
        stages.push({
            id: ProcessStageIDs.NotReadyToProcess,
            name: "Not Ready for Processing",
        });
        stages.push({
            id: ProcessStageIDs.InProcessing,
            name: "In Processing",
        });
        stages.push({
            id: ProcessStageIDs.ReadyForPrinting,
            name: "Ready for Printing",
        });
        /*
        ToDo lists:
            "DT_Incoming"
            "DT_Sign"
            "DT_NotReadyToProcess"
            "DT_ReadyToProcess"
            "DT_InProcessing"
            "DT_ReadyForPrinting"
            "DT_ReadyForPacking"
            "DT_TitlePending"
            "DT_InTransit"
            "DT_Receiving"
            "DT_ShipToLH"
        */
    } else if (lcQueueList.includes(appType)) {
        /* LC */
        stages.push({ id: ProcessStageIDs.HOLD, name: "HOLD" });
        stages.push({ id: ProcessStageIDs.CANCELLED, name: "CANCELLED" });
    } else if (appType === "REG") {
        stages.push({
            id: ProcessStageIDs.MQ_Review,
            name: "New Deals Review",
        });
        stages.push({
            id: ProcessStageIDs.MQ_NotReadyToAccept,
            name: "New Deal - Not Ready To Accept",
        });
        stages.push({
            id: ProcessStageIDs.MQ_FollowUpReview,
            name: "New Deal - Follow - Up Review",
        });
        stages.push({
            id: ProcessStageIDs.MQ_AcceptedIncoming,
            name: "Accepted Incoming",
        });
        stages.push({
            id: ProcessStageIDs.MQ_AcceptedNotReadyToProcess,
            name: "Accepted Not Ready To Process",
        });
        stages.push({
            id: ProcessStageIDs.MQ_Accepted_FollowUpReview,
            name: "Accepted Follow Up Review",
        });
        stages.push({
            id: ProcessStageIDs.MQ_ReadyToProcess,
            name: "Ready To Process",
        });
        stages.push({
            id: ProcessStageIDs.MQ_CouldNotProcess,
            name: "Could Not Process",
        });
        stages.push({
            id: ProcessStageIDs.MQ_ProcessedReconcile,
            name: "Processed - Reconcile",
        });
        stages.push({ id: ProcessStageIDs.MQ_Completed, name: "Completed" });
        stages.push({
            id: ProcessStageIDs.MQ_ReturnedTemporarily,
            name: "Returned Temporarily",
        });
    } else {
        stages.push({ id: ProcessStageIDs.HOLD, name: "HOLD" });
        stages.push({ id: ProcessStageIDs.CANCELLED, name: "CANCELLED" });
        stages.push({ id: ProcessStageIDs.Incoming, name: "Incoming" });
        stages.push({
            id: ProcessStageIDs.NotReadyToProcess,
            name: "Not Ready for Processing",
        });
        stages.push({
            id: ProcessStageIDs.ReadyToProcess,
            name: "Ready for Processing",
        });
        stages.push({
            id: ProcessStageIDs.InProcessing,
            name: "In Processing",
        });
        stages.push({
            id: ProcessStageIDs.ReadyForPrinting,
            name: "Ready For Printing",
        });
        stages.push({
            id: ProcessStageIDs.ReadyForPacking,
            name: "Ready For Packing/Shipping",
        });
        stages.push({ id: ProcessStageIDs.TitlePending, name: "Pending" });
        stages.push({ id: ProcessStageIDs.Completed, name: "Completed" });
        /*
        "DT_Incoming"
        "DT_NotReadyToProcess"
        "DT_ReadyToProcess"
        "DT_InProcessing"
        "DT_ReadyForPacking"
        "DT_Pending"
        "DT_Completed"
*/
    }

    initCodeNoteRemarkChat($form);
    $form.find("#moveToStage").val("HOLD");
    $form.find("#reasonCancelled").val("");
    $form.find("#reasonCancelledDiv").hide();
    $form.find("#requiredEtaDiv").hide();
    console.log($form.find("#stage").length);
    var $ddl = $form.find("#stage");

    $ddl.empty();
    stages.forEach(function (stage) {
        let $option = $("<option/>");
        $option.attr("value", stage.id);
        $option.text(stage.name);
        $ddl.append($option);
    });

    $form
        .find("#stage")
        .off("change")
        .on("change", function () {
            if ($form.find("#stage").val() === ProcessStageIDs.CANCELLED) {
                $form.find("#reasonCancelledDiv").show();
            } else {
                $form.find("#reasonCancelledDiv").hide();
            }
            if (
                $form.find("#stage").val() === ProcessStageIDs.NotReadyToProcess
            ) {
                $form.find("#info_notReadyToProcess").show();
                $form.find("#info_other").hide();
            } else {
                $form.find("#info_notReadyToProcess").hide();
                $form.find("#info_other").show();
            }
        });
    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    $form.find("#okButton").off("click");
    let $validationForm = $form.find("form");

    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                //if (!$validationForm.valid()) {
                //    $form.find("div.alert").text("Please fill in all required fields").show();
                //    return;
                //}
                var data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data)) return;

                var values = [
                    { name: "code", value: data.code },
                    { name: "stage", value: $form.find("#stage").val() },
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "chat", value: data.chat },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    {
                        name: "reasonCancelled",
                        value: $form.find("#reasonCancelled").val(),
                    },
                    { name: "appType", value: appType },
                ];
                var stg = $form.find("#stage").val();

                var tables = [];
                if (!disableRefresh) {
                    tables.push(t);

                    if (stg === ProcessStageIDs.ShipToLienholder) {
                        tables.push($("#DT_ShipToLH"));
                    } else if (appType === "REG") {
                        tables.push(
                            $("#DT_MQ_Review"),
                            $("#DT_MQ_NotReadyToAccept"),
                            $("#DT_MQ_FollowUpReview"),
                            $("#DT_MQ_AcceptedIncoming"),
                            $("#DT_MQ_AcceptedNotReadyToProcess"),
                            $("#DT_MQ_Accepted_FollowUpReview"),
                            $("#DT_MQ_ReadyToProcess"),
                            $("#DT_MQ_CouldNotProcess"),
                            $("#DT_MQ_ProcessedReconcile"),
                            $("#DT_MQ_Completed"),
                            $("#DT_MQ_ReturnedTemporarily")
                        );
                    } else if (stg === ProcessStageIDs.ReadyForPacking) {
                        tables.push($("#DT_ReadyForPacking"));
                    } else if (stg === ProcessStageIDs.ReadyToProcess) {
                        tables.push($("#DT_ReadyToProcess"));
                    } else if (stg === ProcessStageIDs.NotReadyToProcess) {
                        tables.push($("#DT_NotReadyToProcess"));
                    } else if (stg === ProcessStageIDs.InTransit) {
                        tables.push($("#DT_InTransit"));
                    } else if (stg === ProcessStageIDs.ReadyForPrinting) {
                        tables.push($("#DT_ReadyForPrinting"));
                    } else if (stg === ProcessStageIDs.Receiving) {
                        tables.push($("#DT_Receiving"));
                    } else if (stg === ProcessStageIDs.Incoming) {
                        tables.push($("#DT_Incoming"));
                    } else if (stg === ProcessStageIDs.Signing) {
                        tables.push($("#DT_Sign"));
                    } else if (stg === ProcessStageIDs.TitlePending) {
                        // Pending for other queus is not to be confused with Pending status before sent to Vendor
                        tables.push($("#DT_TitlePending"), $("#DT_Pending"));
                    } else if (stg === ProcessStageIDs.HOLD) {
                        // Hold is on a different page
                    } else if (stg === ProcessStageIDs.CANCELLED) {
                        // Cancelled is on a different page
                    }
                }
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/MoveItemsWithNotes",
                    tables,
                    values
                );
            });
    }
    initChatForm($form.find(".chatsEditPanel"), requestId, vin);
    $form.modal("show");
}
function vendor_show_PrintCRs(t) {
    var items = vendorGetSelectedItems(t);
    var html = "No items selected";
    var disabled = true;
    if (items.length !== 0) {
        html =
            "Operation will merge first page of each CR downloaded from AutoIMS.  Processing may take a minute or longer.";
        disabled = false;
    }
    var $form = $("#modal-printCRs");
    vendor_init_dialog($form);
    $form.find(".modal-body").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var f = $form.find("#printCRs");
                $.each(items, function (i, id) {
                    f.append(
                        "<input type='hidden' name='ids[]' value='" + id + "'/>"
                    );
                });
                attachToken(f);
                f.trigger("submit");
                $form.modal("hide");
            });
    }
    $form.modal("show");
}
function vendor_show_AutoIMS(t) {
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-RefreshAutoIMS");
    vendor_init_dialog($form);

    if (items.length !== 0) {
        html =
            "Operation will retry AutoIMS query of the " +
            items.length +
            " selected items.  Please allow several minutes to process.";
        disabled = false;
    }
    $form.find("#selectedCount").html(html);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/RefreshAutoIMS",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}

function vendor_show_ReceiveFromLH(t) {
    var items = vendorGetSelectedItems(t);
    var html = "No items selected";
    var disabled = true;
    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as received and ready for processing";
        disabled = false;
    }
    if (!vendor_CheckAppTypesAllSame(t)) {
        vendor_show_DataTable_alert(
            t,
            "Moving multiple application types not supported"
        );
        return;
    }
    var $form = $("#modal-receiveFromLH");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    $form.find("#selectedCount").html(html);

    var today = moment().format("YYYY-MM-DD");
    $form.find("#date").val(today);
    $form.find("#date").prop("disabled", disabled);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data)) return;

                var values = [
                    { name: "date", value: data.date },
                    { name: "code", value: data.code },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "chat", value: data.chat },
                ];

                tables.push($("#DT_ReadyToProcess"));

                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/ReceiveFromLH",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_ReceiveFromLH_NotReady(t) {
    var items = vendorGetSelectedItemObjects(t);
    var html = "No items selected";
    var disabled = true;
    if (items.length === 0) {
        return;
    }
    if (!vendor_CheckAppTypesAllSame(t)) {
        vendor_show_DataTable_alert(
            t,
            "Moving multiple application types not supported"
        );
        return;
    }

    disabled = false;
    var $form = $("#modal-receiveFromLH_NotReady");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    $form.find("#selectedCount").html(`${items.length} items selected`);

    var requestId = items[0].requestId;
    var vin = items[0].vin;

    var today = moment().format("YYYY-MM-DD");
    $form.find("#requestId").val(requestId);
    initCodeNoteRemarkChat($form);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();

                let data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data, true, true)) return;

                vendor_init_dialog($form);

                var tables = [t];
                var values = [
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "code", value: data.code },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    { name: "chat", value: data.chat },
                ];

                tables.push($("#DT_Incoming"));
                tables.push($("#DT_NotReadyToProcess"));

                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/NotReadyForProcessing",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_TitleIssued(t) {
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var $form = $("#modal-titleIssued");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    var disabled = true;
    if (items.length !== 0) {
        var today = moment().format("YYYY-MM-DD");
        $form.find("#dateTitleIssued").val(today);
        html =
            "Operation will mark the " +
            items.length +
            " selected items as issued.";
        disabled = false;
    }

    $form.find("#titleIssuedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [
                    {
                        name: "dateTitleIssued",
                        value: $form.find("#dateTitleIssued").val(),
                    },
                    {
                        name: "inTransit",
                        value: $form.find("#inTransit").val(),
                    },
                ];
                tables.push($("#DT_InTransit"));
                tables.push($("#DT_Receiving"));

                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/TitleIssued",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_TitleIssuedWorkingList(t) {
    //var items = vendorGetSelectedItems(t);
    var items = vendorGetSelectedItemObjects(t);

    var html = "No items selected";
    var $form = $("#modal-titleIssuedWorkingList");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    var disabled = true;

    if (items.length !== 0) {
        var today = moment().format("YYYY-MM-DD");
        $form.find("#dateTitleIssued").val(today);
        html =
            "Operation will mark the " +
            items.length +
            " selected items as issued.";
        disabled = false;
    }

    var requestId = items[0].requestId;
    var vin = items[0].vin;

    $form.find("#inTransit").val();
    $form.find("#titleIssuedCount").html(html);
    $form.find("#noteDiv").hide();
    $form.find("#internalRemarkDiv").hide();
    $form.find("#chatDiv").hide();
    $form.find("#okButton").prop("disabled", disabled);

    $form
        .find("#inTransit")
        .off("change")
        .on("change", function () {
            if ($form.find("#inTransit").val() === "true") {
                $form.find("#internalRemarkDiv").show();
                $form.find("#chatDiv").show();
                $form.find("#noteDiv").show();
            } else {
                $form.find("#noteDiv").hide();
                $form.find("#internalRemarkDiv").hide();
                $form.find("#chatDiv").hide();
            }
        });

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [];

                if ($form.find("#inTransit").val() === "true") {
                    var data = getCodeNoteRemarkChat($form);
                    if (
                        !validFormFieldsForPending(
                            $form,
                            data,
                            false,
                            true,
                            false,
                            false
                        )
                    )
                        return;

                    values = [
                        { name: "code", value: data.code },
                        { name: "note", value: data.note },
                        { name: "remark", value: data.remark },
                        { name: "chat", value: data.chat },
                        {
                            name: "dateTitleIssued",
                            value: $form.find("#dateTitleIssued").val(),
                        },
                        {
                            name: "inTransit",
                            value: $form.find("#inTransit").val(),
                        },
                    ];
                } else {
                    values = [
                        {
                            name: "dateTitleIssued",
                            value: $form.find("#dateTitleIssued").val(),
                        },
                        {
                            name: "inTransit",
                            value: $form.find("#inTransit").val(),
                        },
                    ];
                }
                tables.push($("#DT_InTransit"));
                tables.push($("#DT_Receiving"));

                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/TitleIssued",
                    tables,
                    values
                );
            });
    }
    initChatForm($form.find(".chatsEditPanel"), requestId, vin);
    $form.modal("show");
}
function vendor_show_WorkingList(t) {
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var $form = $("#modal-workingList");
    //vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    var disabled = true;
    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as working list.";
        disabled = false;
    }
    $form.find("#titleIssuedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [];
                tables.push($("#DT_TitlePending"));
                tables.push($("#DT_WorkingList"));

                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/WorkingList",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}

function viewRequest(reqid) {
    $.ajax({
        url: "/AppForm/View/" + reqid,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (res) {
        let $mymodal = $("#modal-viewDetails");
        var html = "Edit Request";
        let vin = "";
        if (typeof vin !== "undefined") {
            html += ": " + htmlEncode(vin);
        }
        $mymodal.find("#myModal-label").html(html);
        $mymodal.find(".modal-body").html(res);
        resetFormValidator("#dynamicAppForm");

        $mymodal
            .find("#okButton")
            .off("click")
            .on("click", function () {
                $mymodal.modal("hide");
            });
        $mymodal.modal("show");
    });
    return false;
}
function editRequest(reqid, vin) {
    var succeeded = false;
    $.ajax({
        url: "/AppForm/Open/" + reqid,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (res) {
        let $mymodal = $("#modal-editDetails");
        var html = "Edit Request";
        if (typeof vin !== "undefined") {
            html += ": " + htmlEncode(vin);
        }
        $mymodal.find("#myModal-label").html(html);
        $mymodal.find(".modal-body").html(res);
        // setup to prevent redirect on submit
        var $form = $mymodal.find("form");
        vendor_init_dialog($form);
        resetFormValidator("#dynamicAppForm");
        $form.submit(function (e) {
            e.preventDefault();
            var $thisForm = $(this);
            $.ajax({
                url: $thisForm.attr("action"),
                type: "POST",
                headers: {
                    RequestVerificationToken: $(
                        '[name="__RequestVerificationToken"]'
                    ).val(),
                },
                data: $thisForm.serialize(),
            }).done(function (data) {
                // todo
            });
            return false;
        });
        $mymodal.modal("show");
    });
    return false;
}
function vendorEditRequest(reqid, vin) {
    var succeeded = false;
    $.ajax({
        url: "/AppForm/VendorOpen/" + reqid,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (res) {
        var $mymodal = $("#modal-editDetails");
        var html = "Edit Request";
        if (typeof vin !== "undefined") {
            html += ": " + htmlEncode(vin);
        }
        $mymodal.find("#myModal-label").html(html);
        $mymodal.find(".modal-body").html(res);
        var $form = $mymodal.find("form");
        vendor_init_dialog($form);
        resetFormValidator("#dynamicAppForm");
        $form.off("submit").on("submit", function (e) {
            e.preventDefault();
            disableFormSubmit($form);
            // Save Form is not the submit button
            // must find it in the footer
            let $btnSaveForm = $form.closest("#okButton");
            $btnSaveForm.attr("disabled", true);
            var $thisForm = $(this);
            $.ajax({
                url: $thisForm.attr("action"),
                type: "POST",
                headers: {
                    RequestVerificationToken: $(
                        '[name="__RequestVerificationToken"]'
                    ).val(),
                },
                data: $thisForm.serialize(),
            })
                .done(function (data) {
                    $mymodal.modal("hide");
                    enableFormSubmit($form);
                    $btnSaveForm.attr("disabled", false);
                })
                .fail(function () {
                    enableFormSubmit($form);
                    $btnSaveForm.attr("disabled", false);
                });
            return false;
        });
        $mymodal
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();
                $form.trigger("submit");
            });
        $mymodal.modal("show");
    });
    return false;
}
function vendor_show_ReprocessCheckMerge(t) {
    var items = vendorGetSelectedItemObjects(t);

    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-ReprocessCheckMerge");
    vendor_init_dialog($form);

    if (items.length <= 0) {
        vendor_show_alert(t, "Item must be selected for processing");
        return;
    }
    if (items.length > 1) {
        vendor_show_alert(t, "Only one item can be selected for reprocessing");
        return;
    }
    if (items.length === 1) {
        html =
            "Operation will reprocess the check merge for the select item.  Please allow a minute or two for processing.";
        disabled = false;
    }
    var item = items[0];
    $form.find("#selectedCount").html(html);
    $form.find("#forceMerge").prop("checked", false);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                let force = $form.find("#forceMerge").prop("checked");
                var values = [
                    { name: "bulkPrintFileId", value: item.bulkPrintFileId },
                    { name: "force", value: force },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/BulkPrint/Reprocess",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_EditStatus(t) {
    var items = vendorGetSelectedItemObjects(t);
    var item = items[0];
    vendorEditStatus(item["requestId"], item["vin"]);
}
function vendorEditStatus(reqid, vin) {
    var succeeded = false;

    $.ajax({
        url: "/AppForm/VendorEditStatus/" + reqid,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    })
        .done(function (res) {
            var $modal = $("#modal-editDetails");
            var html = "Edit Status";
            if (typeof vin !== "undefined") {
                html += ": " + htmlEncode(vin);
            }
            $modal.find("#myModal-label").html(html);
            $modal.find(".modal-body").html(res);
            var $form = $modal.find("form");
            vendor_init_dialog($form);
            $form.off("submit").on("submit", function (e) {
                e.preventDefault();
                var $thisForm = $(this);
                $.ajax({
                    url: $thisForm.attr("action"),
                    type: "POST",
                    headers: {
                        RequestVerificationToken: $(
                            '[name="__RequestVerificationToken"]'
                        ).val(),
                    },
                    data: $thisForm.serialize(),
                }).done(function (data) {
                    $modal.modal("hide");
                });
                return false;
            });
            $modal
                .find("#okButton")
                .off("click")
                .on("click", function () {
                    $form.trigger("submit");
                });
            $modal.modal("show");
        })
        .fail(function (xhr) {
            processError(xhr);
        });
    return false;
}
function VendorSaveRequest() {
    var $form = $("#modal-editDetails").find("form");
    attachToken($form);
    $form.trigger("submit");
}

function vendor_show_MarkInvoiced(t) {
    var items = vendorGetSelectedItems(t);
    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-markInvoiced");
    vendor_init_dialog($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as invoiced";
        $form.find("#selectedCount").html(html);
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");

    $form.find("#MarkInvoiced_InvoiceDate").val(today);
    $form.find("#MarkInvoiced_InvoiceAmount").val("");
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [
                    {
                        name: "invoiceDate",
                        value: $form.find("#MarkInvoiced_InvoiceDate").val(),
                    },
                    {
                        name: "invoiceNum",
                        value: $form.find("#MarkInvoiced_InvoiceNum").val(),
                    },
                    {
                        name: "invoiceAmount",
                        value: $form.find("#MarkInvoiced_InvoiceAmount").val(),
                    },
                ];
                //if (typeof vendorMarkInvoiced_Table !== 'undefined')
                //    vendorMarkInvoiced_Table.ajax.reload();
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/MarkAsInvoiced",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function isInInvoiceBuilder(reqId) {
    var list = vendorGetInvoiceBuilderIDs();
    var f = list.find(function (id) {
        return id === reqId;
    });
    if (typeof f === "undefined") return false;
    return true;
}
function vendorGetInvoiceBuilderIDs(t) {
    if (typeof t === "undefined") t = $("#DT_invoiceDetailsTable").DataTable();

    var a = [];
    $.each(t.rows().data(), function () {
        a.push(this["requestId"]);
    });
    return a;
}
function vendorGetBulkPayBuilderIDs(t) {
    if (typeof t === "undefined")
        t = $("#DT_bulkPayInvoiceDetailsTable").DataTable();

    var a = [];
    $.each(t.rows().data(), function () {
        a.push(this["invoiceId"]);
    });
    return a;
}
function vendorCreateBulkInvoice_Action() {
    var $form = $("#invoiceBuilderForm");
    vendor_init_dialog($form);

    var table = $("#DT_invoiceDetailsTable").DataTable();

    var details = [];
    table.rows().every(function (rowIdx, tableLoop, rowLoop) {
        var d = this.data();
        var detail = {
            id: d.requestId,
            dmvFee: d.dmvFee,
            svcFee: d.svcFee,
            otherFee: d.otherFee,
            otherDesc: d.otherDesc,
            totalFee: d.totalFee,
            svcFee2: d.svcFee2 || 0,
            otherFee2: d.otherFee2 || 0,
            otherDesc2: d.otherDesc2,
            totalFee2: d.totalFee2 || 0,
            glCode: d.glCode,
        };
        details.push(detail);
    });
    var json = JSON.stringify(details);

    $form.find("#details").val(json);
    $form.find("#singleInvoice").val(false);
    attachToken($form);
    $form.trigger("submit");
    var invoiceDatatable = $("#DT_InvoiceList").DataTable();
    invoiceDatatable.ajax.reload();
}
function vendorCreateSingleInvoice_Action() {
    var $form = $("#invoiceBuilderForm");
    vendor_init_dialog($form);

    var table = $("#DT_invoiceDetailsTable").DataTable();

    var details = [];
    table.rows().every(function (rowIdx, tableLoop, rowLoop) {
        var d = this.data();
        var detail = {
            id: d.requestId,
            dmvFee: d.dmvFee,
            svcFee: d.svcFee,
            otherFee: d.otherFee,
            otherDesc: d.otherDesc,
            totalFee: d.totalFee,
            svcFee2: d.svcFee2,
            otherFee2: d.otherFee2,
            otherDesc2: d.otherDesc2,
            totalFee2: d.totalFee2,
            glCode: d.glCode,
        };
        details.push(detail);
    });
    var json = JSON.stringify(details);

    $form.find("#details").val(json);
    $form.find("#singleInvoice").val(true);
    attachToken($form);
    $form.trigger("submit");
    var invoiceDatatable = $("#DT_InvoiceList").DataTable();
    invoiceDatatable.ajax.reload();
}
function datatableRemoveSelectedRows(t) {
    t.rows({ selected: true }).remove().draw();
}
function deleteRowsAndUpdateInvoice(t) {
    datatableRemoveSelectedRows(t);
    UpdateInvoiceTotal();
    vendorUnbilled_Table.draw("page");
}
const unknownErrorObj = '{"status":"error","message":"unknown error"}';
function vendorVerifyAndDisplayMergeApplicationsForPrinting_Action(t) {
    vendorBulkMerge_Items = vendorGetSelectedItemObjects(t);

    var formData = new FormData();

    var appState = "";
    var appType = "";
    $(vendorBulkMerge_Items).each(function (i, v) {
        if (appState === "") {
            appState = v.state;
        }
        if (appType === "") {
            appType = v.appType;
        }
    });
    formData.append("appState", appState);
    formData.append("appType", appType);
    formData.append("includeCheckMerge", true);

    $.ajax({
        url: "/BulkPrint/GetApplicationsForMerge",
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        data: formData,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (data) {
        data = data || unknownErrorObj;
        if (data.indexOf('"status":"error"') >= 0) {
            var obj = JSON.parse(data);
            alert(obj.message);
        } else {
            var $form = $("#modal-request-merge-selection");
            var $mymodalbody = $form.find(".modal-body");
            $mymodalbody.html(data);
            $form.modal("show");
        }
    });
    return false;
}
function vendorVerifyAndDisplayCheckMerge_Action(t) {
    vendorBulkMerge_Items = vendorGetSelectedItemObjects(t);

    var formData = new FormData();

    var appState = "";
    var appType = "";
    $(vendorBulkMerge_Items).each(function (i, v) {
        if (appState === "") {
            appState = v.state;
        }
        if (appType === "") {
            appType = v.appType;
        }
    });
    formData.append("appState", appState);
    formData.append("appType", appType);
    $(vendorBulkMerge_Items).each(function (i, v) {
        formData.append("requestIds", v.requestId);
    });

    $.ajax({
        url: "/BulkPrint/GetInfoForCheckMerge",
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        data: formData,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (data) {
        data = data || unknownErrorObj;
        if (data.indexOf('"status":"error"') >= 0) {
            var obj = JSON.parse(data);
            alert(obj.message);
        } else {
            var $form = $("#modal-request-checkmerge-selection");
            var $mymodalbody = $form.find(".modal-body");
            $mymodalbody.html(data);
            vendor_hide_alert($form);

            $mymodalbody
                .find("input[name='createNewCheck']")
                .each(function (i1, v1) {
                    let ctl = $(this);
                    let requestno = ctl.data("requestno");

                    $mymodalbody
                        .find(
                            `input[name=useThisCheck${requestno}][value=${requestno}]`
                        )
                        .prop("checked", true);
                });

            $mymodalbody
                .find("input[name='createNewCheck']")
                .off("click")
                .on("click", function () {
                    let ctl = $(this);
                    let requestno = ctl.data("requestno");
                    if (ctl.is(":checked")) {
                        $mymodalbody
                            .find(`[name='useThisCheck${requestno}']`)
                            .each(function (i, v) {
                                let ctl2 = $(this);
                                ctl2.hide();
                            });
                    } else {
                        $mymodalbody
                            .find(`[name='useThisCheck${requestno}']`)
                            .each(function (i, v) {
                                let ctl2 = $(this);
                                ctl2.show();
                            });
                    }
                });

            $form.modal("show");
        }
    });
    return false;
}
function vendorVerifyAndDisplayAttachmentsForPrinting_Action(t) {
    vendorBulkPrint_Items = vendorGetSelectedItemObjects(t);

    var formData = new FormData();

    $(vendorBulkPrint_Items).each(function (i, v) {
        formData.append("requestIds", v.requestId);
    });

    $.ajax({
        url: "/BulkPrint/GetAttachmentsFromRequestIds",
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        data: formData,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (data) {
        data = data || unknownErrorObj;
        if (data.indexOf('"status":"error"') >= 0) {
            var obj = JSON.parse(data);
            alert(obj.message);
        } else {
            var $form = $("#modal-request-printing-selection");
            var $mymodalbody = $form.find(".modal-body");
            $mymodalbody.html(data);
            // init dialog
            $("#bulkPrintingStep2").hide();
            $("#btnStartBulkPrinting").show();
            $("#bulkPrintingStep1").show();
            $form.modal("show");
        }
    });
    return false;
}

function vendorAddToInvoiceBuilder_Action(t) {
    vendorUnbilled_Table = t;
    vendorUnbilled_Items = vendorGetSelectedItemObjects(t);
    var form = $("#invoiceBuilderForm");

    var details = $("#DT_invoiceDetailsTable").DataTable();
    vendorCurrentInvoiceIds = vendorGetInvoiceBuilderIDs(details);

    if (containsItems(vendorUnbilled_Items)) {
        var counter = 1;

        //
        var groupId = "";
        var currentData = details.rows().data();
        if (currentData.length > 0) {
            groupId = currentData[0].groupId;
            // TBD: check that all items are from same group
            //vendor_show_DataTable_alert(t, "All items must be from the same group");
        }

        for (var i = 0; i < vendorUnbilled_Items.length; i++) {
            var data = vendorUnbilled_Items[i];

            // set groupId and groupName on invoice
            if (
                vendorCurrentInvoiceIds.length === 0 ||
                $.inArray(data.requestId, vendorCurrentInvoiceIds) < 0
            ) {
                if (groupId === "") {
                    groupId = data.groupId;
                    form.find("#groupId").val(data.groupId);
                    form.find("#groupName").val(data.groupName);
                    var d = populateBillToList(groupId);
                }
                if (groupId === data.groupId) {
                    // add to list
                    vendorCurrentInvoiceIds.push(data.requestId);

                    var dmvFee = $("#defaultDmvFee").val();
                    data.dmvFee = 0.0;
                    if (dmvFee !== "") {
                        data.dmvFee = roundMoney(parseFloat(dmvFee));
                    }

                    var svcFee = $("#defaultSvcFee").val();
                    data.svcFee = 0.0;
                    if (svcFee !== "") {
                        data.svcFee = roundMoney(parseFloat(svcFee));
                    }

                    var otherFee = $("#defaultOtherFee").val();
                    data.otherFee = 0.0;
                    if (otherFee !== "") {
                        data.otherFee = roundMoney(parseFloat(otherFee));
                    }
                    data.otherDesc = $("#defaultOtherDesc").val();

                    data.totalFee = data.dmvFee + data.svcFee + data.otherFee;

                    data.glCode = $("#glCode").val();

                    details.row.add(data).draw(false);
                }
            }
        }
        UpdateInvoiceTotal();
    }
    t.ajax.reload(null, false);
}

function vendorAddToBulkPayBuilder_Action(t) {
    vendorUnbilled_Table = t;
    vendorUnbilled_Items = vendorGetSelectedItemObjects(t);

    if (vendorUnbilled_Items.length === 0) {
        alert("No items selected");
        return;
    }

    let details = $("#DT_bulkPayInvoiceDetailsTable").DataTable();
    let vendorCurrentBulkPayInvoiceIds = vendorGetBulkPayBuilderIDs(details);

    if (containsItems(vendorUnbilled_Items)) {
        for (const element of vendorUnbilled_Items) {
            let data = element;

            if (
                vendorCurrentBulkPayInvoiceIds.length === 0 ||
                $.inArray(data.invoiceId, vendorCurrentBulkPayInvoiceIds) < 0
            ) {
                vendorCurrentBulkPayInvoiceIds.push(data.invoiceId);
                details.row.add(data).draw(false);
            }
        }
    }
    t.ajax.reload(null, false);
}

var billToListData = {};

function populateBillToList(groupId) {
    billToListData = {};

    let f;
    var formData = new FormData();
    formData.append("groupId", groupId);
    formData.append("name", "BillToList");

    var succeeded = false;
    $.ajax({
        url: "/Vendor/GetGroupSettings",
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        async: true,
        data: formData,
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
        complete: function () {
            // refresh list
        },
        success: function (data) {
            if (data.status === "error") {
                succeeded = vendor_action_HELPER_success(f, data);
            } else {
                var list = $("#billToList");
                billToListData = data;
                populateBillToListOptions(data, list);
            }
        },
        error: function (xhr) {
            succeeded = vendor_action_HELPER_error(xhr, f);
        },
    });
}
function clearBillToListOptions() {
    var ctl = $("#billToList");
    ctl.find("option").remove();
    populateSelectedBillTo();
}
function populateBillToListOptions(data, ctl) {
    // clear current list
    ctl.find("option").remove();
    $(data.BillingAddresses).each(function (index, element) {
        ctl.append($("<option>", { value: index }).text(element.ShortName));
    });
    populateSelectedBillTo();
}
function populateSelectedBillTo() {
    var ctl = $("#billToList");
    var idx = ctl.find(":selected").val();
    var custName = "";
    var custAddr1 = "";
    var custAddr2 = "";
    var custCity = "";
    var custState = "";
    var custZip = "";
    var custAttn = "";

    if (
        typeof idx === "undefined" ||
        typeof billToListData === "undefined" ||
        typeof billToListData.BillingAddresses === "undefined" ||
        billToListData.BillingAddresses.length < idx
    ) {
        // use default value
    } else {
        var data = billToListData.BillingAddresses[idx];
        custName = data.CompanyName;
        custAddr1 = data.AddressLine1;
        custAddr2 = data.AddressLine2;
        custCity = data.City;
        custState = data.State;
        custZip = data.ZipCode;
        custAttn = data.Attn;
    }
    var $form = $("#invoiceBuilderForm");
    vendor_init_dialog($form);

    $form.find("[name='custName']").val(custName);
    $form.find("[name='custAddr1']").val(custAddr1);
    $form.find("[name='custAddr2']").val(custAddr2);
    $form.find("[name='custCity']").val(custCity);
    $form.find("[name='custState']").val(custState);
    $form.find("[name='custZip']").val(custZip);
    $form.find("[name='custAttn']").val(custAttn);
}

function ClearInvoiceDetail() {
    vendorCurrentInvoiceIds = [];
    $("#DT_UnbilledRequests").DataTable().draw("page");
    $("#DT_invoiceDetailsTable").DataTable().clear().draw();

    var $form = $("#invoiceBuilderForm");
    vendor_init_dialog($form);

    var total = 0.0;
    $form.find("#invoiceAmount").val(total.toFixed(2));
    $form.find("#invoiceDate").val("");
    $form.find("#invoiceNum").val("");
    $form.find("#groupName").val("");
    clearBillToListOptions();
}
function UpdateInvoiceTotal() {
    var t = $("#DT_invoiceDetailsTable").DataTable();
    var dataArray = t.data();
    var total = 0.0;
    vendorCurrentInvoiceIds = [];
    for (var i = 0; i < dataArray.length; i++) {
        var data = dataArray[i];
        vendorCurrentInvoiceIds.push(data.requestId);
        total =
            total +
            roundMoney(data.dmvFee) +
            roundMoney(data.svcFee) +
            roundMoney(data.otherFee);
    }
    var form = $("#invoiceBuilderForm");
    form.find("#invoiceAmount").val(total.toFixed(2));
}
function roundMoney(m) {
    return Math.round(m * 100) / 100;
}
function vendorGetSelectedItemData(t) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push(this);
    });
    return a;
}
function vendor_show_DeleteInvoice(t) {
    var items = vendorGetSelectedItemData(t);
    var $form = $("#modal-invoiceDelete");
    vendor_init_dialog($form);

    var disabled = false;
    if (items.length === 0) {
        vendor_show_DataTable_alert(t, "No items selected");
        return;
    }
    if (items.length !== 1) {
        vendor_show_DataTable_alert(t, "Invoices must be deleted individually");
        return;
    }
    if (items.length === 1) {
        $.each(items, function (i, item) {
            $form.find("#InvoiceDelete_GroupName").val(item.groupName); // remove time
            $form
                .find("#InvoiceDelete_InvoiceDate")
                .val(item.invoiceDate.substr(0, 10)); // remove time
            $form.find("#InvoiceDelete_InvoiceAmount").val(item.invoiceAmount);
            $form.find("#InvoiceDelete_InvoiceNum").val(item.invoiceNo);
            $form.find("#InvoiceDelete_InvoiceId").val(item.invoiceId);
        });
        $form.find("#okButton").prop("disabled", disabled);
        if (!disabled) {
            $form
                .find("#okButton")
                .off("click")
                .on("click", function () {
                    var tables = [t];
                    var values = [
                        {
                            name: "invoiceId",
                            value: $form.find("#InvoiceDelete_InvoiceId").val(),
                        },
                    ];
                    tables.push($("#DT_UnbilledRequests"));
                    vendor_action_HELPER(
                        $form,
                        items,
                        "/Vendor/DeleteInvoice",
                        tables,
                        values
                    );
                });
        }
        $form.modal("show");
    }
}
function vendor_download_invoices(t) {
    var invoiceIds = vendorGetSelectedInvoices(t);

    if (containsItems(invoiceIds)) {
        let f = $("#downloadInvoices");
        f.html("");
        let txt = "";
        $.each(invoiceIds, function (i, invoiceId) {
            txt =
                txt +
                '<input type="hidden" name="ids[]" value="' +
                invoiceId +
                '"/>';
        });
        f.append(txt);
        //console.log(txt);
        attachToken(f);
        f.trigger("submit");
    }
}
function vendor_show_MarkAsPaid(t) {
    var items = vendorGetSelectedItemData(t);
    var $form = $("#modal-markPaid");
    vendor_init_dialog($form);

    var disabled = false;
    if (items.length === 0) {
        vendor_show_DataTable_alert(t, "No items selected");
        return;
    }
    if (items.length !== 1) {
        vendor_show_DataTable_alert(
            t,
            "Invoices must be marked paid individually"
        );
        return;
    }
    if (items.length === 1) {
        $.each(items, function (i, item) {
            $form
                .find("#MarkPaid_InvoiceDate")
                .val(item.invoiceDate.substr(0, 10)); // remove time
            $form.find("#MarkPaid_InvoiceAmount").val(item.invoiceAmount);
            $form.find("#MarkPaid_InvoiceNum").val(item.invoiceNo);
            $form.find("#MarkPaid_InvoiceId").val(item.invoiceId);
            $form.find("#MarkPaid_InvoiceNote").val(item.invoiceNote);

            if (
                item.invoiceDatePaid !== "undefined" &&
                item.invoiceDatePaid !== null &&
                item.invoiceDatePaid !== ""
            ) {
                $form
                    .find("#MarkPaid_InvoiceDatePaid")
                    .val(item.invoiceDatePaid.substr(0, 10));
            } else {
                var today = moment().format("YYYY-MM-DD");
                $form.find("#MarkPaid_InvoiceDatePaid").val(today);
            }
        });
        $form.find("#okButton").prop("disabled", disabled);
        if (!disabled) {
            $form
                .find("#okButton")
                .off("click")
                .on("click", function () {
                    var tables = [t];
                    var values = [
                        {
                            name: "invoiceId",
                            value: $form.find("#MarkPaid_InvoiceId").val(),
                        },
                        {
                            name: "invoiceNote",
                            value: $form.find("#MarkPaid_InvoiceNote").val(),
                        },
                        {
                            name: "invoiceDatePaid",
                            value: $form
                                .find("#MarkPaid_InvoiceDatePaid")
                                .val(),
                        },
                    ];
                    vendor_action_HELPER(
                        $form,
                        items,
                        "/Vendor/MarkAsPaid",
                        tables,
                        values
                    );
                });
        }
        $form.modal("show");
    }
}

function vendor_show_bulk_MarkAsPaid() {
    let table = $("#DT_bulkPayInvoiceDetailsTable").DataTable();

    let items = [];
    table.rows().every(function (rowIdx, tableLoop, rowLoop) {
        items.push(this.data());
    });

    if (items.length === 0) {
        alert("No items selected");
        return;
    }

    let disabled = false;
    let $form = $("#modal-bulk-markPaid");

    let invoiceNos = "";
    let invoiceAmountSum = 0;
    let invoiceIdsToMarkAsPaid = [];

    for (const invoice of items) {
        invoiceNos += invoice.invoiceNo + ", ";
        invoiceAmountSum += invoice.invoiceAmount;
        invoiceIdsToMarkAsPaid.push(invoice.invoiceId);
    }

    invoiceNos = invoiceNos.substring(0, invoiceNos.length - 2);

    let invoiceAmountSumToDisplay = roundMoney(parseFloat(invoiceAmountSum));

    $form.find("#MarkPaid_bulk_InvoiceNum").val(invoiceNos);
    $form.find("#MarkPaid_bulk_InvoiceAmount").val(invoiceAmountSumToDisplay);
    $form.find("#MarkPaid_bulk_InvoiceId").val(invoiceIdsToMarkAsPaid);

    var today = moment().format("YYYY-MM-DD");
    $form.find("#MarkPaid_bulk_InvoiceDatePaid").val(today);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();
                markAsPaidMultipleInvoices(
                    invoiceIdsToMarkAsPaid,
                    $form.find("#MarkPaid_bulk_InvoiceNote").val(),
                    $form.find("#MarkPaid_bulk_InvoiceDatePaid").val()
                );
            });
    }
    $form.modal("show");
}

function markAsPaidMultipleInvoices(invoiceIds, invoiceNote, invoiceDatePaid) {
    let formData = new FormData();

    $.each(invoiceIds, function (i, id) {
        formData.append("invoiceIds", id);
    });

    formData.append("invoiceNote", invoiceNote);
    formData.append("invoiceDatePaid", invoiceDatePaid);

    let succeeded = false;
    $.ajax({
        url: "/Vendor/MarkAsPaidMutipleInvoices",
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        async: true,
        data: formData,
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
        complete: function () {
            $("#modal-bulk-markPaid").modal("hide");

            $("#DT_bulkPayInvoiceDetailsTable").DataTable().clear().draw();
            $("#DT_InvoiceList").DataTable().ajax.reload();
            $("#DT_PaidInvoiceList").DataTable().ajax.reload();
        },
        success: function (data) {
            if (data.status === "error") {
                alert(data.message);
            }
            succeeded = true;
        },
        error: function (xhr) {
            processError(xhr);
        },
    }).then(function () {
        let f; // TBD: review if this is needed
        if (typeof f !== "undefined" && f !== null) {
            f.modal("hide");
        }
    });
}

function vendor_clearBulkInvoicesBuilder() {
    $("#DT_bulkPayInvoiceDetailsTable").DataTable().clear().draw();
}

function AssignToUser(t, g) {
    var items = vendorGetSelectedItems(t);
    var html = "No items selected";
    var disabled = true;
    if (items.length !== 0) {
        html = "" + items.length + " items selected.";
        disabled = false;
    }
    var groups = vendorGetSelectedItemValues(t, "groupId");
    var groupId;
    $.each(groups, function (i, g) {
        if (i === 0) {
            groupId = g;
        } else {
            if (groupId !== g) {
                //console.log("Different groups selected");
            }
        }
    });
    var $form = $("#modal-assignToUser");
    vendor_init_dialog($form);

    $form.find("#assignToUser_groupId").val(groupId);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [
                    {
                        name: "userId",
                        value: $form.find("#assignToUser_userId").val(),
                    },
                    {
                        name: "groupId",
                        value: $form.find("#assignToUser_groupId").val(),
                    },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/MyServices/AssignToUser",
                    tables,
                    values
                );
            });
    }
    populateUsersList();
    $form.modal("show");
}
function vendor_show_EditBillToInfo(t) {
    var disabled = true;
    billToInfo_Items = vendorGetSelectedItemObjects(t);
    if (billToInfo_Items.length !== 1) {
        // todo: display error
        return;
    }
    disabled = false;
    var data = billToInfo_Items[0];

    var $form = $("#modal-billToInfo");
    vendor_init_dialog($form);

    setBillToInfoFields($form, data);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                updateBillToInfoRow($form);
            });
    }
    $form.modal("show");
}
function updateBillToInfoRow($form) {
    var t = $("#DT_billToListTable").DataTable();
    var selectedRows = t.rows({ selected: true });
    if (selectedRows.data().length === 0) {
        var data = {
            ShortName: "",
            CompanyName: "",
            AddressLine1: "",
            AddressLine2: "",
            City: "",
            State: "",
            ZipCode: "",
            Attn: "",
            Note: "",
        };
        data.ShortName = $form.find("#shortName").val();
        data.CompanyName = $form.find("#custName").val();
        data.AddressLine1 = $form.find("#custAddr1").val();
        data.AddressLine2 = $form.find("#custAddr2").val();
        data.City = $form.find("#custCity").val();
        data.State = $form.find("#custState").val();
        data.ZipCode = $form.find("#custZip").val();
        data.Attn = $form.find("#custAttn").val();
        data.Note = $form.find("#custNote").val();

        var row = t.row.add(data);
    } else {
        $.each(selectedRows.data(), function () {
            this.ShortName = $form.find("#shortName").val();
            this.CompanyName = $form.find("#custName").val();
            this.AddressLine1 = $form.find("#custAddr1").val();
            this.AddressLine2 = $form.find("#custAddr2").val();
            this.City = $form.find("#custCity").val();
            this.State = $form.find("#custState").val();
            this.ZipCode = $form.find("#custZip").val();
            this.Attn = $form.find("#custAttn").val();
            this.Note = $form.find("#custNote").val();
        });
    }
    t.rows().invalidate().draw();
    updateBillToSaveButton(true);
    $form.modal("hide");
}
function vendor_show_AddBillToInfo(t) {
    //var t = $("#DT_billToListTable").DataTable();
    var disabled = false;
    t.rows().deselect();
    billToInfo_Items = [
        {
            ShortName: "",
            CompanyName: "",
            AddressLine1: "",
            AddressLine2: "",
            City: "",
            State: "",
            ZipCode: "",
            Attn: "",
            Note: "",
        },
    ];

    var $form = $("#modal-billToInfo");
    vendor_init_dialog($form);

    setBillToInfoFields($form, billToInfo_Items[0]);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                updateBillToInfoRow($form);
            });
    }
    $form.modal("show");
}
function setBillToInfoFields(dlg, data) {
    dlg.find("#shortName").val(data.ShortName);
    dlg.find("#custAddr1").val(data.AddressLine1);
    dlg.find("#custName").val(data.CompanyName);
    dlg.find("#custAddr2").val(data.AddressLine2);
    dlg.find("#custCity").val(data.City);
    dlg.find("#custState").val(data.State);
    dlg.find("#custZip").val(data.ZipCode);
    dlg.find("#custAttn").val(data.Attn);
    dlg.find("#custNote").val(data.Note);
}
function leftPad(value, maxLength) {
    return String("0".repeat(maxLength) + value).slice(-maxLength);
}

var vendor_items_LCRequested = [];
var vendor_table_LCRequested;
function vendor_show_LCRequested(t) {
    var disabled = true;
    var items = vendorGetSelectedItems(t);
    vendor_items_LCRequested = items;
    vendor_table_LCRequested = t;

    var html = "No items selected";
    var $form = $("#modal-LCRequested");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as LC requested";
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");
    $form.find("#date").val(today);
    $form.find("#date").prop("disabled", disabled);
    $form.find("#selectedCount").html(html);
    $form.find("#autoIncrement").prop("checked", true);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var values = [
                    { name: "date", value: $form.find("#date").val() },
                    {
                        name: "tracking",
                        value: $form.find("#trackingNumber").val(),
                    },
                    {
                        name: "checkNumber",
                        value: $form.find("#checkNumber").val(),
                    },
                    {
                        name: "autoIncrement",
                        value: $form.find("#autoIncrement").prop("checked"),
                    },
                ];
                var tables = [t, $("#DT_LCLI_Pending"), $("#DT_LC_Completed")];
                vendor_action_HELPER(
                    $form,
                    vendor_items_LCRequested,
                    "/Vendor/LCRequested",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}

var vendor_items_LCReceivedFromDmv = [];
var vendor_table_LCReceivedFromDmv;
function vendor_show_LCReceivedFromDmv(t) {
    var disabled = true;
    var items = vendorGetSelectedItems(t);
    vendor_items_LCReceivedFromDmv = items;
    vendor_table_LCReceivedFromDmv = t;

    var html = "No items selected";
    var $form = $("#modal-LCReceivedFromDmv");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as LC received from DMV";
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");
    $form.find("#date").val(today);
    $form.find("#date").prop("disabled", disabled);
    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var values = [
                    { name: "date", value: $form.find("#date").val() },
                ];
                var tables = [t, $("#DT_CompletedLC")];
                vendor_action_HELPER(
                    $form,
                    vendor_items_LCReceivedFromDmv,
                    "/Vendor/LCReceivedFromDmv",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}

var vendor_items_LCRejected = [];
var vendor_table_LCRejected;
function vendor_show_LCRejected(t) {
    var disabled = true;
    var items = vendorGetSelectedItems(t);
    vendor_items_LCRejected = items;
    vendor_table_LCRejected = t;

    var html = "No items selected";
    var $form = $("#modal-LCRejected");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as LC rejected";
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");
    $form.find("#date").val(today);
    $form.find("#date").prop("disabled", disabled);
    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var values = [
                    { name: "date", value: $form.find("#date").val() },
                ];
                var tables = [t, $("#DT_LC_Completed")];
                vendor_action_HELPER(
                    $form,
                    vendor_items_LCRejected,
                    "/Vendor/LCRejected",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}

var vendor_items_LIRequested = [];
var vendor_table_LIRequested;
function vendor_show_LIRequested(t) {
    var disabled = true;
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var $form = $("#modal-LIRequested");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as LI requested";
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");
    $form.find("#date").val(today);
    $form.find("#date").prop("disabled", disabled);
    $form.find("#trackingNumber").val("");
    $form.find("#trackingNumber").prop("disabled", disabled);
    $form.find("#checkNumber").val("");
    $form.find("#checkNumber").prop("disabled", disabled);
    $form.find("#selectedCount").html(html);
    $form.find("#autoIncrement").prop("checked", true);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var values = [
                    { name: "date", value: $form.find("#date").val() },
                    {
                        name: "tracking",
                        value: $form.find("#trackingNumber").val(),
                    },
                    {
                        name: "checkNumber",
                        value: $form.find("#checkNumber").val(),
                    },
                    {
                        name: "autoIncrement",
                        value: $form.find("#autoIncrement").prop("checked"),
                    },
                ];

                var tables = [t, $("#DT_Pending")];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/LIRequested",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
var vendor_items_LIReceivedFromDmv = [];
var vendor_table_LIReceivedFromDmv;
function vendor_show_LIReceivedFromDmv(t) {
    var disabled = true;
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var $form = $("#modal-LIReceivedFromDmv");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as LI received from DMV";
        disabled = false;
    }
    var today = moment().format("YYYY-MM-DD");
    $form.find("#date").val(today);
    $form.find("#date").prop("disabled", disabled);
    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var values = [
                    { name: "date", value: $form.find("#date").val() },
                ];
                var tables = [t, $("#DT_LC_Request")];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/LIReceivedFromDmv",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function AjaxReload(t, resetPaging) {
    if (typeof t !== "undefined" && t !== null) {
        if (t instanceof jQuery) {
            t = t.DataTable();
        }
        t.ajax.reload(null, resetPaging);
    }
}
function AjaxReloadList(tables, resetPaging) {
    if (typeof tables !== "undefined") {
        // must first convert to DataTable for unique to work
        let tlist = [];
        $.each(tables, function (i, t) {
            if (t instanceof jQuery) {
                tlist.push(t.DataTable());
            } else {
                tlist.push(t);
            }
        });
        tlist = $.uniqueSort(tlist);
        $.each(tlist, function (i, t) {
            AjaxReload(t, resetPaging);
        });
    }
}
function vendor_action_JSON_POST(f, url, refreshList, obj, errctl, callback) {
    var succeeded = false;
    if (errctl) {
        errctl.text("");
    }
    $.ajax({
        url: url,
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        async: true,
        dataType: "json",
        cache: false,
        data: JSON.stringify(obj),
        contentType: "application/json; charset=utf-8",
        processData: false,
        complete: function () {
            // refresh related lists
            if (succeeded && typeof f !== "undefined" && f !== null) {
                f.modal("hide");
            }
            AjaxReloadList(refreshList, false);
        },
        success: function (data) {
            succeeded = vendor_action_HELPER_success(f, data);
        },
        error: function (xhr) {
            succeeded = vendor_action_HELPER_error(xhr, f);
        },
    }).then(function () {
        if (succeeded && typeof f !== "undefined" && f !== null) {
            f.modal("hide");
            if (callback) {
                callback();
            }
        }
    });
}
function vendor_action_HELPER_complete(
    succeeded,
    f,
    refreshList,
    refreshFlag,
    ajaxReload
) {
    if (typeof ajaxReload === "undefined") {
        ajaxReload = true;
    }
    // refresh related lists
    if (typeof f !== "undefined" && f !== null) {
        if (succeeded) {
            f.modal("hide");
        } else {
            // ???
        }
    }
    if (ajaxReload) {
        AjaxReloadList(refreshList, refreshFlag);
    }
}
function vendor_action_HELPER_success(f, data) {
    if (data.status === "error") {
        vendor_show_alert(f, data.message);
        return false;
    } else {
        return true;
    }
}
function vendor_action_HELPER_error(xhr, container) {
    var text = xhr.responseText || "";
    if (text === "") {
        vendor_show_alert(container, "Unknown error occurred", -1);
        return;
    }
    try {
        var obj = JSON.parse(text);
        if (obj) {
            vendor_show_alert(container, obj.message, -1);
        }
    } catch (err) {
        vendor_show_alert(container, text, -1);
    }
}
function vendor_action_HELPER(f, items, url, refreshList, valueList, callback) {
    var formData = new FormData();

    if (containsItems(items)) {
        if (typeof items[0] === "object") {
            $.each(items, function (i, item) {
                let rid = item.requestId || item.RequestId;
                formData.append("ids", rid);
            });
        } else {
            $.each(items, function (i, id) {
                formData.append("ids", id);
            });
        }
        if (typeof valueList !== "undefined") {
            $.each(valueList, function (i, v) {
                if (typeof v !== "undefined" && v !== null) {
                    formData.append(v.name, v.value);
                }
            });
        }

        var succeeded = false;
        $.ajax({
            url: url,
            type: "POST",
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
            async: true,
            data: formData,
            dataType: "json",
            cache: false,
            contentType: false,
            processData: false,
            complete: function () {
                vendor_action_HELPER_complete(
                    succeeded,
                    f,
                    refreshList,
                    false,
                    true
                );
            },
            success: function (data) {
                succeeded = vendor_action_HELPER_success(f, data);
            },
            error: function (xhr) {
                succeeded = vendor_action_HELPER_error(xhr, f);
            },
        }).then(function () {
            if (typeof f !== "undefined" && f !== null && succeeded) {
                f.modal("hide");
            }
            if (callback) callback(succeeded);
        });
    }
}

function vendor_action_Mark_as_Shipped_HELPER(
    f,
    items,
    url,
    refreshList,
    valueList,
    callback
) {
    var formData = new FormData();

    if (containsItems(items)) {
        if (typeof items[0] === "object") {
            $.each(items, function (i, item) {
                let rid = item.documentReceivedID || item.documentReceivedID;
                formData.append("documentReceivedIds", rid);
            });
        } else {
            $.each(items, function (i, id) {
                formData.append("documentReceivedIds", id);
            });
        }
        if (typeof valueList !== "undefined") {
            $.each(valueList, function (i, v) {
                if (typeof v !== "undefined" && v !== null) {
                    formData.append(v.name, v.value);
                }
            });
        }

        var succeeded = false;
        $.ajax({
            url: url,
            type: "POST",
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
            async: true,
            data: formData,
            dataType: "json",
            cache: false,
            contentType: false,
            processData: false,
            complete: function () {
                vendor_action_HELPER_complete(
                    succeeded,
                    f,
                    refreshList,
                    false,
                    true
                );
            },
            success: function (data) {
                succeeded = vendor_action_HELPER_success(f, data);
            },
            error: function (xhr) {
                succeeded = vendor_action_HELPER_error(xhr, f);
            },
        }).then(function () {
            if (typeof f !== "undefined" && f !== null && succeeded) {
                f.modal("hide");
            }
            if (callback) callback(succeeded);
        });
    }
}
function vendor_init_dialog(f) {
    f.find("div.alert").hide();
}
function vendor_show_Rejected(t) {
    var disabled = true;
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var $form = $("#modal-Rejected");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as LC rejected";
        disabled = false;
    }

    var today = moment().format("YYYY-MM-DD");
    $form.find("#date").val(today);
    $form.find("#date").prop("disabled", disabled);
    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [
                    { name: "date", value: $form.find("#date").val() },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/LCRejected",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_ShipToDmv(t) {
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-ShipToDmv");
    vendor_init_dialog($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as shipped to DMV";
        $form.find("#selectedCount").html(html);
        disabled = false;
    }
    $form.find("#dmvCourierName").val("");
    $form.find("#dmvTrackingNumber").val("");
    $form.find("#dmvCourierName").prop("disabled", disabled);
    $form.find("#dmvTrackingNumber").prop("disabled", disabled);
    var today = moment().format("YYYY-MM-DD");
    var eta = moment().add(11, "days").format("YYYY-MM-DD");

    $form.find("#dmvETA").val(eta);
    $form.find("#dmvETA").prop("disabled", disabled);
    $form.find("#dateToDmv").val(today);
    $form.find("#dateToDmv").prop("disabled", disabled);
    $form.find("#selectedCount").html(html);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [
                    { name: "date", value: $form.find("#dateToDmv").val() },
                    {
                        name: "courierName",
                        value: $form.find("#dmvCourierName").val(),
                    },
                    {
                        name: "trackingNumber",
                        value: $form.find("#dmvTrackingNumber").val(),
                    },
                    { name: "eta", value: $form.find("#dmvETA").val() },
                ];
                tables.push($("#DT_TitlePending"));
                tables.push($("#DT_Pending"));
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/DmvShipItems",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_DeleteItems(t) {
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-DeleteRequest");
    vendor_init_dialog($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as delete pending";
        disabled = false;
    }
    $form.find("#selectedCount").html(html);
    $form.find("#DeleteRequest_vin").parent().hide();

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                tables.push($("#DT_DeletePending"));
                var values = [];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/DeleteItems",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_Undelete(t) {
    var items = vendorGetSelectedItems(t);

    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-UndeleteRequest");
    vendor_init_dialog($form);

    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected items as HOLD";
        disabled = false;
    }
    $form.find("#selectedCount").html(html);
    $form.find("#UndeleteRequest_vin").parent().hide();

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                tables.push($("#DT_Holds"));
                var values = [];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/UndeleteItems",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_FinalizeDelete(t) {
    var items = vendorGetSelectedItemObjects(t);
    var item = items[0];

    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-DeleteRequest");
    vendor_init_dialog($form);

    var vin = item["vin"];
    var rno = item["requestNo"];
    $form.find("#hiddenvin").val(vin);
    $form.find("#DeleteRequest_vin").val("");

    if (items.length === 1) {
        html = "Operation will delete the " + items.length + " selected item";
        disabled = false;
    }
    $form.find("#selectedCount").html(html);
    $form
        .find("#myModal-label")
        .html("Confirm Delete of " + htmlEncode(vin) + " (" + rno + ")");
    $form.find("#DeleteRequest_vin").parent().show();
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                var enteredVin = $form.find("#DeleteRequest_vin").val().trim();
                if (vin !== enteredVin) {
                    vendor_show_DataTable_alert(t, "VIN does not match");
                } else {
                    var tables = [t];
                    var values = [
                        {
                            name: "vin",
                            value: $form.find("#DeleteRequest_vin").val(),
                        },
                    ];
                    var singleItem = [item["requestId"]];
                    vendor_action_HELPER(
                        $form,
                        singleItem,
                        "/Vendor/FinalizeDelete",
                        tables,
                        values
                    );
                }
            });
    }
    $form.modal("show");
}
function vendor_show_DeleteShipment(t) {
    var items = vendorGetSelectedItemObjects(t);
    var item = items[0];

    var html = "No shipment selected";
    var disabled = true;
    var $form = $("#modal-DeleteShipment");
    vendor_init_dialog($form);

    var shipmentId = item["shipmentId"];
    $form.find("#shipmentId").val(shipmentId);
    $form.find("#moveToShipToLH").prop("checked", true);

    if (items.length === 1) {
        html = "Operation will delete the selected shipment";
        disabled = false;
    }
    $form.find("#selectedCount").html(html);
    $form.find("#myModal-label").html("Confirm delete of shipment");
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                var tables = [t, $("#shipToLHTable")];
                var values = [
                    {
                        name: "shipmentId",
                        value: $form.find("#shipmentId").val(),
                    },
                    {
                        name: "moveToLHQueue",
                        value: $form.find("#moveToShipToLH").prop("checked"),
                    },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/DeleteShipment",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_show_PrintShipment(t) {
    var items = vendorGetSelectedShipments(t);
    if (items.length === 0) {
        return;
    }

    var $form = $("#statementDownload");
    attachToken($form);
    $form.html("");
    $form.append('<input type="hidden" name="fmt" value="pdf" />');
    $form.append('<input type="hidden" name="open" value="1" />');
    $.each(items, function (i, id) {
        $form.append('<input type="hidden" name="ids" value="' + id + '" />');
    });
    attachToken($form);
    $form.trigger("submit");
}
function getUrlVars() {
    var vars = [],
        hash;
    var hashes = window.location.href
        .slice(window.location.href.indexOf("?") + 1)
        .split("&");
    for (var i = 0; i < hashes.length; i++) {
        hash = hashes[i].split("=");
        vars.push(hash[0]);
        vars[hash[0]] = hash[1];
    }
    return vars;
}
function SaveGroupInfo() {
    var data = getGroupInfoFields();

    var formData = new FormData();
    formData.append("groupId", data.GroupId);
    formData.append("json", JSON.stringify(data));

    $.ajax({
        url: "/Vendor/SaveGroupInfo",
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        data: formData,
        async: true,
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
    })
        .done(function (data) {
            if (data.status === "error") {
                alert(data.message);
            } else {
                // no button to update
                resizeNotes();
            }
        })
        .fail(function (e) {
            //let $e = e;
        });
}
function RefreshGroupDetails() {
    var form = $("#groupInfoSubmit");
}
function ToggleExpandNotes(t, id) {
    var val = GetDtSetting(id, "expandNotes", "false");
    val = val === "true" ? "false" : "true";
    SaveDtSetting(id, "expandNotes", val);
    t.ajax.reload();
    return val;
}
function ExpandNotes(t, prefix) {
    SaveDtSetting(prefix, "expandNotes", "true");
    t.ajax.reload();
}
function CollapseNotes(t, id) {
    var key = `ExpandNotes_${id}`;
    window.sessionStorage.setItem(key, "false");
    t.ajax.reload();
}

function generaterandomGuid() {
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(
        /[xy]/g,
        function (c) {
            var r = (Math.random() * 16) | 0,
                v = c === "x" ? r : (r & 0x3) | 0x8;
            return v.toString(16);
        }
    );
}
function getAuditType(auditname) {
    switch (auditname) {
        case etaId:
            return "ETA";
        case pcodeId:
            return "PCODE";
        case holdId:
            return "HOLD";
        case incomingId:
            return "INCOMING";
        case followUpId:
            return "FOLLOWUP";
        default:
            jqToast.error({
                text: `Audit not available`,
            });
    }
}
function audit_completebutton(t, auditname) {
    let AuditTypeStatus = getAuditType(auditname);

    var items = vendorGetShowItemObjects(t);
    if (!checkAuditRecords(items, auditname)) {
        t.page.len(10).draw();
        return;
    }

    var $form = $("#modal-Completeaudit");
    vendor_init_dialog($form);

    function isValidInput() {
        var name = $form.find("#name").val();
        var notes = $form.find("#notes").val();
        return name.trim() !== "" && notes.trim() !== "";
    }
    function updateOkButtonState() {
        $form.find("#okButton").prop("disabled", !isValidInput());
    }
    $form.on("show.bs.modal", function (e) {
        $form.find("#name").val("");
        $form.find("#notes").val("");
        updateOkButtonState();
    });
    $form.find("#name, #notes").on("input", function () {
        updateOkButtonState();
    });
    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var name = $form.find("#name").val();
            var notes = $form.find("#notes").val();

            if (!isValidInput()) {
                return;
            }
            SaveAuditBatchData(t, AuditTypeStatus);
            SetCompleteStatus(t, AuditTypeStatus);
            FinalnoteFortheaudit(t, notes, name, AuditTypeStatus);
            SetCompleteAppendStatus(t, auditname);
            t.one("draw", function () {
                UpdateHeaderForLatestRecord(auditname);
            });

            t.page.len(10).draw();

            $(".start_DT_Audits").removeClass("disabled");
            $(".complete_DT_Audits").addClass("disabled");
            $form.modal("hide");
        });
    $form
        .find("#cancelButton")
        .off("click")
        .on("click", function () {
            t.page.len(10).draw();
            $form.modal("hide");
        });

    $form.modal("show");
}
function enableDisableOnStart(auditname) {
    switch (auditname) {
        case etaId:
            $(".start_DT_Audits").addClass("disabled");
            break;
        case holdId:
            $(".start_DT_HoldAudit").addClass("disabled");
            break;
        case pcodeId:
            $(".start_DT_PCodeAudit").addClass("disabled");
            break;
        case incomingId:
            $(".start_DT_IncomingAudit").addClass("disabled");
            break;
        case followUpId:
            $(".start_DT_FollowupAudit").addClass("disabled");
            break;
        default:
            break;
    }
}
function audit_startbutton(t, auditname) {
    let AuditTypeStatus = getAuditType(auditname);
    var token = $('[name="__RequestVerificationToken"]').val();
    enableDisableOnStart(auditname);
    let item = "";
    if (t.rows({ selected: false }).data()[0].AuditBatchId) {
        item = t.rows({ selected: false }).data()[0].AuditBatchId;
    } else {
        item = generaterandomGuid();
    }
    var items = t
        .rows({ selected: false })
        .data()
        .toArray()
        .map(function (row) {
            return {
                FollowUpId: row.followUpId,
                RequestNo: row.requestNo,
                Auditstatus: "In Progress",
                AuditType: AuditTypeStatus,
                AuditBatchId: item,
            };
        });

    if (auditname === followUpId) {
        $.ajax({
            url: "/Vendor/StartFollowUpAudit",
            type: "POST",
            contentType: "application/json",
            headers: { RequestVerificationToken: token },
            data: JSON.stringify(items),
            success: function (response) {
                jqToast.success({
                    text: `Audit ${AuditTypeStatus} has been started`,
                });
                t.ajax.reload();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                jqToast.error({
                    text: `Audit ${AuditTypeStatus} has not been started`,
                });
            },
        });
    } else {
        $.ajax({
            url: "/Vendor/StartAudit",
            type: "POST",
            contentType: "application/json",
            headers: { RequestVerificationToken: token },
            data: JSON.stringify(items),
            success: function (response) {
                jqToast.success({
                    text: `Audit ${AuditTypeStatus} has been started`,
                });
                t.ajax.reload();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                jqToast.error({
                    text: `Audit ${AuditTypeStatus} has not been started`,
                });
            },
        });
    }
}
function vendor_BulkUpdateAudit(t, tblid) {
    var items = vendorGetSelectedItemObjects(t);
    var html = "No items selected";
    var disabled = true;
    if (items.length !== 0) {
        html =
            "Operation will update notes for the " +
            items.length +
            " selected items";
        disabled = false;
    }
    var $form = $("#modal-bulkaudit");
    vendor_init_dialog($form);
    $form.find("#selectedCount").html(html);

    $form.find("#internal").val(items[0].internal);
    if (tblid === followUpId) {
        $form.find("#notes").val(items[0].auditNotes);
    } else {
        $form.find("#notes").val(items[0].notes);
    }
    $form.find("#outcome").val(items[0].outcome);

    vendor_bulk_set_multi_code_title_audit_header($form, items, tblid);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var outcome = $form.find("#outcome").val();
                var notes = $form.find("#notes").val();
                var internal = $form.find("#internal").val();
                var assignedUserName = $form.find("#assignedUser").val();
                let items = [];
                if (tblid === followUpId) {
                    items = vendorGetSelectedItemFollowUpObjectsForBulkAudits(
                        t,
                        outcome,
                        notes,
                        internal,
                        assignedUserName
                    );
                } else {
                    items = vendorGetSelectedItemObjectsForBulkAudits(
                        t,
                        outcome,
                        notes,
                        internal,
                        assignedUserName
                    );
                }
                updateAndSaveAuditRecord(items, t, tblid);
                $form.modal("hide");
            });
    }
    $.ajax({
        url: "/Vendor/GetAllAuditOutcomeComboboxData",
        type: "GET",
        success: function (data) {
            var filteredOptions = data.filter(function (option) {
                switch (tblid) {
                    case etaId:
                        return option.auditType === "ETA";
                    case pcodeId:
                        return option.auditType === "PCODE";
                    case holdId:
                        return option.auditType === "HOLD";
                    case incomingId:
                        return option.auditType === "INCOMING";
                    case followUpId:
                        return option.auditType === "FOLLOWUP";
                    default:
                        return false;
                }
            });
            var outcomeOptions = filteredOptions.map(function (option) {
                return `<option value="${option.name}">${option.name}</option>`;
            });

            $form.find("#outcome").html(outcomeOptions.join(""));
            $form.find("#outcome").val(items[0].outcome);
        },
        error: function (xhr, status, error) {
            console.error("Error fetching outcome options: ", error);
        },
    });

    $.ajax({
        url: "/Vendor/GetAssignedUsersDataForCombobox",
        type: "GET",
        success: function (data) {
            var assignedUserOptions = data.map(function (user) {
                return `<option value="${user.userId}">${user.displayName}</option>`;
            });
            $form.find("#assignedUser").html(assignedUserOptions.join(""));
            $form.find("#assignedUser").val(items[0].assignedUser);
        },
        error: function (xhr, status, error) {
            console.error("Error fetching assigned users data: ", error);
        },
    });

    $form.modal("show");
}
function checkAuditRecords(items, auditname) {
    for (let i = 0; i < items.length; i++) {
        if (auditname === followUpId) {
            if (!items[i].auditNotes || !items[i].outcome) {
                jqToast.error({
                    text: "Please ensure that none of the records in the audit have empty values in the auditNotes or outcome columns.",
                });
                return false;
            }
        } else {
            if (!items[i].notes || !items[i].outcome) {
                jqToast.error({
                    text: "Please ensure that none of the records in the audit have empty values in the notes or outcome columns.",
                });
                return false;
            }
        }
    }
    return true;
}
function getStatusText(statusId) {
    switch (statusId) {
        case 0:
            return "Pending";
        case 1:
            return "Active";
        case 2:
            return "Complete";
        case 9:
            return "Hold";
        case 10:
            return "Cancelled";
        case 99:
            return "DeletePending";
        case 100:
            return "Deleted";
        default:
            return "";
    }
}
function getOutcomeOptionsByTblid(tblid) {
    switch (tblid) {
        case etaId:
            return allOutcomeOptions["ETA"] || [];
        case pcodeId:
            return allOutcomeOptions["PCODE"] || [];
        case holdId:
            return allOutcomeOptions["HOLD"] || [];
        case incomingId:
            return allOutcomeOptions["INCOMING"] || [];
        case followUpId:
            return allOutcomeOptions["FOLLOWUP"] || [];
        default:
            return [];
    }
}
function SaveAuditBatchData(t, name) {
    var Saveditems = t
        .rows({ selected: false })
        .data()
        .toArray()
        .map(function (row) {
            var data = "";
            if (name === "FOLLOWUP") {
                data = row.auditNotes;
            } else {
                data = row.notes;
            }
            return {
                AuditBatchId: row.auditBatchId.toString(),
                ProcessStageName: row.processStageName,
                Status: getStatusText(row.statusId),
                Type: row.appType,
                State: row.state,
                Vin: row.vin,
                RequestNo: parseInt(row.requestNo),
                VehicleMake: row.vehicleMake,
                VehicleYear: row.vehicleYear,
                GroupName: row.groupName,
                RT_LH: row.rT_LH,
                DT_LH_Name: row.dT_LH_Name,
                Eta: row.eta,
                DateToDmv: row.dateToDmv || null,
                LH_SToV: row.dateToVendor || null,
                RepoDate: row.repoDate || null,
                Odometer: row.odometer || null,
                Created: row.createdDate || null,
                CompletedDate: row.completedDate || null,
                DueDate: row.dueDate || null,
                Title: row.title || null,
                Outcome: row.outcome,
                Notes: data,
                Internal: row.internal,
                AssignedUser: row.assignedUser,
                AuditType: row.auditType,
                AuditstartTime: row.auditTime,
                AuditCompleteTime: null,
                AuditCompletedBy: row.assignedBy,
                AuditstartedBy: row.assignedBy,
            };
        });
    var token = $('[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: "/Vendor/SaveCompletedAuditData",
        type: "POST",
        contentType: "application/json",
        headers: { RequestVerificationToken: token },
        data: JSON.stringify(Saveditems),
        success: function (response) {},
        error: function (jqXHR, textStatus, errorThrown) {
            jqToast.error({ text: "Audit has not been started" });
        },
    });
}
function Table_TypeData(tblid) {
    switch (tblid) {
        case etaId:
            $("#myModal-labelForPrior").text("Previous ETA Audits");
            return "ETA";
        case holdId:
            $("#myModal-labelForPrior").text("Previous Hold Audits");
            return "HOLD";
        case pcodeId:
            $("#myModal-labelForPrior").text("Previous PCode Audits");
            return "PCODE";
        case incomingId:
            $("#myModal-labelForPrior").text("Previous Incoming Audits");
            return "INCOMING";
        case followUpId:
            $("#myModal-labelForPrior").text("Previous Follow Up Audits");
            return "FOLLOWUP";
        default:
            return null;
    }
}
function Table_TypeDataForNoRecord(tblid) {
    switch (tblid) {
        case etaId:
            return "ETA";
        case holdId:
            return "Hold";
        case pcodeId:
            return "PCode";
        case incomingId:
            return "Incoming";
        case followUpId:
            return "Follow Up";
        default:
            return null;
    }
}
function Prior_Auditbutton(t, tblid) {
    var $form = $("#modal-PriorCompleteaudit");
    var filterData = Table_TypeData(tblid);
    var noRecord = Table_TypeDataForNoRecord(tblid);

    vendor_init_dialog($form);

    $.ajax({
        url: "/Vendor/GetAuditPriorData",
        type: "GET",
        data: { filterData: filterData },
        success: function (data) {
            var $tbody = $("#audit-list-body");
            $tbody.empty();

            if (data && data.length > 0) {
                data.forEach(function (item, index) {
                    var formattedPreviousTotalTime = formatTotalPreviousTime(
                        item.startTime,
                        item.auditFinalTime
                    );
                    var row = `<tr data-id="${item.auditBatchId}">
                                    <td><b>${index + 1}</b></td> <!-- S.No Column -->
                                    <td>${item.auditName}</td>
                                    <td>${item.auditFinalNote}</td>
                                    <td>${new Date(item.auditFinalTime).toDateString()}</td>
                                    <td>${formattedPreviousTotalTime}</td>
                                    <td>${item.auditCompletedBy}</td>                               
                                    <td><a class="btn btn-outline-primary select-audit" href="/Vendor/AuditLookup/${item.auditType}/${item.auditBatchId}" target="_blank">Open</a></td>
                                </tr>`;
                    $tbody.append(row);
                });
            } else {
                $tbody.append(
                    `<tr><td colspan="7" style="text-align: center;">No previous records available for the ${noRecord} Audit</td></tr>`
                );
            }

            var recordCount = $form.find("#record-count");
            recordCount.text(`Total Audits: ${data.length}`);
        },
        error: function (xhr, status, error) {
            $form
                .find(".alert-danger")
                .text("Failed to load audit data")
                .show();
        },
    });

    $form.modal("show");
}
function formatTotalPreviousTime(startTime, completeTime) {
    var start = new Date(startTime);
    var end = new Date(completeTime);

    var years = end.getFullYear() - start.getFullYear();
    var months = end.getMonth() - start.getMonth();
    var days = end.getDate() - start.getDate();

    if (days < 0) {
        months--;
        var lastMonth = new Date(
            end.getFullYear(),
            end.getMonth() - 1,
            start.getDate()
        );
        days += new Date(end.getFullYear(), end.getMonth(), 0).getDate();
    }

    if (months < 0) {
        years--;
        months += 12;
    }
    var diff = end - start;
    var totalSeconds = Math.floor(diff / 1000);
    var totalMinutes = Math.floor(totalSeconds / 60);
    var totalHours = Math.floor(totalMinutes / 60);

    var hours = totalHours % 24;
    var minutes = totalMinutes % 60;
    var seconds = totalSeconds % 60;

    var formattedTime =
        `${years > 0 ? years + "y " : ""}` +
        `${months > 0 ? months + "mo " : ""}` +
        `${days > 0 ? days + "d " : ""}` +
        `${hours}h ` +
        `${minutes}m ` +
        `${seconds}s`;

    return formattedTime;
}
function SetCompleteAppendStatus(t, auditname) {
    var token = $('[name="__RequestVerificationToken"]').val();
    var items = vendorGetShowItemObjects(t);
    items.forEach((item) => {
        var formattedNote = "";
        if (auditname === followUpId) {
            formattedNote = `Audit ${item.auditType} generated note: ${item.auditNotes}. Reviewed by: ${item.assignedUser}`;
        } else {
            formattedNote = `Audit ${item.auditType} generated note: ${item.notes}. Reviewed by: ${item.assignedUser}`;
        }
        var formattedRemark = `Audit ${item.auditType} generated outcome: ${item.outcome}. Reviewed by: ${item.assignedUser}`;
        if (item.internal) {
            formattedRemark += `\nAudit ${item.auditType} generated internal: ${item.internal}. Reviewed by: ${item.assignedUser}`;
        } else {
            formattedRemark += `\nNo internal remark`;
        }
        $.ajax({
            url: "/Vendor/AppendNotesData",
            type: "POST",
            contentType: "application/json",
            headers: { RequestVerificationToken: token },
            data: JSON.stringify({
                RequestId: item.requestId,
                Note: formattedNote,
                Remark: formattedRemark,
            }),
            success: function (response) {},
            error: function (error) {
                jqToast.error({
                    text: `Data not saved for R# ${item.requestNo}`,
                });
            },
        });
    });
}
function SetCompleteStatus(t, AuditTypeStatus) {
    var token = $('[name="__RequestVerificationToken"]').val();
    var items = t
        .rows({ selected: false })
        .data()
        .toArray()
        .map(function (row) {
            return {
                RequestNo: row.requestNo,
                FollowUpId: row.followUpId,
                Auditstatus: "Completed",
                AuditType: AuditTypeStatus,
                AuditBatchId: row.auditBatchId,
            };
        });

    if (AuditTypeStatus === "FOLLOWUP") {
        $.ajax({
            url: "/Vendor/StartFollowUpAudit",
            type: "POST",
            contentType: "application/json",
            headers: { RequestVerificationToken: token },
            data: JSON.stringify(items),
            success: function (response) {
                if (response[0]?.auditstatus === "Completed") {
                    enableDisableOnCompleteEvent(AuditTypeStatus);
                }
                t.ajax.reload();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                jqToast.error({
                    text: `Audit ${AuditTypeStatus} has not been started`,
                });
            },
        });
    } else {
        $.ajax({
            url: "/Vendor/StartAudit",
            type: "POST",
            contentType: "application/json",
            headers: { RequestVerificationToken: token },
            data: JSON.stringify(items),
            success: function (response) {
                if (response[0]?.auditstatus === "Completed") {
                    enableDisableOnCompleteEvent(AuditTypeStatus);
                }
                t.ajax.reload();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                jqToast.error({
                    text: `Audit ${AuditTypeStatus} has not been started`,
                });
            },
        });
    }
}
function enableDisableOnCompleteEvent(AuditTypeStatus) {
    switch (AuditTypeStatus) {
        case "ETA":
            $(".audit_ETAheader").hide();
            $(".audit_ETArealtimer").hide();
            break;
        case "PCODE":
            $(".audit_PCodeheader").hide();
            $(".audit_PCoderealtimer").hide();
            break;
        case "HOLD":
            $(".audit_Holdheader").hide();
            $(".audit_Holdrealtimer").hide();
            break;
        case "INCOMING":
            $(".audit_Incomingheader").hide();
            $(".audit_Incomingrealtimer").hide();
            break;
        case "FOLLOWUP":
            $(".audit_FollowUpheader").hide();
            $(".audit_Followuprealtimer").hide();
            break;
    }
}
function SetStartCompleteButton(tblid) {
    switch (tblid) {
        case etaId:
            $(".start_DT_Audits").addClass("disabled");
            $(".complete_DT_Audits").addClass("disabled");
            break;
        case pcodeId:
            $(".start_DT_PCodeAudit").addClass("disabled");
            $(".complete_DT_PCodeAudit").addClass("disabled");
            break;
        case holdId:
            $(".start_DT_HoldAudit").addClass("disabled");
            $(".complete_DT_HoldAudit").addClass("disabled");
            break;
        case incomingId:
            $(".start_DT_IncomingAudit").addClass("disabled");
            $(".complete_DT_IncomingAudit").addClass("disabled");
            break;
        case followUpId:
            $(".start_DT_FollowupAudit").addClass("disabled");
            $(".complete_DT_FollowupAudit").addClass("disabled");
            break;
        default:
            break;
    }
}
function HandleAuditData(
    data,
    auditType,
    enableDisableInProgress,
    enableDisableNotInProgress
) {
    var hasExecutedOnce = false;
    if (data.auditstatus === "In Progress") {
        if (!hasExecutedOnce) {
            setHeaderTimerforAudit(
                data.auditstatus,
                data.auditTime,
                data.assignedBy,
                auditType
            );
            enableDisableInProgress();
            hasExecutedOnce = true;
        }
    } else {
        if (!hasExecutedOnce) {
            enableDisableNotInProgress();
            hasExecutedOnce = true;
        }
    }
}
function enableDisableInProgressForETA() {
    $(".start_DT_Audits").addClass("disabled");
    $(".complete_DT_Audits").removeClass("disabled");
}
function enableDisableNotInProgressForETA() {
    $(".start_DT_Audits").removeClass("disabled");
    $(".complete_DT_Audits").addClass("disabled");
}
function enableDisableInProgressForPCode() {
    $(".start_DT_PCodeAudit").addClass("disabled");
    $(".complete_DT_PCodeAudit").removeClass("disabled");
}
function enableDisableNotInProgressForPCode() {
    $(".start_DT_PCodeAudit").removeClass("disabled");
    $(".complete_DT_PCodeAudit").addClass("disabled");
}
function enableDisableInProgressForHOLD() {
    $(".start_DT_HoldAudit").addClass("disabled");
    $(".complete_DT_HoldAudit").removeClass("disabled");
}
function enableDisableNotInProgressForHOLD() {
    $(".start_DT_HoldAudit").removeClass("disabled");
    $(".complete_DT_HoldAudit").addClass("disabled");
}
function enableDisableInProgressForIncoming() {
    $(".start_DT_IncomingAudit").addClass("disabled");
    $(".complete_DT_IncomingAudit").removeClass("disabled");
}
function enableDisableNotInProgressForIncoming() {
    $(".start_DT_IncomingAudit").removeClass("disabled");
    $(".complete_DT_IncomingAudit").addClass("disabled");
}
function enableDisableInProgressForFollowup() {
    $(".start_DT_FollowupAudit").addClass("disabled");
    $(".complete_DT_FollowupAudit").removeClass("disabled");
}
function enableDisableNotInProgressForFollowup() {
    $(".start_DT_FollowupAudit").removeClass("disabled");
    $(".complete_DT_FollowupAudit").addClass("disabled");
}

function updateTimer(AuditstartTimeUTC, timerElement) {
    let newdate = convertLocalTime(AuditstartTimeUTC);
    var auditStartDate = new Date(newdate);
    var currentTime = new Date();
    var elapsedTime = currentTime - auditStartDate;

    var years = currentTime.getFullYear() - auditStartDate.getFullYear();
    var months = currentTime.getMonth() - auditStartDate.getMonth();
    var days = currentTime.getDate() - auditStartDate.getDate();
    var hours = currentTime.getHours() - auditStartDate.getHours();
    var minutes = currentTime.getMinutes() - auditStartDate.getMinutes();
    var seconds = currentTime.getSeconds() - auditStartDate.getSeconds();

    if (seconds < 0) {
        seconds += 60;
        minutes--;
    }
    if (minutes < 0) {
        minutes += 60;
        hours--;
    }
    if (hours < 0) {
        hours += 24;
        days--;
    }
    if (days < 0) {
        months--;
        var lastMonth = currentTime.getMonth() - 1;
        if (lastMonth < 0) lastMonth = 11;
        days += new Date(currentTime.getFullYear(), lastMonth + 1, 0).getDate();
    }
    if (months < 0) {
        months += 12;
        years--;
    }

    var timerHtml = '<span class="clock">';
    if (years > 0) timerHtml += `${years}y `;
    if (months > 0) timerHtml += `${months}m `;
    if (days > 0) timerHtml += `${days}d `;
    timerHtml += `${hours}h ${minutes}m ${seconds}s</span>`;

    $(timerElement).html(timerHtml);
}

function convertLocalTime(auditTimeUTC) {
    let auditDate = new Date(auditTimeUTC);
    let offsetMinutes = auditDate.getTimezoneOffset();
    let utcTime = auditDate.getTime();
    let localTime = new Date(utcTime - offsetMinutes * 60 * 1000);
    let localDate = localTime.toLocaleString();
    return localDate;
}
function setHeaderTimerforAudit(
    Auditstatus,
    auditTimeUTC,
    assignedBy,
    auditname
) {
    var auditHeaderHtml =
        '<span class="static-data red">' +
        Auditstatus +
        " – started at " +
        convertLocalTime(auditTimeUTC) +
        " by " +
        assignedBy +
        "</span>";
    var AuditstartTime = auditTimeUTC;
    var timerElement;
    switch (auditname) {
        case "Audits":
            timerElement = ".audit_ETArealtimer";
            $(".audit_ETAheader").html(auditHeaderHtml);
            $(".audit_ETAheader, .audit_ETArealtimer").show();
            break;
        case "PCodeAudit":
            timerElement = ".audit_PCoderealtimer";
            $(".audit_PCodeheader").html(auditHeaderHtml);
            $(".audit_PCodeheader, .audit_PCoderealtimer").show();
            break;
        case "HoldAudit":
            timerElement = ".audit_Holdrealtimer";
            $(".audit_Holdheader").html(auditHeaderHtml);
            $(".audit_Holdheader, .audit_Holdrealtimer").show();
            break;
        case "IncomingAudit":
            timerElement = ".audit_Incomingrealtimer";
            $(".audit_Incomingheader").html(auditHeaderHtml);
            $(".audit_Incomingheader, .audit_Incomingrealtimer").show();
            break;
        case "FollowupAudit":
            timerElement = ".audit_Followuprealtimer";
            $(".audit_FollowUpheader").html(auditHeaderHtml);
            $(".audit_FollowUpheader, .audit_Followuprealtimer").show();
            break;
        default:
            return;
    }

    if (auditTimerIntervals[auditname]) {
        clearInterval(auditTimerIntervals[auditname]);
    }
    auditTimerIntervals[auditname] = setInterval(function () {
        updateTimer(AuditstartTime, timerElement);
    }, 1000);

    updateTimer(AuditstartTime, timerElement);
}
function vendor_show_linkRecord(t) {
    // Get data from data attributes on table element (not DataTable)
    var $panel = $("#linkedRequestsPanel");
    var reqid = $panel.data("reqid");
    var reqno = $panel.data("reqno");
    var vin = $panel.data("vin");

    let $form = $("#modal-LinkRequests");
    vendor_set_single_vin_rno_header($form, "Link Request", reqno, vin);
    var $okButton = $form.find("#okButton");
    $okButton.prop("disabled", true);

    $form.find("#LinkRequests_SearchReqNo").val("");
    $form.find("#LinkRequests_SearchVIN").val(vin);

    var dtLinkSearchTable = $("#DT_LinkSearch").DataTable();

    dtLinkSearchTable
        .off("select deselect")
        .on("select deselect", function (e, dt, type, indexes) {
            if (type === "row") {
                if (
                    dtLinkSearchTable.rows({ selected: true }).indexes()
                        .length === 1
                ) {
                    $okButton.prop("disabled", false);
                } else {
                    $okButton.prop("disabled", true);
                }
            }
        });
    $okButton.off("click").on("click", function () {
        var items = GetSelectedLinkSearchRequests(dtLinkSearchTable);
        if (items.length !== 1) {
            // tbd - show error
            return;
        }
        var linkRequestId = items[0].RequestId;
        if (linkRequestId !== "") {
            var model = {
                RequestId: reqid,
                LinkRequestId: linkRequestId,
            };
            var table = $("#DT_LinkedRequests").DataTable();
            vendor_action_JSON_POST(
                $form,
                "/Requests/LinkRequest",
                [table],
                model,
                $("#LinkRequests_error"),
                function () {
                    $form.find("#LinkRequests_SearchVIN").val("");
                    $form.find("#LinkRequests_SearchReqNo").val("");
                    table.ajax.reload();
                }
            );
        } else {
            let msg = "Record Number required";
            let errctl;
            if (errctl) {
                errctl.val(msg);
            } else {
                alert(msg);
            }
        }
    });
    $form.modal("show");
    // Initiate vin search
    $form.find("#LinkRequests_SearchVIN_btn").trigger("click");
}
function GetSelectedLinkSearchRequests(t) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push({ RequestId: this["requestId"] });
    });
    return a;
}
function vendorGetSelectedLinkedRequests(t) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push({ RequestId: this["requestId"], RequestNo: this["requestNo"] });
    });
    return a;
}
function vendor_show_unlinkRequests(t) {
    var items = vendorGetSelectedLinkedRequests(t);

    if (items.length !== 1) {
        return;
    }
    var $panel = $("#linkedRequestsPanel");
    var reqid = $panel.data("reqid");
    var reqno = $panel.data("reqno");
    var vin = $panel.data("vin");

    var $form = $("#modal-UnlinkRequests");
    vendor_init_dialog($form);

    vendor_set_single_vin_rno_header($form, "Unlink Request", reqno, vin);
    var $okButton = $form.find("#okButton");
    var dtLinkSearchTable = $("#DT_LinkSearch").DataTable();

    $form.find("#UnlinkRequests_RequestNo").text(reqno);
    $form.find("#UnlinkRequests_LinkedRequestNo").text(items[0].RequestNo);

    $okButton.off("click").on("click", function () {
        var model = {
            RequestId: reqid,
            LinkRequestId: items[0].RequestId,
        };
        var table = $("#DT_LinkedRequests").DataTable();
        vendor_action_JSON_POST(
            $form,
            "/Requests/UnlinkRequest",
            [table],
            model,
            $("#UnlinkRequests_error"),
            function () {
                $form.find("#UnlinkRequests_RequestNo").text("");
                $form.find("#UnlinkRequests_LinkRequestNo").text("");
                $form.find("#UnlinkRequests_RequestId").text("");
            }
        );
    });
    $form.modal("show");
}
function vendor_linkSearch_vin() {
    var vin = ($("#LinkRequests_SearchVIN").val() || "").trim();
    if (vin.length > 17) {
        vin = vin.substr(0, 17);
    }
    if (vin.length === 0) {
        return;
    }
    var $panel = $("#linkedRequestsPanel");
    var reqno = $panel.data("reqno");
    var url = `/RequestStatus/LinkSearchVin/${vin}/${reqno}`;
    var table = $("#DT_LinkSearch").DataTable();
    table.ajax.url(url).load();
}
function vendor_linkSearch_clear() {
    var url = `/RequestStatus/LinkSearchVin`;
    $("#DT_LinkSearch").DataTable().ajax.url(url).load();
    return false;
}
function vendor_linkSearch_reqno() {
    var reqno = ($("#LinkRequests_SearchReqNo").val() || "").trim();
    var url = `/RequestStatus/LinkSearchReqNo/${reqno}`;
    $("#DT_LinkSearch").DataTable().ajax.url(url).load();
    return false;
}
function vendor_action_JSON_GET(refreshtables, url, obj, completefunc) {
    $.ajax({
        url: url,
        type: "GET",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        async: true,
        dataType: "json",
        cache: false,
        data: obj === null ? "" : JSON.stringify(obj),
        contentType: obj === null ? "" : "application/json; charset=utf-8",
        processData: false,
        complete: function () {
            //func(resultData);
        },
        success: function (data) {
            completefunc(refreshtables, data);
        },
        error: function (xhr) {
            processError(xhr);
        },
    });
}
function vendor_BulkUpdateCode(t) {
    var items = vendorGetSelectedItemObjects(t);
    var html = "No items selected";
    var disabled = true;
    if (items.length !== 0) {
        html =
            "Operation will update codes for the " +
            items.length +
            " selected items";
        disabled = false;
    }
    var $form = $("#modal-bulkcode");
    vendor_init_dialog($form);

    $form.find("#selectedCount").html(html);
    $form.find("#bulkCode").val("");
    var today = moment().format("YYYY-MM-DD");
    vendor_bulk_set_multi_code_title_header($form, items);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [
                    { name: "code", value: $form.find("#bulkCode").val() },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/BulkUpdate",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendorGetSelectedItemObjectsForAudits(t, name) {
    var a = [];
    t.rows({ selected: true }).every(function () {
        var rowData = this.data();
        rowData.outcome = $(this.node()).find(".getoutcomedata").val();
        if (name === followUpId) {
            rowData.auditNotes = $(this.node()).find(".getnotesdata").val();
        } else {
            rowData.notes = $(this.node()).find(".getnotesdata").val();
        }
        rowData.internal = $(this.node()).find(".getinputdata").val();
        let $node = $(this.node());
        let $selectElement = $node.find(".getassigneduserdata");
        let selectedUserID = $selectElement.val();
        let $selectedOption = $selectElement.find(
            `option[value="${selectedUserID}"]`
        );
        let userName = $selectedOption.length ? $selectedOption.text() : "";
        rowData.assignedUser = userName;
        a.push(rowData);
    });
    return a;
}
function updateAndSaveAuditRecord(items, t, name) {
    if (items.length <= 0) {
        jqToast.error({
            text: "Select at least one record to save.",
        });
        return;
    }
    let auditrows;
    if (name === followUpId) {
        auditrows = items.filter((x) => x.auditNotes && x.assignedUser);
    } else {
        auditrows = items.filter((x) => x.notes && x.assignedUser);
    }

    if (auditrows.length > 0 && items.length === auditrows.length) {
        var token = $('[name="__RequestVerificationToken"]').val();
        if (auditrows[0].followUpId) {
            $.ajax({
                url: "/Vendor/BulkUpdateFollowUpAudit",
                type: "POST",
                contentType: "application/json",
                headers: { RequestVerificationToken: token },
                data: JSON.stringify(items),
                success: function (response) {
                    jqToast.success({
                        text: "Audit Records have been successfully assigned." /**/,
                    });
                    t.ajax.reload();
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    jqToast.error({
                        text: "Error in UpdateAudit",
                    });
                },
            });
        } else {
            $.ajax({
                url: "/Vendor/BulkUpdateAudit",
                type: "POST",
                contentType: "application/json",
                headers: { RequestVerificationToken: token },
                data: JSON.stringify(items),
                success: function (response) {
                    jqToast.success({
                        text: "Audit Records have been successfully assigned.",
                    });
                    t.ajax.reload();
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    jqToast.error({
                        text: "Error in UpdateAudit",
                    });
                },
            });
        }
    } else {
        jqToast.error({
            text: "Please add a note and assign a user to proceed further.",
        });
    }
}
function UpdateHeaderForLatestRecord(id) {
    var token = $('[name="__RequestVerificationToken"]').val();
    let data = getAuditType(id);
    $.ajax({
        url: "/Vendor/GetLatestAuditByTypeAsync",
        type: "GET",
        data: { auditType: data },
        headers: { RequestVerificationToken: token },
        success: function (data) {
            if (data) {
                var auditHeaderlookup =
                    '<span class="static-data green">Last audit was completed at ' +
                    convertLocalTime(data.auditFinalTime) +
                    " by " +
                    data.auditCompletedBy +
                    " . The total time taken is " +
                    formatTotalPreviousTime(
                        data.startTime,
                        data.auditFinalTime
                    ) +
                    "</span>";
                switch (id) {
                    case etaId:
                        $(".audit_ETAheader").html(auditHeaderlookup);
                        $(".audit_ETAheader").show();
                        break;
                    case pcodeId:
                        $(".audit_PCodeheader").html(auditHeaderlookup);
                        $(".audit_PCodeheader").show();
                        break;
                    case holdId:
                        $(".audit_Holdheader").html(auditHeaderlookup);
                        $(".audit_Holdheader").show();
                        break;
                    case incomingId:
                        $(".audit_Incomingheader").html(auditHeaderlookup);
                        $(".audit_Incomingheader").show();
                        break;
                    case followUpId:
                        $(".audit_FollowUpheader").html(auditHeaderlookup);
                        $(".audit_FollowUpheader").show();
                        break;
                    default:
                        break;
                }
            }
        },
        error: function (xhr, textStatus, errorThrown) {
            console.error("Error fetching latest audit:", errorThrown);
        },
    });
}
function SetheaderForPreviousCompleteButton(t, id) {
    var allHaveClass = true;
    if (t.rows().data().length > 0) {
        t.rows().every(function () {
            if (!$(this.node()).hasClass("row-Frozen")) {
                allHaveClass = false;
                return false;
            }
        });
        if (!allHaveClass) {
            UpdateHeaderForLatestRecord(id);
        }
    } else if (t.rows().data().length === 0) {
        UpdateHeaderForLatestRecord(id);
    }
}
function FinalnoteFortheaudit(t, notes, name, AuditTypeStatus) {
    let AuditBatchId = t.rows({ selected: false }).data()[0].auditBatchId;
    let AuditBatchType = t.rows({ selected: false }).data()[0].auditType;
    let AuditStartTime = t.rows({ selected: false }).data()[0].auditTime;
    var token = $('[name="__RequestVerificationToken"]').val();
    var items = [
        {
            AuditBatchId: AuditBatchId,
            AuditFinalNote: notes,
            Auditname: name,
            Audittype: AuditBatchType,
            Starttime: AuditStartTime,
        },
    ];
    $.ajax({
        url: "/Vendor/PriorAuditDataList",
        type: "POST",
        contentType: "application/json",
        headers: { RequestVerificationToken: token },
        data: JSON.stringify(items),
        success: function (response) {
            if (response.success) {
                jqToast.success({
                    text: `Audit ${AuditTypeStatus} has been Completed`,
                });
                t.ajax.reload();
            } else {
                jqToast.error({
                    text: response.message || "Data not saved in batch",
                });
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            jqToast.error({
                text: "An error occurred while saving the data.",
            });
        },
    });
}
function vendorGetSelectedItemObjectsForBulkAudits(
    t,
    outcome,
    notes,
    internal,
    assignedUer
) {
    var a = [];
    t.rows({ selected: true }).every(function () {
        var rowData = this.data();
        rowData.outcome = outcome;
        rowData.notes = notes;
        rowData.internal = internal;
        rowData.assignedUser = assignedUer;
        a.push(rowData);
    });
    return a;
}

function vendorGetSelectedItemFollowUpObjectsForBulkAudits(
    t,
    outcome,
    notes,
    internal,
    assignedUer
) {
    var a = [];
    t.rows({ selected: true }).every(function () {
        var rowData = this.data();
        rowData.outcome = outcome;
        rowData.auditNotes = notes;
        rowData.internal = internal;
        rowData.assignedUser = assignedUer;
        a.push(rowData);
    });
    return a;
}
function vendor_UpdateAudit(t, name) {
    var items = vendorGetSelectedItemObjectsForAudits(t, name);

    if (items.length <= 0) {
        jqToast.error({
            text: "Select at least one row for audit assigned.",
        });
        return;
    }
    updateAndSaveAuditRecord(items, t, name);
}
function vendor_BulkUpdateNote(t) {
    var items = vendorGetSelectedItemObjects(t);
    var html = "No items selected";
    var disabled = true;
    if (items.length !== 0) {
        html =
            "Operation will update notes for the " +
            items.length +
            " selected items";
        disabled = false;
    }
    var $form = $("#modal-bulknote");
    vendor_init_dialog($form);

    $form.find("#selectedCount").html(html);
    $form.find("#note").val("");
    $form.find("#remark").val("");
    var today = moment().format("YYYY-MM-DD");
    vendor_bulk_set_multi_code_title_header($form, items);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [
                    { name: "note", value: $form.find("#note").val() },
                    { name: "remark", value: $form.find("#remark").val() },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/BulkUpdate",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_CheckAppTypesAllSame(t) {
    var items = vendorGetSelectedItemObjects(t);
    var types = [];
    var result = true;
    for (let idx = 0; idx < items.length; idx++) {
        let item = items[idx];
        let key = `${item.state}_${item.appType}`.toLowerCase();
        if (types.length === 0) {
            types.push(key);
        } else if (types.indexOf(key) === -1) {
            // must be same type and state
            return false;
        }
    }
    return true;
}
function vendor_UniversalBulkEditSelectedGroup(t) {
    var items = vendorGetSelectedItemObjects(t);
    var types = [];
    var result = true;
    for (let idx = 0; idx < items.length; idx++) {
        let item = items[idx];
        let key = `${item.GroupId}`.toLowerCase();
        if (types.length === 0) {
            types.push(key);
        } else if (types.indexOf(key) === -1) {
            // must be same group
            return null;
        }
    }
    return types[0];
}
function vendor_UniversalBulkEdit(name, t) {
    if (!vendor_CheckAppTypesAllSame(t)) {
        vendor_show_DataTable_alert(
            t,
            "Bulk edit requires all identical application types"
        );
        return;
    }
    var exportItems = vendorGetSelectedItems(t);
    if (exportItems && exportItems.length !== 0) {
        let $form = $("#universalEdit");
        if ($form.length === 0) {
            $(document.body).append(
                "<form id='universalEdit' target='_blank' action='/Vendor/BulkEdit' method='post' type='submit' style='display:none'></form>"
            );
            $form = $("#universalEdit");
        }
        $form.html("");
        $form.append(
            '<input type="hidden" name="queue" value="' +
                htmlEncode(name) +
                '" />'
        );
        $.each(exportItems, function (i, id) {
            $form.append(
                '<input type="hidden" name="ids" value="' + id + '" />'
            );
        });
        attachToken($form);
        $form.trigger("submit");
    }
}
function vendor_UniversalBulkEditToInProcess(name, t) {
    var exportItems = vendorGetSelectedItems(t);
    if (exportItems && exportItems.length !== 0) {
        let $form = $("#bulkEditToInProcess");
        if ($form.length === 0) {
            $(document.body).append(
                "<form id='bulkEditToInProcess' target='_blank' action='/Vendor/BulkEditToInProcess' method='post' type='submit' style='display:none'></form>"
            );
            $form = $("#bulkEditToInProcess");
        }
        $form.html("");
        $form.append(
            '<input type="hidden" name="queue" value="' + name + '" />'
        );
        $.each(exportItems, function (i, id) {
            $form.append(
                '<input type="hidden" name="ids" value="' + id + '" />'
            );
        });
        attachToken($form);
        $form.trigger("submit");
    }
}
function vendor_BulkUpdateETA(t) {
    var items = vendorGetSelectedItemObjects(t);
    var html = "No items selected";
    var disabled = true;
    if (items.length !== 0) {
        html =
            "Operation will update ETA for the " +
            items.length +
            " selected items";
        disabled = false;
    }
    var $form = $("#modal-bulketa");
    vendor_init_dialog($form);

    $form.find("#selectedCount").html(html);
    $form.find("#bulkETA").val("");
    var today = moment().format("YYYY-MM-DD");
    vendor_bulk_set_multi_code_title_header($form, items);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                var values = [
                    { name: "eta", value: $form.find("#bulkETA").val() },
                    { name: "isEtaSet", value: true },
                ];
                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/BulkUpdate",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function vendor_bulk_set_multi_code_title_audit_header(form, items, tbid) {
    var div = "";
    if (typeof items !== "undefined") {
        var id = `bulk_vin_code_header${vin_rno_header_cnt}`;
        div += `<div style="text-align:right;" width="100%"><button class="fa fa-clipboard float-right" title="Copy to clipboard" class="fa fa-clipboard" onclick="copyText(&quot;${id}&quot;)"></button></div>`;
        div += `<div class ="bulk_scrollbar">`;
        div += `<table id="${id}" class="vin_rno_header">`;
        if (tbid === followUpId) {
            div += `<thead><tr><th>VIN</th><th>R#</th><th>Title</th></tr></thead>`;
            $.each(items, function (idx, item) {
                div += `<tr class="bulk_vin_code_detail"><td>${item.vin}</td><td>${item.requestNo}</td><td><span class="vin">${item.title}</span></tr>`;
            });
        } else if (tbid === etaId || tbid === pcodeId) {
            div += `<thead><tr><th>State</th><th>App Type</th><th>VIN</th><th>ETA</th></tr></thead>`;
            $.each(items, function (idx, item) {
                let eta = "";
                if (item.eta) eta = moment(item.eta).format("MM-DD-YYYY");
                div += `<tr class="bulk_vin_code_detail"><td>${item.state}</td><td>${item.appType}</td><td><span class="vin">${item.vin}</span></td><td>${eta}</td></tr>`;
            });
        } else {
            div += `<thead><tr><th>State</th><th>App Type</th><th>VIN</th><th>Code</th><th>Group</th></tr></thead>`;
            $.each(items, function (idx, item) {
                let code = "";
                if (item.code) code = item.code;
                div += `<tr class="bulk_vin_code_detail"><td>${item.state}</td><td>${item.appType}</td><td><span class="vin">${item.vin}</span></td><td>${code}</td><td>${item.groupName}</td></tr>`;
            });
        }
        div += "</table>";
        div += "</div>";
    }
    form.find("#REQ_BULK_MULTI_VIN_CODE_HEADER").html(div);
}
function vendor_bulk_set_multi_code_title_header(form, items) {
    var div = "";
    if (typeof items !== "undefined") {
        //<th class="fa fa-clipboard" onclick="copyText(&quot;${id}&quot;)">&nbsp;</th>
        var id = `bulk_vin_code_header${vin_rno_header_cnt}`;
        div += `<div style="text-align:right;" width="100%"><button class="fa fa-clipboard float-right" title="Copy to clipboard" class="fa fa-clipboard" onclick="copyText(&quot;${id}&quot;)"></button></div>`;
        div += `<div class ="bulk_scrollbar">`;
        div += `<table id="${id}" class="vin_rno_header">`;
        div += `<thead><tr><th>State</th><th>App Type</th><th>VIN</th><th>Code</th><th>ETA</th></tr></thead>`;
        $.each(items, function (idx, item) {
            let code = "";
            let eta = "";
            if (item.code) code = item.code;
            if (item.eta) eta = moment(item.eta).format("MM-DD-YYYY");
            div += `<tr class="bulk_vin_code_detail"><td>${item.state}</td><td>${item.appType}</td><td><span class="vin">${item.vin}</span></td><td>${code}</td><td>${eta}</td></tr>`;
        });
        div += "</table>";
        div += "</div>";
    }
    form.find("#REQ_BULK_MULTI_VIN_CODE_HEADER").html(div);
}
function allowFormSubmit(frm) {
    if ($(frm).hasClass("submitting")) {
        return false;
    }
    $(frm).addClass("submitting");
    $(frm).trigger("submit");
    return true;
}
function submitFormHandler(e, frm) {
    e.preventDefault();
    e.stopPropagation();

    var $frm = $(frm);

    var valid = $frm.valid();
    if (typeof valid !== "undefined" && !valid) {
        return false;
    }

    var $btn = $frm.find(":submit");
    if ($btn.attr("disabled")) {
        return false;
    }
    $btn.attr("disabled", true);

    //console.log("Executing ajax UpdateNotes call");
    $.ajax({
        url: $frm.attr("action"),
        type: "POST",
        enctype: $frm.attr("enctype"),
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        data: $frm.serialize(),
    })
        .done(function (data) {
            // todo
            //console.log("done() Submitted by submitFormHandler");
            $btn.attr("disabled", false);
        })
        .fail(function () {
            // todo
            //console.log("fail() Submitted by submitFormHandler");
            $btn.attr("disabled", false);
        });
    return false;
}
function rowChanged(id, data) {}

var bulkEditChangedRows = [];
var bulkEditOriginalData = [];

var hiddenFieldsList = [
    "groupProfileID",
    "groupProfileTypeID",
    "groupProfileTypeName",
    "groupProfileName",
    "R#",
    "Group",
    "Vendor",
    "Submitted_By",
    "AppType",
    "AppTypeState",
];

function isFieldHiddenInBulkEdit(fieldName) {
    const found = hiddenFieldsList.find((element) => element === fieldName);
    return found;
}
function addEditorFieldCheckboxes(editor) {
    let alreadyInitialized = $(
        "div.profileEditForm > fieldset > div.DTE_Field > div > input.bulkProfileEditorCheckbox"
    );
    let data = editor.get();
    //console.log(data);
    // turn off all checkboxes
    let allCheckboxes = $("input.bulkProfileEditorCheckbox");
    allCheckboxes.prop("checked", false);
    //console.log(`Found ${allCheckboxes.length} checkboxes`);

    let fieldCtls = $(
        "#profileEditForm .DTE_Field_InputControl input, #profileEditForm .DTE_Field_InputControl select"
    ).not(".bulkProfileEditorCheckbox");
    if (alreadyInitialized.length > 0) {
        $.each(fieldCtls, function (i, item) {
            let id = $(item).attr("id");
            let fname = id;
            if (id.startsWith("DTE_Field_")) {
                fname = id.substr(10);
            }
            // do not update the special fields
            if (isFieldHiddenInBulkEdit(fname)) return;

            //console.log(`field: '${fname}'`);
            var checkbox = $(item.parentElement).find(
                ".bulkProfileEditorCheckbox"
            );
            if (checkbox.length === 0) {
                // add checkbox
                //console.log("checkbox missing, adding...");
                addBulkEditCheckbox(item, data, fname);
            } else {
                let dval = getDataField(data, fname);
                if (
                    typeof dval === "undefined" ||
                    dval === null ||
                    dval === ""
                ) {
                    //console.log(`dval is undefined or null for ${fname}`);
                    checkbox.prop("checked", false);
                } else {
                    //console.log(`dval is not undefined or null for ${fname}`);
                    checkbox.prop("checked", true);
                }
            }
        });
        return;
    }

    $.each(fieldCtls, function (i, item) {
        let id = $(item).attr("id");
        let fname = id;
        if (typeof id === "undefined") {
            return;
        }
        if (id.startsWith("DTE_Field_")) {
            fname = id.substr(10);
        }
        // do not update the special fields
        if (isFieldHiddenInBulkEdit(fname)) {
            // do nothing, no checkbox
        } else {
            //console.log(`field: '${fname}'`);
            addBulkEditCheckbox(item, data, fname);
        }
    });
}
function getDataField(data, fname) {
    let fnameNoUnderscore = fname.replace(/_/g, " ");
    let dval = data[fname] || data[fnameNoUnderscore];
    return dval;
}
function addBulkEditCheckbox(ctl, data, fname) {
    let p = $(ctl.parentElement);
    let dval = getDataField(data, fname);

    var checkbox = p.find("input.bulkProfileEditorCheckbox");

    if (checkbox.length === 0) {
        // add checkbox if not found
        checkbox = $(
            "<input type='checkbox' class='bulkProfileEditorCheckbox' />"
        );
        checkbox.first().data("field", fname);
        $(ctl).before(checkbox);
    }
    if (fname === "Client_ID_Number") {
        //console.log("Client ID Number");
    }
    if (typeof dval === "undefined" || dval === null || dval === "") {
        checkbox.prop("checked", false);
    } else {
        checkbox.prop("checked", true);
    }
}
function initGroupProfileTypeNameDropdown(editor, currentSelection) {
    let $select = $("#DTE_Field_groupProfileTypeID");
    // #DTE_Field_groupProfileTypeID
    let data = editor.get();
    //console.log("Initializing groupProfileTypeName dropdown");
    $select.empty();
    let $option = $("<option value=''>Any Type</option>");
    if (currentSelection === null || currentSelection === "") {
        $option.attr("selected", "selected");
    }
    $select.append($option);
    return new Promise((resolve, reject) => {
        var optionsA = [];
        $.ajax({
            url: "/Vendor/GetProfileTypesForGroupId",
            type: "GET",
            async: true,
        })
            .done(function (data) {
                if (Array.isArray(data)) {
                    var option = {};
                    option = { label: "Any Type", id: "" };
                    optionsA.push(option);
                    $.each(data, function (i, e) {
                        try {
                            option = {};
                            option.label = e.groupProfileTypeName;
                            option.value = e.groupProfileTypeID;
                            optionsA.push(option);
                        } catch (err) {
                            console.error(err);
                        }
                    });
                    editor.field("groupProfileTypeID").update(optionsA);
                    resolve();
                } else {
                    reject();
                }
            })
            .fail(function () {
                $select.append(
                    $(
                        "<option disabled='disabled'>Error retrieving profile types</option>"
                    )
                );
                reject();
            });
    });
}
function configureBulkEditEditor(editor) {
    // TBD: Review if this is behavior we want
    //  it might be too intrusive
    var openVals;
    editor.on("open", function (e, type) {
        openVals = JSON.stringify(editor.get());
    });
    editor.on("initEdit", function (e, type) {});
    editor.on("preBlur", function (e) {
        let newVals = JSON.stringify(editor.get());
        if (openVals !== newVals) {
            return confirm(
                "You have unsaved changes. Are you sure you want to exit?"
            );
        }
    });

    editor.on("preEdit", function (e, json, data, id) {});
    editor.on("postEdit", function (e, json, data, id) {
        bulkEditChangedRows.push(id);
        highlightDatatableSaveButton(editor);
    });

    editor.on("edit", function (e, json, data, id) {});
    editor.off("preRemove").on("preRemove", function () {
        // prevents the editor from removing the row
        return false;
    });
    editor.off("close").on("close", function () {
        // hide the profile page
        $(".profilePanel").hide();
    });
}

function configureDirtyPageWarning(data) {
    bulkEditOriginalData = data;

    window.thisPage = window.thisPage || {};
    window.thisPage.isDirty = false;
    window.onbeforeunload = function (event) {
        if (bulkEditChangedRows.length > 0)
            return "You have unsaved changes, are you sure you want to leave?";
        else return undefined;
    };
}
function configureProfileEditor(editor, tableId, data) {
    bulkEditOriginalData = data;

    // TBD: Review if this is behavior we want
    //  it might be too intrusive
    var openVals;
    editor.on("open", function (e, type) {
        openVals = JSON.stringify(editor.get());
        addEditorFieldCheckboxes(editor);
    });
    editor.on("initEdit", function (e, type) {
        // initialize groupProfileTypeName dropdown
        initGroupProfileTypeNameDropdown(editor, data["groupProfileTypeID"]);
    });
    editor.on("preBlur", function (e) {
        let newVals = JSON.stringify(editor.get());
        if (openVals !== newVals) {
            return confirm(
                "You have unsaved changes. Are you sure you want to exit?"
            );
        }
    });

    editor.on("preEdit", function (e, json, data, id) {
        // iterate over the checkboxes and set the values
        // to null for those fields that are not checked
        let fieldCtls = $("#profileEditForm .bulkProfileEditorCheckbox");
        $.each(fieldCtls, function (i, item) {
            let field = $(item)
                .closest(".DTE_Field")
                .prev("editor-field")
                .attr("name");
            if (field) {
                if (
                    field === "groupProfileID" ||
                    field === "groupProfileTypeID" ||
                    field === "groupProfileTypeName" ||
                    field === "groupProfileName"
                )
                    return;
                if (item.checked) {
                    // leave the value alone
                    data[field] = data[field] || "";
                } else {
                    data[field] = null;
                }
            }
        });
        // Always set the groupProfileTypeID to null if it is empty
        if (data["groupProfileTypeID"] === "") {
            data["groupProfileTypeID"] = null;
        }
    });
    editor.on("postEdit", function (e, json, data, id) {
        bulkEditChangedRows.push(id);
        highlightDatatableSaveButton(editor);
    });

    editor.on("edit", function (e, json, data, id) {});

    window.thisPage = window.thisPage || {};
    window.thisPage.isDirty = false;

    window.thisPage.closeEditorWarning = function (event) {
        if (bulkEditChangedRows.length > 0) return "You have unsaved changes";
        else return undefined;
    };
    window.onbeforeunload = window.thisPage.closeEditorWarning;
}

function vendorSaveBulkEdits_NotReady(editor, table, sendToStage) {
    // prompt for notes/remark/code
}
function vendorSaveBulkEdits(editor, table, sendToStage) {
    if (bulkEditChangedRows.length === 0) {
        vendor_show_DataTable_alert(
            table,
            "No changes to save",
            2500,
            alert_level_info
        );
        return;
    }
    let json = editor.get();
    let formdata = new FormData();
    let changedData = table.data().toArray();
    formdata.append("before", JSON.stringify(bulkEditOriginalData));
    formdata.append("after", JSON.stringify(changedData));

    if (sendToStage) formdata.append("sendToStage", sendToStage);

    $.ajax({
        url: "/Vendor/BulkEditSave",
        type: "POST",
        async: true,
        data: formdata,
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
    })
        .done(function (data) {
            if (data.success) {
                bulkEditChangedRows = [];
                bulkEditOriginalData = changedData;
                unhighlightDatatableSaveButton(editor);
                table.draw();
                vendor_show_DataTable_alert(
                    table,
                    "Changes saved successfully",
                    5000,
                    alert_level_info
                );
            } else {
                // show alert
                vendor_show_DataTable_alert(table, data.message, -1);
            }
        })
        .fail(function () {
            // todo
            //console.log("save error");
        });
}

function vendorSaveBulkProfileEdits(editor, table) {
    if (bulkEditChangedRows.length === 0) {
        return;
    }
    let json = editor.get();
    let formdata = new FormData();
    let groupId = $("#vendorGroupId").val();
    formdata.append("groupId", groupId);
    let changedData = table.data().toArray();
    formdata.append("before", JSON.stringify(bulkEditOriginalData));
    formdata.append("after", JSON.stringify(changedData));
    $.ajax({
        url: "/Vendor/BulkEditProfileSave",
        type: "POST",
        async: true,
        data: formdata,
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
    })
        .done(function (data) {
            if (data.success) {
                // todo
                let timeout;
                vendor_show_DataTable_alert(
                    table,
                    "Changes saved successfully",
                    timeout,
                    alert_level_info
                );
                bulkEditChangedRows = [];
                bulkEditOriginalData = changedData;
                highlightDatatableSaveButton(editor);
                table.draw();
            } else {
                // show alert
                vendor_show_DataTable_alert(table, data.message, -1);
            }
        })
        .fail(function () {
            // todo
            //console.log("error saving");
        });
}
function bulkeditprofile_row_callback(
    row,
    data,
    displayNum,
    displayIndex,
    dataIndex
) {
    let dt = $(row).closest("table").DataTable();
    if (bulkEditChangedRows.indexOf(data["groupProfileID"]) >= 0) {
        $(row).addClass("rowchanged");
    } else {
        $(row).removeClass("rowchanged");
    }
}

function bulkedit_row_callback(row, data, displayNum, displayIndex, dataIndex) {
    let dt = $(row).closest("table").DataTable();
    if (bulkEditChangedRows.indexOf(data["RequestId"]) >= 0) {
        $(row).addClass("rowchanged");
    } else {
        $(row).removeClass("rowchanged");
    }
}
function vendorLoadGroupLibraryData(groupId, dt) {
    dt.ajax
        .url("/GroupLibrary/GetLibrary?groupId=" + groupId)
        .load()
        .draw();
}
function vendorLoadProfileData(groupId, dt, categoryName) {
    let url = "/Vendor/GetProfileData";

    let formData = new FormData();
    formData.append("groupId", groupId);
    formData.append("categoryName", categoryName || "");

    $.ajax({
        url: url,
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        data: formData,
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
    })
        .done(function (data) {
            let rows = [];
            if (Array.isArray(data)) {
                // Add the necessary data for showing in the table
                $.each(data, function (i, item) {
                    let row = JSON.parse(item.profile || "{}");
                    let groupProfileInfo = {
                        groupProfileID: item.groupProfileID,
                        groupProfileTypeID: item.groupProfileTypeID,
                        groupProfileName: item.groupProfileName,
                        groupProfileTypeName: item.groupProfileTypeName,
                    };
                    row = Object.assign(row, groupProfileInfo);
                    rows.push(row);
                });
            }
            bulkEditOriginalData = rows;
            dt.clear().rows.add(rows);
            vendorProfileResetFilter(dt);
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
            //console.log(jqXHR);
            //console.log(textStatus);
            //console.log(errorThrown);
        });
}
function vendorProfileResetFilter(table) {
    try {
        table.columns().every(function () {
            var column = this;
            if (column.searchable === false) {
                return;
            }
            var select = $('<select><option value=""></option></select>')
                .appendTo($(column.footer()).empty())
                .on("change", function () {
                    var val = $.fn.dataTable.util.escapeRegex($(this).val());
                    column
                        .search(val ? "^" + val + "$" : "", true, false)
                        .draw();
                });
            column
                .data()
                .unique()
                .sort()
                .each(function (d, j) {
                    d = d || "";
                    if (d !== null && d.trim() !== "") {
                        var opt = $("<option></option>")
                            .attr({ value: d })
                            .text(d);
                        select.append(opt);
                    }
                });
        });
        //console.log("Calling draw() from vendorProfileResetFilter");
        table.search("").columns().search("").draw();
    } catch (e) {
        console.error(e);
    }
}
function vendor_sort_table(table, sortorder) {
    table.order(sortorder).draw();
}
function vendorUndoBulkEdits(table) {
    if (bulkEditChangedRows.length === 0) {
        return;
    }
    bulkEditChangedRows = [];
    unhighlightDatatableSaveButton(editor);
    table.clear().rows.add(bulkEditOriginalData).draw();
}
function highlightDatatableSaveButton(editor) {
    $(editor.table() + "_wrapper")
        .find(".dtBulkEditSaveButton")
        .addClass("unsavedChanges");
}
function unhighlightDatatableSaveButton(editor) {
    $(editor.table() + "_wrapper")
        .find(".dtBulkEditSaveButton")
        .removeClass("unsavedChanges");
}
// FIX FOR INCORRECT COLUMN WIDTH WHEN USING SCROLLX
function fixupColumnWidths(datatable) {
    datatable.api().table().columns.adjust();
}
function refreshDropdownFilters(
    api,
    tblid,
    colFilters,
    defaultStatusFilterValue,
    includeCancelledStatus
) {
    api.columns([".drfilter", ".dfilter"]).every(
        getfn_datatable_drfilter(tblid, true, colFilters)
    );
    api.columns([".drofilter"]).every(
        getfn_datatable_drofilter(tblid, colFilters)
    );
    api.columns([".dtfilter"]).every(
        getfn_datatable_dtfilter(tblid, false, colFilters)
    );
    api.columns([".afilter"]).every(
        getfn_datatable_afilter(tblid, true, colFilters)
    );
    api.columns([".statusFilter"]).every(
        getfn_datatable_statusfilter(
            tblid,
            defaultStatusFilterValue,
            includeCancelledStatus,
            colFilters
        )
    );
    api.columns([".stageFilter"]).every(
        getfn_datatable_stagefilter(tblid, colFilters)
    );
}
function vendor_show_RollbackRequests(t) {
    // Can only be called from bulk edit page, so must be uppercase RequestId
    var items = vendorGetSelectedItemValues(t, "RequestId"); // Must be RequestId, not requestId (lower-case r)
    var disabled = true;

    var $form = $("#modal-RequestRollback");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);
    $form.find("#RequestRollback_Verify").val("");

    var html = "No items selected";
    if (items.length !== 0) {
        html =
            "Operation will rollback to original submission for " +
            items.length +
            " requests";
        disabled = false;
    }
    $form.find("#selectedCount").html(html);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                let data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data, false, false, true)) return;

                let verify = $form.find("#RequestRollback_Verify").val();
                if (verify !== "ROLLBACK") {
                    vendor_show_alert(
                        $form,
                        "You must type 'ROLLBACK' to proceed"
                    );
                    return;
                }
                var values = [
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "code", value: data.code },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                    { name: "chat", value: data.chat },
                ];
                var tables = [];

                vendor_action_HELPER(
                    $form,
                    items,
                    "/Vendor/RollbackRequest",
                    tables,
                    values
                );
            });
    }
    $form.modal("show");
}
function renderInternalAttachmentDropdown(
    data,
    type,
    row,
    meta,
    attachmentTypeId
) {
    return data;
}
function populateProfileForm($dt, profileId) {
    var ctl = $("#bulkEditProfiles");
    ctl.empty();
    for (var i = 0; i < profileData.length; i++) {
        let profile = profileData[i];
        if (profile.groupProfileID === profileId) {
            let values = profile.fields; // JSON.parse(profile.fields || "{}");
            let editor = $dt.DataTable().editor();
            let keyOrder = editor.order();
            $.each(keyOrder, function (idx, key) {
                if (keysToSkip.some((element) => element === key)) return;

                let value = values[key];
                if (typeof value === "undefined" || value === null) return;

                let div = $("<div class='GPS_Field'></div>");
                let label = $("<label></label>")
                    .text(key)
                    .addClass("col-lg-4 control-label")
                    .attr({ for: key });
                let div2 = $("<div></div>").addClass("col-lg-8 controls");
                let div3 = $("<div class='GPS_Field_InputControl'></div>").attr(
                    "style",
                    "display:block"
                );

                let input;
                if (
                    key.startsWith(constVendorInternalDocumentPrefix) ||
                    key.startsWith(constGroupInternalDocumentPrefix)
                ) {
                    let optText;
                    if (key.startsWith(constVendorInternalDocumentPrefix)) {
                        optText = vendor_getVendorAttachmentName(value);
                    } else {
                        optText = vendor_getGroupAttachmentName(value);
                    }
                    let val = value;
                    let select = $("<select readonly='readonly'/>");
                    let option = $("<option/>");
                    option.val(value).text(optText);
                    option.attr({ selected: "selected" });
                    select.append(option);
                    input = select;
                } else {
                    input = $("<input type='text' readonly='readonly'/>")
                        .attr({ name: key })
                        .val(value);
                }
                div3.append(input);
                div2.append(div3);
                div.append(label);
                div.append(div2);
                ctl.append(div);
            });
            break;
        }
    }
}

function Reports_RefreshViewFilters(ctl) {
    // get selected group
    let groupId = $(ctl).val();
    // update filters for all report views
    let views = $("table.reportTable");

    let filters = [];
    if (groupId && groupId !== "") {
        filters.push({ name: "groupId", value: groupId });
    }

    let tables = [];
    let filter = JSON.stringify(filters);
    $.each(views, function (idx, view) {
        if (view.id) {
            if (view.id.startsWith("Reports_")) {
                $(view).data("jsonfilter", filter);
                $(`#download_${view.id}`).data("jsonfilter", filter);
                $(`#download_${view.id}`)
                    .find("input[name='jsonFilter']")
                    .val(filter);
            }
            tables.push($(`#${view.id}`));
        }
    });

    AjaxReloadList(tables, true);
}

function PaymentsReports_GetJsonFilter(reportId, paymentId, completeStartDate) {
    let filters = [];
    if (paymentId && paymentId !== "") {
        filters.push({ name: "paymentType", value: paymentId });
    }
    if (reportId === "download_Reports_PaymentsAndDisbursement") {
        let completeStartDate = $("#reportStartDate").val();
        let searchVal = `${completeStartDate};-to-;`;
        filters.push({ name: "PaymentDate", value: searchVal });
        $("#Reports_Completed").find("#mindate").val(completeStartDate);
    } else if (reportId === "download_Reports_Holds") {
    } else if (reportId === "download_Reports_IncomingHolds") {
    }
    return JSON.stringify(filters);
}
function Reports_GetJsonFilter(reportId, groupId, completeStartDate) {
    let filters = [];
    if (groupId && groupId !== "") {
        filters.push({ name: "groupId", value: groupId });
    }
    if (reportId === "download_Reports_Active") {
        //
    } else if (reportId === "download_Reports_Completed") {
        let completeStartDate = $("#reportStartDate").val();
        let searchVal = `${completeStartDate};-to-;`;
        filters.push({ name: "dateShipped", value: searchVal });
        $("#Reports_Completed").find("#mindate").val(completeStartDate);
        //$("#Reports_Completed").DataTable()
        //    .columns('.dateShippedFilter')
        //    .search(searchVal, true, false);
        //.draw(); // don't redraw immediately
    } else if (reportId === "download_Reports_Holds") {
    } else if (reportId === "download_Reports_IncomingHolds") {
    }
    return JSON.stringify(filters);
}
function expandColumnsToFitContent(worksheet) {
    const range = XLSX.utils.decode_range(worksheet["!ref"]);

    for (let C = range.s.c; C <= range.e.c; ++C) {
        let maxWidth = 0;

        for (let R = range.s.r; R <= range.e.r; ++R) {
            const cell = worksheet[XLSX.utils.encode_cell({ r: R, c: C })];
            if (!cell) continue;

            const cellValue = cell.w || `${cell.v}` || "";
            const cellWidth = cellValue.length;
            if (cellWidth > maxWidth) {
                maxWidth = cellWidth;
            }
        }
        //console.log(`Column ${C} width: ${maxWidth}`);
        worksheet["!cols"] = worksheet["!cols"] || [];
        worksheet["!cols"][C] = { width: maxWidth + 2 };
    }
}

async function Reports_Download(groupId, groupName, combineAsWorkbook) {
    groupId = groupId || "";
    if (groupId === null || groupId === "") {
        groupName = "ALL GROUPS";
    }

    var order = 0;
    const formList = [
        { id: "download_Reports_Active", name: "Active", order: order++ },
        { id: "download_Reports_Completed", name: "Completed", order: order++ },
        { id: "download_Reports_Holds", name: "Holds", order: order++ },
        {
            id: "download_Reports_IncomingHolds",
            name: "IncomingHolds",
            order: order++,
        },
    ];
    const today = moment().format("YYYY-MM-DD");
    const zip = new JSZip();
    const startDate = $("#reportStartDate").val();
    const workbooks = [];

    // pre-create list so we can insert in right order
    // as some queries may take longer than others
    formList.map(async (form, index) => {
        workbooks.push({ workbook: null, sheetName: null });
    });
    const fetchPromises = formList.map(async (form, index) => {
        const formId = form.id;
        const formUrl = document.getElementById(formId).action;
        const filter = Reports_GetJsonFilter(formId, groupId, startDate);
        $(`#${formId} input[name="jsonFilter"]`).val(filter);
        const formData = new FormData(document.getElementById(formId));
        const response = await fetch(formUrl, {
            method: "POST",
            body: formData,
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
        });
        const text = await response.text();
        const reportName = form.name;
        if (combineAsWorkbook) {
            const workbook = XLSX.read(text, { type: "binary" });
            let sheetName = form.name;
            workbooks[form.order] = { workbook, sheetName };
        } else {
            zip.file(`${today}_${groupName}_${reportName}.csv`, text);
        }
    });

    await Promise.all(fetchPromises);

    if (combineAsWorkbook) {
        const mergedWorkbook = workbooks.reduce(
            (merged, { workbook, sheetName }) => {
                workbook.SheetNames.forEach((ws) => {
                    merged.SheetNames.push(sheetName);
                    const sheet = workbook.Sheets[ws];

                    // Apply default date format to all date cells
                    const range = XLSX.utils.decode_range(sheet["!ref"]);

                    //expandColumnsToFitContent(sheet, range);

                    for (let R = range.s.r; R <= range.e.r; ++R) {
                        for (let C = range.s.c; C <= range.e.c; ++C) {
                            const cellAddress = { r: R, c: C };
                            const cellRef = XLSX.utils.encode_cell(cellAddress);
                            const cell = sheet[cellRef];
                            if (cell) {
                                if (cell.t === "d") {
                                    cell.z = "MM-DD-YYYY";
                                } else if (
                                    cell.t === "n" &&
                                    cell.w &&
                                    cell.w.includes("/")
                                ) {
                                    cell.z = "MM-DD-YYYY";
                                }
                            }
                        }
                    }
                    expandColumnsToFitContent(sheet, range);

                    const table = {
                        name: `${sheetName}Table`,
                        ref: XLSX.utils.encode_range(range),
                        headerRow: 1, // Specify the row number where the headers are located
                    };
                    sheet["!tables"] = [table];
                    sheet["!autofilter"] = { ref: range };

                    merged.Sheets[sheetName] = sheet;
                });
                return merged;
            },
            { SheetNames: [], Sheets: {} }
        );

        const excelFile = XLSX.write(mergedWorkbook, {
            bookType: "xlsx",
            type: "binary",
        });
        const blob = new Blob([s2ab(excelFile)], {
            type: "application/octet-stream",
        });
        const excelFileName = `${today}_${groupName}_Reports.xlsx`;

        const downloadLink = document.createElement("a");
        downloadLink.href = URL.createObjectURL(blob);
        downloadLink.download = excelFileName;
        downloadLink.click();
    } else {
        const content = await zip.generateAsync({ type: "blob" });

        const zipFileName = `${today}_${groupName}_Reports.zip`;

        const downloadLink = document.createElement("a");
        downloadLink.href = URL.createObjectURL(content);
        downloadLink.download = zipFileName;
        downloadLink.click();
    }
}

async function Payments_Reports_Download(paymentId, combineAsWorkbook) {
    var groupName = "ALLGroup";
    var order = 0;
    const formList = [
        {
            id: "download_Reports_PaymentsAndDisbursement",
            name: "PaymentAndDisburseMent",
            order: order++,
        },
    ];
    const today = moment().format("YYYY-MM-DD");
    const zip = new JSZip();
    const startDate = $("#reportStartDate").val();
    const workbooks = [];

    // pre-create list so we can insert in right order
    // as some queries may take longer than others
    formList.map(async (form, index) => {
        workbooks.push({ workbook: null, sheetName: null });
    });
    const fetchPromises = formList.map(async (form, index) => {
        const formId = form.id;
        const formUrl = document.getElementById(formId).action;
        const filter = PaymentsReports_GetJsonFilter(
            formId,
            paymentId,
            startDate
        );
        $(`#${formId} input[name="jsonFilter"]`).val(filter);
        const formData = new FormData(document.getElementById(formId));
        const response = await fetch(formUrl, {
            method: "POST",
            body: formData,
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
        });
        const text = await response.text();
        const reportName = form.name;
        if (combineAsWorkbook) {
            const workbook = XLSX.read(text, { type: "binary" });
            let sheetName = form.name;
            workbooks[form.order] = { workbook, sheetName };
        } else {
            zip.file(`${today}_${groupName}_${reportName}.csv`, text);
        }
    });

    await Promise.all(fetchPromises);

    if (combineAsWorkbook) {
        const mergedWorkbook = workbooks.reduce(
            (merged, { workbook, sheetName }) => {
                workbook.SheetNames.forEach((ws) => {
                    merged.SheetNames.push(sheetName);
                    const sheet = workbook.Sheets[ws];

                    const range = XLSX.utils.decode_range(sheet["!ref"]);
                    for (let R = range.s.r; R <= range.e.r; ++R) {
                        for (let C = range.s.c; C <= range.e.c; ++C) {
                            const cellAddress = { r: R, c: C };
                            const cellRef = XLSX.utils.encode_cell(cellAddress);
                            const cell = sheet[cellRef];
                            if (cell) {
                                if (cell.t === "d") {
                                    cell.z = "MM-DD-YYYY";
                                } else if (
                                    cell.t === "n" &&
                                    cell.w &&
                                    cell.w.includes("/")
                                ) {
                                    cell.z = "MM-DD-YYYY";
                                }
                            }
                        }
                    }
                    expandColumnsToFitContent(sheet, range);

                    const table = {
                        name: `${sheetName}Table`,
                        ref: XLSX.utils.encode_range(range),
                        headerRow: 1, // Specify the row number where the headers are located
                    };
                    sheet["!tables"] = [table];
                    sheet["!autofilter"] = { ref: range };

                    merged.Sheets[sheetName] = sheet;
                });
                return merged;
            },
            { SheetNames: [], Sheets: {} }
        );

        const excelFile = XLSX.write(mergedWorkbook, {
            bookType: "xlsx",
            type: "binary",
        });
        const blob = new Blob([s2ab(excelFile)], {
            type: "application/octet-stream",
        });
        const excelFileName = `${today}_${groupName}_Reports.xlsx`;

        const downloadLink = document.createElement("a");
        downloadLink.href = URL.createObjectURL(blob);
        downloadLink.download = excelFileName;
        downloadLink.click();
    } else {
        const content = await zip.generateAsync({ type: "blob" });

        const zipFileName = `${today}_${groupName}_Reports.zip`;

        const downloadLink = document.createElement("a");
        downloadLink.href = URL.createObjectURL(content);
        downloadLink.download = zipFileName;
        downloadLink.click();
    }
}

function s2ab(s) {
    const buf = new ArrayBuffer(s.length);
    const view = new Uint8Array(buf);
    for (let i = 0; i < s.length; i++) {
        view[i] = s.charCodeAt(i) & 0xff;
    }
    return buf;
}

/* functions used by invoicing */
function vendorSelectUnbilledInvoiceItem_Action(t) {
    $("#feePanel").empty("");

    vendorUnbilled_Table = t;
    vendorUnbilled_Items = vendorGetSelectedItemObjects(t);
    if (vendorUnbilled_Items.length === 0) {
        return;
    }
    var succeeded = false;
    var errorMsg = null;
    var appType = vendorUnbilled_Items[0].appType;
    var appState = vendorUnbilled_Items[0].state;
    var groupId = vendorUnbilled_Items[0].groupId;
    vendorUnbilled_Items.forEach(function (item) {
        if (errorMsg === null) {
            if (item.groupId !== groupId) {
                errorMsg = "All items must be from the same group";
            } else if (item.appType !== appType) {
                errorMsg = "All items must be from the same request Type";
            } else if (item.state !== appState) {
                errorMsg = "All items must be from the same request State";
            }
        }
    });

    if (errorMsg !== null) {
        //var msg = `<div class="alert-danger" style="text-align:center;margin-bottom:10px">${errorMsg}</div>`;
        //$("#feePanel").html(msg);
        vendor_show_DataTable_alert(t, errorMsg, 5000);
        return;
    }

    var data = vendorUnbilled_Items[0];

    $.ajax({
        url: "/Vendor/GetApplicationFees",
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
        data: $.param({
            groupId: data.groupId,
            appType: data.appType,
            appState: data.state,
            lienholderName: "",
        }),
    }).done(function (res) {
        $("#feePanel").html(res);
    });
}

function showLoader() {
    $("#loader").show();
    $("#content").addClass("loader-active");
}

function hideLoader() {
    $("#loader").hide();
    $("#content").removeClass("loader-active");
}

async function vendorOcrReviewticket(t) {
    const data = vendorGetSelectedItemObjects(t);
    const selectedRequest = data[0].requestId;
    const token = $('input[name="__RequestVerificationToken"]').val();
    const requestData = {
        requestId: selectedRequest,
    };

    try {
        showLoader();

        const response = await $.ajax({
            url: "/Vendor/GetAttachmentsByRequestId",
            type: "POST",
            contentType: "application/json",
            headers: { RequestVerificationToken: token },
            data: JSON.stringify(requestData),
        });
        const {
            requestId,
            filename,
            fileContent,
            authNumber,
            baseUrl,
            cookies,
            backendBaseUrl,
            webBaseUrl,
        } = response;
        const byteCharacters = atob(fileContent);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);

        const formData = new FormData();
        formData.append(
            "file",
            new Blob([byteArray], { type: "application/octet-stream" }),
            filename
        );
        formData.append("requestId", requestId);
        formData.append("filename", filename);
        formData.append("auth", authNumber);
        formData.append("baseUrl", baseUrl);
        formData.append("token", token);
        try {
            const uploadResponse = await fetch(`${backendBaseUrl}/upload`, {
                method: "POST",
                body: formData,
                /* headers: {
                    'webcookie': cookies 
                }*/
            });
            if (uploadResponse.ok) {
                const jsonResponse = await uploadResponse.json();
                const uniqueId = jsonResponse.uniqueId;
                const url = `${webBaseUrl}/createForm/${uniqueId}/${authNumber}`;

                hideLoader();
                const newWindow = window.open(url);

                if (newWindow) {
                    const checkWindowClosed = setInterval(() => {
                        if (newWindow.closed) {
                            clearInterval(checkWindowClosed);
                            location.reload();
                        }
                    }, 1000);
                }

                jqToast.success({
                    text: `OCR window successfully opened`,
                });
            } else {
                hideLoader();
                jqToast.error({
                    text: `Failed to upload data. Please try again.`,
                });
            }
        } catch (error) {
            hideLoader();
            jqToast.error({
                text: `An error occurred. Please refresh the page.`,
            });
        }
    } catch (error) {
        hideLoader();
        jqToast.error({
            text: `No documents available for upload.`,
        });
    }
}

function formatDateForDisplay(date) {
    return new Date(date).toLocaleDateString();
}

function formatDateTime(data) {
    if (data) {
        var date = new Date(data);
        var hours = date.getHours();
        var minutes = date.getMinutes();
        var seconds = date.getSeconds();
        var ampm = hours >= 12 ? "PM" : "AM";
        hours = hours % 12;
        hours = hours ? hours : 12; // the hour '0' should be '12'

        var formattedDate =
            ("0" + (date.getMonth() + 1)).slice(-2) +
            "-" +
            ("0" + date.getDate()).slice(-2) +
            "-" +
            date.getFullYear().toString().slice(-2);
        var formattedTime =
            ("0" + hours).slice(-2) +
            ":" +
            ("0" + minutes).slice(-2) +
            ":" +
            ("0" + seconds).slice(-2) +
            " " +
            ampm;
        return formattedDate + " " + formattedTime;
    }
    return "";
}

function vendorAddToInvoiceBuilderNew_Action(t) {
    vendorUnbilled_Table = t;
    vendorUnbilled_Items = vendorGetSelectedItemObjects(t);
    var form = $("#invoiceBuilderForm");

    var details = $("#DT_invoiceDetailsTable").DataTable();
    vendorCurrentInvoiceIds = vendorGetInvoiceBuilderIDs(details);

    if (containsItems(vendorUnbilled_Items)) {
        var counter = 1;

        //
        var groupId = "";
        var currentData = details.rows().data();
        if (currentData.length > 0) {
            groupId = currentData[0].groupId;
        }

        var checkGroupId = groupId;
        for (let i = 0; i < vendorUnbilled_Items.length; i++) {
            if (checkGroupId === "") {
                checkGroupId = vendorUnbilled_Items[i].groupId;
            } else if (checkGroupId !== vendorUnbilled_Items[i].groupId) {
                vendor_show_DataTable_alert(
                    t,
                    "All items must be from the same group",
                    5000
                );
                return;
            }
        }

        var appType = vendorUnbilled_Items[0].appType;
        for (let i = 1; i < vendorUnbilled_Items.length; i++) {
            if (appType !== vendorUnbilled_Items[i].appType) {
                vendor_show_DataTable_alert(
                    t,
                    "All items must be from the same request Type",
                    5000
                );
                return;
            }
        }
        var appState = vendorUnbilled_Items[0].state;
        for (let i = 1; i < vendorUnbilled_Items.length; i++) {
            if (appState !== vendorUnbilled_Items[i].state) {
                vendor_show_DataTable_alert(
                    t,
                    "All items must be from the request Type/State",
                    5000
                );
                return;
            }
        }

        // primary invoice
        var dmvFee = $("#defaultDmvFee").val();
        var svcFee = $("#defaultSvcFee").val();
        var otherFee = $("#defaultOtherFee").val();
        var otherDesc = $("#defaultOtherDesc").val();
        var glCode = $("#glCode").val();

        if (dmvFee.trim() === "") {
            vendor_show_DataTable_alert(t, "DMV Fee is required", 5000);
            return;
        }
        if (svcFee.trim() === "") {
            vendor_show_DataTable_alert(t, "Service fee is required", 5000);
            return;
        }
        // secondary
        var svcFee2 = $("#defaultSvcFee_2").val();
        var otherFee2 = $("#defaultOtherFee_2").val();
        var otherDesc2 = $("#defaultOtherDesc_2").val();

        if ($("#defaultSvcFee_2").is(":visible")) {
            if (svcFee2.trim() === "") {
                vendor_show_DataTable_alert(
                    t,
                    "Secondary service fee is required",
                    5000
                );
                return;
            }
        }

        for (var i = 0; i < vendorUnbilled_Items.length; i++) {
            var data = vendorUnbilled_Items[i];

            // set groupId and groupName on invoice
            if (
                vendorCurrentInvoiceIds.length === 0 ||
                $.inArray(data.requestId, vendorCurrentInvoiceIds) < 0
            ) {
                if (groupId === "") {
                    groupId = data.groupId;
                    form.find("#groupId").val(data.groupId);
                    form.find("#groupName").val(data.groupName);
                    var d = populateBillToList(groupId);
                }
                if (groupId === data.groupId) {
                    // add to list
                    vendorCurrentInvoiceIds.push(data.requestId);

                    data.dmvFee = 0.0;
                    if (dmvFee !== "") {
                        data.dmvFee = parseFloat(dmvFee);
                    }

                    data.svcFee = 0.0;
                    if (svcFee !== "") {
                        data.svcFee = roundMoney(parseFloat(svcFee));
                    }

                    data.otherFee = 0.0;
                    if (otherFee !== "") {
                        data.otherFee = roundMoney(parseFloat(otherFee));
                    }
                    data.otherDesc = otherDesc;

                    data.totalFee = roundMoney(
                        data.dmvFee + data.svcFee + data.otherFee
                    );
                    data.glCode = glCode;

                    // Secondary invoice
                    data.svcFee2 = 0.0;
                    if (svcFee2 && svcFee2 !== "") {
                        data.svcFee2 = roundMoney(parseFloat(svcFee2));
                    }
                    data.otherFee2 = 0.0;
                    if (otherFee2 && otherFee2 !== "") {
                        data.otherFee2 = roundMoney(parseFloat(otherFee2));
                    }
                    data.otherDesc2 = otherDesc2;
                    data.totalFee2 = roundMoney(data.svcFee2 + data.otherFee2);

                    details.row.add(data).draw(false);
                }
            }
        }
        UpdateInvoiceTotal();
    }
    t.ajax.reload(null, false);
}
function vendor_hide_global_search() {
    $("#globalSearchDiv").hide();
}
function vendor_show_global_search() {
    $("#globalSearchDiv").show();
}

function requestLookup_initChangeAppType(requestId) {
    var f = $("#changeAppTypePanel").is(":visible");
    $("#changeAppTypeForm_requestId").val(requestId);
    requestLookup_showChangeAppTypePanel(!f);
}
// add function to add items to select using a json array
function addItemsToSelect($ctl, data) {
    $ctl.empty();

    $.each(data, function (index, item) {
        let $option = $("<option/>").val(item.value).text(item.text);
        if (item.selected) {
            $option.attr("selected", "selected");
        }
        $ctl.append($option);
    });
}
function requestLookup_showChangeAppTypePanel(f) {
    if (f) {
        $("#changeAppTypePanel").show();
        $("#detailsEditPanel_changeAppTypeButton").hide();
        // TBD: show a spinner
        $.ajax({
            url: "/Requests/GetNewAppTypes",
            type: "GET",
            data: { requestId: $("#changeAppTypeForm_requestId").val() },
            success: function (data) {
                addItemsToSelect($("#newAppState"), data.states);
                addItemsToSelect($("#newAppType"), data.appTypes);
            },
            error: function (jqXHR, textStatus, errorThrown) {
                vendor_show_alert(container, "Unknown error occurred", -1);
            },
        });

        $("#btnAttUploadClose").show();
        $("#btnChangeTypeSubmit")
            .off("click")
            .on("click", function () {
                //
            });
    } else {
        $("#changeAppTypePanel").hide();
        $("#btnChangeTypeSubmit").show();
        $("#btnAttUploadClose").hide();
        $("#detailsEditPanel_changeAppTypeButton").show();
    }
}
function getfn_datatable_data_dictionary_handler(dictionaryName, fieldName) {
    return function (row, type, val, meta) {
        if (type === "set") {
            row[dictionaryName][fieldName] = val;
            return;
        }
        if (row[dictionaryName]) {
            let fldval = null;
            if (type === "display") {
                fldval = row[dictionaryName][fieldName];
            } else if (type === "filter") {
                fldval = row[dictionaryName][fieldName];
            } else {
                fldval = row[dictionaryName][fieldName];
            }
            return fldval;
        } else if (row[fieldName]) {
            let fldval = null;
            if (type === "display") {
                fldval = row[fieldName];
            } else if (type === "filter") {
                fldval = row[fieldName];
            } else {
                fldval = row[fieldName];
            }
            return fldval;
        }
        return "";
    };
}
function formatProcessingDay(data) {
    if (data) {
        return String(data); // Convert data to a string
    }
    return "Nothing";
}
function vendor_UpdateNeedToProcess(t, tblid) {
    var items = vendorGetSelectedItemObjectsForNeedToProcess(t, tblid);

    if (items.length <= 0) {
        jqToast.error({
            text: "Select at least one row for shipper or processor assigned.",
        });
        return;
    }
    if (!items[0].assignedProcessor && !items[0].assignedShipper) {
        jqToast.error({
            text: "Please assign at least a processor or a shipper.",
        });
        return;
    }
    updateAndSaveNeedToProcessRecord(items, t, tblid);
}
function vendor_BulkUpdateNeedToProcess(t, tblid) {
    var items = vendorGetSelectedItemObjects(t);
    var html = "No items selected";
    var disabled = true;
    if (items.length !== 0) {
        html =
            "Operation will update notes for the " +
            items.length +
            " selected items";
        disabled = false;
    }
    var $form = $("#modal-bulkNeedToProcess");
    vendor_init_dialog($form);
    $form.find("#selectedCount").html(html);

    vendor_bulk_set_multi_code_title_audit_header($form, items, tblid);

    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var assignedShipperName = $form.find("#assignedShipper").val();
                var assignedProcessorName = $form
                    .find("#assignedProcessor")
                    .val();
                if (!assignedShipperName && !assignedProcessorName) {
                    jqToast.error({
                        text: "Please assign at least a processor or a shipper.",
                    });
                    return;
                }
                var items = vendorGetSelectedItemObjectsForBulkNeedToProcess(
                    t,
                    tblid,
                    assignedProcessorName,
                    assignedShipperName
                );
                updateAndSaveNeedToProcessRecord(items, t, tblid);
                $form.modal("hide");
            });
    }
    $.ajax({
        url: "/Vendor/GetAssignedUsersDataForCombobox",
        type: "GET",
        success: function (data) {
            var assignedUserOptions = data.map(function (user) {
                return `<option value="${user.userId}">${user.displayName}</option>`;
            });
            $form.find("#assignedProcessor").html(assignedUserOptions.join(""));
            $form.find("#assignedProcessor").val(items[0].assignedUser);
            $form.find("#assignedShipper").html(assignedUserOptions.join(""));
            $form.find("#assignedShipper").val(items[0].assignedUser);
        },
        error: function (xhr, status, error) {
            console.error("Error fetching assigned users data: ", error);
        },
    });
    $form.modal("show");
}
function vendorGetSelectedItemObjectsForNeedToProcess(t, tblid) {
    var a = [];
    var stageName = getNeedToProcessType(tblid);
    t.rows({ selected: true }).every(function () {
        var rowData = this.data();
        let $node = $(this.node());
        let $selectShipperElement = $node.find(".getassignedShipperdata");
        let selectedShipperUserID = $selectShipperElement.val();
        let $selectedShipperOption = $selectShipperElement.find(
            `option[value="${selectedShipperUserID}"]`
        );
        let userShipperName = $selectedShipperOption.length
            ? $selectedShipperOption.text()
            : "";
        rowData.assignedShipper = userShipperName;
        let $selectProcessorElement = $node.find(".getassignedProcessordata");
        let selectedProcessorUserID = $selectProcessorElement.val();
        let $selectedProcessorOption = $selectProcessorElement.find(
            `option[value="${selectedProcessorUserID}"]`
        );
        let userProcessorName = $selectedProcessorOption.length
            ? $selectedProcessorOption.text()
            : "";
        rowData.assignedProcessor = userProcessorName;
        rowData.needToProcessStageName = stageName;
        a.push(rowData);
    });
    return a;
}
function vendorGetSelectedItemObjectsForBulkNeedToProcess(
    t,
    tblid,
    assignedProcessor,
    assignedShipper
) {
    var a = [];
    var stageName = getNeedToProcessType(tblid);
    t.rows({ selected: true }).every(function () {
        var rowData = this.data();
        rowData.assignedProcessor = assignedProcessor;
        rowData.assignedShipper = assignedShipper;
        rowData.needToProcessStageName = stageName;
        a.push(rowData);
    });
    return a;
}
function updateAndSaveNeedToProcessRecord(items, t, name) {
    if (items.length <= 0) {
        jqToast.error({
            text: "Select at least one record to save.",
        });
        return;
    }

    if (items.length > 0) {
        var token = $('[name="__RequestVerificationToken"]').val();
        $.ajax({
            url: "/Vendor/BulkUpdateNeedToProcess",
            type: "POST",
            contentType: "application/json",
            headers: { RequestVerificationToken: token },
            data: JSON.stringify(items),
            success: function (response) {
                jqToast.success({
                    text: "Records have been successfully assigned." /**/,
                });
                t.ajax.reload();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                jqToast.error({
                    text: "Error in Update record",
                });
            },
        });
    }
}
function getNeedToProcessType(tblid) {
    switch (tblid) {
        case "DT_All_Incoming":
            return "Incoming";

        case "DT_ReadyToProcess":
            return "Ready To Process";

        case "DT_ReadyForPrinting":
            return "Ready For Printing";

        default:
            return tblid;
    }
}

function NeedToProcess_RefreshViewFilters(ctl) {
    let day = $("#processingDay").val();

    let assignedProcessor = $("#assignedProcessorFilter").val();

    let views = $("table.needToProcess");

    let filters = [];
    if (day && day !== "") {
        filters.push({ name: "processingDay", value: day });
    }
    if (assignedProcessor && assignedProcessor !== "") {
        filters.push({ name: "assignedProcessor", value: assignedProcessor });
    }
    let tables = [];
    let filter = JSON.stringify(filters);
    $.each(views, function (idx, view) {
        if (view.id) {
            if (view.id.startsWith("DT_")) {
                $(view).data("jsonfilter", filter);
                let table = $(view).DataTable();

                let dayColumnIndex = table.column(':contains("Day")').index();

                table
                    .column(dayColumnIndex)
                    .search(day ? "^" + day + "$" : "", true, false);

                let assignedProcessorColumnIndex = table
                    .column(':contains("Assigned Processor")')
                    .index();

                table
                    .column(assignedProcessorColumnIndex)
                    .search(
                        assignedProcessor ? "^" + assignedProcessor + "$" : "",
                        true,
                        false
                    );

                table.draw();
            }
            tables.push($(`#${view.id}`));
        }
    });
}
function fetchUsers() {
    const url = "/Vendor/GetAssignedUsersDataForCombobox"; // Hardcoded URL
    return new Promise((resolve, reject) => {
        $.ajax({
            url: url,
            type: "GET",
            success: function (data) {
                // Check if data is null or undefined
                if (data === null) {
                    console.error("No data received from the API.");
                    reject("No data received");
                } else {
                    resolve(data); // Resolve the promise with the fetched data
                }
            },
            error: function (xhr, status, error) {
                console.error("Error fetching data: ", error);
                reject(error); // Reject the promise in case of an error
            },
        });
    });
}
function fetchProcessingDays() {
    const url = "/Vendor/GetProcessingDay"; // Hardcoded URL
    return new Promise((resolve, reject) => {
        $.ajax({
            url: url,
            type: "GET",
            success: function (data) {
                // Check if data is null or undefined
                if (data === null) {
                    console.error("No data received from the API.");
                    reject("No data received");
                } else {
                    resolve(data); // Resolve the promise with the fetched data
                }
            },
            error: function (xhr, status, error) {
                console.error("Error fetching data: ", error);
                reject(error); // Reject the promise in case of an error
            },
        });
    });
}
function populateAssignedShipperDropdown(dropdownSelector) {
    // Check if the dropdown selector is valid and the element exists
    const $dropdown = $(dropdownSelector);
    if (!$dropdown.length) {
        console.warn(
            `Dropdown with selector "${dropdownSelector}" does not exist.`
        );
        return; // Exit if the dropdown element is invalid
    }

    fetchUsers()
        .then((data) => {
            if (!Array.isArray(data) || data.length === 0) {
                console.warn(
                    "Data is not an array or is empty. No options will be added."
                );
                return; // Exit if data is not valid
            }

            var options = data.map(function (user) {
                return `<option value="${user.displayName}">${user.displayName}</option>`;
            });
            console.log("Generated options:", options);
            // Select the dropdown using the passed selector and append options
            $dropdown.append(options.join(""));
        })
        .catch((error) => {
            console.error("Failed to populate dropdown: ", error);
        });
}

function populateProcessingDayDropdown(dropdownSelector) {
    // Check if the dropdown selector is valid and the element exists
    const $dropdown = $(dropdownSelector);
    if (!$dropdown.length) {
        console.warn(
            `Dropdown with selector "${dropdownSelector}" does not exist.`
        );
        return; // Exit if the dropdown element is invalid
    }

    fetchProcessingDays()
        .then((data) => {
            if (!Array.isArray(data) || data.length === 0) {
                console.warn(
                    "Data is not an array or is empty. No options will be added."
                );
                return; // Exit if data is not valid
            }

            var options = data.map(function (day) {
                return `<option value="${day.dayid}">${day.daydesc}</option>`;
            });
            console.log("Generated options:", options);
            // Select the dropdown using the passed selector and append options
            $dropdown.append(options.join(""));
        })
        .catch((error) => {
            console.error("Failed to populate dropdown: ", error);
        });
}

function groupProfile_defaultGroupId() {
    return $("#vendorGroupId").val();
}

function changevin_show() {
    let rid, vin, year, make;

    let dynform = $("#dynamicAppForm");
    rid = dynform.find("input[name='RequestId']").val();
    vin = dynform.find("input[name='Vehicle Vin']").val();
    year = dynform.find("input[name='Vehicle Year']").val();
    make = dynform.find("input[name='Vehicle Make']").val();

    let $form = $("#modal-vinChange");
    vendor_init_dialog($form);
    $form
        .find("#ChangeVIN_NewVIN")
        .off("input")
        .on("input", function (e) {
            let vin = $(this).val();
            var ctl = $(this).parent().parent().find(".vinvalidation");
            getVinDetail(vin, ctl, $form);
        });

    $form.find("#ChangeVIN_RequestId").val(rid);
    $form.find("#ChangeVIN_CurrentVIN").val(vin);
    $form.find("#ChangeVIN_CurrentVIN_Year").val(year);
    $form.find("#ChangeVIN_CurrentVIN_Make").val(make);
    $form.find("#ChangeVIN_NewVIN").val("");
    $form.find("#ChangeVIN_NewVIN_Year").val("");
    $form.find("#ChangeVIN_NewVIN_Make").val("");

    let disabled = false;
    $form.find("#okButton").prop("disabled", disabled);
    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                $form.find("#ChangeVIN_NewVIN").val();
                $form.find("#ChangeVIN_NewVIN_Year").val();
                $form.find("#ChangeVIN_NewVIN_Make").val();

                dynform
                    .find("input[name='Vehicle Vin']")
                    .val($form.find("#ChangeVIN_NewVIN").val());
                dynform
                    .find("input[name='Vehicle Year']")
                    .val($form.find("#ChangeVIN_NewVIN_Year").val());
                dynform
                    .find("input[name='Vehicle Make']")
                    .val($form.find("#ChangeVIN_NewVIN_Make").val());
                $form.modal("hide");
            });
    }
    $form.modal("show");
}

function changeVinHandler() {
    changevin_show();
}

function loadPaymentAndDisbursement(IsCredit, requestId) {
    var $panel = $("#paymentAndDisbursementPanel");

    $.ajax({
        url:
            "/Requests/GetAllPayments" +
            "?IsCredit=" +
            IsCredit +
            "&requestId=" +
            requestId,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        contentType: false,
        processData: false,
        data: {
            IsCredit: IsCredit,
        },
        success: function (response) {
            $panel.show();

            var dataResponse = JSON.parse(response);

            if (
                $.fn.DataTable.isDataTable(
                    "#paymentAndDisbursementPanelDataTable"
                )
            ) {
                $("#paymentAndDisbursementPanelDataTable")
                    .DataTable()
                    .clear()
                    .destroy();
                $("#paymentAndDisbursementPanelDataTable tbody").empty();
            }

            var table = $("#paymentAndDisbursementPanelDataTable").DataTable({
                data: dataResponse.data,
                autoWidth: false,
                paging: true,
                pageLength: 10,
                scrollX: true,
                scrollY: "400px",
                scrollCollapse: true,
                searching: true,
                info: true,
                lengthChange: true,
                ordering: true,
                language: {
                    emptyTable: "No payment and disbursement available",
                },
                buttons: [
                    {
                        extend: "collection",
                        className: "btn-export-auditlookup",
                        text: "Export",
                        buttons: [
                            {
                                extend: "copy",
                                text: "Copy",
                                exportOptions: {
                                    columns: ":visible",
                                },
                            },
                            {
                                extend: "excelHtml5",
                                text: "Excel",
                                autoFilter: true,
                                sheetName: "myDMV.pro",
                                exportOptions: {
                                    columns: ":visible",
                                },
                            },
                            {
                                extend: "pdfHtml5",
                                text: "PDF",
                                orientation: "landscape",
                                pageSize: "LEGAL",
                                exportOptions: {
                                    columns: ":visible",
                                },
                            },
                            {
                                extend: "print",
                                text: "Print",
                                exportOptions: {
                                    columns: ":visible",
                                },
                            },
                        ],
                    },
                ],
                columns: [
                    { data: "vin", title: "VIN" },
                    {
                        data: "amount",
                        title: "Amount",
                        render: function (data) {
                            return "$" + data;
                        },
                    },
                    {
                        data: "paymentTypeID",
                        title: "Payment Type",
                        render: function (data) {
                            return getPaymentTypeName(data);
                        },
                    },
                    { data: "processingFee", title: "Processing Fee" },
                    { data: "totalCharge", title: "Total Charge" },
                    {
                        data: "paymentDate",
                        title: "Payment Date",
                        render: function (data) {
                            return data ? formatDateForDisplay(data) : "";
                        },
                    },
                    {
                        data: "requestId",
                        title: "Actions",
                        orderable: false,
                        searchable: false,
                        render: function (data, type, row) {
                            return `
                            <button class="btn btn-primary btn-sm edit-btn-payment-and-disbursement" data-id="${data}">Edit</button>
                            <button class="btn btn-danger btn-sm delete-btn-payment-and-disbursement" data-id="${data}">Delete</button>`;
                        },
                    },
                ],
            });

            table
                .buttons()
                .container()
                .appendTo("#export-button-paymentAndDisbursement");

            table.columns.adjust();
        },
        error: function (xhr, status, error) {
            console.error("Error occured while processing the request.");
        },
    });
}


function vendor_PrintInstructionPackets(t) {
    var items = vendorGetSelectedItems(t);
    if (items.length === 0) {
        return;
    }

    var $form = $("#downloadInstructionPacket");
    attachToken($form);
    $form.html("");
    $.each(items, function (i, id) {
        $form.append('<input type="hidden" name="ids" value="' + id + '" />');
    });
    attachToken($form);
    $form.trigger("submit");
}

function GetBillingAccessData() {
    $.ajax({
        url: "/Vendor/GetFeaturePermissionForParticularUser",
        type: 'GET',
        headers: { 'RequestVerificationToken': $('[name="__RequestVerificationToken"]').val() },
        async: true,
    }).done(function (response) {
        if (response.success && response.data) {
            if (response.data.readState) {
                userReadPermission = response.data.readState

            }
            if (response.data.writeState) {
                userWritePermission = response.data.writeState
            }
        }
        else {
            console.error("Response does not have expected success or data.");
        }
    }).fail(function (xhr, status, error) {
        console.error("AJAX failed: " + status + " " + error); // Detailed error
    });
}
