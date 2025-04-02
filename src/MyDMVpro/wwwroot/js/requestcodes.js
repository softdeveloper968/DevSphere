"use strict";

var taggedFields = [];
var requestTagListSuggestionLimit = 10;
var requestFieldListSuggestionLimit = 10;

function requestcodes_updateFormTags($panel) {
    taggedFields = taggedFields || [];
    $panel = $panel || $("#detailsEditPanel");

    if ($panel.length === 0) {
        console.log("panel not found");
        return;
    }
    var $fields = $panel.find("[data-fieldid]");

    if ($fields.length === 0) {
        console.log("no fields found");
        return;
    }

    $fields.removeClass("taggedField");

    if (taggedFields.length === 0) {
        console.log("no tagged fields");
        return;
    }
    $.each($fields, function (index, field) {
        let fid = $(field).data("fieldid").toLowerCase();
        if (taggedFields.indexOf(fid) >= 0) {
            $(field).addClass("taggedField");
        }
    });
}

function requestcodes_addFields(e, settings, json, xhr) {
    if (json === null) return;
    if (json.data) {
        taggedFields = [];

        json.data.forEach((tag) => {
            if (tag.cleared) return;
            tag.fields.forEach((field) => {
                taggedFields.push(field.fieldId.toLowerCase());
            });
        });
        requestcodes_updateFormTags();
    }
}

