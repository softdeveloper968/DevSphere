"use strict";

var formfill_enableLog = false;
function formfill_log(msg) {
    if (formfill_enableLog) console.log(msg);
}
function getZipInfo(zip, cityField, stateField, countyField) {
    if (zip.length !== 5) {
        return;
    }
    $.ajax({
        url: "/AppForm/ZipLookup/" + zip,
        type: "GET",
        async: true,
        complete: function () {},
        success: function (data) {
            setZipRelatedFields(data, cityField, stateField, countyField);
        },
        error: function () {
        },
    });
}
function setZipRelatedFields(data, cityField, stateField, countyField) {
    if (data) {
        let $form = $("#dynamicAppForm");
        setFormField($form, "[name='" + cityField + "']", data.city);
        setFormField($form, "[name='" + stateField + "']", data.state);
        if (countyField) {
            setFormField($form, "[name='" + countyField + "']", data.county);
        }
    }
}
function setFormField($form, selector, value) {
    var $ctl = $form.find(selector);
    $ctl.val(value);
}

function parseAddress(
    $form,
    v,
    nameField,
    streetField,
    cityField,
    stateField,
    zipField
) {
    var formData = new FormData();
    formData.append("address", v);
    $.ajax({
        url: "/AppForm/ParseAddress",
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        async: true,
        cache: false,
        dataType: "json",
        contentType: false,
        processData: false,
        data: formData,
    })
        .done(function (data) {
            setAddressFields(
                data,
                nameField,
                streetField,
                cityField,
                stateField,
                zipField
            );
        })
        .fail(function (jqXHR, textStatus) {
        });
}
function setAddressFields(
    data,
    nameField,
    streetField,
    cityField,
    stateField,
    zipField
) {
    if (data && data.data) {
        let $form = $("#dynamicAppForm");
        var d = data.data;
        setFormFieldByName($form, nameField, d.name);
        setFormFieldByName($form, streetField, d.street);
        setFormFieldByName($form, zipField, d.zip5);
        setFormFieldByName($form, cityField, d.city);
        setFormFieldByName($form, stateField, d.state);
    }
}
function setFormFieldByName($form, fieldName, value) {
    if (isNotUndefined(name)) {
        var $ctl = $form.find("[name='" + fieldName + "']");
        $ctl.val(value);
    }
}
function isNotUndefined(v) {
    if (v === undefined || v === null) {
        return false;
    }
    return true;
}
function resetFormValidator(formId) {
    $(formId).removeData("validator");
    $(formId).removeData("unobtrusiveValidation");
    AddValidators();
    $.validator.unobtrusive.parse(formId);
}
function AddValidators() {
    $.validator.addMethod(
        "pastdate",
        function (value, element) {
            if (value) {
                var currentDate = new Date().setHours(0, 0, 0, 0);
                var selectedDate = new Date(value).setHours(0, 0, 0, 0);
                return currentDate >= selectedDate;
            }
            return true;
        },
        "Date must be today or earlier."
    );
    $.validator.addMethod(
        "futuredate",
        function (value, element) {
            if (value) {
                var currentDate = new Date().setHours(0, 0, 0, 0);
                var selectedDate = new Date(value).setHours(0, 0, 0, 0);
                return currentDate <= selectedDate;
            }
            return true;
        },
        "Date must be today or later."
    );
}

function appform_configure_profileselect(
    rooturl,
    groupId,
    sectionId,
    categoryId,
    categoryName
) {
    let wrapper = $(`[data-categoryid="${categoryId}"]`);
    let dataurl = `${rooturl}/${groupId}/${categoryName}`;

    wrapper.find("button.profile-refresh").click(function () {
        formfill_log("refresh clicked");
    });
    var selectCtl = wrapper.find("select.group-profile-select");
    selectCtl.change(function () {
        var groupProfileId = $(this).val();
        var autoapply = wrapper.data("autoapply") || false;
        if (autoapply) {
            formfill_log("Automatically apply profile");
            let appTypeId = ""; // not yet used
            appform_apply_profile(
                groupId,
                groupProfileId,
                appTypeId,
                sectionId
            );
        } else {
            formfill_log("Manually apply profile");
            $(this).find("button").show();
        }
    });

    $.ajax({
        url: dataurl,
        type: "POST",
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
        async: true,
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (data) {
        formfill_log(data);
        let profiles = data.data.map(function (profile) {
            return new Option(
                profile.fields.XPProfileName,
                profile.groupProfileID
            );
        });
        profiles.forEach((option) => {
            selectCtl.append(option);
        });
        //data.data.forEach(function (item) {
        //    profiles.push('<option value="' + item.groupProfileID + '">' + item.fields.XPProfileName + '</option>');
        //});
    });
}

function appform_apply_profile(groupId, groupProfileId, appTypeId, sectionId) {
    let url = `/GroupProfiles/GetProfileForUpdate/${groupProfileId}/${sectionId}`;
    $.ajax({
        url: url,
        type: "GET",
        async: true,
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (data) {
        formfill_log(data);
        formfill_apply_profilefields($("#newFormPanel"), data);
    });
}

function formfill_apply_profilefields($panel, data) {
    let fieldKeys = Object.keys(data);
    $.each(fieldKeys, function (idx, f) {
        var v = data[f];

        if (f === "VIN" || f === "Vehicle Vin") {
            // Don't set vin
            return;
        }
        if (typeof f !== "undefined" && f != null && f !== "") {
            if (typeof v !== "undefined") {
                if (v !== null) {
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
                                $matches.addClass("formfill_error");
                            }
                        }
                        if (currentvalue != v) {
                            if (currentvalue.trim() != v.trim()) {
                                $matches.addClass("formfill_changed");
                            }
                            return v;
                        }
                        return currentvalue;
                    });
                }
            }
        }
    });
}
