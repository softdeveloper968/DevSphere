"use strict";

jQuery.extend(jQuery.fn.dataTableExt.oSort, {
    "text-vin-pre": function (a) {
        return a.substr(-6);
    },
    //,
    //"text-vin-asc": function (a, b) {
    //    return ((a < b) ? -1 : ((a > b) ? 1 : 0));
    //},

    //"text-vin-desc": function (a, b) {
    //    return ((a < b) ? 1 : ((a > b) ? -1 : 0));
    //}
});

//var nhtsaUrl = 'https://vpic.nhtsa.dot.gov/api/vehicles/DecodeVinValues/';
var vinLookupUrl = "/Vin/Lookup/";
//?format=json';
function getVinDetail(vin, $ctl, $form) {
    try {
        vin = vin || "";
        vin = vin.trim().toUpperCase();
        $ctl.removeClass("badvinflag badvinchecksum");
        $ctl.addClass("vinlookupflag");
        if (vin.length !== 17) {
            $ctl.removeClass("vinlookupflag");
            if (vin.length !== 0) {
                $ctl.addClass("badvinflag");
            }
            return;
        }
        $.ajax({
            url: vinLookupUrl + vin,
            type: "GET",
            async: true,
            complete: function () {},
            success: function (data) {
                $ctl.removeClass("badvinchecksum vinlookupflag");
                setVinFields(data, $ctl, $form);
            },
            error: function () {
                $ctl.removeClass("badvinchecksum vinlookupflag");
                $ctl.addClass("badvinflag");
            },
        });
    } catch (e) {}
}
function setVinFields(vinData, $ctl, $form) {
    var $dl = $ctl.parent().parent().find(".vin-correction-container");
    $dl.empty();
    if (typeof vinData !== "undefined") {
        if (vinData.validChecksum) {
            $ctl.removeClass("badvinchecksum vinlookupflag");
        } else {
            if (vinData.vpd.length === 0) {
                $ctl.removeClass("badvinchecksum");
                $ctl.addClass("badvinflag");
            } else {
                $ctl.removeClass("badvinflag");
                $ctl.addClass("badvinchecksum");
            }
        }
        if (vinData.vpd.length !== 0) {
            var v = vinData.vpd[0];

            if (v.fieldMappings) {
                $form = $form || $("#dynamicAppForm");
                let mappings = v.fieldMappings || [];
                Object.keys(mappings).forEach((key) => {
                    setIfEmpty($form, `[name="${key}"]`, mappings[key]);
                });
            }
        } else {
            if (vinData.possibleVins.length !== 0) {
                var $select = $dl.append("<select></select>");
                $.each(vinData.possibleVins, function () {
                    var $opt = $select.append("<option></option>");
                    $opt.val(this.Vin);
                    $opt.text(this.Desc);
                });
            }
        }
    } else {
        $ctl.removeClass("badvinchecksum");
        $ctl.addClass("badvinflag");
    }
}
function setIfEmpty($form, selector, value) {
    var $ctl = $form.find(selector);
    //var currentVal = $ctl.val() || "";
    // REVIEW: Should we not overwrite?  
    // if vin changed, probably should be updated
    //if (currentVal.trim() === "") {
        $ctl.val(value);
    //}
}
