"use strict";

function vendor_show_FollowUpCode_New(t) {
    var $form = $("#modal-FollowUpCode-New");
    vendor_init_dialog($form);

    $form.find("#FollowUpCode_New_Code").tagsinput("removeAll");
    $form.find("#FollowUpCode_New_Description").val("");
    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var tables = [t];
            vendor_action_JSON_POST(
                $form,
                "/Tags/Add",
                tables,
                FollowUpCode_NewModel($form)
            );
        });
    $form.modal("show");
}
function FollowUpCode_NewModel($form) {
    let tagName = $form.find("#FollowUpCode_New_Name").val().toUpperCase();
    let tagDesc = $form.find("#FollowUpCode_New_Description").val();

    var model = {
        TagName: tagName,
        TagDesc: tagDesc,
        TagType: "FollowUp",
    };
    return model;
}
function FollowUpCode_EditModel($form) {
    let tagId = $form.find("#FollowUpCode_Edit_Id").val().toUpperCase();
    let tagName = $form.find("#FollowUpCode_Edit_Name").val().toUpperCase();
    let tagDesc = $form.find("#FollowUpCode_Edit_Description").val();

    var model = {
        TagId: parseInt(tagId),
        TagName: tagName,
        TagDesc: tagDesc,
        TagType: "FollowUp",
    };
    return model;
}
function vendor_show_FollowUpCode_Edit(t) {
    var items = vendorGetSelectedCodeItems(t);

    if (items.length !== 1) {
        return;
    }
    var item = items[0];
    var $form = $("#modal-FollowUpCode-Edit");
    vendor_init_dialog($form);

    $form.find("#FollowUpCode_Edit_Id").val(item.TagId);
    $form.find("#FollowUpCode_Edit_Name").val(item.TagName);
    $form.find("#FollowUpCode_Edit_Description").val(item.TagDesc);
    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var tables = [t];
            // FollowUpCode_NewModel used here since same as edit
            var model = FollowUpCode_EditModel($form);
            vendor_action_JSON_POST($form, "/Tags/Edit", tables, model);
        });
    $form.modal("show");
}

function vendor_show_FollowUpCode_Delete(t) {
    var items = vendorGetSelectedCodeItems(t);

    if (items.length !== 1) {
        return;
    }
    var item = items[0];
    var $form = $("#modal-FollowUpCode-Delete");
    vendor_init_dialog($form);

    $form.find("#FollowUpCode_Delete_Id").val(item.TagId);
    $form.find("#FollowUpCode_Delete_Name").val(item.TagName);
    $form.find("#FollowUpCode_Delete_Description").val(item.TagDesc);
    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var tables = [t];
            vendor_action_JSON_POST($form, "/Tags/Delete", tables, {
                TagId: item.TagId,
                TagType: "FollowUp",
            });
        });
    $form.modal("show");
}
