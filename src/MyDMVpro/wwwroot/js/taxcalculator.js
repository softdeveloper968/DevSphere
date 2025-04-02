
function reloadTaxFormulaSection() {
    var selectedStateId = $('#stateSelect').val();
    var selectedJurisdictionId = $('#jurisdictionSelect').val();
    var selectedFormula = document.querySelector('input[name="formula"]:checked').value;
    if (selectedJurisdictionId && selectedStateId) {
        $.ajax({
            url: '/Tax/FetchFormula',
            type: 'POST',
            data: {
                stateId: selectedStateId,
                jurisdictionId: selectedJurisdictionId,
                selectedTaxFormula: selectedFormula
            },
            headers: {
                'RequestVerificationToken': $('[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                $('#currentFormulaDisplay').text(response.formula || response);
                $('#formulaDisplay').text('');
                // jqToast.success({
                //     text: 'Tax formula fetched successfully!'
                // });
            },
            error: function () {
                jqToast.error({
                    text: 'An error occurred while fetching the tax formula.'
                });
            }
        });
    }
}
function calculateTax(selectedStateId, selectedJurisdictionId, itemValues, requestId, streetAddress, zipCode) {
    return new Promise((resolve, reject) => {
        // Check if both jurisdiction and state are selected
        if (selectedJurisdictionId && selectedStateId) {
            // Prepare the payload
            let requestData = {
                stateId: selectedStateId,
                jurisdictionId: selectedJurisdictionId,
                items: itemValues
            };

            // If requestId is present, include it in the payload; otherwise, add streetAddress and zipCode
            if (requestId) {
                requestData.requestId = requestId;
            } else {
                requestData.streetAddress = streetAddress;
                requestData.zipCode = zipCode;
            }

            $.ajax({
                url: '/Tax/Calculate',
                type: 'POST',
                data: JSON.stringify(requestData),
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': $('[name="__RequestVerificationToken"]').val()
                },
                success: function (response) {
                    resolve(response); // Return the response
                },
                error: function (xhr) {
                    reject(new Error('An error occurred while calculating the tax.'));
                }
            });
        } else {
            reject(new Error('State ID and Jurisdiction ID are required.'));
        }
    });
}
function reloadAllFormulaSection() {
    var selectedStateId = $('#stateSelect').val();
    var selectedJurisdictionId = $('#jurisdictionSelect').val();
    if (selectedJurisdictionId && selectedStateId) {
        $.ajax({
            url: '/Tax/GetFormulas',
            type: 'GET',
            data: {
                stateId: selectedStateId,
                jurisdictionId: selectedJurisdictionId
            },
            headers: {
                'RequestVerificationToken': $('[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                $('#totalTaxableAmount').text(response.totalTaxableAmount || '');
                $('#totalTaxDue').text(response.totalTaxDue || '');
                $('#totalTaxPaidOtherState').text(response.totalTaxPaidOtherState || '');
                $('#totalLeftOverTitlingState').text(response.totalLeftOverTitlingState || '');
                // jqToast.success({
                //     text: 'Tax formula(s) fetched successfully!'
                // });
            },
            error: function () {
                // jqToast.error({
                //     text: 'An error occurred while fetching the tax formula(s).'
                // });
            }
        });
    }
}
function checkForChanges(row, itemId) {
    var isApplicableChanged = $("input[name='IsApplicable_" + itemId + "']").prop('checked') !== (row.data('originalIsApplicable') === 'true');
    var isRequiresTaxRateFetchChanged = $("input[name='RequiresTaxRateFetch_" + itemId + "']").prop('checked') !== (row.data('originalRequiresTaxRateFetch') === 'true');
    var taxableChanged = ($("input[name='Taxable_" + itemId + "']").val()) !== (row.data('originalTaxable'));
    var taxRateChanged = ($("input[name='TaxRate_" + itemId + "']").val()) !== (row.data('originalTaxRate'));
    var maxValueChanged = ($("input[name='MaxValue_" + itemId + "']").val()) !== (row.data('originalMaxValue'));
    return isApplicableChanged || taxableChanged || taxRateChanged || maxValueChanged || isRequiresTaxRateFetchChanged;
}
function toggleSection() {
    resetFields();
    var section = document.getElementById("calculateSection");
    if (section.style.display === "none") {
        section.style.display = "block"; // Show section
    } else {
        section.style.display = "none"; // Hide section
    }
}
function resetFields() {

    var itemValues = document.querySelectorAll('#itemGrid input[type="number"]');
    itemValues.forEach(function (input) {
        input.value = '';
    });

    // Reset tax due fields only within the itemGrid table
    var taxDueFields = document.querySelectorAll('#itemGrid input.tax-due');
    taxDueFields.forEach(function (input) {
        input.value = '';
    });
    var taxableAmountFields = document.querySelectorAll('#itemGrid input.taxable-amount');
    taxableAmountFields.forEach(function (input) {
        input.value = '';
    });
    // Reset total fields
    document.getElementById("calTotalTaxableAmount").value = '';
    document.getElementById("calTotalTaxDue").value = '';
    document.getElementById("calTotalTaxPaidOtherState").value = '';
    document.getElementById("calTotalLeftOverTitlingState").value = '';
    document.getElementById("streetAddressTR").value = '';
    document.getElementById("zipCodeTR").value = '';
    document.getElementById("taxBreakdownTR").innerHTML = "";
}
function setRandomValues() {
    var itemValues = document.querySelectorAll('#itemGrid input[type="number"]');
    itemValues.forEach(function (input) {
        var randomValue = Math.floor(Math.random() * (10000 - 1000 + 1)) + 1000;
        input.value = randomValue; // Set random value between 1000 and 10000
    });
}
function reloadTaxRulesSection() {
    var selectedStateId = $('#stateSelect').val();
    var selectedJurisdictionId = $('#jurisdictionSelect').val();

    if (selectedJurisdictionId && selectedStateId) {
        $.ajax({
            url: '/Tax/FetchRules',
            type: 'POST',
            data: {
                stateId: selectedStateId,
                jurisdictionId: selectedJurisdictionId
            },
            headers: {
                'RequestVerificationToken': $('[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                $('#taxRulesSection').html(response);
                $('#saveTaxRulesBtn').show();
                // jqToast.success({
                //     text: 'Tax rules fetched successfully!'
                // });
            },
            error: function () {
                $('#taxRulesSection').html('');
                $('#saveTaxRulesBtn').hide();
                jqToast.error({
                    text: 'An error occurred while fetching the tax rules.'
                });
            }
        });
    } else {
        $('#taxRulesSection').html('');
        $('#saveTaxRulesBtn').hide();
    }
}
function updateTaxUI(response) {
    const taxItems = response.calculatedTaxItems;
    taxItems.forEach(item => {
        const taxInput = document.querySelector(`input[name="taxDue_${item.itemId}"]`);
        if (taxInput) taxInput.value = item.taxDue;

        const taxableInput = document.querySelector(`input[name="taxableAmount_${item.itemId}"]`);
        if (taxableInput) taxableInput.value = item.taxableAmount;
    });

     document.getElementById('calTotalTaxableAmount').value =
         (isNaN(response.totalTaxableAmount) ? response.totalTaxableAmount : '$' + parseFloat(response.totalTaxableAmount).toString());

     document.getElementById('calTotalTaxDue').value =
         (isNaN(response.totalTaxDue) ? response.totalTaxDue : '$' + parseFloat(response.totalTaxDue).toString());

     document.getElementById('calTotalTaxPaidOtherState').value =
         (isNaN(response.totalTaxPaidOtherState) ? response.totalTaxPaidOtherState : '$' + parseFloat(response.totalTaxPaidOtherState).toString());

     document.getElementById('calTotalLeftOverTitlingState').value =
         (isNaN(response.totalLeftOverTitlingState) ? response.totalLeftOverTitlingState : '$' + parseFloat(response.totalLeftOverTitlingState).toString());
}
function toggleTaxOptions() {
    var isApiFetchEnabled = $('#enableTaxRateFetchFromApiCheckbox').prop('checked');

    // Enable or disable other checkboxes based on API fetch checkbox state
    $('#includeStateTaxCheckbox, #includeCityTaxCheckbox, #includeSpecialDistrictTaxCheckbox, #includeCountyTaxCheckbox')
        .prop('disabled', !isApiFetchEnabled);

    // If API fetch is disabled, uncheck all checkboxes
    if (!isApiFetchEnabled) {
        $('#includeStateTaxCheckbox, #includeCityTaxCheckbox, #includeSpecialDistrictTaxCheckbox, #includeCountyTaxCheckbox')
            .prop('checked', false);
    }
}
function fetchStateConfig() {
    var selectedState = $('#stateSelect').val();
    if (!selectedState) return;

    $.ajax({
        url: '/Tax/StateConfig',
        type: 'GET',
        data: { stateAbbreviation: selectedState },
        headers: {
            'RequestVerificationToken': $('[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            // Update checkboxes based on the fetched data
            $('#enableTaxRateFetchFromApiCheckbox').prop('checked', response.enableTaxRateFetchFromApi);
            $('#includeStateTaxCheckbox').prop('checked', response.includeStateTax);
            $('#includeCityTaxCheckbox').prop('checked', response.includeCityTax);
            $('#includeSpecialDistrictTaxCheckbox').prop('checked', response.includeSpecialDistrictTax);
            $('#includeCountyTaxCheckbox').prop('checked', response.includeCountyTax);

            // Apply logic to enable/disable options
            toggleTaxOptions();
        },
        error: function () {
            $('#enableTaxRateFetchFromApiCheckbox').prop('checked', false).prop('disabled', false);
            $('#includeStateTaxCheckbox, #includeCityTaxCheckbox, #includeSpecialDistrictTaxCheckbox, #includeCountyTaxCheckbox')
                .prop('checked', false)
                .prop('disabled', true);
        }
    });
}
function saveStateConfig() {
    var selectedState = $('#stateSelect').val();
    if (!selectedState) return;

    var isApiFetchEnabled = $('#enableTaxRateFetchFromApiCheckbox').prop('checked');

    var configData = {
        StateAbbreviation: selectedState,
        EnableTaxRateFetchFromApi: isApiFetchEnabled,
        IncludeStateTax: isApiFetchEnabled ? $('#includeStateTaxCheckbox').prop('checked') : false,
        IncludeCityTax: isApiFetchEnabled ? $('#includeCityTaxCheckbox').prop('checked') : false,
        IncludeSpecialDistrictTax: isApiFetchEnabled ? $('#includeSpecialDistrictTaxCheckbox').prop('checked') : false,
        IncludeCountyTax: isApiFetchEnabled ? $('#includeCountyTaxCheckbox').prop('checked') : false
    };

    $.ajax({
        url: '/Tax/SaveStateConfig',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(configData),
        headers: {
            'RequestVerificationToken': $('[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            console.log("State config saved successfully");
        },
        error: function () {
            console.error("Error saving state config");
        }
    });
}
function createRow(type, name, tax, districts = []) {
    const row = $('<div>').addClass('tax-row');
    const typeCell = $('<div>').addClass('tax-cell').text(type);
    const nameCell = $('<div>').addClass('tax-cell').html(name || '&nbsp;');
    const taxCell = $('<div>').addClass('tax-cell').text(tax || "");
    const breakdown = $('<div>').addClass('hidden-content');
    if (type === "Total") {
        row.css({ 'font-weight': 'bold', 'background-color': '#d3d3d3' });
    }
    if (districts.length > 0) {
        const icon = $('<i>').addClass('fa fa-chevron-down expand-icon');
        nameCell.append(icon);

        districts.forEach(d => {
            const hrow = $('<div>').addClass('tax-row');
            const tyCell = $('<div>').addClass('tax-cell').css({ 'border': 'none' }).text(''); 
            const nCell = $('<div>').addClass('tax-cell').css({ 'background-color': '#d6d4d4', 'border': 'none' }).text(d.name || "");
            const tCell = $('<div>').addClass('tax-cell').css({ 'background-color': '#d6d4d4', 'border': 'none' }).text(d.tax || "");
            breakdown.append(hrow.append(tyCell, nCell, tCell));
        });

        nameCell.on('click', function () {
            const isVisible = breakdown.is(':visible');
            $('.hidden-content').slideUp();
            $('.expand-icon').removeClass('fa-chevron-up').addClass('fa-chevron-down');

            if (!isVisible) {
                breakdown.slideDown();
                icon.removeClass('fa-chevron-down').addClass('fa-chevron-up');
            }
        });
    }

    return $('<div>').append(row.append(typeCell, nameCell, taxCell), breakdown);
}
function populateTaxData(data,page) {
    if (!data) return;

    const taxContainer = $(`#taxBreakdown${page}`).empty();
    let totalTax = 0;
    const parseTax = (tax) => tax ? parseFloat(tax.replace('%', '')) || 0 : 0;

    // State Tax
    if (data.stateTax) {
        totalTax += parseTax(data.stateTax);
        taxContainer.append(createRow("State", data.stateJurisdictionName || "", data.stateTax));
    }

    // County Tax
    if (data.countyTax) {
        totalTax += parseTax(data.countyTax);
        taxContainer.append(createRow("County", data.countyJurisdictionName || "", data.countyTax, data.countyDistricts || []));
    }

    // City Tax
    if (data.cityTax) {
        totalTax += parseTax(data.cityTax);
        taxContainer.append(createRow("City", data.cityJurisdictionName || "", data.cityTax, data.cityDistricts || []));
    }

    // Special District Tax
    let specialDistrictTotal = 0;
    if (Array.isArray(data.specialDistricts) && data.specialDistricts.length > 0) {
        data.specialDistricts.forEach(sd => specialDistrictTotal += parseTax(sd.tax));
        totalTax += specialDistrictTotal;
        taxContainer.append(createRow("Special District", "", `${specialDistrictTotal.toFixed(4)}%`, data.specialDistricts));
    }

    // Total Tax
    if (totalTax !== 0) {
        taxContainer.append(createRow("Total", "", `${totalTax.toFixed(4)}%`));
    }
}
function bindExpandFunctionality() {
    $(".tax-cell").off("click").on("click", function () {
        const breakdown = $(this).closest(".tax-row").next(".hidden-content");
        const icon = $(this).find(".expand-icon");

        // Close all other breakdowns first
        $(".hidden-content").not(breakdown).slideUp();
        $(".expand-icon").not(icon).removeClass('fa-chevron-up').addClass('fa-chevron-down');

        // Toggle the clicked one
        if (breakdown.is(":visible")) {
            breakdown.slideUp();
            icon.removeClass('fa-chevron-up').addClass('fa-chevron-down');
        } else {
            breakdown.slideDown();
            icon.removeClass('fa-chevron-down').addClass('fa-chevron-up');
        }
    });
}
function positionModal($infoIcon,$taxBreakdownModal) {
    const iconRect = $infoIcon[0].getBoundingClientRect();
    const modalWidth = $modalForm.outerWidth();
    const modalHeight = $modalForm.outerHeight();
    const screenWidth = $(window).width();
    const screenHeight = $(window).height();

    let left = iconRect.right + 10; 
    let top = iconRect.top;

    if (left + modalWidth > screenWidth) {
        left = iconRect.left - modalWidth - 10; 
    }


    if (top + modalHeight > screenHeight) {
        top = screenHeight - modalHeight - 10; 
    }

    $modalForm.css({ left: left, top: top });
}