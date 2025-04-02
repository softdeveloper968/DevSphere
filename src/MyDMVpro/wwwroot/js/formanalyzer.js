"use strict";
function formAnalyzer_show_CorrectVin(t) {}
function formAnalzyer_show_FormFields(vwfields, hiddenfields) {
    if (vwfields && vwfields.length > 0) {
        // hide all fields, then show those we want
        $("[data-section]").hide();
        $("[data-field]").hide();
        vwfields.forEach(function (val, index) {
            let fld = $('[data-field="' + val + '"]');
            fld.closest("[data-section]").show();
            fld.show();
        });
    } else if (hiddenfields && hiddenfields.length > 0) {
        // show all fields first, then hide
        $("[data-section]").show();
        $("[data-field]").show();
        hiddenfields.forEach(function (val, index) {
            let fld = $('[data-field="' + val + '"]');
            fld.hide();
        });
    } else {
        // nothing to hide or show
        $("[data-section]").show();
        $("[data-field]").show();
    }
}
function formAnalyzer_fill_vinlist(txt) {
    var $datalist = $("#partialVinSearchList");
    if (txt.length < 3) {
        $datalist.empty();
        return;
    }
    var formData = new FormData();
    formData.append("partialVin", txt);
    $.ajax({
        url: "/FormAnalyzer/VinSearch",
        type: "POST",
        async: true,
        data: formData,
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
            if (data.status === "success") {
                $datalist.empty();
                $.each(data.data, function () {
                    $datalist.append('<option value="' + this + '"/>');
                });
            } else {
                $datalist.empty();
            }
        })
        .fail(function (xhr) {
            formAnalyzer_processError(xhr);
        });
}
function formAnalyzer_processError(xhr, container) {
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
function formAnalyzer_FilterOnVin(t, vin) {
    var match_DT = $("#DT_FormAnalyzerMatch").DataTable();

    if (!isVIN(vin)) {
        // tbd
        formAnalyzer_hide_ApplyToPanel();
        formAnalyzer_clearEditForm();
    } else {
        match_DT.columns([".reqVIN"]).every(function () {
            this.search(vin ? vin : "", false, false).draw();
        });
        formAnalyzer_show_ApplyToPanel();
    }
}
function formAnalyzer_apply_SplitNameAddressField($ctl) {
    var v = $ctl.val();
}
function getSplitAddress(addr) {
    try {
        var data = new FormData();
        data.append("addr", addr);
        $.ajax({
            url: "/FormAnalyzer/SplitAddress",
            type: "POST",
            data: data,
            async: true,
            complete: function () {},
            success: function (data) {},
            error: function () {},
        });
    } catch (e) {}
}

function formAnalyzer_apply_ExtractedFields($panel, fields, vin) {
    var vinIndex = -1;

    $.each(fields, function (idx, fobj) {
        if (fobj.field === "VIN" || fobj.field === "Vehicle Vin") {
            vinIndex = idx;
        }
    });

    if (vinIndex === -1) {
        var vv = { field: "Vehicle Vin", value: "" };
        fields.push(vv);
    }
    $.each(fields, function () {
        var f = this.field;
        var v = this.value;
        var triggerInput = false;
        if (f === "VIN" || f === "Vehicle Vin") {
            v = vin;
            triggerInput = true;
        }
        if (typeof f !== "undefined" && f !== null && f !== "") {
            if (typeof v !== "undefined") {
                if (v !== null && v !== "") {
                    var $matches = $panel.find("*[name='" + f + "']");
                    $matches.val(function (index, currentvalue) {
                        if (this.type === "date") {
                            // format in expected format
                            v = v.split(" ").join("");
                            v = v.split("/").join("-");

                            if (validateDate_MM_DD_YYYY(v)) {
                                v = moment(v, "MM-DD-YYYY").format(
                                    "YYYY-MM-DD"
                                );
                            } else if (validateDate_MM_DD_YY(v)) {
                                v = moment(v, "MM-DD-YY").format("YYYY-MM-DD");
                            } else if (!validateDate_YYYY_MM_DD(v)) {
                                $matches.addClass("formscan_error");
                            }
                        }
                        if (currentvalue !== v) {
                            if (currentvalue.trim() !== v.trim()) {
                                $matches.addClass("formscan_changed");
                            }
                            return v;
                        }
                        return currentvalue;
                    });
                }
            }
        }
        if (triggerInput) {
            getVinDetail(vin, $panel.find(".vinvalidation"));
        }
    });
}
function validateDate_MM_DD_YYYY(testdate) {
    var date_regex = /^\d{2}-\d{2}-\d{4}$/;
    return date_regex.test(testdate);
}
function validateDate_MM_DD_YY(testdate) {
    var date_regex = /^\d{2}-\d{2}-\d{2}$/;
    return date_regex.test(testdate);
}
function xvalidateDate_MM_DD_YYYY(testdate) {
    var date_regex =
        /^(0?[1-9]|1[0-2])\/(0?[1-9]|1\d|2\d|3[01])\/(19|20)\d{2}$/;
    return date_regex.test(testdate);
}
function validateDate_YYYY_MM_DD(testdate) {
    var date_regex = /^\d{4}-\d{2}-\d{2}$/;
    return date_regex.test(testdate);
}
function xvalidateDate_YYYY_MM_DD(testdate) {
    var date_regex =
        /^(0?[1-9]|1[0-2])\/(0?[1-9]|1\d|2\d|3[01])\/(19|20)\d{2}$/;
    return date_regex.test(testdate);
}
function formAnalyzer_match_selected() {
    formAnalyzer_show_ViewDetails(false);
}
function formAnalyzer_match_deselected() {
    formAnalyzer_hide_ViewDetails();
    $("#vinUpdater").hide();
    formAnalyzer_clearEditForm();
    formAnalyzer_show_NewFormPanel();
}
function formAnalyzer_upload_selected() {
    formAnalyzer_show_ViewDetails(true);
}
function formAnalyzer_upload_deselected() {
    formAnalyzer_hide_ApplyToPanel();
    formAnalyzer_hide_ViewDetails();
    formAnalyzer_hide_Attachment();
}

function formAnalyzer_show_ViewDetails(updateMatchFilter) {
    var $uploadTable = $("#DT_AnalyzerUploads");
    var upload_DT = $uploadTable.DataTable();
    var items = getSelectedItemObjects(upload_DT);

    if (items.length === 1) {
        var item = items[0];
        var attachmentId = item["attachmentId"];
        var fileUploadId = item["fileUploadId"];
        var vin = item["vin"].toUpperCase();
        var isValidVin = isVIN(vin);
        var matchCount = item["activeMatches"];

        if (updateMatchFilter) {
            var fname = item["uploadFileName"];
            formAnalyzer_show_Attachment(attachmentId, fname);
        }

        //$uploadTable.data("attachmentid", attachmentId);
        $("#formAnalyzer_uploadId").val(fileUploadId);
        $("#formAnalyzer_vinUpdater").val(vin);
        if (isValidVin && matchCount === "0") {
            $("#unmatchedvalidvintext").show();
            $("#invalidvintext").hide();
            $("#detailsEditPanel").show();
        } else if (!isValidVin) {
            $("#unmatchedvalidvintext").hide();
            $("#invalidvintext").show();
            $("#detailsEditPanel").show();
            // trigger vin check
            $("#formAnalyzer_vinUpdater").trigger("input");
        }
        if (matchCount === "0") {
            formAnalyzer_updatePanelView(isValidVin);
            updateMatchFilter = false;
        } else {
            $("#vinUpdater").hide();
            $("#opStatusPanel").hide();
            $("#createFormPanelBody").hide();
            $("#newFormPanel").html("");
        }
        var fields = JSON.parse(item["extractedFields"] || "[]");
        var vwfields = JSON.parse(item["visibleFields"] || "[]");
        var hiddenfields = JSON.parse(item["hiddenFields"] || "[]");

        $("#createNewForm").data("extractedfields", item["extractedFields"]);
        $("#createNewForm").data("vin", vin);

        if (updateMatchFilter) {
            let match_DT = $("#DT_FormAnalyzerMatch").DataTable();
            $("#DT_FormAnalyzerMatch").show();
            match_DT.off("draw").on("draw", function () {
                var reqItems = getSelectedItemValuesOrActiveDefault(
                    match_DT,
                    "requestId"
                );
                if (reqItems.length === 1) {
                    vendorEditRequestInline(
                        fileUploadId,
                        reqItems[0],
                        vin,
                        fields,
                        vwfields,
                        hiddenfields
                    );
                } else if (reqItems.length > 1) {
                    vendorEditRequestInline(
                        fileUploadId,
                        reqItems[0],
                        vin,
                        fields,
                        vwfields,
                        hiddenfields
                    );
                } else {
                    formAnalyzer_match_deselected();
                }
            });
            formAnalyzer_FilterOnVin(match_DT, vin);
        } else {
            let match_DT = $("#DT_FormAnalyzerMatch").DataTable();
            let reqItems = getSelectedItemValuesOrActiveDefault(
                match_DT,
                "requestId"
            );
            if (reqItems.length === 1) {
                vendorEditRequestInline(
                    fileUploadId,
                    reqItems[0],
                    vin,
                    fields,
                    vwfields,
                    hiddenfields
                );
                $("#DT_FormAnalyzerMatch").show();
            } else {
                // Show Create form
            }
        }
    } else {
        formAnalyzer_hide_ViewDetails();
    }
}
function formAnalyzer_updatePanelView(isValidVin) {
    if (isValidVin) {
        // Valid VIN, but no matches
        // show the "New Form" panel
        $("#vinUpdater").hide();
        $("#opStatusPanel").hide();
        $("#createFormPanelBody").show();
        enable_Details_SaveButton();
    } else {
        // Invalid VIN, require VIN correction
        formAnalyzer_show_VinCorrection();
    }
}
function formAnalyzer_hide_Attachment() {
    var $panel = $("#docViewPanel");
    $panel.data("attachmentid", "");
    $panel.empty();
}
function formAnalyzer_show_Attachment(attachmentId, filename) {
    var $panel = $("#docViewPanel");
    const currentAttachmentId = $panel.data("attachmentid");
    if (currentAttachmentId && currentAttachmentId === attachmentId) {
        // no change, ignore
    } else {
        var fext = "";
        if (filename) {
            fext = `/${encodeURIComponent(filename)}`;
        }
        var pdfurl = `/FormAnalyzer/ViewAttachment/${attachmentId}${fext}`;
        var embedtext = `<p>This browser does not support PDFs. Please download the PDF to view it: <a href="${pdfurl}">Download PDF</a>.</p>`;
        let html = `<object data="${pdfurl}" type="application/pdf" width="100%" height="100%"><embed src="${pdfurl}" type="application/pdf">${embedtext}</embed></object>`;
        html = `<iframe id="pdfFrame" src="${pdfurl}#view=FitH" />`;
        $panel.data("attachmentid", attachmentId);
        $panel.empty().html(html);
    }
}
function enable_Details_SaveButton() {
    if ($("#newFormPanel").is(":visible")) {
        // enable when newFormPanel is visible, not just child form
        // submit will only occur if child form visible
        $("#detailsEditPanel_saveButton").prop("disabled", false);
        return;
    } else if ($("#detailsEditPanelBody").find("form").is(":visible")) {
        $("#detailsEditPanel_saveButton").prop("disabled", false);
        return;
    }
    $("#detailsEditPanel_saveButton").prop("disabled", true);
}
function formAnalyzer_show_VinCorrection() {
    $("#vinUpdater").show();
    $("#opStatusPanel").hide();
    $("#createFormPanelBody").hide();
    $("#newFormPanel").html("");
    $("#detailsEditPanel").show();
    enable_Details_SaveButton();
}
function formAnalyzer_hide_NewFormPanel() {
    $("#createFormPanelBody").hide();
    $("#newFormPanel").html("");
    enable_Details_SaveButton();
}
function formAnalyzer_show_NewFormPanel() {
    $("#newFormPanel").html("");
    $("#createFormPanelBody").show();
    enable_Details_SaveButton();
}
function formAnalyzer_hide_ApplyToPanel() {
    $("#applyToListPanel").hide();
}
function formAnalyzer_show_ApplyToPanel() {
    $("#applyToListPanel").show();
}
function formAnalyzer_selection_updated(items) {
    if (items.length === 1) {
        formAnalyzer_upload_selected();
        var attid = items[0].attachmentId;
        var fname = items[0].uploadFileName;
        formAnalyzer_show_Attachment(attid, fname);
    } else {
        formAnalyzer_upload_deselected();
    }
}
function formAnalyzer_hide_ViewDetails() {
    formAnalyzer_hide_NewFormPanel();
    $("#detailsEditPanel").hide();
    $("#vinUpdater").hide();
    formAnalyzer_clearEditForm();
}
function formAnalyzer_clearEditForm() {
    var $pnl = $("#detailsEditPanel");
    $pnl.data("reqid", "");
    $pnl.find(".panel-body").empty().html("");
    //$pnl.find("#okButton").prop('disabled', true);
}

function getSelectedItems(t) {
    return getSelectedItemValues(t, "fileUploadId");
}
function getSelectedItemValues(t, f) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push(this[f]);
    });
    return a;
}
function getSelectedItemValuesOrActiveDefault(t, f) {
    var a = [];

    $.each(t.rows({ selected: true }).data(), function () {
        a.push(this[f]);
    });
    if (a.length === 0) {
        // return active record if only one
        $.each(t.rows().data(), function (idx, data) {
            if (data.statusId === "1") {
                t.rows(idx).select();
                a.push(this[f]);
            }
        });
    }
    return a;
}
function getSelectedItemValuesOrDefault(t, f) {
    var a = [];
    if (t.rows().count() === 1) {
        $.each(t.rows().data(), function () {
            a.push(this[f]);
        });
    } else {
        $.each(t.rows({ selected: true }).data(), function () {
            a.push(this[f]);
        });
        if (a.length === 0) {
            t.row(0).select();
            $.each(t.rows({ selected: true }).data(), function () {
                a.push(this[f]);
            });
        }
    }
    return a;
}
function getSelectedItemObjects(t) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push(this);
    });
    return a;
}
function formAnalyzer_update_request(uid, rid, json) {
    var formData = new FormData();
    formData.append("uploadId", uid);
    formData.append("requestId", rid);
    if (typeof json !== "undefined" && json !== null) {
        formData.append("json", json);
    }
    $.ajax({
        url: "/FormAnalyzer/UpdateRequest",
        type: "POST",
        async: true,
        data: formData,
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
            if (data.success === true) {
                var t = $("#DT_AnalyzerUploads").DataTable();
                var selindex = t.row({ selected: true }).index();
                t.rows(selindex + 1).select(true);
                t.ajax.reload();
            } else {
                vendor_show_alert(undefined, data.message, -1);
            }
        })
        .fail(function (xhr) {
            formAnalyzer_processError(xhr);
        });
}
function formAnalyzer_show_AttachNoUpdate(t) {
    var uploadIDs = getSelectedItemValues(t, "fileUploadId");

    if (uploadIDs.length > 0) {
        var $form = $("#modal-AttachNoUpdate");
        $form.find("#selectedCount").html("Attach upload to matched request");
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();
                var formData = new FormData();
                $.each(uploadIDs, function () {
                    formData.append("uploadIds", this);
                });
                $.ajax({
                    url: "/FormAnalyzer/AttachWithoutUpdate",
                    type: "POST",
                    async: true,
                    data: formData,
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
                        $("#DT_AnalyzerUploads").DataTable().ajax.reload();
                        $form.modal("hide");
                    })
                    .fail(function (xhr) {
                        formAnalyzer_processError(xhr);
                    });
            });
        $form.modal("show");
    } else {
        alert("No item selected");
    }
}
function formAnalyzer_show_CreateRequest(t) {}
function formAnalyzer_show_ProcessTitles(t) {
    var uploadIDs = getSelectedItemValues(t, "fileUploadId");

    if (uploadIDs.length > 0) {
        var $form = $("#modal-ProcessTitles");
        $form
            .find("#selectedCount")
            .html(
                "Requests will be marked as title received and title scan will be deleted."
            );
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();
                var formData = new FormData();
                $.each(uploadIDs, function () {
                    formData.append("uploadIds", this);
                });
                $.ajax({
                    url: "/FormAnalyzer/ProcessTitles",
                    type: "POST",
                    async: true,
                    data: formData,
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
                        $("#DT_AnalyzerUploads").DataTable().ajax.reload();
                        $form.modal("hide");
                    })
                    .fail(function (xhr) {
                        formAnalyzer_processError(xhr);
                    });
            });
        $form.modal("show");
    } else {
        alert("No item selected");
    }
}
function formAnalyzer_show_RescanUpload(t) {
    var uploadIDs = getSelectedItemValues(t, "fileUploadId");

    if (uploadIDs.length > 0) {
        var $form = $("#modal-RescanUpload");
        $form
            .find("#selectedCount")
            .html(
                "Uploaded attachments will be rescanned.  Only use if failed to process on first upload."
            );
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();
                var formData = new FormData();
                $.each(uploadIDs, function () {
                    formData.append("uploadIds", this);
                });
                $.ajax({
                    url: "/FormAnalyzer/RescanUploads",
                    type: "POST",
                    async: true,
                    data: formData,
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
                        $("#DT_AnalyzerUploads").DataTable().ajax.reload();
                        $form.modal("hide");
                    })
                    .fail(function (xhr) {
                        formAnalyzer_processError(xhr);
                    });
            });
        $form.modal("show");
    } else {
        alert("No item selected");
    }
}
function formAnalyzer_newFormSubmitted() {
    var uploadId = $("#formAnalyzer_uploadId").val();
    formAnalyzer_deleteUpload(null, uploadId);
}
function formAnalyzer_deleteUpload($form, uploadId) {
    var formData = new FormData();
    formData.append("uploadId", uploadId);
    $.ajax({
        url: "/FormAnalyzer/DeleteUpload",
        type: "POST",
        async: true,
        data: formData,
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
            $("#DT_AnalyzerUploads").DataTable().ajax.reload();
            if ($form !== null) $form.modal("hide");
        })
        .fail(function (xhr) {
            formAnalyzer_processError(xhr);
        });
}
function formAnalyzer_show_DeleteItems(t) {
    var uploadIDs = getSelectedItemValues(t, "fileUploadId");
    if (uploadIDs.length === 1) {
        let $form = $("#modal-DeleteUpload");
        $form
            .find("#selectedCount")
            .html("Uploaded attachment will be deleted.");
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();
                formAnalyzer_deleteUpload($form, uploadIDs[0]);
            });
        $form.modal("show");
    } else if (uploadIDs.length > 1) {
        let $form = $("#modal-DeleteUpload");
        $form
            .find("#selectedCount")
            .html("Uploaded attachments will be deleted.");
        $form
            .find("#okButton")
            .off("click")
            .on("click", function (e) {
                e.preventDefault();
                var formData = new FormData();
                $.each(uploadIDs, function () {
                    formData.append("uploadIds", this);
                });
                $.ajax({
                    url: "/FormAnalyzer/DeleteUploads",
                    type: "POST",
                    async: true,
                    data: formData,
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
                        $("#DT_AnalyzerUploads").DataTable().ajax.reload();
                        $form.modal("hide");
                    })
                    .fail(function (xhr) {
                        formAnalyzer_processError(xhr);
                    });
            });
        $form.modal("show");
    } else {
        alert("No item selected");
    }
}
var requestCounter = 0;
function vendorEditRequestInline(
    uploadId,
    reqid,
    vin,
    fields,
    vwfields,
    hiddenfields
) {
    var $panel = $("#detailsEditPanel");

    if ($panel.data("reqid") === reqid) {
        $panel.find(".panel-body").show();
        $panel.show();
        return;
    }

    let thisCounter = ++requestCounter;

    $panel.find(".panel-body").hide();
    $panel.data("reqid", reqid);
    $panel.find(".panel-body").empty().html("");

    $.ajax({
        url: "/AppForm/VendorOpen/" + reqid,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (res) {
        //if (thisCounter < requestCounter) {
        //    // a later request has come through, just throw out the result
        //    $panel.find(".panel-body").hide();

        //    console.log("** skipping delayed result");
        //    return;
        //}
        if ($panel.data("reqid") === reqid) {
            $panel.find(".panel-body").html(res);
            var $btnSave = $panel.find("#detailsEditPanel_saveButton");

            $panel = $("#detailsEditPanel"); //refresh after reload

            var $form = $panel.find(".panel-body").find("form");
            $form.off("submit").submit(function (e) {
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
                })
                    .done(function (data) {
                        // now match attachment with request
                        $btnSave.attr("disabled", false);
                        try {
                            formAnalyzer_update_request(uploadId, reqid, null);
                        } catch (e) {}
                    })
                    .fail(function (xhr) {
                        formAnalyzer_processError(xhr);
                    });
                return false;
            });
            $btnSave.prop("disabled", false);

            formAnalyzer_apply_ExtractedFields($panel, fields, vin);
            formAnalzyer_show_FormFields(vwfields, hiddenfields);

            if ($form.find("input[name='RequestId']").val() !== reqid) {
                return;
            }

            if (thisCounter === requestCounter) {
                $panel.find(".panel-body").show();
                formAnalyzer_hide_NewFormPanel();
            }
        }
    });
    return false;
}
var vinLookupUrl = "/Vin/Lookup/";
//?format=json';
function formAnalyzer_isValidVin(vin) {
    var isValid = false;
    try {
        vin = vin || "";
        vin = vin.trim().toUpperCase();
        if (vin.length !== 17) {
            return false;
        }
        $.ajax({
            url: vinLookupUrl + vin,
            type: "GET",
            async: false,
        })
            .done(function (data) {
                if (data.VIN) {
                    isValid = true;
                }
            })
            .fail(function (xhr) {
                formAnalyzer_processError(xhr);
            });
    } catch (e) {
        console.log(e);
    }
    return isValid;
}
function getVinDetail(vin, $ctl, $form) {
    try {
        vin = vin || "";
        vin = vin.trim().toUpperCase();
        $ctl.removeClass("badvinflag badvinchecksum");
        $ctl.addClass("vinlookupflag");
        if (vin.length !== 17) {
            $ctl.removeClass("vinlookupflag");
            if (vin.length !== 0) {
                $ctl.addClass("badvinflag");
            }
            return;
        }
        $.ajax({
            url: vinLookupUrl + vin,
            type: "GET",
            async: true,
            complete: function () {},
            success: function (data) {
                $ctl.removeClass("badvinchecksum vinlookupflag");
                setVinFields(data, $ctl, $form);
            },
            error: function () {
                $ctl.removeClass("badvinchecksum vinlookupflag");
                $ctl.addClass("badvinflag");
            },
        });
    } catch (e) {}
}

