"use strict";

function vendor_show_RequestTag_New(t) {
    var $form = $("#modal-RequestTag-New");
    vendor_init_dialog($form);
    requesttag_SetFields($form);

    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var tables = [t];
            vendor_action_JSON_POST(
                $form,
                "/Tags/Add",
                tables,
                RequestTag_NewModel($form)
            );
        });
    $form.modal("show");
}
function requestTag_fill_TagCategories($selectElement, selectValue, readonly) {
    $.get("/Tags/Categories", function (data) {
        if (data.length > 0) {
            $selectElement.empty();
            var $noneOption = $('<option value="">(none)</option>');
            $selectElement.append($noneOption);
            if (readonly && selectValue !== "") {
                $noneOption.attr("disabled", "disabled");
            }
            data.forEach(function (item, index) {
                let $option = $(
                    "<option>" + htmlEncode(item.tagCategoryName) + "</option>"
                );
                let tagCategoryId = `${item.tagCategoryId}`;
                $option.attr("value", tagCategoryId);
                $selectElement.append($option);
                if (readonly && selectValue !== tagCategoryId) {
                    $option.attr("disabled", "disabled");
                }
            });
            if (selectValue) {
                $selectElement.val(selectValue);
            }
        }
    });
}

function RequestTag_NewModel($form) {
    let tagName = $form.find("#RequestTag_Name").val().toUpperCase();
    let tagDesc = $form.find("#RequestTag_Description").val();

    var model = {
        TagName: tagName,
        TagDesc: tagDesc,
        TagType: "Request",
    };
    return model;
}
function RequestTag_EditModel($form) {
    let tagId = $form.find("#RequestTag_Id").val().toUpperCase();
    let tagName = $form.find("#RequestTag_Name").val().toUpperCase();
    let tagDesc = $form.find("#RequestTag_Description").val();
    let tagCategoryId = $form.find("#RequestTag_Category").val();
    let disabled = $form.find("#RequestTag_Disabled").prop("checked");

    var model = {
        TagId: parseInt(tagId),
        TagName: tagName,
        TagDesc: tagDesc,
        TagType: "Request",
        TagCategoryId: parseInt(tagCategoryId),
        Disabled: disabled,
    };
    return model;
}
function vendor_show_RequestTag_Edit(t) {
    var items = vendorGetSelectedItems_Tags(t);

    if (items.length !== 1) {
        return;
    }
    var item = items[0];
    var $form = $("#modal-RequestTag-Edit");
    vendor_init_dialog($form);
    requesttag_SetFields($form, item);

    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var tables = [t];
            // RequestTag_NewModel used here since same as edit
            var model = RequestTag_EditModel($form);
            vendor_action_JSON_POST($form, "/Tags/Edit", tables, model);
        });
    $form.modal("show");
}

function vendorGetSelectedItems_Tags(t) {
    var a = [];
    $.each(t.rows({ selected: true }).data(), function () {
        a.push({
            TagId: this["tagId"],
            TagName: this["tagName"],
            TagDesc: this["tagDesc"],
            TagCategoryId: this["tagCategoryId"],
            Disabled: this["disabled"],
        });
    });
    return a;
}

function requesttag_SetFields($form, item, readonly) {
    readonly = readonly || false;

    item = item || {
        TagId: "",
        TagName: "",
        TagDesc: "",
        TagCategoryId: null,
        Disabled: false,
    };

    $form.find("#RequestTag_Id").val(item.TagId);
    $form.find("#RequestTag_Name").val(item.TagName);
    $form.find("#RequestTag_Description").val(item.TagDesc);
    $form.find("#RequestTag_Category").val(item.TagCategoryId);
    $form.find("#RequestTag_Disabled").prop("checked", item.Disabled || false);

    $form.find("#RequestTag_Name").attr("readonly", readonly);
    $form.find("#RequestTag_Description").attr("readonly", readonly);
    $form.find("#RequestTag_Disabled").attr("readonly", readonly);

    $form.find("#RequestTag_Disabled").attr("disabled", readonly);

    let categoryCtl = $form.find("#RequestTag_Category");
    categoryCtl.val("");
    requestTag_fill_TagCategories(categoryCtl, item.TagCategoryId, readonly);
}
function vendor_show_RequestTag_Delete(t) {
    var items = vendorGetSelectedItems_Tags(t);

    if (items.length === 0) {
        vendor_show_DataTable_alert(t, "Item not selected");
        return;
    }
    if (items.length !== 1) {
        vendor_show_DataTable_alert(t, "Only 1 tag can be deleted at a time.");
        return;
    }

    var item = items[0];
    var $form = $("#modal-RequestTag-Delete");
    vendor_init_dialog($form);
    requesttag_SetFields($form, item, true);

    $form
        .find("#okButton")
        .off("click")
        .on("click", function () {
            var tables = [t];
            vendor_action_JSON_POST($form, "/Tags/Delete", tables, {
                TagId: item.TagId,
                TagType: "Request",
            });
        });
    $form.modal("show");
}