function vendor_show_RequestCode_Create(t) {
    var $form = $("#modal-RequestCode-New");
    vendor_init_dialog($form);

    let pnl = $("#requestCodesPanel");
    let requestId = pnl.data("reqid");
    let reqno = pnl.data("reqno");
    let vin = pnl.data("vin");

    vendor_set_single_vin_rno_header($form, "Add Code", reqno, vin);

    $form.find("#RequestCode_New_Code").val("");
    $form.find("#RequestCode_New_Note").val("");
    $form.find("#RequestCode_New_Resolution").val("");
    $form.find("#RequestCode_New_Fields").val("");

    initRequestCodeTags($form);
    initRequestCodeFields($form, requestId);

    let ctl = $form.find("#RequestCode_New_Code");
    ctl.tagsinput("removeAll");

    let ctlFields = $form.find("#RequestCode_New_Fields");
    ctlFields.tagsinput("removeAll");

    $form.find(".bootstrap-tagsinput").addClass("form-control");
    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var refreshtables = [t];
            vendor_action_JSON_POST(
                $form,
                "/RequestCodes/Add",
                refreshtables,
                RequestCodeCreateModel($form),
                undefined,
                function () {
                    $form.modal("hide");
                    requestLookup_vendorShowNotes(requestId);
                }
            );
        });
    $form.modal("show");
}
function vendor_show_RequestCode_Clear(t) {
    var items = vendorGetSelectedRequestCodeItems(t);

    if (items.length !== 1) {
        return;
    }
    var item = items[0];

    var $form = $("#modal-RequestCode-Clear");
    vendor_init_dialog($form);

    let pnl = $("#requestCodesPanel");
    let requestId = pnl.data("reqid");
    let reqno = pnl.data("reqno");
    let vin = pnl.data("vin");

    vendor_set_single_vin_rno_header($form, "Clear Request Code", reqno, vin);

    vendor_action_JSON_GET(
        [t],
        `/RequestCodes/Edit/${item.RequestCodeId}`,
        null,
        vendor_clear_RequestCode
    );
}
function vendor_clear_RequestCode(refreshtables, data) {
    if (data.length === 0) {
        console.log(`ERROR: no data returned`);
        return;
    }
    let requestcode = data[0];

    var $form = $("#modal-RequestCode-Clear");

    vendor_init_common_RequestCode_fields($form, requestcode);
    if (requestcode.cleared) {
        let cleareddt = moment
            .utc(requestcode.clearedDate)
            .local()
            .format("M-D-YY h:mm A");
        let html = `<div>Cleared ${htmlEncode(cleareddt)} by ${requestcode.clearedByUserName}</div><div><b>${htmlEncode(requestcode.clearedNote)}</b></div>`;
        $form.find("#RequestCode_Edit_ClearedInfo").html(html);
        $form
            .find("#RequestCode_Edit_ClearedNote")
            .val(requestcode.clearedNote);
        $form.find("#RequestCode_Edit_ClearedNote").prop("readonly", true);
        $form.find("#RequestCode_Edit_OverrideClear").prop("readonly", true);
        $form.find("#RequestCode_Edit_OverrideClear_DIV").hide();
        $form.find("#okButton").hide();
    } else {
        $form.find("#RequestCode_Edit_ClearedNote").val("");
        $form.find("#RequestCode_Edit_ClearedNote").prop("readonly", false);
        $form.find("#RequestCode_Edit_OverrideClear").prop("readonly", false);
        $form.find("#RequestCode_Edit_OverrideClear").prop("checked", false);
        $form.find("#RequestCode_Edit_OverrideClear_DIV").show();
        $form.find("#okButton").show();
    }

    $form.find("#RequestCode_Edit_ResolutionInfo").val(requestcode.resolution);
    $form.find("#RequestCode_Edit_Fields").prop("readonly", true);
    $form.find("#RequestCode_Edit_Fields").prop("disabled", true);
    $form.find("#RequestCode_Edit_Resolution").prop("readonly", true);
    $form.find("#RequestCode_Edit_Note").prop("readonly", true);

    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            if (requestcode.cleared) {
                vendor_show_alert(
                    $form,
                    "Request Code already cleared",
                    "warning"
                );
            } else {
                vendor_action_JSON_POST(
                    $form,
                    "/RequestCodes/Clear",
                    refreshtables,
                    RequestCodeClearModel($form, requestcode),
                    undefined,
                    function () {
                        $form.modal("hide");
                        requestLookup_vendorShowNotes(requestcode.requestId);
                    }
                );
            }
        });

    $form.modal("show");
}
function vendor_init_common_RequestCode_fields($form, requestcode) {
    vendor_init_dialog($form);
    initRequestCodeTags($form);
    initRequestCodeFields($form, requestcode.requestId);

    let ctl = $form.find("#RequestCode_Edit_Code");
    ctl.tagsinput("removeAll");
    try {
        ctl.tagsinput("add", {
            id: requestcode.tagId,
            tag: requestcode.tagName,
        });
    } catch (e) {
        console.log(e);
    }
    $form.find("#RequestCode_Edit_Code").prop("readonly", true);
    $form.find("#RequestCode_Edit_Code").prop("disabled", true);

    let fieldsCtl = $form.find("#RequestCode_Edit_Fields");
    fieldsCtl.tagsinput("removeAll");
    $.each(requestcode.fields, function (index, field) {
        try {
            fieldsCtl.tagsinput("add", {
                id: field.fieldId,
                tag: field.label,
                desc: field.excelName,
            });
        } catch (e) {
            console.log(e);
        }
    });

    $form.find("#RequestCode_Edit_Note").val(requestcode.note);
    $form.find("#RequestCode_Edit_Resolution").val(requestcode.resolution);

    if (requestcode.cleared) {
        let cleareddt = moment
            .utc(requestcode.clearedDate)
            .local()
            .format("M-D-YY h:mm A");
        let html = `<div>Cleared ${htmlEncode(cleareddt)} by ${requestcode.clearedByUserName}</div><div><b>${htmlEncode(requestcode.clearedNote)}</b></div>`;

        $form.find("#RequestCode_Edit_ClearedInfo").html(html);
        $form.find("#RequestCode_Edit_Fields").prop("readonly", true);
        $form.find("#RequestCode_Edit_Fields").prop("disabled", true);
        $form.find("#RequestCode_Edit_Resolution").prop("readonly", true);
        $form.find("#RequestCode_Edit_Note").prop("readonly", true);
        $form.find("#okButton").hide();
    } else {
        $form.find("#RequestCode_Edit_ClearedInfo").empty();
        $form.find("#RequestCode_Edit_Fields").prop("readonly", false);
        $form.find("#RequestCode_Edit_Fields").prop("disabled", false);
        $form.find("#RequestCode_Edit_Resolution").prop("readonly", false);
        $form.find("#RequestCode_Edit_Note").prop("readonly", false);
        $form.find("#okButton").show();
    }
    $form.find(".bootstrap-tagsinput").addClass("form-control");

    if (requestcode.LastModified) {
        let moddt = moment
            .utc(requestcode.LastModified)
            .local()
            .format("M-D-YY h:mm A");
        let modby = `Last modified ${moddt} by ${requestcode.LastModifiedBy}`;
        $form.find("#RequestCode_Edit_LastModified").text(modby);
    } else if (requestcode.CreatedDate) {
        let moddt = moment
            .utc(requestcode.CreatedDate)
            .local()
            .format("M-D-YY h:mm A");
        let modby = `Created ${moddt} by ${requestcode.CreatedBy}`;
        $form.find("#RequestCode_Edit_LastModified").text(modby);
    }
}

