"use strict";

function vendorLibrary_populate_AttachmentTypes(f, attachmentTypeId) {
    attachmentTypeId = attachmentTypeId || "";

    $.ajax({
        url: "/VendorLibrary/AttachmentTypes",
        type: "GET",
        processData: false,
        contentType: false,
    }).done(function (data) {
        var $select = f.find("#vl_attachmentTypeId");
        $select.empty();
        $select.append(
            $("<option>", {
                value: "",
                text: "Select Attachment Type",
            })
        );
        $.each(data.data, function (idx, item) {
            let $option = $select.append(
                $("<option>", {
                    value: item.attachmentTypeId,
                    text: item.name,
                })
            );
        });
        if (attachmentTypeId !== "") {
            $select.val(attachmentTypeId);
        }
    });
}
function vendorLibrary_show_Upload() {
    let $modalForm = $("#modal-vlUpload");
    let $attForm = $modalForm.find("#vl_uploadDocument");

    vendorLibrary_populate_AttachmentTypes($attForm);

    $attForm.find("#vl_description").val("");
    $attForm.find("#vl_displayName").val("");
    $attForm.find("#vl_attachmentTypeId").val("");

    $attForm.off("submit").on("submit", function (e) {
        e.preventDefault();

        var formData = new FormData();
        var description = $attForm.find("#vl_description").val();
        var displayName = $attForm.find("#vl_displayName").val();
        var attachmentTypeId = $attForm.find("#vl_attachmentTypeId").val();

        formData.append("displayName", displayName);
        formData.append("description", description);
        formData.append("attachmentTypeId", attachmentTypeId);

        var $file = $attForm.find("input[type='file']")[0];
        if ($file.length === 0) {
            alert("Please select a file for upload");
            return;
        }
        formData.append("file", $file.files[0]);

        $.ajax({
            url: $attForm.attr("action"),
            type: "POST",
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
            processData: false,
            contentType: false,
            data: formData,
        }).done(function (data) {
            var $input = $attForm.find("input[type='file']");
            $input.replaceWith($input.val("").clone(true));

            $attForm.find("#vl_description").val("");
            $attForm.find("#vl_displayName").val("");

            $modalForm.modal("hide");
            $("#DT_VendorLibrary").DataTable().ajax.reload();
        });
        return false;
    });
    $modalForm
        .find("#okButton")
        .off("click")
        .on("click", function (e) {
            e.preventDefault();
            $attForm.trigger("submit");
        });
    //$modalForm.find(".close").off('click').on('click', function (e) {
    //    e.preventDefault();
    //    $modalForm = $("#modal-vlUpload");
    //    $modalForm.hide();
    //});
    $modalForm.modal("show");
}
function vendorLibrary_show_Edit(t) {
    let $modalForm = $("#modal-vlEdit");
    let $attForm = $modalForm.find("#vl_editDocument");

    var items = getSelectedItemObjects(t);
    if (items.length !== 1) {
        alert("Can only edit a single document at a time");
        return;
    }
    var item = items[0];
    $attForm.find("#vl_attachmentId").val(item.vendorAttachmentId);
    $attForm.find("#vl_description").val(item.description);
    $attForm.find("#vl_displayName").val(item.displayName);
    $attForm.find("#vl_attachmentTypeId").val(item.attachmentTypeId);

    // query for latest values
    $.ajax({
        url: `/VendorLibrary/PreEdit/${item.vendorAttachmentId}`,
        type: "GET",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        processData: false,
        contentType: false,
        data: null,
    }).done(function (data) {
        if (data.success) {
            let d = data.data;
            $attForm.find("#vl_description").val(d.description);
            $attForm.find("#vl_displayName").val(d.displayName);
            //$attForm.find("#vl_attachmentTypeId").val(d.attachmentTypeId);
            vendorLibrary_populate_AttachmentTypes(
                $attForm,
                d.attachmentTypeId
            );
        }
    });

    $attForm.off("submit").on("submit", function (e) {
        e.preventDefault();

        var formData = new FormData();
        var description = $attForm.find("#vl_description").val();
        var displayName = $attForm.find("#vl_displayName").val();
        var attachmentId = $attForm.find("#vl_attachmentId").val();
        var attachmentTypeId = $attForm.find("#vl_attachmentTypeId").val();

        formData.append("displayName", displayName);
        formData.append("description", description);
        formData.append("attachmentId", attachmentId);
        formData.append("attachmentTypeId", attachmentTypeId);

        var $file = $attForm.find("input[type='file']")[0];
        if ($file.files.length > 0) {
            formData.append("file", $file.files[0]);
        }

        $.ajax({
            url: $attForm.attr("action"),
            type: "POST",
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
            processData: false,
            contentType: false,
            data: formData,
        }).done(function (data) {
            var $input = $attForm.find("input[type='file']");
            $input.replaceWith($input.val("").clone(true));

            $attForm.find("#vl_description").val("");
            $attForm.find("#vl_displayName").val("");

            $modalForm.modal("hide");
            $("#DT_VendorLibrary").DataTable().ajax.reload();
        });
        return false;
    });
    $modalForm
        .find("#okButton")
        .off("click")
        .on("click", function (e) {
            e.preventDefault();
            $attForm.trigger("submit");
        });
    //$modalForm.find(".close").off('click').on('click', function (e) {
    //    e.preventDefault();
    //    $modalForm = $("#modal-vlUpload");
    //    $modalForm.hide();
    //});
    $modalForm.modal("show");
}

function vendorLibrary_show_Delete() {
    var t = $("#DT_VendorLibrary").DataTable();

    var items = getSelectedItemObjects(t);
    if (items.length !== 1) {
        alert("Can only delete a single document at a time");
        return;
    }
    var $form = $("#modal-vlDelete");
    $form
        .find("#selectedCount")
        .html(`Selected ${items.length} document will be deleted.`);
    $form.find("#vl_displayName").val(items[0].displayName);
    $form.find("#vl_description").val(items[0].description);
    $form.find("#vl_fileName").val(items[0].fileName);
    $form.find("#vl_attachmentId").val(items[0].vendorAttachmentId);

    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var formData = new FormData();
            formData.append("attachmentId", items[0].vendorAttachmentId);

            $.ajax({
                url: "/vendorLibrary/Delete",
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
                $form.modal("hide");
                $("#DT_VendorLibrary").DataTable().ajax.reload();
            });
        });
    $form.modal("show");
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
function vendorLibrary_show_Attachment(attachmentId, filename) {
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
        var pdfurl = `/VendorLibrary/View/${attachmentId}${fext}`;
        var html = `<iframe id="pdfFrame" src="${pdfurl}#view=FitH" />`;
        $panel.data("attachmentid", attachmentId);
        $panel.empty().html(html);
        $panel.show();
    }
}
function vendorLibrary_selection_updated(items) {
    if (items.length === 1) {
        var attid = items[0].vendorAttachmentId;
        var filename = items[0].fileName;
        vendorLibrary_show_Attachment(attid, filename);
    } else {
        vendorLibrary_show_Attachment();
    }
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
function vendorLibrary_show_SelectAttachmentType() {}
function vendorLibrary_setAttachmentType(typeName) {}

var vinLookupUrl = "/Vin/Lookup/";
//?format=json';
function getVinDetail(vin, $ctl, $form) {
    try {
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
