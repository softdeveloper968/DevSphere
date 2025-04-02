"use strict";
function storageAvailable(type) {
    var storage;
    try {
        storage = window[type];
        var x = "__storage_test__";
        storage.setItem(x, x);
        storage.removeItem(x);
        return true;
    } catch (e) {
        return (
            e instanceof DOMException &&
            // everything except Firefox
            (e.code === 22 ||
                // Firefox
                e.code === 1014 ||
                // test name field too, because code might not be present
                // everything except Firefox
                e.name === "QuotaExceededError" ||
                // Firefox
                e.name === "NS_ERROR_DOM_QUOTA_REACHED") &&
            // acknowledge QuotaExceededError only if there's something already stored
            storage &&
            storage.length !== 0
        );
    }
}
var s_storageAvailable = storageAvailable("sessionStorage") || false;
function GetGlobalSetting(key, defaultVal) {
    if (s_storageAvailable) {
        return window.sessionStorage.getItem(key) || defaultVal;
    }
    return defaultVal;
}
function SaveGlobalSetting(key, value) {
    if (s_storageAvailable) {
        window.sessionStorage.setItem(key, value);
    }
}
function SaveDtSetting(prefix, key, value) {
    SaveGlobalSetting(`${prefix}_${key}`, value);
}
function GetDtSetting(prefix, key, defaultVal) {
    return GetGlobalSetting(`${prefix}_${key}`, defaultVal);
}
function RemoveGlobalSetting(key) {
    if (s_storageAvailable) {
        return window.sessionStorage.removeItem(key);
    }
}
function RemoveDtSetting(prefix, key) {
    RemoveGlobalSetting(`${prefix}_${key}`);
}
function ClearSettings(key, value) {
    if (s_storageAvailable) {
        return window.sessionStorage.clear();
    }
}
function SaveViewSettings(keyprefix, data) {
    SaveDtSetting(keyprefix, "expandNotes", data.expandNotes);
    SaveDtSetting(keyprefix, "includeArchived", data.includeArchived);
}
function LoadViewSettings(keyprefix, data) {
    data.expandNotes = GetDtSetting(keyprefix, "expandNotes", "false");
    data.includeArchived = GetDtSetting(keyprefix, "includeArchived", "false");
}
function GetExpandNotesButtonText(keyprefix) {
    if (GetDtSetting(keyprefix, "expandNotes", "false") === "true") {
        return "Collapse Notes";
    } else {
        return "Expand Notes";
    }
}
function GetIncludedArchivedButtonText(keyprefix) {
    if (GetDtSetting(keyprefix, "includeArchived", "false") === "true") {
        return "Exclude Archived";
    } else {
        return "Include Archived";
    }
}
function ToggleIncludeArchived(t, id) {
    var val = GetDtSetting(id, "includeArchived", "false");
    val = val === "true" ? "false" : "true";
    SaveDtSetting(id, "includeArchived", val);
    t.ajax.reload();
    return val;
}
