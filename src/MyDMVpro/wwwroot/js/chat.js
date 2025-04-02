"use strict";

function showChat(requestId, vin) {
    $.ajax({
        url: "/Chats/View/" + requestId,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (data) {
        let $mymodal = $("#modal-reqChat");
        $("#myModal-label").html(
            "CHAT: <span class='chat-vin'>" + vin + "</span>"
        );
        $mymodal.find("#chat-RequestId").val(requestId);
        let $mymodalbody = $mymodal.find(".modal-body");
        $mymodalbody.html(data);
        var hasUnreadChats = $mymodalbody
            .find("[data-unreadchats]")
            .data("unreadchats");
        if (typeof hasUnreadChats !== "undefined") {
            $mymodal
                .find("#chatMarkAsRead")
                .attr("disabled", hasUnreadChats !== "1");

            if(hasUnreadChats != 0){
                $mymodal.find("#chatMarkAsRead").attr("disabled", false);
            }
            else{
                $mymodal.find("#chatMarkAsRead").attr("disabled", true);
            }
        } else {
            $mymodal.find("#chatMarkAsRead").attr("disabled", true);
        }
        $mymodalbody.animate(
            { scrollTop: $mymodalbody.prop("scrollHeight") },
            1000
        );
        $mymodal.modal("show");
    });
    return false;
}

function refreshChats() {
    $("a[data-requestid]").each(function () {
        var reqid = $(this).data("requestid");
        if (reqid && reqid !== "") refreshChat(reqid);
    });
}
function refreshChat(reqid) {
    $.ajax({
        url: "/Chats/ChatStatus/" + reqid,
        type: "GET",
        async: true,
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (data) {
        let $linkElement = $("a[data-requestid='" + reqid + "']");
        $linkElement.removeClass("chatNew chatActive chatAlert chatWaiting");
        if (data === null) return;
        var isVendor =
            window.location.pathname.toLowerCase().indexOf("/vendor") > -1;
        if (isVendor) {
            if (data.waitingForVendorReply) {
                $linkElement.addClass("chatAlert");
            } else if (data.waitingForUserReply) {
                $linkElement.addClass("chatWaiting");
            } else if (data.hasActiveChat) {
                $linkElement.addClass("chatActive");
            } else if (data.hasNewChat) {
                $linkElement.addClass("chatNew");
            }
        } else {
            if (data.waitingForUserReply) {
                $linkElement.addClass("chatAlert");
            } else if (data.waitingForVendorReply) {
                $linkElement.addClass("chatWaiting");
            } else if (data.hasActiveChat) {
                $linkElement.addClass("chatActive");
            } else if (data.hasNewChat) {
                $linkElement.addClass("chatNew");
            }
        }
    });
    return false;
}

var lastUpdate;
function updateVendorChats() {
    updateChats(true);
}
function updateChats(isVendorPage) {
    var formData = new FormData();
    if (!(typeof lastUpdate === "undefined")) {
        formData.append("date", moment.utc(lastUpdate).format());
    }
    $.ajax({
        url: "/Chats/ChatStatusUpdates",
        type: "POST",
        async: true,
        dataType: "json",
        data: formData,
        cache: false,
        contentType: false,
        processData: false,
        headers: {
            RequestVerificationToken: $(
                '[name="__RequestVerificationToken"]'
            ).val(),
        },
    }).done(function (data) {
        $(data).each(function (index) {
            var item = data[index];
            var reqid = item.requestId;
            let $linkElement = $("a[data-requestid='" + reqid + "']");
            if ($linkElement) {
                $linkElement.removeClass(
                    "chatNew chatActive chatAlert chatWaiting"
                );
                if (isVendorPage) {
                    if (item.waitingForVendorReply) {
                        $linkElement.addClass("chatAlert");
                    } else if (item.waitingForUserReply) {
                        $linkElement.addClass("chatWaiting");
                    } else if (item.hasActiveChat) {
                        $linkElement.addClass("chatActive");
                    } else if (item.hasNewChat) {
                        $linkElement.addClass("chatNew");
                    }
                } else {
                    if (item.waitingForUserReply) {
                        $linkElement.addClass("chatAlert");
                    } else if (item.waitingForVendorReply) {
                        $linkElement.addClass("chatWaiting");
                    } else if (item.hasActiveChat) {
                        $linkElement.addClass("chatActive");
                    } else if (item.hasNewChat) {
                        $linkElement.addClass("chatNew");
                    }
                }
                if (
                    typeof lastUpdate === "undefined" ||
                    item.lastChatUpdate > lastUpdate
                ) {
                    lastUpdate = item.lastChatUpdate;
                }
            }
        });
    });
    return false;
}

const chatTimeoutInMinutes = 3;
function timerChatMenuRefresh() {
    $.ajax({
        url: "/Chats/Unread",
        type: "GET",
    }).done(function (response) {
        $("#mainNavMenuGroup").find("#messagesMenu").replaceWith(response);
    });

    var reviewMenu = $("#mainNavMenuGroup").find(".attachmentreview-menu");
    if (reviewMenu.length > 0) {
        // Include attachment review at same time
        $.ajax({
            url: "/AttachmentReview/ReviewCount",
            type: "GET",
        }).done(function (response) {
            reviewMenu.replaceWith(response);
        });
    }
    setTimeout(timerChatMenuRefresh, chatTimeoutInMinutes * 60 * 1000);
}

function initChatForm($chatsEditPanel, requestId, vin) {
    let $chatForm = $chatsEditPanel.find(".chatform");
    let $chatRequestId = $chatForm.find("#chat-RequestId");
    let $chatRefreshBtn = $chatForm.find("#chatRefreshBtn");
    let $chatMarkAsRead = $chatForm.find("#chatMarkAsRead");
    let $chatMsg = $chatForm.find("#chat-newMsg");
    $chatRequestId.val(requestId);

    $chatForm.off("submit").on("submit", function (e) {
        e.preventDefault();
        let msg = $chatMsg.val().trim();
        if (msg === "") return false;
        e.preventDefault();
        $.ajax({
            url: $chatForm.attr("action"),
            type: "POST",
            data: $chatForm.serialize(),
        }).done(function (data) {
            $chatMsg.val("");
            var $form = $chatsEditPanel;
            var $body = $form.find(".panel-body");
            $body.html(data);
            $body.animate({ scrollTop: $body.prop("scrollHeight") }, 1000);
        });
        return false;
    });

    $chatRefreshBtn.off("click").on("click", function (e) {
        e.stopPropogation();
        populateChat($chatsEditPanel, requestId, vin);
    });
    $chatMarkAsRead.off("click").on("click", function (e) {
        e.stopPropogation();
        var lastcreated = $chatsEditPanel
            .find("[data-lastcreated]")
            .data("lastcreated");

        var formdata = new FormData();
        formdata.append("id", requestId);
        formdata.append("lastcreated", lastcreated);
        $.ajax({
            url: "/Chats/MarkAsRead",
            type: "POST",
            headers: {
                RequestVerificationToken: $(
                    '[name="__RequestVerificationToken"]'
                ).val(),
            },
            data: formdata,
            cache: false,
            contentType: false,
            processData: false,
        }).done(function (data) {
            populateChat($chatsEditPanel, requestId, vin);
        });

        return false;
    });
    populateChat($chatsEditPanel, requestId, vin);
}
function populateChat($chatContainer, requestId, vin) {
    $.ajax({
        url: "/Chats/View/" + requestId,
        type: "GET",
        async: true,
        dataType: "text",
        cache: false,
        contentType: false,
        processData: false,
    }).done(function (data) {
        $chatContainer
            .find("#myModal-label")
            .html("Chat (VIN " + htmlEncode(vin) + ")");
        let $modalbody = $chatContainer.find(".chat-body");
        $chatContainer.find("#chat-RequestId").val(requestId);
        //console.log(data);
        $modalbody.html(data);
        var hasUnreadChats = $modalbody
            .find("[data-unreadchats]")
            .data("unreadchats");
        if (typeof hasUnreadChats !== "undefined") {
            $chatContainer
                .find("#chatMarkAsRead")
                .attr("disabled", hasUnreadChats !== "1");
        } else {
            $chatContainer.find("#chatMarkAsRead").attr("disabled", false);
        }
        $modalbody.animate(
            { scrollTop: $modalbody.prop("scrollHeight") },
            1000
        );
    });
    return false;
}

$(document).ready(function () {
    setTimeout(timerChatMenuRefresh, chatTimeoutInMinutes * 60 * 1000);
});