function vendor_show_RequestCode_Edit(t) {
    var items = vendorGetSelectedRequestCodeItems(t);

    if (items.length != 1) {
        return;
    }
    var item = items[0];

    var $form = $("#modal-RequestCode-Edit");
    vendor_init_dialog($form);

    let pnl = $("#requestCodesPanel");
    let requestId = pnl.data("reqid");
    let reqno = pnl.data("reqno");
    let vin = pnl.data("vin");

    vendor_set_single_vin_rno_header($form, "Edit Request Code", reqno, vin);

    vendor_action_JSON_GET(
        [t],
        `/RequestCodes/Edit/${item.RequestCodeId}`,
        null,
        vendor_edit_RequestCode
    );
}
function vendor_edit_RequestCode(refreshtables, data) {
    if (data.length === 0) {
        console.log(`ERROR: no data returned`);
        return;
    }
    let requestcode = data[0];

    var $form = $("#modal-RequestCode-Edit");
    vendor_init_common_RequestCode_fields($form, requestcode);
    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            vendor_action_JSON_POST(
                $form,
                "/RequestCodes/Edit",
                refreshtables,
                RequestCodeEditModel($form, requestcode),
                undefined,
                function () {
                    $form.modal("hide");
                    requestLookup_vendorShowNotes(requestcode.requestId);
                }
            );
        });

    $form.modal("show");
}
function RequestCodeCreateModel($form) {
    let pnl = $("#requestCodesPanel");
    let requestId = pnl.data("reqid");
    let reqno = pnl.data("reqno");
    let vin = pnl.data("vin");

    let note = $form.find("#RequestCode_New_Note").val();
    let resolution = $form.find("#RequestCode_New_Resolution").val();

    let codeCtl = $form.find("#RequestCode_New_Code");

    let tagItems = codeCtl.tagsinput("items");
    let tagId = 0;
    if (tagItems.length > 0) {
        tagId = tagItems[0].id;
    }

    let fieldsCtl = $form.find("#RequestCode_New_Fields");
    let fieldIds = fieldsCtl.val();

    var model = {
        RequestId: requestId,
        Note: note,
        Resolution: resolution,
        TagId: tagId,
        FieldIds: fieldIds,
    };
    return model;
}
function vendor_show_RequestCode_Delete(t) {}
function vendorGetSelectedRequestCodeItems(t) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push({
            RequestId: this["requestId"],
            RequestCodeId: this["requestCodeId"],
            RequestNo: this["requestNo"],
            Vin: this["vin"],
            Title: this["title"],
        });
    });
    return a;
}

function RequestCodeClearModel($form, requestcode) {
    let note = $form.find("#RequestCode_Edit_ClearedNote").val();
    let override = $form
        .find("#RequestCode_Edit_OverrideClear")
        .prop("checked");

    var model = {
        RequestId: requestcode.requestId,
        RequestCodeId: requestcode.requestCodeId,
        Cleared: true,
        ClearedNote: note,
        OverrideChecks: override,
    };
    return model;
}

function RequestCodeEditModel($form, requestcode) {
    let requestId = requestcode.requestId;

    let note = $form.find("#RequestCode_Edit_Note").val();
    let resolution = $form.find("#RequestCode_Edit_Resolution").val();

    let codeCtl = $form.find("#RequestCode_Edit_Code");
    let tagItems = codeCtl.tagsinput("items");
    let tagIds = [];
    tagItems.map(function (item) {
        tagIds.push(item.id);
    });
    let tagId = tagIds[0];

    let fieldsCtl = $form.find("#RequestCode_Edit_Fields");
    let fieldIds = fieldsCtl.val();

    var model = {
        RequestId: requestId,
        RequestCodeId: requestcode.requestCodeId,
        TagId: tagId,
        FieldIds: fieldIds,
        Note: note,
        Resolution: resolution,
    };
    return model;
}

