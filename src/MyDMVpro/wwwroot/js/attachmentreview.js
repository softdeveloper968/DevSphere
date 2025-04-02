/* attachment review */
function attachmentReview_attachment_selected() {
    attachmentReview_show_FormValues();
}
function attachmentReview_attachment_deselected() {
    attachmentReview_show_FormValues();
}
function attachmentReview_show_FormValues() {
    var $attachmentTable = $("#DT_AttachmentReview");
    var attachment_DT = $attachmentTable.DataTable();
    var items = getSelectedItemObjects(attachment_DT);

    if (items.length === 0) {
        attachmentReview_show_Attachment();
    } else {
        if (items.length === 1) {
            var item = items[0];
            var attachmentId = item["attachmentId"];
            var attachmentTypeId = item["attachmentTypeId"];
            var attachmentTypeName = item["attachmentTypeName"];
            var requestId = item["requestId"];
            var filename = item["filename"];

            attachmentReview_show_Attachment(attachmentId, filename);
            // Show attachment selection panel
            if (attachmentTypeId) {
                attachmentReview_load_ReviewFields(
                    requestId,
                    attachmentId,
                    attachmentTypeId,
                    attachmentTypeName
                );
                return;
            }
        }
    }
    $("#reviewPanel").find(".panel-body").html("");
    $("#reviewPanel").data("attachmentid", "");
    $("#reviewPanel").hide();
    let $types = $("#reviewPanel_changeType").siblings("ul");
    $types.empty();
}
function attachmentReview_load_ReviewFields(
    requestId,
    attachmentId,
    attachmentTypeId,
    attachmentTypeName
) {
    $.ajax({
        url: `/AttachmentReview/Review/${requestId}/${attachmentTypeId}`,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        processData: false,
    }).done(function (data) {
        $("#reviewPanel_type").text(attachmentTypeName);
        $("#reviewPanel").show();

        $("#reviewPanel").find(".panel-body").html(data);
    });
}
function attachmentReview_updateDocViewInfo() {
    var $info = $("#docViewInfo");
    var $attachmentTable = $("#DT_AttachmentReview");
    var attachment_DT = $attachmentTable.DataTable();
    var items = getSelectedItemObjects(attachment_DT);

    if (items.length === 1) {
        var item = items[0];
        var appType = item["appType"];
        var appState = item["state"];
        var vin = item["vin"];
        var attachmentType = item["attachmentTypeName"];

        $info.show();
        $info.find("#docViewAppType").text(appType);
        $info.find("#docViewAppState").text(appState);
        $info.find("#docViewVin").text(vin);
        $info.find("#docViewAttachmentType").text(attachmentType);
    } else {
        $info.hide();
        $info.find("#docViewAppType").text("");
        $info.find("#docViewAppState").text("");
        $info.find("#docViewVin").text("");
        $info.find("#docViewAttachmentType").text("");
    }
}
function attachmentReview_show_Attachment(attachmentId, filename) {
    $("#reviewPanel").data("attachmentid", attachmentId || "");

    var $panel = $("#docViewPanel");
    const currentAttachmentId = $panel.data("attachmentid");

    if (!attachmentId) {
        // no attachment, hide
        $panel.hide();
        $panel.data("attachmentid", "");
        $panel.empty().html("");

        attachmentReview_updateDocViewInfo();

        return;
    } else if (currentAttachmentId && currentAttachmentId === attachmentId) {
        // no change, ignore
    } else {
        let fext = "";
        if (filename) {
            //fext = `/${encodeURIComponent(filename)}`;
        }
        attachmentReview_updateDocViewInfo();
        var pdfurl = `/AttachmentReview/View/${attachmentId}${fext}`;
        var html = `<iframe id="pdfFrame" src="${pdfurl}#view=FitH" />`;
        $panel.data("attachmentid", attachmentId);
        $panel.empty().html(html);
        $panel.show();
    }
}
function attachmentReview_selection_updated(items) {
    if (items.length === 1) {
        attachmentReview_attachment_selected();
        //attachmentReview_show_Attachment(attid, filename);
    } else {
        //attachmentReview_show_Attachment();
        attachmentReview_attachment_deselected();
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
function attachmentReview_show_Approve(t) {
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
    var attachmentId = items[0].attachmentId;
    var attachmentTypeId = items[0].attachmentTypeId;
    var attachmentTypeName = items[0].attachmentTypeName;

    var vin = items[0].vin;
    var appType = items[0].appType;
    let $form = $("#reviewPanel");

    // check if message needs to be shown with additional information
    $.ajax({
        url: `/AttachmentReview/ApproveCheck/${attachmentId}/${attachmentTypeId}`,
        async: false,
        type: "GET",
        dataType: "json",
        cache: false,
        processData: false,
    }).done(function (obj) {
        let reviewMessage = obj.data.html || "";
        let isExtraType = attachmentTypeName === "Extra";
        let $modal = isExtraType
            ? $("#modal-Confirm-Extra")
            : $("#modal-Confirm");

        var approveUrl = `/AttachmentReview/Approve/${attachmentId}/${attachmentTypeId}`;

        $modal.find("#confirm-vin").text(vin);

        if (reviewMessage === "" && !isExtraType) {
            vendor_action_HELPER(
                $form,
                items,
                approveUrl,
                [],
                [],
                function (succeeded) {
                    $form.hide();
                    let $panel = $("#docViewPanel");
                    $panel.hide();
                    attachmentReview_RefreshListAndSelectNext(t);
                }
            );
        } else {
            $modal.find("#reviewMessage").html(reviewMessage);
            $modal
                .find("#okButton")
                .off("click")
                .on("click", function () {
                    vendor_action_HELPER(
                        $form,
                        items,
                        approveUrl,
                        [],
                        [],
                        function (succeeded) {
                            if (succeeded) {
                                $form.hide();
                                let $panel = $("#docViewPanel");
                                $panel.hide();
                                attachmentReview_RefreshListAndSelectNext(t);
                                $modal.modal("hide");
                            } else {
                                vendor_show_alert($modal, "Error approving");
                            }
                        }
                    );
                });
            $modal.modal("show");
        }
    });
}
function attachmentReview_show_Reject(t) {
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
    var attachmentId = items[0].attachmentId;
    var attachmentTypeId = items[0].attachmentTypeId;
    var vin = items[0].vin;
    var appType = items[0].appType;

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

    var $form = $("#modal-rejectAttachment");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    $form.find("#requestId").val(requestId);
    $form.find("#attachmentId").val(attachmentId);
    $form.find("#attachmentTypeId").val(attachmentTypeId);

    // Get stages for app type
    var stages = [];
    initCodeNoteRemarkChat($form);
    //$form.find("#predefinedReasons").val('');
    //$form.find("#predefinedReasonsDiv").hide();

    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    $form.find("#okButton").off("click");
    $validationForm = $form.find("form");

    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data)) return;

                let requestId = $form.find("#requestId").val();
                let attachmentId = $form.find("#attachmentId").val();
                let attachmentTypeId = $form.find("#attachmentTypeId").val();

                var values = [
                    { name: "attachmentId", value: attachmentId },
                    { name: "attachmentTypeId", value: attachmentTypeId },
                    { name: "code", value: data.code },
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                ];

                vendor_action_HELPER(
                    $form,
                    items,
                    "/AttachmentReview/Reject",
                    [],
                    values,
                    function (succeeded) {
                        $form.modal("hide");
                        let $panel = $("#docViewPanel");
                        $panel.hide();
                        attachmentReview_RefreshListAndSelectNext(t);
                    }
                );
            });
    }
    initChatForm($form.find(".chatsEditPanel"), requestId, vin);
    $form.modal("show");
}
function attachmentReview_RefreshListAndSelectNext(t) {
    var nextRequestIds = vendorGetNextItemToSelect(t);
    if (nextRequestIds.length > 0) {
        let requestId = nextRequestIds[0];
        t.ajax.reload(function (data) {
            t.rows().deselect();
            t.rows(0).select();
            //    t.rows().filter(function () {
            //        let d = this.data()[0];
            //        let f = d.requestId === requestId;
            //        console.log("filtering", f);
            //        return f;
            //    }).select();
        }, false);
    }
}
function vendorGetNextItemToSelect(t) {
    let a = [];
    let found = false;
    t.rows().every(function () {
        if (this.selected()) {
            found = true;
        } else {
            let requestId = this.data().requestId;
            if (found) {
                a.push(requestId);
                found = false;
            }
        }
    });
    return a;
}
function attachmentReview_populateChangeTypeList(ctl) {
    let attachmentId = $("#reviewPanel").data("attachmentid");
    let $types = ctl.siblings("ul");
    $.ajax({
        url: `/Requests/ChangeTypes/${attachmentId}`,
        async: false,
        type: "GET",
        dataType: "text",
        cache: false,
        processData: false,
    }).done(function (data) {
        $types.html(data);
        $types.find("li").on("click", function () {
            let $li = $(this);
            let newTypeId = $li.data("attachmenttypeid");
            attachmentChangeType(attachmentId, newTypeId);
        });
    });
}
function attachmentChangeType(attachmentId, newAttachmentTypeId) {
    if (attachmentId && newAttachmentTypeId) {
        var formData = new FormData();
        formData.append("attachmentId", attachmentId);
        formData.append("attachmentTypeId", newAttachmentTypeId);

        $.ajax({
            url: "/Requests/ChangeType",
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
            type: "POST",
            async: true,
            cache: false,
            contentType: false,
            processData: false,
            data: formData,
            dataType: "json",
        }).done(function (data) {
            //console.log("Attachment type changed");
            if (data.success) {
                //attachmentReview_show_FormValues(attachmentId);
                attachmentReview_refreshList(attachmentId);
            }
        });
    } else {
        console.error("Invalid attachmentId or newAttachmentTypeId");
    }
}
function attachmentReview_refreshList(attachmentId) {
    var $attachmentTable = $("#DT_AttachmentReview");
    var attachment_DT = $attachmentTable.DataTable();
    attachment_DT.ajax.reload(function () {
        // reselect this attachment
        //attachment_DT.rows()
    }, false);
}

