"use strict";
function fileLibrary_FilterMatchListOnVin(t, vin) {
    var request_DT = $("#DT_FileLibraryMatch").DataTable();

    request_DT.columns([".reqVIN"]).every(function () {
        this.search(vin ? vin : "XXXXXXXXXXXXXXXXX", false, false).draw();
    });
    fileLibrary_show_MatchListPanel();
}
function fileLibrary_match_selected() {
    fileLibrary_populateAttachmentTypeSelector();
}
function fileLibrary_match_deselected() {
    fileLibrary_populateAttachmentTypeSelector();
}
function fileLibrary_upload_selected() {
    fileLibrary_show_ViewDetails(true);
}
function fileLibrary_upload_deselected() {
    fileLibrary_show_ViewDetails(false);
    fileLibrary_hide_MatchListPanel();
}

function fileLibrary_show_ViewDetails(updateMatchFilter) {
    var $uploadTable = $("#DT_FileLibrary");
    var upload_DT = $uploadTable.DataTable();
    var items = getSelectedItemObjects(upload_DT);

    if (items.length === 0) {
        fileLibrary_show_Attachment();
    } else if (items.length === 1) {
        var item = items[0];
        var attachmentId = item["attachmentId"];
        var attachmentTypeId = item["attachmentTypeId"];
        var attachmentTypeName = item["typename"];
        var filename = item["filename"];
        var vin = getVINfromFilename(filename);
        var isValidVin = isVIN(vin);
        if (isValidVin === false) {
            vin = getPartialVIN(filename);
            if (vin.length >= 6) {
                isValidVin = true;
            }
        }

        fileLibrary_show_Attachment(attachmentId, filename);
        // Show attachment selection panel
        fileLibrary_updateAttachmentTypeSelector(
            attachmentId,
            attachmentTypeId,
            attachmentTypeName
        );

        $("#fileLibrary_uploadId").val(attachmentId);
        if (isValidVin) {
            $("#unmatchedvalidvintext").show();
            $("#invalidvintext").hide();
        } else {
            $("#unmatchedvalidvintext").hide();
            $("#invalidvintext").show();
            // trigger vin check
            $("#fileLibrary_vinUpdater").trigger("input");
        }

        if (updateMatchFilter) {
            let match_DT = $("#DT_FileLibraryMatch").DataTable();
            $("#DT_FileLibraryMatch").show();
            match_DT.off("draw").on("draw", function () {
                let reqItems = getSelectedItemValuesOrActiveDefault(
                    match_DT,
                    "requestId"
                );
                if (reqItems.length === 1) {
                    //vendorEditRequestInline(fileUploadId, reqItems[0], vin, fields);
                } else if (reqItems.length > 1) {
                    //vendorEditRequestInline(fileUploadId, reqItems[0], vin, fields);
                }
            });
            fileLibrary_FilterMatchListOnVin(match_DT, vin);
        } else {
            let match_DT = $("#DT_FileLibraryMatch").DataTable();
            let reqItems = getSelectedItemValuesOrActiveDefault(
                match_DT,
                "requestId"
            );
            if (reqItems.length === 1) {
                $("#DT_FileLibraryMatch").show();
            }
        }
    }
}
function fileLibrary_updateAttachmentTypeSelector(
    attachmentId,
    attachmentTypeId,
    attachmentTypeName
) {
    var $panel = $(".match_attachmentType");

    $panel.empty("");
    $panel.data("attachmentid", attachmentId);
    $panel.data("attachmenttypeid", attachmentTypeId);
    $panel.data("attachmenttypename", attachmentTypeName);

    //    fileLibrary_populateAttachmentTypeSelector(attachmentId, attachmentTypeId, attachmentTypeName);
}
function fileLibrary_populateAttachmentTypeSelector(
    attachmentId,
    attachmentTypeId,
    attachmentTypeName
) {
    if (typeof attachmentId === "undefined") {
        var $panel = $(".match_attachmentType");
        attachmentId = $panel.data("attachmentid");
        attachmentTypeId = $panel.data("attachmenttypeid");
        attachmentTypeName = $panel.data("attachmenttypename");
    }

    var formData = new FormData();
    var selectedRequest = getSelectedMatch();

    if (selectedRequest.length !== 1) {
        $(".match_attachmentType").empty();
        return;
    }
    var requestId = selectedRequest[0];

    formData.append("id", requestId);

    $.ajax({
        url: "/FileLibrary/GetRequestAttachmentTypes",
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
    }).done(function (data) {
        if (data.success) {
            let content = $("#fl_ConfirmTypeTemplate").html();
            let div = $(content);
            div.find("#fl_requestedTypes").html("");
            let $select = div.find("#fl_requestedTypes");

            let hasType = false;
            let hasExtra = false;

            for (let i = 0; i < data.data.length; i++) {
                let atype = data.data[i];
                let selected = "";
                if (atype.attachmentTypeId === attachmentTypeId) {
                    hasType = true;
                    selected = "selected";
                }
                $select.append(
                    `<option value="${atype.attachmentTypeId}" ${selected}>${atype.attachmentTypeName}</option>`
                );
            }
            if (!hasExtra) {
                $select.append(
                    `<option value="" class="fl_extraType">Extra</option>`
                );
            }
            let $errorMsg = div.find(".fl_ConfirmTypeAlert");
            $errorMsg.empty();
            $errorMsg.hide();

            if (!hasType) {
                $select.find(".fl_extraType").attr("selected", true);
                if (attachmentTypeName === null) {
                    $errorMsg.html(
                        `Attachment type not specified. If added, will appear as 'Extra'.`
                    );
                } else {
                    $errorMsg.html(
                        `Attachment type '${attachmentTypeName}' is not available for this application type. If added, will appear as 'Extra'.`
                    );
                }
                $errorMsg.show();
            }
            $(".match_attachmentType").empty().append(div);
        } else {
            alert("Error getting attachment types");
        }
    });
}
/*
{
    "success": true,
    "status": "success",
    "data": [
        {
            "attachmentTypeName": "Condition Report",
            "attachmentTypeId": "6361f852-4b9e-4fc6-951d-0b35d69c6767",
            "needsAttachment": true,
            "success": true
        },
        {
            "attachmentTypeName": "Contract",
            "attachmentTypeId": "a78cab07-6ce9-4a09-86c9-154f6db47a15",
            "needsAttachment": true,
            "success": true
        }
    ]
}
*/
function fileLibrary_show_Attachment(attachmentId, filename) {
    var $panel = $("#docViewPanel");
    const currentAttachmentId = $panel.data("attachmentid");

    if (!attachmentId) {
        // no attachment, hide
        $panel.hide();
        $panel.data("attachmentid", "");
        $panel.empty().html("");
        return;
    } else if (currentAttachmentId && currentAttachmentId === attachmentId) {
        // no change, ignore
    } else {
        let fext = "";
        if (filename) {
            fext = `/${encodeURIComponent(filename)}`;
        }
        var pdfurl = `/FileLibrary/ViewAttachment/${attachmentId}${fext}`;
        var html = `<iframe id="pdfFrame" src="${pdfurl}#view=FitH" />`;
        $panel.data("attachmentid", attachmentId);
        $panel.empty().html(html);
        $panel.show();
    }
}
function fileLibrary_hide_MatchListPanel() {
    $("#matchListPanel").hide();
}
function fileLibrary_show_MatchListPanel() {
    $("#matchListPanel").show();
}
function fileLibrary_selection_updated(items) {
    if (items.length === 1) {
        var attid = items[0].attachmentId;
        var filename = items[0].filename;
        fileLibrary_upload_selected();
        fileLibrary_show_Attachment(attid, filename);
    } else {
        fileLibrary_show_Attachment();
        fileLibrary_upload_deselected();
    }
}
function getSelectedMatch() {
    var t = $("#DT_FileLibraryMatch").DataTable();
    return getSelectedItemValues(t, "requestId");
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
function fileLibrary_show_SelectAttachmentType() {}
function fileLibrary_setAttachmentType(typeName) {
    var match_DT = $("#DT_FileLibraryMatch").DataTable();
    match_DT.buttons(".fl_typeSelector").text(`Type: ${typeName}`);
}
function fileLibrary_show_AttachToRequest() {
    var attachment_DT = $("#DT_FileLibrary").DataTable();
    var match_DT = $("#DT_FileLibraryMatch").DataTable();
    var attachmentIDs = getSelectedItemValues(attachment_DT, "attachmentId");
    var requestIDs = getSelectedItemValues(match_DT, "requestId");
    if (requestIDs.length === 0) {
        alert("Please select a request");
    } else if (requestIDs.length > 1) {
        alert("Attachment can only be attached to one request");
    } else if (attachmentIDs.length > 0) {
        var $form = $("#modal-AttachToRequest");
        var $errorMsg = $form.find("#AttachToRequest_Error");
        $errorMsg.text("");
        $errorMsg.hide();
        var vin = getSelectedItemValues(match_DT, "vin")[0];
        var reqno = getSelectedItemValues(match_DT, "requestNo")[0];

        var filenames = getSelectedItemValues(attachment_DT, "filename").join(
            "<br/>"
        );
        var msg = `Associate selected upload(s) to R# ${reqno}, VIN ${vin}<br/><br/>${filenames}`;
        $form.find("#selectedCount").html(msg);
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var formData = new FormData();
                $.each(attachmentIDs, function () {
                    formData.append("attachmentIds", this);
                });
                formData.append("requestId", requestIDs[0]);
                let attachmentTypeId = $(".match_attachmentType")
                    .find("#fl_requestedTypes")
                    .find(":selected")
                    .val();
                formData.append("attachmentTypeId", attachmentTypeId);

                $.ajax({
                    url: "/FileLibrary/AttachToRequest",
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
                }).done(function (data) {
                    if (data.success) {
                        $("#DT_FileLibrary").DataTable().ajax.reload();
                        $form.modal("hide");
                        fileLibrary_show_Attachment();
                    } else {
                        $errorMsg.show();
                    }
                });
            });
        $form.modal("show");
    } else {
        alert("No item selected");
    }
}

