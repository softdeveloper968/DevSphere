"use strict";

function vendor_edit_FollowUp(refreshtables, fu) {
    var $form = $("#modal-FollowUp-Edit");
    vendor_init_dialog($form);

    $form
        .find("#FollowUp_Edit_CreatedDate")
        .val(moment(fu.CreatedDate).format("YYYY-MM-DD"));

    var duedt = moment(fu.DueDate).format("YYYY-MM-DD");
    $form.find("#FollowUp_Edit_DueDate").val(duedt);
    $form.find("#FollowUp_Edit_StatusOfRecord").val(fu.StatusOfRecord);
    $form.find("#FollowUp_Edit_Title").val(fu.Title);
    $form.find("#FollowUp_Edit_ContactAction").val(fu.ContactAction);

    let ctl = $form.find("#FollowUp_Edit_Code");
    ctl.tagsinput("removeAll");
    try {
        fu.Tags.forEach((element, index) => {
            ctl.tagsinput("add", { id: fu.TagIds[index], tag: element });
        });
    } catch (e) {
        //console.log(e);
    }

    let contactsCtl = $form.find("#FollowUp_Edit_Contacts");
    contactsCtl.tagsinput("removeAll");
    try {
        fu.Contacts.forEach((element, index) => {
            contactsCtl.tagsinput("add", {
                id: fu.ContactIds[index],
                tag: element,
            });
        });
    } catch (e) {
        //console.log(e);
    }

    $form.find(".bootstrap-tagsinput").addClass("form-control");

    var completed = fu.CompletedDate !== null;

    $form.find("#FollowUp_Edit_IsCompleted").prop("checked", completed);

    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            vendor_action_JSON_POST(
                $form,
                "/FollowUp/Edit",
                refreshtables,
                FollowUpEditModel($form, fu),
                undefined,
                function () {
                    $form.modal("hide");
                }
            );
        });
    if (fu.LastModified) {
        let moddt = moment.utc(fu.LastModified).local().format("M-D-YY h:mm A");
        let modby = `Last modified ${moddt} by ${fu.LastModifiedBy}`;
        let modctl = $form.find("#FollowUp_Edit_LastModified").text(modby);
    } else if (fu.CreatedDate) {
        let moddt = moment.utc(fu.CreatedDate).local().format("M-D-YY h:mm A");
        let modby = `Created ${moddt} by ${fu.CreatedBy}`;
        let modctl = $form.find("#FollowUp_Edit_LastModified").text(modby);
    }
    // async population of notes and remark listbox
    //
    let $notehist = $form.find("#FollowUp_NoteHistory");
    $notehist.html("<div>loading...</div>");
    vendor_populate_followup_notehistory(
        `/FollowUp/NoteHistoryForFollowUp/${fu.RequestId}`,
        function (data) {
            $notehist.html(data);
            $form.modal("show");
        }
    );
}
function vendor_populate_followup_notehistory(url, callback) {
    $.ajax({
        url: url,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        data: null,
        contentType: false,
        processData: false,
        complete: function () {},
        success: function (data) {
            callback(data);
        },
        error: function (xhr) {
            callback("Error loading");
            processError(xhr);
        },
    });
}
function vendor_show_FollowUp_Edit(t) {
    var items = vendorGetSelectedFollowUpItems(t);

    if (items.length !== 1) {
        return;
    }
    var item = items[0];

    var $form = $("#modal-FollowUp-Edit");
    vendor_init_dialog($form);

    let requestId = item.RequestId;
    let reqno = item.RequestNo;
    let vin = item.Vin;

    vendor_set_single_vin_rno_header($form, "Edit Follow-Up", reqno, vin);

    vendor_action_JSON_GET(
        [t],
        `/FollowUp/${item.FollowUpId}/${item.RequestId}/`,
        null,
        vendor_edit_FollowUp
    );
}
function vendor_show_FollowUp_Create(t) {
    var $form = $("#modal-FollowUp-New");
    vendor_init_dialog($form);

    let pnl = $("#followUpsPanel");
    let requestId = pnl.data("reqid");
    let reqno = pnl.data("reqno");
    let vin = pnl.data("vin");

    vendor_set_single_vin_rno_header($form, "Create Follow-Up", reqno, vin);

    var dueDate = moment().add(7, "days").format("YYYY-MM-DD");

    $form.find("#FollowUp_New_DueDate").val(dueDate);
    $form.find("#FollowUp_New_StatusOfRecord").val("");
    $form.find("#FollowUp_New_Title").val("");
    $form.find("#FollowUp_NewNote").val("");
    $form.find("#FollowUp_NewRemark").val("");
    $form.find("#FollowUp_New_ContactAction").val("");

    let ctl = $form.find("#FollowUp_New_Code");
    ctl.tagsinput("removeAll");

    let contactsCtl = $form.find("#FollowUp_New_Contacts");
    contactsCtl.tagsinput("removeAll");

    $form.find(".bootstrap-tagsinput").addClass("form-control");
    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var refreshtables = [t];
            vendor_action_JSON_POST(
                $form,
                "/FollowUp/Create",
                refreshtables,
                FollowUpCreateModel($form)
            );
        });
    let $notehist = $form.find("#FollowUp_NoteHistory");
    $notehist.html("<div>loading...</div>");
    vendor_populate_followup_notehistory(
        `/FollowUp/NoteHistoryForFollowUp/${requestId}`,
        function (data) {
            $notehist.html(data);
            $form.modal("show");
        }
    );
}
function FollowUpCreateModel($form) {
    let pnl = $("#followUpsPanel");
    let requestId = pnl.data("reqid");
    let reqno = pnl.data("reqno");
    let vin = pnl.data("vin");

    let dtDue = $form.find("#FollowUp_New_DueDate").val();
    if (dtDue === "") dtDue = null;

    let note = $form.find("#FollowUp_New_StatusOfRecord").val();
    let code = $form.find("#FollowUp_New_Code").val();
    let title = $form.find("#FollowUp_New_Title").val();
    let newnote = $form.find("#FollowUp_NewNote").val();
    let newremark = $form.find("#FollowUp_NewRemark").val();
    let action = $form.find("#FollowUp_New_ContactAction").val();

    let tagCtl = $form.find("#FollowUp_New_Code");
    let tagIds = tagCtl.val();
    let tagItems = tagCtl.tagsinput("items");
    let tags = [];
    tagItems.map(function (item) {
        tags.push(item.tag);
    });

    let contactsCtl = $form.find("#FollowUp_New_Contacts");
    let contactIds = contactsCtl.val();
    let contactItems = contactsCtl.tagsinput("items");
    let contactsList = [];
    contactItems.map(function (item) {
        contactsList.push(item.tag);
    });

    var model = {
        RequestId: requestId,
        DueDate: dtDue,
        Title: title,
        StatusOfRecord: note,
        NewNote: newnote,
        NewRemark: newremark,
        ContactAction: action,
        Contacts: contactsList,
        ContactIds: contactIds,
        Tags: tags,
        TagIds: tagIds,
    };
    return model;
}
function vendor_show_FollowUp_Delete(t) {}
function vendorGetSelectedFollowUpItems(t) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push({
            RequestId: this["requestId"],
            FollowUpId: this["followUpId"],
            RequestNo: this["requestNo"],
            Vin: this["vin"],
            Title: this["title"],
        });
    });
    return a;
}
function FollowUpEditModel($form, fupid) {
    let requestId = fupid.RequestId;
    let followUpId = fupid.FollowUpId;

    let dtDue = $form.find("#FollowUp_Edit_DueDate").val();
    if (dtDue === "") dtDue = null;

    let completed = $form.find("#FollowUp_Edit_IsCompleted").prop("checked");
    let note = $form.find("#FollowUp_Edit_StatusOfRecord").val();
    let title = $form.find("#FollowUp_Edit_Title").val();
    let tagCtl = $form.find("#FollowUp_Edit_Code");
    let newnote = $form.find("#FollowUp_NewNote").val();
    let newremark = $form.find("#FollowUp_NewRemark").val();
    let action = $form.find("#FollowUp_Edit_ContactAction").val();
    let tagIds = tagCtl.val();
    let tagItems = tagCtl.tagsinput("items");
    let tags = [];
    tagItems.map(function (item) {
        tags.push(item.tag);
    });

    let contactsCtl = $form.find("#FollowUp_Edit_Contacts");
    let contactIds = contactsCtl.val();
    let contactItems = contactsCtl.tagsinput("items");
    let contactsList = [];
    contactItems.map(function (item) {
        contactsList.push(item.tag);
    });

    var model = {
        RequestId: requestId,
        FollowUpId: followUpId,
        DueDate: dtDue,
        IsCompleted: completed,
        Title: title,
        Tags: tags,
        TagIds: tagIds,
        StatusOfRecord: note,
        ContactAction: action,
        Contacts: contactsList,
        ContactIds: contactIds,
        NewNote: newnote,
        NewRemark: newremark,
    };
    return model;
}