/* data review */
function dataReview_selection_updated(items) {
    if (items.length === 1) {
        dataReview_show_FormValues();
    } else {
        $("#reviewPanel").find(".panel-body").html("");
        $("#reviewPanel").hide();

        $("#dataReviewWidgetPanel").find(".panel-body").html("");
        $("#dataReviewWidgetPanel").hide();
    }
}
function dataReview_show_FormValues() {
    var $attachmentTable = $("#DT_DataReview");
    var attachment_DT = $attachmentTable.DataTable();
    var items = getSelectedItemObjects(attachment_DT);

    if (items.length === 0) {
        $("#reviewPanel").find(".panel-body").html("");
        $("#reviewPanel").data("attachmentid", "");
        $("#reviewPanel").hide();
        let $types = $("#reviewPanel_changeType").siblings("ul");
        $types.empty();
    } else {
        var item = items[0];
        var requestId = item["requestId"];
        var requestCodeId = item["requestCodeId"];
        var fieldId = item["fieldId"];

        if (requestId) {
            dataReview_load_ReviewFields(requestId, fieldId, requestCodeId);
            return;
        }
    }
    $("#reviewPanel").find(".panel-body").html("");
    $("#reviewPanel").data("attachmentid", "");
    $("#reviewPanel").hide();
    let $types = $("#reviewPanel_changeType").siblings("ul");
    $types.empty();
}
function dataReview_load_ReviewFields(requestId, fieldId, requestCodeId) {
    $.ajax({
        url: `/DataReview/Review/${requestCodeId}/${fieldId}`,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        processData: false,
    }).done(function (data) {
        $("#reviewPanel").find(".panel-body").html(data);
        $("#reviewPanel").show();
    });
}
function dataReview_show_Approve(t) {
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
    var fieldId = items[0].fieldId;
    var proposedUpdateId = items[0].proposedUpdateId;

    var vin = items[0].vin;
    var appType = items[0].appType;
    let $form = $("#reviewPanel");

    // check if message needs to be shown with additional information
    $.ajax({
        url: `/DataReview/ApproveCheck/${proposedUpdateId}/${requestId}`,
        async: false,
        type: "GET",
        dataType: "json",
        cache: false,
        processData: false,
    }).done(function (obj) {
        let reviewMessage = obj.data.html || "";
        let $modal = $("#modal-Confirm");

        var approveUrl = `/DataReview/Approve/${proposedUpdateId}/${fieldId}`;

        $modal.find("#confirm-vin").text(vin);

        if (reviewMessage === "") {
            vendor_action_HELPER(
                $form,
                items,
                approveUrl,
                [],
                [],
                function (succeeded) {
                    $form.hide();
                    let $panel = $("#docViewPanel");
                    $panel.hide();
                    dataReview_RefreshListAndSelectNext(t);
                }
            );
        } else {
            $modal.find("#reviewMessage").html(reviewMessage);
            $modal
                .find("#okButton")
                .off("click")
                .on("click", function () {
                    vendor_action_HELPER(
                        $form,
                        items,
                        approveUrl,
                        [],
                        [],
                        function (succeeded) {
                            if (succeeded) {
                                $form.hide();
                                let $panel = $("#docViewPanel");
                                $panel.hide();
                                dataReview_RefreshListAndSelectNext(t);
                                $modal.modal("hide");
                            } else {
                                vendor_show_alert($modal, "Error approving");
                            }
                        }
                    );
                });
            $modal.modal("show");
        }
    });
}
function dataReview_RefreshListAndSelectNext(t) {
    var nextRequestIds = vendorGetNextItemToSelect(t);
    if (nextRequestIds.length > 0) {
        let requestId = nextRequestIds[0];
        t.ajax.reload(function () {
            t.rows().deselect();
        }, false);
    } else {
        t.ajax.reload(function () {
            t.rows().deselect();
        }, false);
    }
}
function dataReview_show_Reject(t) {
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
    var proposedUpdateId = items[0].proposedUpdateId;
    var fieldId = items[0].fieldId;
    var vin = items[0].vin;
    var appType = items[0].appType;

    // check if all items are of same app type
    for (var idx = 1; idx < items.length; idx++) {
        if (items[idx].appType !== appType) {
            vendor_show_DataTable_alert(
                t,
                "Cannot move items of different app types"
            );
            return;
        }
    }

    var $form = $("#modal-rejectAttachment");
    vendor_init_dialog($form);
    initCodeNoteRemarkChat($form);

    $form.find("#requestId").val(requestId);
    $form.find("#fieldId").val(fieldId);
    $form.find("#proposedUpdateId").val(proposedUpdateId);

    // Get stages for app type
    var stages = [];
    initCodeNoteRemarkChat($form);

    $form.find("#selectedCount").html(html);
    $form.find("#okButton").prop("disabled", disabled);
    $form.find("#okButton").off("click");
    let $validationForm = $form.find("form");

    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var data = getCodeNoteRemarkChat($form);
                if (!validFormFields($form, data)) return;

                let requestId = $form.find("#requestId").val();
                let attachmentId = $form.find("#fieldId").val();
                let attachmentTypeId = $form.find("#proposedUpdateId").val();

                var values = [
                    { name: "proposedUpdateId", value: proposedUpdateId },
                    { name: "fieldId", value: fieldId },
                    { name: "code", value: data.code },
                    { name: "note", value: data.note },
                    { name: "remark", value: data.remark },
                    { name: "codeNotRequired", value: data.codeNotRequired },
                ];

                vendor_action_HELPER(
                    $form,
                    items,
                    "/DataReview/Reject",
                    [],
                    values,
                    function (succeeded) {
                        $form.modal("hide");
                        let $panel = $("#docViewPanel");
                        $panel.hide();
                        dataReview_RefreshListAndSelectNext(t);
                    }
                );
            });
    }
    initChatForm($form.find(".chatsEditPanel"), requestId, vin);
    $form.modal("show");
}