function fileLibrary_show_DeleteAttachments() {
    var t = $("#DT_FileLibrary").DataTable();
    var uploadIDs = getSelectedItemValues(t, "attachmentId");
    if (uploadIDs.length > 0) {
        var $form = $("#modal-DeleteAttachment");
        $form
            .find("#selectedCount")
            .html(
                `Selected ${uploadIDs.length} attachment(s) will be deleted.`
            );
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var formData = new FormData();
                $.each(uploadIDs, function () {
                    formData.append("attachmentIds", this);
                });
                $.ajax({
                    url: "/FileLibrary/DeleteAttachments",
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
                }).done(function (data) {
                    $("#DT_FileLibrary").DataTable().ajax.reload();
                    $form.modal("hide");
                });
            });
        $form.modal("show");
    } else {
        alert("No item selected");
    }
}
var vinLookupUrl = "/Vin/Lookup/";
//?format=json';
function getVinDetail(vin, $ctl, $form) {
    try {
        vin = vin.trim().toUpperCase();
        $ctl.removeClass("badvinflag badvinchecksum");
        $ctl.addClass("vinlookupflag");
        if (vin.length != 17) {
            $ctl.removeClass("vinlookupflag");
            if (vin.length != 0) {
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
                succeeded = true;
                $ctl.removeClass("badvinchecksum vinlookupflag");
                setVinFields(data, $ctl, $form);
            },
            error: function () {
                $ctl.removeClass("badvinchecksum vinlookupflag");
                succeeded = false;
                $ctl.addClass("badvinflag");
            },
        });
    } catch (e) {}
}
function getVINfromFilename(f) {
    if (f) {
        f = f.replace(/\.[^.]*$/, ""); // remove ext
        var regexp = /[A-HJ-NPR-Z\d]{8}[\dX][A-HJ-NPR-Z\d]{8}/gi;

        const matches_array = f.match(regexp);
        if (matches_array && matches_array.length > 0) return matches_array[0];
    }
    return "";
}
function getPartialVIN(f) {
    if (f) {
        f = f.replace(/\.[^.]*$/, ""); // remove ext
        const tag = f.match(/(.*)([A-HJ-NPR-Z\d]{6})/i);
        if (tag && tag.length > 2) {
            return tag[2];
        }
    }
    return "";
}
function isPartialVin(v) {
    if (v) {
        var regexp = /^[A-HJ-NPR-Z\d]{6}$/gi;
        return regexp.test(v);
    }
    return false;
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
    if (typeof v === undefined || v === null || v.length != 17) return false;

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