var requestFieldsSource;
function initRequestCodeFields($form, requestId) {
    if (requestId === null) return;

    try {
        requestFieldsSource = new Bloodhound({
            name: "requestFields",
            identify: function (item) {
                return item.id;
            },
            sufficient: 1,
            datumTokenizer: Bloodhound.tokenizers.obj.whitespace("tag", "desc"),
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: `/RequestCodes/Fields/${requestId}`,
                cache: false,
            },
        });
        requestFieldsSource.initialize();

        let fieldSettings = {
            tagClass: function (item) {
                switch (item.class) {
                    case "primary":
                        return "label label-primary";
                    case "danger":
                        return "label label-danger label-important";
                    case "success":
                        return "label label-success";
                    case "default":
                        return "label label-default";
                    case "warning":
                        return "label label-warning";
                    default:
                        return "label label-primary";
                }
            },
            //confirmKeys: [13, 32, 44],
            maxTags: 10,
            itemValue: "id",
            itemText: "tag",
            freeInput: false,
            trimValue: true,
            typeaheadjs: [
                {
                    minLength: 0,
                    showHintOnFocus: true,
                },
                {
                    name: "requestFields",
                    displayKey: "tag",
                    limit: requestFieldListSuggestionLimit, // This controls the number of suggestions displayed
                    allowDuplicates: false,
                    source: function (q, sync) {
                        if (q === "") {
                            sync(requestFieldsSource.all());
                        } else {
                            requestFieldsSource.ttAdapter()(q, sync);
                        }
                    },
                },
            ],
        };

        $form.find("#RequestCode_New_Fields").tagsinput(fieldSettings);
        $form.find("#RequestCode_Edit_Fields").tagsinput(fieldSettings);
    } catch (e) {
        console.log(e);
    }
}

var requestTagsSource;
function initRequestCodeTags($form) {
    try {
        requestTagsSource = new Bloodhound({
            name: "requestTags",
            identify: function (item) {
                return item.id;
            },
            sufficient: 1,
            datumTokenizer: Bloodhound.tokenizers.obj.whitespace("tag", "desc"),
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: { url: "/Tags/RequestCodeTags", cache: false },
        });
        requestTagsSource.initialize();

        let tagSettings = {
            tagClass: function (item) {
                switch (item.class) {
                    case "primary":
                        return "label label-primary";
                    case "danger":
                        return "label label-danger label-important";
                    case "success":
                        return "label label-success";
                    case "default":
                        return "label label-default";
                    case "warning":
                        return "label label-warning";
                    default:
                        return "label label-primary";
                }
            },

            //confirmKeys: [13, 32, 44],
            maxTags: 5,
            itemValue: "id",
            itemText: "tag",
            freeInput: false,
            trimValue: true,
            typeaheadjs: [
                {
                    minLength: 0,
                    showHintOnFocus: true,
                    hint: true,
                    highlight: true,
                    showDefault: true,
                    //limit: requestTagListSuggestionLimit // This controls the number of suggestions displayed
                },
                {
                    name: "requestTags",
                    displayKey: "tag",
                    limit: requestTagListSuggestionLimit, // This controls the number of suggestions displayed
                    allowDuplicates: false,
                    source: function (q, sync) {
                        if (q === "") {
                            sync(requestTagsSource.all());
                        } else {
                            requestTagsSource.ttAdapter()(q, sync);
                        }
                    },
                },
            ],
        };

        $form.find("#RequestCode_New_Code").tagsinput(tagSettings);
        $form.find("#RequestCode_Edit_Code").tagsinput(tagSettings);
    } catch (e) {
        console.log(e);
    }
}
function requestCodes_LoadViewSettings(keyprefix, data) {
    data.expandNotes = GetDtSetting(keyprefix, "expandNotes", "false");
    data.includeCleared = GetDtSetting(keyprefix, "includeCleared", "false");
}
function requestCodes_SaveViewSettings(keyprefix, data) {
    SaveDtSetting(keyprefix, "expandNotes", data.expandNotes);
    SaveDtSetting(keyprefix, "includeCleared", data.includeCleared);
}
function requestCodes_ToggleIncludeCleared(t, id) {
    var val = GetDtSetting(id, "includeCleared", "false");
    val = val === "true" ? "false" : "true";
    SaveDtSetting(id, "includeCleared", val);
    t.ajax.reload();
    return val;
}
function requestCodes_GetIncludedClearedButtonText(keyprefix) {
    if (GetDtSetting(keyprefix, "includeCleared", "false") === "true") {
        return "Exclude Cleared";
    } else {
        return "Include Cleared";
    }
}
function datatable_rowCallback_requestcode(row, data) {
    let cleared = data.cleared || null;

    if (cleared) {
        $(row).addClass("requestCodeCleared");
    } else {
        $(row).removeClass("requestCodeCleared");
    }
}
