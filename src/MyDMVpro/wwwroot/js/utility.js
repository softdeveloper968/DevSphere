"use strict";

const constVendorInternalDocumentPrefix = "XV-";
const constGroupInternalDocumentPrefix = "XG-";

var defaultDateFormat = "MM-DD-YY";
var defaultDateTimeFormat = "MM-DD-YY h:mm A";

var htmlEncodeContainer = $("<div />");

function htmlEncode(value) {
    if (value) {
        return htmlEncodeContainer.text(value).html();
    }
    return "";
}

function attrEncode(value) {
    if (value) {
        let encoded = htmlEncode(value).replace(/"/g, "&quot;");
        encoded = encoded.replace(/'/g, "&#39;");
        return encoded;
    }
    return "";
}
function htmlDecode(value) {
    return htmlEncodeContainer.html(value).text();
}
function quoteString(str) {
    // Escape backslashes first
    let escapedStr = str.replace(/\\/g, "\\\\");

    // Escape single and double quotes
    escapedStr = escapedStr.replace(/'/g, "\\'");
    escapedStr = escapedStr.replace(/"/g, '\\"');

    // Wrap the string in quotes (can use either single or double quotes)
    return `'${escapedStr}'`;
}
function getOuterHtml($element) {
    return $element.prop("outerHTML");
}
function getfn_datatable_render_date(fmt) {
    return function (data, type, full, meta) {
        if (typeof data === "undefined" || data === null || data === "")
            return "";

        if (type === "sort" || type === "type" || type === "filter")
            return moment.utc(data).format("YYYY-MM-DD");

        if (type === "display") {
            try {
                return moment.utc(data).format(fmt);
            } catch (e) {}
        }
        return moment.utc(data).format(fmt);
    };
}
function datatable_render_date(data, type, full, meta) {
    if (typeof data === "undefined" || data === null || data === "") return "";

    if (type === "sort" || type === "type" || type === "filter")
        return moment.utc(data).format("YYYY-MM-DD");

    if (type === "display") {
        try {
            return moment.utc(data).format(defaultDateFormat);
        } catch (e) {}
    }
    return moment.utc(data).format(defaultDateFormat);
}
function datatable_render_date_with_elapseddays(data, type, full, meta) {
    if (typeof data === "undefined" || data === null || data === "") return "";

    if (type === "sort" || type === "type" || type === "filter")
        return moment.utc(data).format("YYYY-MM-DD");

    if (type === "display") {
        try {
            var elapsedDays = moment().diff(moment.utc(data), "days");
            return `${moment.utc(data).format(defaultDateFormat)} (${elapsedDays}d)`;
        } catch (e) {}
    }
    return moment.utc(data).format(defaultDateFormat);
}
// [{ "attachmentId": "...", "attachmentTypeId": "...", "attachmentName": "..." }]
var vendor_vendorAttachmentNames = [];
function datatable_render_vendorAttachment(
    data,
    type,
    full,
    meta,
    attachmentTypeId
) {
    if (type === "type") return data || "";

    if (type === "display" || type === "sort" || type === "filter") {
        try {
            if (data === void 0) {
                return "";
            } else {
                return vendor_getVendorAttachmentName(data);
            }
        } catch (e) {
            console.error(e);
        }
    }
    return "";
}

function vendor_initVendorAttachmentNames() {
    $.ajax({
        url: `/Vendor/GetVendorAttachmentNames`,
        async: false,
        type: "get",
        success: function (data) {
            if (data.success) {
                vendor_vendorAttachmentNames = data.data;
            } else {
                vendor_vendorAttachmentNames = [];
            }
        },
    });
}
function vendor_getGroupAttachmentName(attachmentId) {
    var name = "";
    vendor_groupAttachmentNames.forEach(function (item, index) {
        if (item.attachmentId === attachmentId) {
            name = item.attachmentName;
        }
    });
    return name;
}
function vendor_getVendorAttachmentName(attachmentId) {
    var name = "";
    vendor_vendorAttachmentNames.forEach(function (item, index) {
        if (item.attachmentId === attachmentId) {
            name = item.attachmentName;
        }
    });
    return name;
}
function vendor_getVendorAttachmentNames(excelName) {
    var list = [];
    vendor_vendorAttachmentNames.forEach(function (item, index) {
        if (item.excelName === excelName) {
            list.push(item);
        }
    });
    return list;
}
function vendor_getVendorAttachmentName(attachmentId) {
    var name = "";
    vendor_vendorAttachmentNames.forEach(function (item, index) {
        if (item.attachmentId === attachmentId) {
            name = item.attachmentName;
        }
    });
    return name;
}
// [{ "attachmentId": "...", "attachmentTypeId": "...", "attachmentName": "...", "excelName": "...", "groupId": "..." }]
var vendor_groupAttachmentNames = [];

function vendor_initGroupAttachmentNames(groupId) {
    $.ajax({
        url: `/Vendor/GetGroupAttachmentNames`,
        async: false,
        type: "get",
        success: function (data) {
            if (data.success) {
                vendor_groupAttachmentNames = data.data;
            } else {
                vendor_groupAttachmentNames = [];
            }
        },
    });
}
function vendor_getGroupAttachmentNames(groupId, excelName) {
    var list = [];
    //if (attachmentTypeId.toLowerCase() !== attachmentTypeId) {
    //    console.log("Group attachmentTypeId case mismatch")
    //    attachmentTypeId = attachmentTypeId.toLowerCase();
    //}
    vendor_groupAttachmentNames.forEach(function (item, index) {
        if (item.excelName === excelName && item.groupId === groupId) {
            list.push(item);
        }
    });
    return list;
}

//function vendor_getGroupAttachmentName(attachmentId, attachmentTypeId) {
//    var name = '';
//    if (attachmentId.toLowerCase() !== attachmentId) {
//        console.log("Group attachmentId case mismatch")
//        attachmentId = attachmentId.toLowerCase();
//    }
//    if (attachmentTypeId.toLowerCase() !== attachmentTypeId) {
//        console.log("Group attachmentTypeId case mismatch")
//        attachmentTypeId = attachmentTypeId.toLowerCase();
//    }
//    vendor_groupAttachmentNames.forEach(function (item, index) {
//        if (item.attachmentId === attachmentId) {
//            name = item.attachmentName;
//        }
//        else if (item.attachmentId.toLowerCase() === attachmentId.toLowerCase()) {
//            console.log("Group attachmentId case mismatch");
//            name = item.attachmentName;
//        }
//    });
//    return name;
//}
function datatable_render_groupAttachment(
    data,
    type,
    full,
    meta,
    attachmentTypeId
) {
    if (type === "sort" || type === "type" || type === "filter") return data || "";

    if (type === "display") {
        try {
            //
            if (data === void 0) {
                return "";
            } else {
                return vendor_getGroupAttachmentName(data);
            }
        } catch (e) {
            console.error(e);
        }
    }
    return "(unknown)";
}

function datatable_render_datetime_with_hi_res_sort(data, type, full, meta) {
    if (typeof data === "undefined" || data === null || data === "") return "";

    if (type === "sort") return data;
    if (type === "type") return moment.utc(data).format("YYYY-MM-DD HH:mm");
    if (type === "filter") return moment.utc(data).format("YYYY-MM-DD");

    if (type === "display") {
        try {
            return moment.utc(data).local().format(defaultDateTimeFormat);
        } catch (e) {}
    }
    return moment.utc(data).format(defaultDateTimeFormat);
}
function getfn_datatable_render_datetime_short() {
    return getfn_datatable_render_datetime("M-D-YY h:mm A");
}
function getfn_datatable_render_datetime(fmt) {
    return function (data, type, full, meta) {
        if (typeof data === "undefined" || data === null || data === "")
            return "";

        if (type === "sort")
            return moment.utc(data).format("YYYY-MM-DD HH:mm:ss.SSS");
        if (type === "type") return moment.utc(data).format("YYYY-MM-DD HH:mm");
        if (type === "filter") return moment.utc(data).format("YYYY-MM-DD");

        if (type === "display") {
            try {
                return moment.utc(data).local().format(fmt);
            } catch (e) {
                return "";
            }
        }
        return moment.utc(data).format(fmt);
    };
}
function datatable_render_datetime(data, type, full, meta) {
    if (typeof data === "undefined" || data === null || data === "") return "";

    if (type === "sort")
        return moment.utc(data).format("YYYY-MM-DD HH:mm:ss.SSS");
    if (type === "type") return moment.utc(data).format("YYYY-MM-DD HH:mm");
    if (type === "filter") return moment.utc(data).format("YYYY-MM-DD");

    if (type === "display") {
        try {
            return moment.utc(data).local().format(defaultDateTimeFormat);
        } catch (e) {
            return "";
        }
    }
    return moment.utc(data).format(defaultDateTimeFormat);
}
function datatable_render_shortdate() {
    return function (data, type, full) {
        if (typeof data === "undefined" || data === "" || date === null)
            return "";
        if (type === "sort" || type === "type" || type === "filter")
            return moment.utc(data).format("YYYY-MM-DD");
        if (type === "display") {
            try {
                return moment.utc(data).format(defaultDateFormat);
            } catch (e) {}
        }
        return moment.utc(data).format(defaultDateFormat);
    };
}
function datatable_render_internalhelplink(data, type, full, meta) {
    if (type === "display") {
        if (full.hasInternalHelp) {
            return gethelplink(full);
        }
    }
    return "";
}
function gethelplink(full) {
    return `<a href="/help/${full.appType}/${full.state}" target="_blank"><span style="font-family:FontAwesome;font-size:small">&#x0f05a;</span></a>`;
}
function datatable_render_publichelplink(data, type, full, meta) {
    if (type === "display") {
        if (full.hasPublicHelp) {
            return gethelplink(full);
        }
    }
    return "";
}
function datatable_render_autoims(data, type, full, meta) {
    //-1 === NO CALL TO AUTOIMS
    //0 = SUCCESSFUL AUTOIMS UPDATE
    //1 = ERROR PROCESSING AUTOIMS
    var errorStatus = -1;
    if (full.autoImsStatus === "ERROR") {
        errorStatus = 1;
    } else if (full.autoImsStatus === "OK" || full.autoImsStatus === "200") {
        errorStatus = 0;
    }

    if (type === "display") {
        if (errorStatus === 0 || errorStatus === 1) {
            var className = "";

            if (errorStatus === 0) {
                className = "autoIms autoImsOk";
            } else if (errorStatus === 1) {
                className = "autoIms autoImsError";
            }
            var html = `<div title=${quoteString(htmlEncode(full.autoImsError))} class="${className}" />`;
            return html;
        }
        return "";
    }
    return "";
}
function datatable_render_checkbox(data, type, full, meta) {
    let checked = data || false;
    if (checked) {
        return '<input type="checkbox" checked="checked" disabled="disabled" />';
    }
    return "";
}

function datatable_render_processingday(data, type, full, meta) {
    let processingDay = data || 0;

    if (type === "display") {
        if (processingDay === "0") {
            return "Day 1";
        }
        return processingDay === 3 ? "Both" : data === 2 ? "Day 2" : "Day 1";
    }
    return processingDay;
}

function datatable_render_chat(data, type, full, meta) {
    var val = 0;
    if (full.waitingForVendorReply) val = 3;
    else if (full.waitingForUserReply) val = 2;
    else if (full.hasActiveChat) val = 1;

    if (type === "display") {
        var chatClass = "chatIcon";
        if (full.waitingForVendorReply) {
            chatClass = chatClass + " chatAlert";
        } else if (full.waitingForUserReply) {
            chatClass = chatClass + " chatWaiting";
        } else if (full.hasActiveChat) {
            chatClass = chatClass + " chatActive";
        }
        var chatTitle = "Chat";
        var chatTime = full.lastChatDate;
        if (
            typeof chatTime !== "undefined" &&
            chatTime !== null &&
            chatTime !== ""
        ) {
            var localtime = moment
                .utc(chatTime)
                .local()
                .format("M-D-YY h:mm A");
            chatTitle = "Chat (last: " + localtime + ")";
        }
        let $link = $(
            `<a href="#" tabIndex="-1" data-requestid="${full.requestId}" class="${chatClass}" />`
        )
            .attr("title", chatTitle)
            .attr(
                "onclick",
                `return showChat(${quoteString(full.requestId)}, ${quoteString(htmlEncode(full.vin))});`
            );

        var html = getOuterHtml($link);
        return html;
    }
    if (type === "sort" || type === "type") return val;
    return val;
}
function datatable_render_attIcon(data, type, full, meta) {
    if (type === "display") {
        var attClass = "attIcon";
        var status = full.attachmentStatus;

        if (typeof status !== "undefined") status = 3;
        switch (full.attachmentStatus) {
            case 0:
                attClass += " hasAllAttachments";
                break;
            case 1:
                attClass += " missingSomeAttachments";
                break;
            case 2:
                attClass += " missingAllAttachments";
                break;
            case 3:
                attClass += " noAttachmentsRequired";
                break;
            case 4:
                attClass += " needsApproval";
                break;
            case 5:
                attClass += " missingOneOffAttachment";
                break;
        }

        if (full.attachmentCount && full.attachmentCount > 0) {
            attClass += " hasAtt";
        }

        let $link = $(
            `<a attIcon href="#" tabIndex="-1" data-requestid="${full.requestId}" class="${attClass}" />`
        )
            .attr("title", "Attachments")
            .attr(
                "onclick",
                `return showAttachments(${quoteString(full.requestId)}, ${quoteString(htmlEncode(full.vin))});`
            );

        var html = getOuterHtml($link); //`onclick="return showAttachments('${full.requestId}', '${full.vin}');"></a>`;

        return html;
    }
    return "";
}
function datatable_render_blank(data, type, full, meta) {
    return "";
}
function datatable_render_vendorlinks(data, type, full, meta) {
    if (type === "display") {
        var html = "";
        if (
            (full.signed && full.signed === true) ||
            (full.DirectToVendor !== "true" &&
                full.DateToVendor &&
                full.DateToVendor !== "")
        ) {
            var dt = new Date(full.dateSigned);
            html +=
                '<a title="View Signed Form" class="forms-signed" tabIndex="-1" href="/Forms/ViewSigned/' +
                full.requestId +
                '"><img width="32px" height="32px" src="/images/signed.png"/></a>';
        } else {
            html +=
                '<a title="Review" tabIndex="-1" href="/Forms/Sign/' +
                full.requestId +
                '"><img width="32px" height="32px" src="/images/sign.png"/></a>';
        }
        return html;
    }
    return "";
}
function datatable_render_editrequest(data, type, full, meta) {
    if (type === "display") {
        let $link = $(
            `<a href="#" tabIndex="-1" data-requestid="${full.requestId}" class="editForm" />`
        )
            .attr("title", "Edit")
            .attr(
                "onclick",
                `return vendorEditRequest(${quoteString(full.requestId)}, ${quoteString(htmlEncode(full.vin))});`
            );

        var html = getOuterHtml($link);
        return html;
    }
    return "";
}
function datatable_render_linkedicon(data, type, full, meta) {
    if (type === "display") {
        if (full.hasLink) {
            var html = '<span class="linkForm"></span>';
            return html;
        } else {
            return "";
        }
    }
    return "";
}
function datatable_render_editrequest_link(data, type, full, meta) {
    if (type === "display") {
        var html =
            '<a title="Edit" target="_blank" tabIndex="-1" data-requestid="' +
            htmlEncode(full.requestId) +
            '" class="editForm" href="/Vendor/RequestLookup/' +
            htmlEncode(full.vin) +
            "/" +
            htmlEncode(full.requestNo) +
            '"></a>';
        return html;
    }
    return "";
}
function datatable_render_editstatus(data, type, full, meta) {
    if (type === "display") {
        let $link = $(
            `<a href="#" tabIndex="-1" data-requestid="${full.requestId}" class="editStatus" />`
        )
            .attr("title", "Status")
            .attr(
                "onclick",
                `return vendorEditStatus(${quoteString(full.requestId)}, ${quoteString(htmlEncode(full.vin))});`
            );
        var html = getOuterHtml($link);
        return html;
    }
    return "";
}
function getStageName(stageId) {
    switch (stageId) {
        case 1:
            return "Pending";
        case 2:
            return "Incoming";
        case 3:
            return "Signing";
        case 4:
            return "Ready for Processing";
        case 5:
            return "Ready for Packing/Shipping";
        case 6:
            return "Receive From DMV";
        case 7:
            return "Ship to Lienholder";
        case 8:
            return "Ship To Vendor";
        case 9:
            return "Title Pending";
        case 10:
            return "Invoicing";
        case 11:
            return "In-Transit";
        case 12:
            return "Not Ready for Processing";
        case 13:
            return "In Processing";
        case 14:
            return "Ready for Printing";
        case 15:
            return "Ready for Printing";
        case 16:
            return "WV Rejections";
        case 17:
            return "WV Send Queue";
    }
    return "Other";
}
function datatable_render_specialbilling(data, type, full, meta) {
    if (type === "display") {
        if (data === true) {
            return "Yes";
        } else {
            return "";
        }
    }
    return data;
}
function datatable_render_mileagebrand(data, type, full, meta) {
    if (type === "display") {
        let brand = (data || "").toUpperCase();
        if (brand === "A") {
            return "Actual";
        } else if (brand === "N") {
            return "Not Actual";
        } else if (brand === "E") {
            return "Exceeds";
        } else if (brand === "X") {
            return "Exempt";
        } else {
            return data || "";
        }
    }
    return data;
}
function datatable_render_stage(data, type, full, meta) {
    if (type === "display" || type === "sort") return getStageName(data);
    return "";
}
function datatable_render_status(data, type, full, meta) {
    if (type === "display" || type === "sort") {
        switch (data) {
            case 0:
                return "Pending";
            case 1:
                return "Active";
            case 2:
                return "Completed";
            case 9:
                return "Hold";
            case 10:
                return "Cancelled";
            default:
                return "Other";
        }
    }
    return data;
}

function datatable_render_codecount(data, type, full, meta) {
    if (type === "display" || type === "sort") {
        switch (data) {
            case 0:
                return "";
            default:
                return "C";
        }
    }
    return data;
}
function datatable_render_dupecheck(data, type, full, meta) {
    /*
    -1 === NO CALL TO AUTOIMS
    0 = SUCCESSFUL AUTOIMS UPDATE
    1 = ERROR PROCESSING AUTOIMS
    */
    if (type === "display") {
        if (full.dupeAppTypeForVin > 0 || full.activeCountForVin > 0) {
            var className = "";
            var dupeMsg = "";
            if (full.dupeAppTypeForVin > 1) {
                className = "dupeAppTypeForVin";
                dupeMsg =
                    "" +
                    full.dupeAppTypeForVin +
                    " instances found for this vin and application type";
            } else if (full.activeCountForVin > 0) {
                className = "activeCountForVin";
                dupeMsg =
                    "" +
                    full.activeCountForVin +
                    " instances found for this vin for other application types";
            } else {
                return "";
            }
            var html = `<div title=${quoteString(htmlEncode(dupeMsg))} class="${className}" />`;
            return html;
        }
        return "";
    }
    return "";
}

function getfn_datatable_render_number(nullIsZero, blankIsZero) {
    return function (data, type, full, meta) {
        if (type === "display") {
            if (data === null && nullIsZero) return "0";
            return data;
        }
        if ((data === null && nullIsZero) || (data === "" && blankIsZero))
            return "0";
        return data;
    };
}
function datatable_render_vin(data, type, full, meta) {
    if (type === "sort") {
        if (data === null) return "";
        return data.substr(-6);
    }
    if (type === "display") {
        if (data === null) return "";
        var html =
            "<span class='vin'>" +
            htmlEncode(data.substr(0, data.length - 6)) +
            "<span>" +
            htmlEncode(data.substr(-6)) +
            "</span></div>";
        return html;
    }
    return data;
}
function datatable_render_group_autoims(data, type, full, meta) {
    /*
    -1 === NO CALL TO AUTOIMS
    0 = SUCCESSFUL AUTOIMS UPDATE
    1 = ERROR PROCESSING AUTOIMS
    */
    var errorStatus = -1;
    if (full.autoImsStatus === "ERROR") {
        errorStatus = 1;
    } else if (full.autoImsStatus === "OK" || full.autoImsStatus === "200") {
        errorStatus = 0;
    }

    if (type === "display") {
        if (errorStatus === 0 || errorStatus === 1) {
            var className = "";

            if (errorStatus === 0) {
                className = "autoIms autoImsOk";
            } else if (errorStatus === 1) {
                className = "autoIms autoImsError";
            }
            var html = `<div title=${quoteString(htmlEncode(full.autoImsError))} class="${className}" />`;
            return html;
        }
        return "";
    }
    return "";
}
function createLink(href, tabIndex, title, requestId, className) {
    let $link = $(`<a/>`)
        .attr("href", href)
        .attr("title", title)
        .attr("tabIndex", tabIndex)
        .data("requestid", requestId)
        .addClass(className);
    return $link;
}
function datatable_render_group_viewrequest(data, type, full, meta) {
    if (type === "display") {
        let $link = $(
            `<a href="#" tabIndex="-1" data-requestid="${full.requestId}" class="editStatus"><i class="fa fa-square-o" aria-hidden="true"></a>`
        )
            .attr("title", "View")
            .attr(
                "onclick",
                `return viewRequest(${quoteString(full.requestId)});`
            );

        var html = getOuterHtml($link);
        //'<a title="View" href="#" tabIndex="-1" data-requestid="' + htmlEncode(full.requestId) + '"
        // onclick = "return viewRequest(\'' + htmlEncode(full.requestId) + '\');" > <i class="fa fa-square-o" aria-hidden="true"></a>';
        return html;
    }
    return "";
}
function getfn_datatable_render_group_editrequest(userid, groupAdmin) {
    return function (data, type, full, meta) {
        if (type === "display") {
            var url = `/MyServices/RequestLookup/${encodeURIComponent(full.vin)}/${encodeURIComponent(full.requestNo)}`;
            var iconclass = "viewForm";
            if (
                (full.processStageId === "1" || full.processStageId === "2") && // Pending or Incoming
                (full.statusId === "0" || full.statusId === "1") && // Pending or Active
                (full.userId.toLowerCase() === userid || groupAdmin === true)
            ) {
                iconclass = "editForm";
            }
            var html = `<a class="${iconclass}" tabIndex="-1" title="Edit" href="${url}" target="_blank"></a>`;
            return html;
        }
        return "";
    };
}
function datatable_render_group_viewsign(data, type, full, meta) {
    if (type === "display") {
        var html = "<div>";
        if (
            (full.dateSigned && full.dateSigned !== "") ||
            (full.directToVendor !== "true" &&
                full.dateToVendor &&
                full.dateToVendor !== null &&
                full.dateToVendor !== "")
        ) {
            var dt = new Date(full.dateSigned);
            let $link = $(
                '<a title="View Signed Form" tabIndex="-1" ><img width="32px" height="32px" src="/images/signed.png"/></a>'
            );
            $link.attr(
                "href",
                "/Forms/ViewSigned/" +
                    htmlEncode(encodeURIComponent(full.requestId))
            );
            html += getOuterHtml($link);
        } else {
            let $link = $(
                '<a title="Review" tabIndex="-1"><img width="32px" height="32px" src="/images/sign.png"/></a>'
            );
            $link.attr(
                "href",
                "/Forms/Sign/" + htmlEncode(encodeURIComponent(full.requestId))
            );
            html += getOuterHtml($link);
        }
        html += "</div>";
        return html;
    }
    return data;
}
function datatable_render_group_links(data, type, full, meta) {
    if (type === "display") {
        var html = "<div>";
        let $link = $('<a title="Notices" tabIndex="-1"></a>')
            .attr("href", "/Requests/Notices/" + full.requestId)
            .attr(
                "onclick",
                `return showReqDetails(this,${quoteString(htmlEncode(full.vin))});`
            )
            .append(
                '<img width="32px" height="32px" src="/images/magnify.png" />'
            );
        html += getOuterHtml($("<div/>").append($link));
        return html;
    }
    return data;
}
function datatable_render_group_notices(data, type, full, meta) {
    if (type === "display") {
        var html = "<div>";
        let $link = $('<a title="Notices" tabIndex="-1"></a>')
            .attr("href", "/Requests/Notices/" + htmlEncode(full.requestId))
            .attr(
                "onclick",
                `return showReqDetails(this,${quoteString(htmlEncode(full.vin))});`
            )
            .append(
                '<img width="32px" height="32px" src="/images/magnify.png" />'
            );

        //html += '<a title="Notices" tabIndex="-1" href="/Requests/Notices/' + htmlEncode(full.requestId) + '"
        //onclick = "return showReqDetails(this,\x27' + htmlEncode(full.vin) + '\x27);" > ';
        //html += '<img width="32px" height="32px" src="/images/magnify.png" /></a >';
        html += getOuterHtml($("<div/>").append($link));
        html += "</div>";
        return html;
    }
    return data;
}
function getTransposedColumn(column) {
    let originalColIndex = column.index();
    let newColIndex = originalColIndex;
    if (typeof column.colReorder !== "undefined") {
        newColIndex = column.colReorder.transpose(column.index());
    }
    if (originalColIndex === newColIndex) return column;
    return column.column(newColIndex);
}
function getfn_datatable_dtfilter(id, emptySupport, colFilters) {
    return function () {
        var column = this;
        var filterRowSelector = `#filterRow-${id} th`;
        var $colHdr = $(filterRowSelector).eq(column.index());
        var colName = column.dataSrc();

        // get the current filter value to set the default
        var oldVal = $($colHdr).find("select.simple").val() || "";
        var oldMinVal = $($colHdr).find("#min").val() || "";
        var oldMaxVal = $($colHdr).find("#max").val() || "";

        var selectDiv = $(
            '<div class="filterToggle"><select class="customFilter simple"></select><div class="customFilter advanced"><input id="min" class="mindate" type="date"></input><input id="max" class="maxdate" type="date"></input></div></div>'
        ).appendTo($colHdr.empty());

        // set value before connecting change events
        $colHdr.find("select.simple").val(oldVal);
        $colHdr.find("#min").val(oldMinVal);
        $colHdr.find("#max").val(oldMaxVal);

        selectDiv.find("select").on("change", function () {
            let col = getTransposedColumn(column);
            var val = $(this).val();
            var dt;
            if (isNotBlankOrEmpty(val)) dt = moment.utc(val);
            if (isNotBlankOrEmpty(val) && !dt.isValid()) {
                $(this).addClass("invalidDate");
            } else {
                $(this).removeClass("invalidDate");
                if (isNotBlankOrEmpty(val)) {
                    val = dt.format("YYYY-MM-DD");
                    col.search(val ? val : "", false, false).draw();
                } else {
                    col.search(val, false, false).draw();
                }
            }
        });
        selectDiv
            .find("div")
            .find("#min, #max")
            .on("change", function () {
                if ($(this).is(":visible")) {
                    let col = getTransposedColumn(column);
                    var fromval = $(this).parent().find("#min").val();
                    var toval = $(this).parent().find("#max").val();
                    var rangeVal = "";
                    if (fromval !== "" || toval !== "")
                        rangeVal = fromval + ";-to-;" + toval;

                    col.search(rangeVal ? rangeVal : "", false, false).draw();
                }
            });
        var select = selectDiv.find("select");
        colName = column.dataSrc();
        if (colName && colFilters) {
            if (
                !(typeof colName === "undefined") &&
                !(typeof colFilters[colName] === "undefined")
            ) {
                var filterCount = colFilters[colName].length;
                var addEmpty = false;
                $.each(colFilters[colName], function (index, value) {
                    if (isNull(value) || isEmptyString(value)) {
                        if (emptySupport && filterCount > 1) {
                            addEmpty = true;
                        }
                    } else {
                        //var d = moment.utc(value).format('MM-DD-YYYY');
                        //var dval = moment.utc(value).format('YYYY-MM-DD');
                        //var $opt = $("<option value=''></option>");
                        //$opt.val(dval);
                        //$opt.text(d);
                        select.append(buildOption_Date(value));
                    }
                });
                AddSelectDefaults(select, addEmpty, false);
            }
        }
    };
}
function buildOption_Date(d, fmt) {
    if (typeof fmt === "undefined") fmt = defaultDateFormat;

    var $opt = $("<option value=''></option>");
    var dtext = moment.utc(d).format(fmt);
    var dval = moment.utc(d).format("YYYY-MM-DD");
    $opt.text(dtext);
    $opt.val(dval);
    return $opt;
}
function getfn_datatable_getyesnofilter(id) {
    return function () {
        var column = this;
        var filterRowSelector = `#filterRow-${id} th`;
        var $colHdr = $(filterRowSelector).eq(column.index());

        // get the current filter value to set the default
        var oldVal = $($colHdr).find("select").val() || "";

        var selectDiv = $(
            '<div><select class="customFilter"></select></div>'
        ).appendTo($colHdr.empty());

        selectDiv.find("select").val(oldVal);

        selectDiv.find("select").on("change", function () {
            let col = getTransposedColumn(column);
            var val = $(this).val();
            col.search(val).draw();
        });

        var select = selectDiv.find("select");

        select.prepend(
            '<option value=""></option><option value="true">Yes</option><option value="false">No</option>'
        );
    };
}
function getfn_datatable_emptyornotfilter(id, emptySupport) {
    return function () {
        var column = this;
        var filterRowSelector = `#filterRow-${id} th`;
        var $colHdr = $(filterRowSelector).eq(column.index());
        var colName = column.dataSrc();

        // get the current filter value to set the default
        var oldVal = $($colHdr).find("select").val() || "";

        var selectDiv = $(
            '<div><select class="customFilter"></select></div>'
        ).appendTo($colHdr.empty());

        selectDiv.find("select").val(oldVal);

        selectDiv.find("select").on("change", function () {
            let col = getTransposedColumn(column);
            var val = $(this).val();
            col.search(val).draw();
        });

        var select = selectDiv.find("select");
        AddSelectDefaults(select, true, true);
    };
}
function getfn_datatable_drofilter(id, emptySupport) {
    // Advanced Date range only
    return function () {
        var column = this;

        var selectDiv = null;
        let emptySupportSelect = "";
        if (emptySupport) {
            emptySupportSelect =
                '<select class="customFilter simple"></select>';
        }
        selectDiv = $(
            `<div class="filterToggle">${emptySupportSelect}<div class="customFilter advanced"><input id="min" class="mindate" type="date"></input><input id="max" class="maxdate" type="date"></input></div></div>`
        ).appendTo(
            $("#filterRow-" + id + " th")
                .eq(column.index())
                .empty()
        );
        selectDiv
            .find("div")
            .find("#min, #max")
            .on("change", function () {
                if ($(this).is(":visible")) {
                    let col = getTransposedColumn(column);
                    var fromval = $(this).parent().find("#min").val();
                    var toval = $(this).parent().find("#max").val();
                    var rangeVal = "";
                    if (fromval !== "" || toval !== "")
                        rangeVal = fromval + ";-to-;" + toval;

                    col.search(rangeVal ? rangeVal : "", false, false).draw();
                }
            });
        if (emptySupport) {
            let select = selectDiv.find("select");
            select.on("change", function () {
                if ($(this).is(":visible")) {
                    let col = getTransposedColumn(column);
                    var val = $(this).val();
                    col.search(val).draw();
                }
            });
            AddSelectDefaults(select, true, true);
        }
    };
}
function getfn_datatable_requesttagfilter(id, emptySupport, colFilters) {
    return function () {
        try {
            var column = this;

            var selectDiv = $(
                `<div><select class="simple requesttagselect" multiple="multiple" style="width:100%"></select></div>`
            ).appendTo($(`#filterRow-${id} th`).eq(column.index()).empty());

            // init list
            var $selectElement = selectDiv.find("select");
            $.get("/Requests/Tags", function (data, status) {
                if (data.length > 0) {
                    data.forEach(function (item, index) {
                        var $option = $(
                            "<option>" + htmlEncode(item.tag) + "</option>"
                        );
                        $option.attr("value", `${item.id}`);
                        $selectElement.append($option);
                    });
                }
                $selectElement.selectpicker({ container: "body" });
            });
            $selectElement.parent().on("hide.bs.dropdown", function () {
                let col = getTransposedColumn(column);
                var val = $(this).val();

                var options = $selectElement.find("option:selected");
                var sValues = "";
                $.map(options, function (option) {
                    if (sValues.length > 0) sValues += ",";
                    sValues += option.value;
                    return option.value;
                });
                $(`#${id}`)
                    .DataTable()
                    .ajax.url(`/Requests/RequestsTagged/${sValues}`)
                    .draw();
            });
        } catch (e) {}
    };
}
function getfn_datatable_followupcontactfilter(id, emptySupport, colFilters) {
    return function () {
        try {
            var column = this;

            var selectDiv = $(
                `<div><select class="simple followupcontactselect" multiple="multiple" style="width:100%"></select></div>`
            ).appendTo($(`#filterRow-${id} th`).eq(column.index()).empty());

            // init list
            var $selectElement = selectDiv.find("select");
            $.get("/FollowUp/Contacts", function (data, status) {
                if (data.length > 0) {
                    data.forEach(function (item, index) {
                        var $option = $(
                            "<option>" + htmlEncode(item.tag) + "</option>"
                        );
                        $option.attr("value", `${item.id}`);
                        $selectElement.append($option);
                    });
                }
                $selectElement.selectpicker({ container: "body" });
            });
            $selectElement.parent().on("hide.bs.dropdown", function () {
                let col = getTransposedColumn(column);
                var val = $(this).val();

                var options = $selectElement.find("option:selected");
                var contactValues = "";
                $.map(options, function (option) {
                    if (contactValues.length > 0) contactValues += ",";
                    contactValues += option.value;
                    return option.value;
                });

                getFollowupTagUrl(id, undefined, contactValues);
            });
        } catch (e) {}
    };
}
function getFollowupTagUrl(id, tags, contacts) {
    let baseUrl = "/FollowUp/VendorFollowUpsTagged";

    let url = $(`#${id}`).DataTable().ajax.url();
    let tagValues = "";
    let contactValues = "";

    if (url.includes("/VendorFollowUpsTagged/")) {
        const parts = url.slice(1).split("/");
        if (tags === "") {
            tagValues = "tags";
        } else {
            tagValues = tags || parts[2] || "tags";
        }
        if (contacts === "") {
            contactValues = "contacts";
        } else {
            contactValues = contacts || parts[3] || "contacts";
        }
    } else {
        // should not occur
    }
    $(`#${id}`)
        .DataTable()
        .ajax.url(`${baseUrl}/${tagValues}/${contactValues}`)
        .draw();
}
function getfn_datatable_followuptagfilter(id, emptySupport, colFilters) {
    return function () {
        try {
            var column = this;

            var selectDiv = $(
                `<div><select class="simple followuptagselect" multiple="multiple" style="width:100%"></select></div>`
            ).appendTo($(`#filterRow-${id} th`).eq(column.index()).empty());

            // init list
            var $selectElement = selectDiv.find("select");
            $.get("/FollowUp/Tags", function (data, status) {
                if (data.length > 0) {
                    data.forEach(function (item, index) {
                        var $option = $(
                            "<option>" + htmlEncode(item.tag) + "</option>"
                        );
                        $option.attr("value", `${item.id}`);
                        $selectElement.append($option);
                    });
                }
                $selectElement.selectpicker({ container: "body" });
            });
            $selectElement.parent().on("hide.bs.dropdown", function () {
                let col = getTransposedColumn(column);
                var val = $(this).val();

                var options = $selectElement.find("option:selected");
                var tagValues = "";
                $.map(options, function (option) {
                    if (tagValues.length > 0) tagValues += ",";
                    tagValues += option.value;
                    return option.value;
                });
                getFollowupTagUrl(id, tagValues, undefined);
            });
        } catch (e) {}
    };
}
var htmlEncodeContainer = $("<div />");
function htmlEncode(value) {
    if (value) {
        return htmlEncodeContainer.text(value).html();
    } else {
        return "";
    }
}
function getfn_datatable_drfilter(id, emptySupport, colFilters) {
    return function () {
        var column = this;
        var filterRowSelector = `#filterRow-${id} th`;
        var $colHdr = $(filterRowSelector).eq(column.index());
        var colName = column.dataSrc();

        // get the current filter value to set the default
        var oldVal = $($colHdr).find("select.simple").val() || "";
        var oldMinVal = $($colHdr).find("#min").val() || "";
        var oldMaxVal = $($colHdr).find("#max").val() || "";

        var selectDiv = $(
            '<div class="filterToggle"><select class="customFilter simple"></select><div class="customFilter advanced"><input id="min" class="mindate" type="date"></input><input id="max" class="maxdate" type="date"></input></div></div>'
        ).appendTo($colHdr.empty());

        // set value before connecting change events
        $colHdr.find("select.simple").val(oldVal);
        $colHdr.find("#min").val(oldMinVal);
        $colHdr.find("#max").val(oldMaxVal);

        selectDiv.find("select").on("change", function () {
            let col = getTransposedColumn(column);
            var val = $(this).val();
            var dt;
            if (isNotBlankOrEmpty(val)) dt = moment.utc(val);
            if (isNotBlankOrEmpty(val) && !dt.isValid()) {
                $(this).addClass("invalidDate");
            } else {
                $(this).removeClass("invalidDate");
                if (isNotBlankOrEmpty(val)) {
                    val = dt.format("YYYY-MM-DD");
                    col.search(val ? val : "", false, false).draw();
                } else {
                    col.search(val, false, false).draw();
                }
            }
        });
        selectDiv
            .find("div")
            .find("#min, #max")
            .on("change", function () {
                if ($(this).is(":visible")) {
                    let col = getTransposedColumn(column);
                    var fromval = $(this).parent().find("#min").val();
                    var toval = $(this).parent().find("#max").val();
                    var rangeVal = "";
                    if (fromval !== "" || toval !== "")
                        rangeVal = fromval + ";-to-;" + toval;

                    col.search(rangeVal ? rangeVal : "", false, false).draw();
                }
            });
        var select = selectDiv.find("select");

        if (colName && colFilters) {
            if (
                !(typeof colName === "undefined") &&
                !(typeof colFilters[colName] === "undefined")
            ) {
                var addEmpty = false;
                $.each(colFilters[colName], function (index, value) {
                    if (isNull(value) || isEmptyString(value)) {
                        if (emptySupport) {
                            addEmpty = true;
                        }
                    } else {
                        //var d = moment.utc(value).format('MM-DD-YYYY');
                        //var dval = moment.utc(value).format('YYYY-MM-DD');
                        //var $opt = $("<option value=''></option>");
                        //$opt.val(dval);
                        //$opt.text(d);
                        select.append(buildOption_Date(value));
                    }
                });
                AddSelectDefaults(select, addEmpty);
            }
        }
    };
}
function datatable_initcomplete(id) {
    //finish up an initialization
    $(`#${id}`)
        .find(".norowselect")
        .on("click", function (e) {
            e.stopPropagation();
        });
}
var selectedProfile = {};
function bulkEditColorizeColumns(tableElement, f) {
    if (f) {
        $(tableElement).removeClass("stripe");
        $(tableElement).removeClass("table-striped");
        $(tableElement).addClass("colorize");
    } else {
        $(tableElement).removeClass("colorize");
        $(tableElement).addClass("table-striped");
        $(tableElement).addClass("stripe");
    }
}
function datatable_addProfile(datatable) {
    var api = datatable.api();
    var table = api.table();
    let wrapper = $(table.container());
    let content = $("#bulkEditProfileTemplate").html();
    let profileContainer = wrapper.find(".bulkedit-profile-container");
    profileContainer.append(content);

    // get app type
    let data = api.data();
    if (data.length > 0) {
        let appType = data[0].AppType;
        let appState = data[0].AppTypeState;

        $.ajax({
            url: `/Vendor/BulkEditProfiles/${appType}/${appState}`,
            type: "get",
            success: function (data) {
                if (data.success) {
                    let select = profileContainer.find("#profileId");
                    select.empty();
                    let profileArray = data.data;
                    if (profileArray && profileArray.length > 0) {
                        $.each(profileArray, function (index, value) {
                            select.append(
                                $(
                                    '<option value="' +
                                        value.bulkEditProfileNameId +
                                        '"></option>'
                                ).text(value.bulkEditProfileName)
                            );
                        });
                        select.selectedIndex = 0;
                        select.off("change").on("change", function () {
                            let fCollapse = profileContainer
                                .find("#profileCollapseCol")
                                .prop("checked");
                            let fColorize = profileContainer
                                .find("#profileColorCode")
                                .prop("checked");
                            updateBulkEditColumnClasses(
                                table,
                                this,
                                wrapper,
                                profileArray,
                                fColorize,
                                fCollapse
                            );
                        });
                        // init on startup
                        {
                            let fCollapse = profileContainer
                                .find("#profileCollapseCol")
                                .prop("checked");
                            let fColorize = profileContainer
                                .find("#profileColorCode")
                                .prop("checked");
                            updateBulkEditColumnClasses(
                                table,
                                select,
                                wrapper,
                                profileArray,
                                fColorize,
                                fCollapse
                            );
                        }
                    }
                    profileContainer
                        .find("#profileColorCode")
                        .on("change", function () {
                            let fCollapse = profileContainer
                                .find("#profileCollapseCol")
                                .prop("checked");
                            updateBulkEditColumnClasses(
                                table,
                                select,
                                wrapper,
                                profileArray,
                                this.checked,
                                fCollapse
                            );
                        });
                    profileContainer
                        .find("#profileCollapseCol")
                        .on("change", function () {
                            let fColorize = profileContainer
                                .find("#profileColorCode")
                                .prop("checked");
                            updateBulkEditColumnClasses(
                                table,
                                select,
                                wrapper,
                                profileArray,
                                fColorize,
                                this.checked
                            );
                        });
                } else {
                    // show error alert
                }
            },
            error: function (xhr) {
                processError(xhr);
            },
        });
        try {
            //console.log(`apptype: ${data[0].appType} -test`);
            // initialize
        } catch (e) {}
    }
}
function configureColumnVisibility(
    curCol,
    fieldName,
    type,
    fColorize,
    fCollapse
) {
    let className = "";

    type = type || "";

    if (
        fieldName.startsWith(constVendorInternalDocumentPrefix) ||
        fieldName.startsWith(constGroupInternalDocumentPrefix)
    ) {
        type = "X";
    }

    fCollapse = fCollapse || false;
    let fMakeVisible = false;
    if (fCollapse) {
        // X === internal attachment
        // M === Maggard supplied
        // C === Client supplied
        // I = Informational
        if (
            type === "M" ||
            type === "C" ||
            type === "A" ||
            type === "I" ||
            type === "X"
        ) {
            fMakeVisible = true;
        } else {
            if (typeof type === "undefined") {
                fMakeVisible = true;
            } else {
                fMakeVisible = false;
            }
        }
    } else {
        fMakeVisible = true;
    }
    if (type !== " " && type !== "") {
        className = "bulkEditRequired-" + type;
    } else {
        className = "";
    }
    if (curCol.visible() !== fMakeVisible) {
        curCol.visible(fMakeVisible, false);
    }
    try {
        $(curCol.header()).removeClass(
            "bulkEditRequired-M bulkEditRequired-A bulkEditRequired-C bulkEditRequired-I"
        );
        if (className !== "") {
            $(curCol.header()).addClass(className);
        }
        if (fColorize) {
            // add attribute to all td elements in the column
            $(curCol.nodes()).each(function () {
                $(this).removeClass(
                    "bulkEditRequired-M bulkEditRequired-A bulkEditRequired-C bulkEditRequired-I"
                );
                if (className !== "") {
                    $(this).addClass(className);
                }
            });
        }
    } catch (e) {
        console.log(e);
    }
}

function configureColumnColorization(wrapper, fieldName, type) {
    let col = $(wrapper).find(`[data-field='${htmlEncode(fieldName)}']`);
    if (col.length === 0) return;

    col.removeClass("bulkEditRequired-NO");
    switch (type) {
        case "M":
            col.addClass("bulkEditRequired-M");
            break;
        case "C":
            col.addClass("bulkEditRequired-C");
            break;
        case "A":
            col.addClass("bulkEditRequired-A");
            break;
        case "I":
            col.addClass("bulkEditRequired-I");
            break;
        case "X":
            col.addClass("bulkEditRequired-X");
            break;
        default:
            col.addClass("bulkEditRequired-NO");
            break;
    }
}
function updateBulkEditColumnClasses(
    datatable,
    profileSelect,
    wrapper,
    profileArray,
    fColorize,
    fHideUnused
) {
    // get selected profile and use the value to find the profile in the profileArray
    //let selectedProfileId = $(selectThis).val();
    let selectedProfileIndex = $(profileSelect).prop("selectedIndex");
    if (selectedProfileIndex === -1) {
        jqToast.error({
            text: `No items selected`,
        });
        return;
    }

    selectedProfile = profileArray[selectedProfileIndex];
    let fields = selectedProfile.fields || [];

    if (fields && fields.length > 0) {
        var fieldNames = datatable.settings().init().columns;
        datatable
            .columns()
            .flatten()
            .each(function (colIdx) {
                let curCol = datatable.column(colIdx);
                var specificFieldName = "";
                //let dataSrc = datatable.column(colIdx).dataSrc;
                //console.log(dataSrc);
                let type = null; // not found
                try {
                    if (colIdx === 0) {
                        type = "I";
                    } else {
                        specificFieldName = fieldNames[colIdx].data; // Replace columnIndex with the index of your column
                        if (specificFieldName) {
                            //console.log(specificFieldName);
                            let field = fields.find(
                                (element) =>
                                    element.excelName.toLowerCase() ==
                                    specificFieldName.toLowerCase()
                            );
                            if (field) {
                                type = field.type;
                            }
                        }
                    }
                } catch (e) {
                    console.error(e);
                }
                configureColumnVisibility(
                    curCol,
                    specificFieldName,
                    type,
                    fColorize,
                    fHideUnused
                );
            });
        datatable.columns.adjust();
    }
    bulkEditColorizeColumns(datatable.node(), fColorize);
}
function showBulkEditRequiredFields(editor) {
    $.each(editor.order(), function (idx, fname) {
        let fields = selectedProfile.fields || [];
        let field = fields.find(
            (element) => element.excelName.toLowerCase() === fname.toLowerCase()
        );
        if (field) {
            // Check if the field value is blank
            if (
                field.type === "M" ||
                field.type === "C" ||
                field.type === "A" ||
                field.type === "I"
            ) {
                editor.field(fname).show();
                return;
            }
        } else {
            //console.log(`field not found: ${fname}`);
        }
        // Hide the field
        editor.field(fname).hide();
    });
}

function AddSelectDefaults(select, addEmpty, addNotEmpty) {
    if (
        addEmpty &&
        (typeof addNotEmpty === "undefined" || addNotEmpty === true)
    ) {
        select.prepend(
            '<option value="(empty)">(empty)</option><option value="(not empty)">(not empty)</option>'
        );
    } else if (addEmpty === true) {
        select.prepend('<option value="(empty)">(empty)</option>');
    }
    select.prepend('<option value="" selected="selected"></option>');
}
function getfn_datatable_asearch(id) {
    return function () {
        var column = this;
        var select = $(
            '<input class="customFilter" width="90%" type="text" placeholder="Search" />'
        )
            .appendTo(
                $("#filterRow-" + id + " th")
                    .eq(column.index())
                    .empty()
            )
            .on("change", function () {
                let col = getTransposedColumn(column);
                var val = $(this).val();
                col.search(val ? val : "", false, false).draw();
            });
    };
}
function getfn_datatable_usersearch(id) {
    return function () {
        var column = this;
        var select = $(
            '<select class="customFilter auditFilter" width="90%"><option value="">Select User</option></select>'
        )
            .appendTo(
                $("#filterRow-" + id + " th")
                    .eq(column.index())
                    .empty()
            )
            .on("change", function () {
                let col = getTransposedColumn(column);
                var val = $(this).val();
                col.search(val ? val : "", false, false).draw();
            });

        $.ajax({
            url: "/Vendor/GetAssignedUsersDataForCombobox",
            type: "GET",
            success: function (data) {
                if (data) {
                    data.forEach(function (item) {
                        select.append(
                            $("<option>", {
                                value: item.displayName,
                                text: item.displayName,
                            })
                        );
                    });
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Error fetching data for combobox:", errorThrown);
            },
        });
    };
}
function getfn_datatable_outcomesearch(id) {
    return function () {
        var column = this;
        var select = $(
            '<select class="customFilter auditFilter" width="90%"><option value="">Select Outcome</option></select>'
        )
            .appendTo(
                $("#filterRow-" + id + " th")
                    .eq(column.index())
                    .empty()
            )
            .on("change", function () {
                let col = getTransposedColumn(column);
                var val = $(this).val();
                col.search(val ? val : "", false, false).draw();
            });

        $.ajax({
            url: "/Vendor/GetAllAuditOutcomeComboboxData",
            type: "GET",
            success: function (data) {
                if (data) {
                    let filterData = getAuditType(id);
                    let filteredData = data
                        .filter((item) => item.auditType === filterData)
                        .sort((a, b) => a.auditOrder - b.auditOrder);
                    filteredData.forEach(function (item) {
                        select.append(
                            $("<option>", {
                                value: item.name,
                                text: item.name,
                            })
                        );
                    });
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Error fetching data for combobox:", errorThrown);
            },
        });
    };
}
function getfn_datatable_afilter(id, emptySupported, colFilters) {
    return function () {
        var column = this;
        var filterRowSelector = `#filterRow-${id} th`;
        var $colHdr = $(filterRowSelector).eq(column.index());

        // get the current filter value to set the default
        var oldVal = $colHdr.find("select").val() || "";

        var select = $('<select class="customFilter"></select>').appendTo(
            $colHdr.empty()
        );

        var colName = column.dataSrc();
        if (colName && colFilters) {
            if (
                !(colName === undefined) &&
                !(colFilters[colName] === undefined)
            ) {
                var filterCount = colFilters[colName].length;
                var addEmpty = false;
                $.each(colFilters[colName], function (index, value) {
                    if (isNull(value) || isEmptyString(value)) {
                        if (filterCount > 1 && emptySupported) {
                            addEmpty = true;
                        }
                    } else {
                        var v = value;
                        if (v.length > 50) {
                            v = v.substr(v, 47) + "...";
                        }
                        select.append(
                            '<option value="' +
                                htmlEncode(value) +
                                '">' +
                                htmlEncode(v) +
                                "</option>"
                        );
                    }
                });
                AddSelectDefaults(select, addEmpty);
            }
        }

        $($colHdr).find("select").val(oldVal);
        select.on("change", function () {
            let col = getTransposedColumn(column);
            var val = $(this).val();
            if (typeof val === "undefined") val = "";
            col.search(val, false, false).draw();
        });
    };
}
function getfn_datatable_dsearch(id) {
    return function () {
        var column = this;

        var selectDiv = null;
        let simpleSelectSearch = "";

        simpleSelectSearch =
            '<div style="width:150px"><input class="customFilter simple" style="width:120px" type="text" placeholder="Search" /></div>';

        selectDiv = $(
            `<div class="filterToggle">${simpleSelectSearch}<div class="customFilter advanced"><input id="min" class="mindate" type="date"></input><input id="max" class="maxdate" type="date"></input></div></div>`
        ).appendTo(
            $("#filterRow-" + id + " th")
                .eq(column.index())
                .empty()
        );
        selectDiv.find("input.simple").on("change", function () {
            let col = getTransposedColumn(column);
            var val = $(this).val();
            col.search(val ? val : "", false, false).draw();
        });
        selectDiv
            .find("div")
            .find("#min, #max")
            .on("change", function () {
                if ($(this).is(":visible")) {
                    let col = getTransposedColumn(column);
                    var fromval = $(this).parent().find("#min").val();
                    var toval = $(this).parent().find("#max").val();
                    var rangeVal = "";
                    if (fromval !== "" || toval !== "")
                        rangeVal = fromval + ";-to-;" + toval;

                    col.search(rangeVal ? rangeVal : "", false, false).draw();
                }
            });
    };
}
function getfn_datatable_reqno_search(id) {
    return function () {
        var column = this;
        var select = $(
            '<input style="width:80px" class="customFilter" type="text" onkeypress="reqnoValidate(event)"  placeholder="Search" />'
        )
            .appendTo(
                $("#filterRow-" + id + " th")
                    .eq(column.index())
                    .empty()
            )
            .on("change", function () {
                let col = getTransposedColumn(column);
                var val = $(this).val();
                col.search(val ? val : "", false, false).draw();
            });
    };
}
function reqnoValidate(evt) {
    var theEvent = evt || window.event;

    // Handle paste
    if (theEvent.type === "paste") {
        key = event.clipboardData.getData("text/plain");
    } else {
        // Handle key press
        if (theEvent.which === 13 && !theEvent.shiftKey) {
            theEvent.returnValue = false;
            theEvent.target.dispatchEvent(
                new Event("change", { cancelable: true })
            );
            theEvent.preventDefault();
            return;
        }
        var key = theEvent.keyCode || theEvent.which;
        key = String.fromCharCode(key);
    }
    var regex = /[0-9]|;/;
    if (!regex.test(key)) {
        theEvent.returnValue = false;
        if (theEvent.preventDefault) theEvent.preventDefault();
    }
}
const masterStatesList = [
    "AL",
    "AK",
    "AZ",
    "AR",
    "CA",
    "CO",
    "CT",
    "DE",
    "DC",
    "FL",
    "GA",
    "GU",
    "HI",
    "ID",
    "IL",
    "IN",
    "IA",
    "KS",
    "KY",
    "LA",
    "ME",
    "MD",
    "MA",
    "MI",
    "MN",
    "MS",
    "MO",
    "MT",
    "NE",
    "NV",
    "NH",
    "NJ",
    "NM",
    "NY",
    "NC",
    "ND",
    "OH",
    "OK",
    "OR",
    "PA",
    "PR",
    "RI",
    "SC",
    "SD",
    "TN",
    "TX",
    "UT",
    "VT",
    "VA",
    "WA",
    "WV",
    "WI",
    "WY"
];

function getAllStatesSelectFilter() {

    let select = $(
        '<select class="customFilter"><option value=""></option></select>'
    );

    masterStatesList.forEach((item) => {
        select.append(`<option value="${item}">${item}</option>`);
    });

    return select;
}
function getStatusSelectFilter(inc_cancelled, colFilters) {
    inc_cancelled = inc_cancelled || false;

    let select = $(
        '<select class="customFilter"><option value=""></option></select>'
    );
    var colName = "statusId";
    if (
        colName &&
        colFilters &&
        !(typeof colFilters === "undefined") &&
        !(typeof colFilters[colName] === "undefined")
    ) {
        var filterValues = colFilters[colName];

        if ($.inArray("0", filterValues) >= 0) {
            select.append('<option value="0">Pending</option>');
        }
        if ($.inArray("1", filterValues) >= 0) {
            select.append('<option value="1">Active</option>');
        }
        if ($.inArray("2", filterValues) >= 0) {
            select.append('<option value="2">Completed</option>');
        }
        if ($.inArray("9", filterValues) >= 0) {
            select.append('<option value="9">Hold</option>');
        }
        if ($.inArray("10", filterValues) >= 0) {
            select.append('<option value="10">Cancelled</option>');
        }
    } else {
        select.append('<option value="0">Pending</option>');
        select.append('<option value="1">Active</option>');
        select.append('<option value="2">Completed</option>');
        select.append('<option value="9">Hold</option>');
        if (inc_cancelled) {
            select.append('<option value="10">Cancelled</option>');
        }
    }
    return select;
}
function getfn_datatable_allStatesFilter(id) {
    return function () {
        var column = this;
        var filterRowSelector = `#filterRow-${id} th`;
        var $colHdr = $(filterRowSelector).eq(column.index());

        // get the current filter value to set the default
        var oldVal = $colHdr.find("select").val();

        var select = getAllStatesSelectFilter();
        select.val(oldVal);
        select.appendTo($colHdr.empty());

        select.on("change", function () {
            let col = getTransposedColumn(column);
            var val = $(this).val();
            col.search(val ? val : "", false, false).draw();
        });
    };
}
function getfn_datatable_statusfilter(
    id,
    defaultValue,
    inc_cancelled,
    colFilters
) {
    return function () {
        var column = this;
        var filterRowSelector = `#filterRow-${id} th`;
        var $colHdr = $(filterRowSelector).eq(column.index());

        // get the current filter value to set the default
        var oldVal = $colHdr.find("select").val();

        if (typeof defaultValue === "undefined") {
            defaultValue = "";
        }
        if (typeof oldVal === "undefined") {
            oldVal = defaultValue;
        }
        var select = getStatusSelectFilter(inc_cancelled, colFilters);
        select.val(oldVal);
        select.appendTo($colHdr.empty());

        select.on("change", function () {
            let col = getTransposedColumn(column);
            var val = $(this).val();
            col.search(val ? val : "", false, false).draw();
        });
    };
}

const ProcessStageIDs_Pending = 1;
const ProcessStageIDs_Incoming = 2;
const ProcessStageIDs_Signing = 3;
const ProcessStageIDs_ReadyToBeProcessed = 4;
const ProcessStageIDs_ReadyForPacking = 5;
const ProcessStageIDs_ReceiveFromDMV = 6;
const ProcessStageIDs_ShipToLienholder = 7;
const ProcessStageIDs_SendToVendor = 8;
const ProcessStageIDs_TitlePending = 9;
//const ProcessStageIDs_Invoicing = 10;
const ProcessStageIDs_InTransit = 11;
const ProcessStageIDs_NotReadyForProcessing = 12;
const ProcessStageIDs_InProcessing = 13;
const ProcessStageIDs_ReadyForPrinting = 14;

const stageOrder = [
    ProcessStageIDs_Pending,
    //ProcessStageIDs_SendToVendor
    ProcessStageIDs_Incoming,
    ProcessStageIDs_Signing,
    ProcessStageIDs_NotReadyForProcessing,
    ProcessStageIDs_ReadyToBeProcessed,
    ProcessStageIDs_InProcessing,
    ProcessStageIDs_ReadyForPrinting,
    ProcessStageIDs_ReadyForPacking,
    ProcessStageIDs_TitlePending,
    ProcessStageIDs_InTransit,
    ProcessStageIDs_ReceiveFromDMV,
    ProcessStageIDs_ShipToLienholder,
];
function getfn_datatable_stagefilter(id, colFilters) {
    return function () {
        var column = this;
        var filterRowSelector = `#filterRow-${id} th`;
        var $colHdr = $(filterRowSelector).eq(column.index());
        var colName = column.dataSrc();

        var oldVal = $colHdr.find("select").val() || "";

        var select = $(
            '<select class="customFilter"><option value=""></option>'
        );

        if (colName && colFilters) {
            if (
                !(typeof colName === "undefined") &&
                !(typeof colFilters[colName] === "undefined")
            ) {
                var filterValues = colFilters[colName];

                for (let i = 0; i < stageOrder.length; i++) {
                    if ($.inArray(`${stageOrder[i]}`, filterValues) >= 0) {
                        select.append(
                            '<option value="' +
                                stageOrder[i] +
                                '">' +
                                getStageName(stageOrder[i]) +
                                "</option>"
                        );
                    }
                }
            }
        } else {
            // add all stages
            let i = 0;
            for (i = 0; i < stageOrder.length; i++) {
                select.append(
                    '<option value="' +
                        stageOrder[i] +
                        '">' +
                        getStageName(stageOrder[i]) +
                        "</option>"
                );
            }
        }
        select.appendTo($colHdr.empty());

        select.val(oldVal);

        select.on("change", function () {
            let col = getTransposedColumn(column);
            var val = $(this).val();
            col.search(val ? val : "", false, false).draw();
        });
    };
}
function getfn_datatable_clientref(id) {
    return function () {
        var column = this;
        var title = "Ref#";
        var select = $(
            '<input class="customFilter" type="text" class="clientRefSearch" />'
        )
            .attr("placeholder", "Search " + title)
            .appendTo(
                $("#filterRow-" + id + " th")
                    .eq(column.index())
                    .empty()
            )
            .on("change", function () {
                let col = getTransposedColumn(column);
                var val = $(this).val();
                col.search(val).draw();
            });
    };
}

function getfn_datatable_vin(id, AllowMultilineVin) {
    return function () {
        var column = this;
        var title = "VIN";
        if (AllowMultilineVin) {
            let select = $(
                '<textarea class="customFilter multivinSearch" rows="1" />'
            )
                .attr("placeholder", "Search " + title)
                .appendTo(
                    $("#filterRow-" + id + " th")
                        .eq(column.index())
                        .empty()
                )
                .on("change keypress", function (e) {
                    if (
                        e.type === "change" ||
                        (e.type === "keypress" && e.which === 13 && !e.shiftKey)
                    ) {
                        let col = getTransposedColumn(column);
                        if (e.type === "keypress") e.preventDefault();
                        var values = $(this)
                            .val()
                            .split(/\r\n|\r|\n/);
                        var lines = [];
                        for (let i = 0; i < values.length; i++) {
                            var x = values[i].trim();
                            if (x.length > 0) {
                                lines.push(x);
                            }
                        }
                        var val = "";
                        if (lines.length > 0) val = lines.join("\r\n");
                        col.search(val ? val : "", false, false).draw();
                    }
                });
        } else {
            let select = $('<input class="customFilter" type="text" />')
                .attr("placeholder", "Search " + title)
                .appendTo(
                    $("#filterRow-" + id + " th")
                        .eq(column.index())
                        .empty()
                )
                .on("change", function () {
                    let col = getTransposedColumn(column);
                    var val = $(this).val();
                    col.search(val ? val : "", false, false).draw();
                });
        }
    };
}

function datatable_setup_select_deselect(table) {
    table.on("select deselect", function (e, dt, type, indexes) {
        if (type === "row") {
            var items = table.rows({ selected: true }).data();
            if (items.length === 0) {
                table.buttons(".selectOnOff").disable();
            } else if (items.length === 1) {
                table.buttons(".selectOnOff").enable();
            } else {
                table.buttons(".selectOnOff").enable();
                table.buttons(".selectOnOff.selectOne").disable();
            }
        }
    });
    table.buttons(".selectOnOff").disable();
}

function datatable_filter_setup(table, id, advanced) {
    var filterrowid = "#filterRow-" + id;
    var $filterrow = $(filterrowid);
    $filterrow.find(".clearFilter").on("click", function (e) {
        $filterrow.find("input.customFilter").val("");
        $filterrow.find("select.customFilter").val("");
        $filterrow.find(".followuptagselect").val([]);
        $filterrow.find(".followuptagselect").trigger("change");
        $filterrow
            .find(".followuptagselect")
            .parent()
            .trigger("hide.bs.dropdown");
        $filterrow.find(".followupcontactselect").val([]);
        $filterrow.find(".followupcontactselect").trigger("change");
        $filterrow
            .find(".followupcontactselect")
            .parent()
            .trigger("hide.bs.dropdown");
        table.search("").columns().search("").draw();
    });
    $filterrow.find(".advancedFilter").on("click", function (e) {
        $filterrow.find(".filterToggle").toggleClass("advancedOn");
        table.columns.adjust().draw();
        table.draw();
    });
}
function datatable_multivin_search_setup(table) {
    $(table)
        .find(".multivinSearch")
        .on("input", function (e) {
            if (e.which === 13 && !e.shiftKey) {
                e.target.dispatchEvent(
                    new Event("change", { cancelable: true })
                );
                e.preventDefault();
            }
        });
}
function datatable_rowCallback_titlePending(row, data) {
    let dt = data.dateToDmv || null;
    if (dt === null) return;

    if (data.statusId !== 1) return;

    let date = moment.utc(dt);
    let now = moment.utc();
    let diff = now.diff(date, "days");
    if (diff <= 30) {
        // not past due
    } else if (diff >= 60) {
        $(row).addClass("toDmv60days");
    } else if (diff >= 30) {
        $(row).addClass("toDmv30days");
    }
}
function datatable_rowCallback_etaCheck(row, data) {
    let eta = data.eta || null;
    if (eta === null) return;

    if (data.statusId !== 1) return;

    let etaDate = moment.utc(eta);
    let now = moment.utc();
    let diff = etaDate.diff(now, "days");
    if (diff >= 0) {
        // not past due
    } else if (diff >= -2) {
        $(row).addClass("etaPastDue1dayRow");
    } else {
        $(row).addClass("etaPastDueRow");
    }
}
function datatable_rowCallback_unbilled(row, data) {
    if (typeof vendorCurrentInvoiceIds !== "undefined") {
        if (vendorCurrentInvoiceIds.includes(data.requestId)) {
            $(row).addClass("billedRow");
        } else {
            $(row).removeClass("billedRow");
        }
    }
}
function isString(variable) {
    return typeof variable === "string" || variable instanceof String;
}

function isNumber(variable) {
    return typeof variable === "number" || variable instanceof Number;
}

function isBoolean(variable) {
    return typeof variable === "boolean";
}

function isArray(variable) {
    return Array.isArray(variable);
}

function isFunction(variable) {
    return typeof variable === "function";
}

function isObject(variable) {
    return variable !== null && typeof variable === "object";
}

function isNull(variable) {
    return variable === null;
}

function isUndefined(variable) {
    return variable === undefined;
}
function isNotBlankOrEmpty(val) {
    return val !== "" && val !== "(empty)" && val !== "(not empty)";
}
function isEmptyString(val) {
    if (typeof val === "string" && val === "") return true;
    return false;
}
/*function isNull(val) {
    if (typeof val === 'object' && val === null)
        return true;
    return false;
}
*/
function datatable_group_shiptovendor_sec425(api) {
    var sec425 =
        "By submitting this spreadsheet, under the penalties of perjury, " +
        "I affirm that I have personal knowledge of each application submitted, " +
        "and affirm that I have complied with Section 425(1) of the New York State Vehicle " +
        "and Traffic Law and/or any applicable laws in the State where the vehicle was repossessed, " +
        "pertaining to the repossession of the vehicle described above. " +
        "I further affirm that, if there are any other open perfected liens on the vehicle, " +
        "and a lien release is not obtained, I have advised the purchaser of the outstanding liens on the vehicle. ";
    $("#sec425").html("<span><i>" + htmlEncode(sec425) + "</i><span>");
}

// allDocuments node contains { displayName, attachmentId, attachmentTypeId }
var vendorDocuments = {
    vendorId: null,
    allDocuments: [],
    lastAttachmentTypeId: null,
    lastSelections: [],
};

function vendor_getVendorDocuments(excelName, vendorId, attachmentTypeId) {
    if (
        vendorDocuments.vendorId === vendorId &&
        vendorDocuments.attachmentTypeId === attachmentTypeId
    ) {
        return vendorDocuments.lastSelections;
    }
    var vals = [];

    var formdata = new FormData();
    formdata.append("vendorId", vendorId);
    //formdata.append('attachmentTypeId', attachmentTypeId); // don't pass this, we want all
    formdata.append("isClientDocument", false);

    $.ajax({
        url: "/Vendor/GetInternalAttachments",
        type: "POST",
        data: formdata,
        dataType: "json",
        async: false,
        cache: false,
        contentType: false,
        processData: false,
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
    }).done(function (data) {
        data.forEach(function (item, idx) {
            if (item.attachmentTypeId === attachmentTypeId) {
                vals.push({
                    label: item.displayName,
                    value: item.attachmentId,
                    attachmentTypeId: item.attachmentTypeId,
                });
            }
        });

        vendorDocuments = {
            vendorId: vendorId,
            allDocuments: data,
            lastAttachmentTypeId: attachmentTypeId,
            lastSelections: vals,
        };
    });

    return vals;
}
var groupDocuments = {
    vendorId: null,
    allDocuments: [],
    lastGroupId: null,
    lastAttachmentTypeId: null,
    lastSelections: [],
};
function vendor_getGroupDocuments(
    excelName,
    vendorId,
    attachmentTypeId,
    refresh
) {
    if (refresh === undefined) refresh = true;

    let groupId = $("#vendorGroupId").val();

    if (
        !refresh &&
        groupDocuments.vendorId === vendorId &&
        groupDocuments.lastAttachmentTypeId === attachmentTypeId &&
        groupDocuments.lastGroupId === groupId
    ) {
        return groupDocuments.lastSelections;
    }

    if (refresh) {
        var formdata = new FormData();
        formdata.append("vendorId", vendorId);
        //formdata.append('groupId', groupId);
        //formdata.append('attachmentTypeId', attachmentTypeId); // don't pass this, we want all
        formdata.append("isClientDocument", true);

        $.ajax({
            url: "/Vendor/GetInternalAttachments",
            type: "POST",
            data: formdata,
            dataType: "json",
            async: false,
            cache: false,
            contentType: false,
            processData: false,
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
        }).done(function (data) {
            groupDocuments = {
                vendorId: vendorId,
                allDocuments: data,
                lastAttachmentTypeId: attachmentTypeId,
                lastSelections: null,
                lastGroupId: groupId,
            };
        });
    }
    let vals = [];
    groupDocuments.allDocuments.forEach(function (item, idx) {
        if (
            item.groupId === groupId &&
            item.attachmentTypeId === attachmentTypeId
        ) {
            vals.push({
                label: item.displayName,
                value: item.attachmentId,
                attachmentTypeId: item.attachmentTypeId,
            });
        }
    });
    groupDocuments.lastSelections = vals;
    groupDocuments.lastGroupId = groupId;
    groupDocuments.lastAttachmentTypeId = attachmentTypeId;

    return vals;
}
function getInternalGroupAttachmentFields(editor, groupId) {
    return getInternalAttachmentFields(editor, groupId, true);
}
function getInternalVendorAttachmentFields(editor) {
    return getInternalAttachmentFields(editor, null, false);
}
function getInternalAttachmentFields(editor, groupId, isGroupDoc) {
    var result = [];
    var excelNames = editor.fields();
    for (var i = 0; i < excelNames.length; i++) {
        var excelName = excelNames[i];
        var field = editor.field(excelName);

        if (isGroupDoc) {
            if (excelName.startsWith(constGroupInternalDocumentPrefix)) {
                let names = vendor_getGroupAttachmentNames(groupId, excelName);
                let opts = [];
                names.forEach(function (item, idx) {
                    opts.push({
                        label: item.attachmentName,
                        value: item.attachmentId,
                    });
                });
                if (opts.length > 0) {
                    let hasBlank = opts.some(function (item) {
                        item.value === "";
                    });
                    if (!hasBlank) {
                        // TBD: Should we add a non-blank label for the first item?
                        opts.unshift({ label: "", value: "" });
                    }
                }
                field.update(opts);
            }
        } else {
            if (excelName.startsWith(constVendorInternalDocumentPrefix)) {
                let names = vendor_getVendorAttachmentNames(excelName);
                let opts = [];
                names.forEach(function (item, idx) {
                    opts.push({
                        label: item.attachmentName,
                        value: item.attachmentId,
                    });
                });
                if (opts.length > 0) {
                    let hasBlank = opts.some(function (item) {
                        item.value === "";
                    });
                    if (!hasBlank) {
                        // TBD: Should we add a non-blank label for the first item?
                        opts.unshift({ label: "", value: "" });
                    }
                }
                field.update(opts);
            }
        }
    }
    return result;
}
function updateAttachmentSelects(editor, groupId) {
    var vendorAttachmentFields = getInternalVendorAttachmentFields(editor);

    if (groupId) {
        var groupAttachmentFields = getInternalGroupAttachmentFields(
            editor,
            groupId
        );

        groupAttachmentFields.forEach((item, index) => {
            let fldname = field.name();
            // TODO: Where do these values come from?
            let vendorId;
            let excelName;
            let attachmentTypeId;
            let options = vendor_getGroupDocuments(
                excelName,
                vendorId,
                attachmentTypeId,
                false
            );
            editor.field(excelName).update(options);
        });
    } else {
        // hide all but vendor internal document fields
        showBulkEditVendorDocumentFields(editor);
    }
}
function showBulkEditVendorDocumentFields(editor) {
    $.each(editor.order(), function (idx, fname) {
        let fields = vendor_vendorAttachmentNames || [];
        let field = fields.find(
            (element) => element.excelName.toLowerCase() === fname.toLowerCase()
        );
        if (field) {
            editor.field(fname).show();
        }
    });
}
function getfn_datatable_processingDaysearch(id) {
    return function () {
        var column = this;
        var select = $(
            '<select class="customFilter auditFilter" width="90%"><option value="">Select day</option></select>'
        )
            .appendTo(
                $("#filterRow-" + id + " th")
                    .eq(column.index())
                    .empty()
            )
            .on("change", function () {
                let col = getTransposedColumn(column);
                var val = $(this).val();
                col.search(val ? val : "", false, false).draw();
            });

        $.ajax({
            url: "/Vendor/GetProcessingDay",
            type: "GET",
            success: function (data) {
                if (data) {
                    data.forEach(function (item) {
                        select.append(
                            $("<option>", {
                                value: item.dayid,
                                text: item.daydesc,
                            })
                        );
                    });
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("Error fetching data for combobox:", errorThrown);
            },
        });
    };
}

function addVerificationToken(xhr) {
    xhr.setRequestHeader(
        "RequestVerificationToken",
        $('input[name="__RequestVerificationToken"]').val()
    );
}