function FollowUpId(rid, fid) {
    var id = { RequestId: rid, FollowUpId: fid };
    return id;
}
function FollowUpCompleteModel($form, items) {
    let dt = $form.find("#FollowUp_Completed_CompletedDate").val();
    if (dt === "") dt = null;
    let ids = $.map(items, function (i) {
        return FollowUpId(i.RequestId, i.FollowUpId);
    });
    var model = {
        CompletedDate: dt,
        FollowUpIDs: ids,
    };
    return model;
}
function vendor_show_FollowUp_Complete(t) {
    var items = vendorGetSelectedFollowUpItems(t);
    var html = "No items selected";
    var disabled = true;
    var $form = $("#modal-FollowUp-Completed");
    vendor_init_dialog($form);

    if (items.length <= 0) {
        return;
    }
    if (items.length !== 0) {
        html =
            "Operation will mark the " +
            items.length +
            " selected follow-ups as completed";
        $form.find("#selectedCount").html(html);
        disabled = false;
    }
    var item = items[0];
    var today = moment().format("YYYY-MM-DD");
    $form.find("#FollowUp_Completed_CompletedDate").val(today);
    $form.find("#FollowUp_Completed_CompletedDate").prop("disabled", disabled);
    vendor_set_multi_vin_rno_title_header($form, items, "Complete Follow-Ups");

    $form.find("#okButton").prop("disabled", disabled);

    if (!disabled) {
        $form
            .find("#okButton")
            .off("click")
            .on("click", function () {
                var tables = [t];
                vendor_action_JSON_POST(
                    $form,
                    "/FollowUp/Complete",
                    tables,
                    FollowUpCompleteModel($form, items)
                );
            });
    }
    $form.modal("show");
}