function isVIN(v) {
    var i = "";
    var d = "";
    var sum = 0;
    var weights = [8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2];
    var transliterations = [
        1, //a
        2, //b
        3, //c
        4, //d
        5, //e
        6, //f = 6,
        7, //g = 7,
        8, //h = 8,
        0, //i unused
        1, //j = 1,
        2, //k = 2,
        3, //l = 3,
        4, //m = 4,
        5, //n = 5,
        0, //o unused
        7, //p = 7,
        0, //q unused
        9, //r = 9,
        2, //s = 2,
        3, //t = 3,
        4, //u = 4,
        5, //v = 5,
        6, //w = 6,
        7, //x = 7,
        8, //y = 8,
        9, //z = 9
    ];
    if (typeof v === "undefined" || v === null || v.length !== 17) return false;

    v = v.toUpperCase();
    //var vinRegex = "(?x)    ## allow comments
    //    ^                    ## from the start of the string
    //                        ## see http://en.wikipedia.org/wiki/Vehicle_Identification_Number for VIN spec
    //    [A-Z\d]{3}            ## World Manufacturer Identifier (WMI)
    //    [A-Z\d]{5}            ## Vehicle decription section (VDS)
    //    [\dX]                ## Check digit
    //    [A-Z\d]                ## Model year
    //    [A-Z\d]                ## Plant
    //    \d{6}                ## Sequence
    //    $                    ## to the end of the string
    //";
    var vinRegex = "^[A-HJ-NPR-Z\\d]{8}[\\dX][A-HJ-NPR-Z\\d]{8}$";

    let re = new RegExp(vinRegex);

    if (re.test(v) === false) {
        return false;
    }

    var zeroCharCode = "0".charCodeAt(0);
    var nineCharCode = "9".charCodeAt(0);
    var aCharCode = "A".charCodeAt(0);

    for (i = 0; i < v.length; i++) {
        var charCode = v.charCodeAt(i);
        if (i === 8) continue; // skip checkdigit
        if (charCode >= zeroCharCode && charCode <= nineCharCode) {
            var num = charCode - zeroCharCode;
            sum += num * weights[i];
        } else {
            var offset = charCode - aCharCode;
            sum += transliterations[offset] * weights[i];
        }
    }

    var checkDigit = sum % 11;

    if (checkDigit === 10) {
        return "X" === v.charAt(8);
    } else {
        checkDigit = zeroCharCode + checkDigit;
        return checkDigit === v.charCodeAt(8);
    }
}
