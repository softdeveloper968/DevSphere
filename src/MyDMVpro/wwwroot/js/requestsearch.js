"use strict";

//Autocomplete search
var hasChosen = false;

var globalBasePath;
function getBaseUrl() {
    if (globalBasePath) return globalBasePath;
    globalBasePath = "/";
    return globalBasePath;
}
function initGlobalAutoComplete($ctl, groupId) {
    var curloc = document.location.href.toLowerCase();
    if (
        curloc.includes("/vendor/requestlookup") ||
        curloc.includes("/myservices/requestlookup")
    ) {
        initAutoComplete($ctl);
        return;
    }
    var actionPath = "";
    if (typeof groupId === "undefined" || groupId == null || groupId == "") {
        actionPath = "/Vendor/RequestLookup/";
    } else {
        actionPath = "/MyServices/RequestLookup/";
    }
    //https://github.com/pawelczak/EasyAutocomplete/issues/256 (allowing search on both the actual value (result) and the cleaned up value (result))
    var searchOptions = {
        url: function (q) {
            return getBaseUrl() + "RequestStatus/RequestSearch";
        },
        adjustWidth: false,
        getValue: function (element) {
            var v = element.vin;
            if (typeof v !== "undefined") {
                v = v.trim();
            }
            return v;
        },
        ajaxSettings: {
            dataType: "json",
            method: "POST",
            data: {
                vin: "",
                __RequestVerificationToken: "",
            },
        },
        preparePostData: function (data) {
            data.vin = $ctl.val().trim();
            data.__RequestVerificationToken = $(
                '[name="__RequestVerificationToken"]'
            ).val();
            return data;
        },
        requestDelay: 300,
        minCharNumber: 4,
        template: {
            type: "custom",
            method: function (value, item) {
                if (value.length > 25) {
                    value = value.substr(0, 25);
                }
                var displayValue =
                    item.requestNo +
                    " " +
                    item.appType +
                    " " +
                    item.state +
                    " " +
                    item.groupName +
                    " " +
                    item.submittedBy;
                return (
                    "<div class='ac-item'><span class='vin'>" +
                    value +
                    "</span>&nbsp;<span class='reqdesc'>" +
                    htmlEncode(displayValue) +
                    "</span></div>"
                );
            },
        },
        listLocation: "data",
        list: {
            onLoadEvent: function () {
                hasChosen = false;
            },
            onSelectItemEvent: function () {
                var inputValue = $ctl.getSelectedItemData().resultName;
                $ctl.val(inputValue);
            },
            onChooseEvent: function () {
                hasChosen = true;

                var seldata = $ctl.getSelectedItemData();
                var resultValue = seldata.vin;
                $ctl.val(resultValue);

                var $selectedVin = $("#globalSelectedVin");
                $selectedVin.val(seldata.vin);
                $selectedVin.data("requestid", seldata.requestId);
                $selectedVin.data("reqno", seldata.requestNo);

                var win = window.open(
                    actionPath + seldata.vin + "/" + seldata.requestNo,
                    "_blank"
                );
                win.focus();
            },
            maxNumberOfElements: 25,
            match: {
                enabled: true,
            },
            onHideListEvent: function () {
                //https://github.com/pawelczak/EasyAutocomplete/issues/274
                var containerList = $ctl
                    .next(".easy-autocomplete-container")
                    .find("ul");
                if ($(containerList).children("li").length <= 0) {
                    $(containerList)
                        .html(
                            '<li><div class="ac-item">No results found</div></li>'
                        )
                        .show();
                }
            },
        },
    };

    $ctl.easyAutocomplete(searchOptions);
}
function initAutoComplete($ctl) {
    //https://github.com/pawelczak/EasyAutocomplete/issues/256 (allowing search on both the actual value (result) and the cleaned up value (result))
    var searchOptions = {
        url: function (q) {
            return getBaseUrl() + "RequestStatus/RequestSearch";
        },
        adjustWidth: false,
        getValue: function (element) {
            var v = element.vin;
            if (typeof v !== "undefined") {
                v = v.trim();
            }
            return v;
        },
        ajaxSettings: {
            dataType: "json",
            method: "POST",
            data: {
                vin: "",
                __RequestVerificationToken: "",
            },
        },
        preparePostData: function (data) {
            data.vin = $ctl.val().trim();
            data.__RequestVerificationToken = $(
                '[name="__RequestVerificationToken"]'
            ).val();
            return data;
        },
        requestDelay: 300,
        minCharNumber: 4,
        template: {
            type: "custom",
            method: function (value, item) {
                if (value.length > 25) {
                    value = value.substr(0, 25);
                }
                var displayValue =
                    item.requestNo +
                    " " +
                    item.appType +
                    " " +
                    item.state +
                    " " +
                    item.groupName +
                    " " +
                    item.submittedBy;
                return (
                    "<div class='ac-item'><span class='vin'>" +
                    value +
                    "</span>&nbsp;<span class='reqdesc'>" +
                    htmlEncode(displayValue) +
                    "</span></div>"
                );
            },
        },
        listLocation: "data",
        list: {
            onLoadEvent: function () {
                hasChosen = false;
            },
            onSelectItemEvent: function () {
                var inputValue = $ctl.getSelectedItemData().resultName;
                $ctl.val(inputValue);
            },
            onChooseEvent: function () {
                hasChosen = true;

                var seldata = $ctl.getSelectedItemData();
                var resultValue = seldata.vin;
                $ctl.val(resultValue);

                var $selectedVin = $("#selectedVin");
                $selectedVin.val(seldata.vin);
                $selectedVin.data("requestid", seldata.requestId);
                $selectedVin.data("reqno", seldata.requestNo);

                // now populate the page form
                requestLookup_fill_form(
                    seldata.requestId,
                    seldata.requestNo,
                    seldata.vin
                );
            },
            maxNumberOfElements: 25,
            match: {
                enabled: true,
            },
            onHideListEvent: function () {
                //https://github.com/pawelczak/EasyAutocomplete/issues/274
                var containerList = $ctl
                    .next(".easy-autocomplete-container")
                    .find("ul");
                if ($(containerList).children("li").length <= 0) {
                    $(containerList)
                        .html(
                            '<li><div class="ac-item">No results found</div></li>'
                        )
                        .show();
                }
            },
        },
    };

    $ctl.easyAutocomplete(searchOptions);
}

//$(".search-box-icon").on("click", function () {
//	$(".nav-top-links").toggleClass("hideMe");
//	$(".search-box").toggleClass("showFlex");
//	$(".search-or-close").toggleClass("hideMe");
//	$(".globalSearch").focus().val('');

//	if (window.matchMedia("(max-width: 759px)").matches) {
//		$(".logo").toggleClass("hideMe");
//		$(".globalSearch").attr("placeholder", "")
//	}
//});

//$(".burger").on("click", function () {
//	$(".subnav").toggleClass("subnav-mobile");
//	$(".easy-autocomplete input.globalSearch").val('');
//});
