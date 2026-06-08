// ?????????????????????????????????????????????
// Deposit Form State
// Single source of truth for all deposit form selections
// ?????????????????????????????????????????????

const depositFormState = {
    id: null,
    cbCode: null,
    bouBranchId: null,
    bouBranchCode: null,
    depositDate: null,
    rtgsDate: null,
    rtgsCode: null,
    requiresSpecialApproval: false,
    specialApprovalReason: null,
    agentIds: [],         // exactly 2 required
    remarks: null,
    cbId: null,
    createdDate: null,
};

// ?????????????????????????????????????????????
// Branch selection
// ?????????????????????????????????????????????

function onDepositBouBranchSelected(id) {
    depositFormState.bouBranchId = id ? parseInt(id) : null;
}

// ?????????????????????????????????????????????
// Agent selection
// ?????????????????????????????????????????????

function onDepositAgentSelected(slot, id) {
    depositFormState.agentIds[slot - 1] = id || null;
}

// ?????????????????????????????????????????????
// Build denomination lines from deposit-denom-table.js
// Includes subCategoryId
// ?????????????????????????????????????????????
function buildDepositDenomLines() {
    const rows = window.depDtGetRows();
    return Object.values(rows)
        .filter(r => r.qty > 0 && r.denomValue > 0)
        .map(r => ({
            denominationId: r.denomId,
            denominationTypeId: r.typeId,
            denominationValue: r.denomValue,
            bundles: r.type === 'notes' ? r.qty : null,
            boxes: r.type === 'coins' ? r.qty : null,
            amount: r.qty * 1000 * r.denomValue,
            subCategoryId: r.subCategoryId,
        }));
}

// ?????????????????????????????????????????????
// Validation
// ?????????????????????????????????????????????

function validateDepositForm(isDraft) {
    const errors = [];

    if (!isDraft) {
        if (!depositFormState.bouBranchId)
            errors.push('Please select a bank branch.');

        if (depositFormState.agentIds.filter(Boolean).length !== 2)
            errors.push('Exactly 2 agents are required.');

        if (!window.depDtHasValue())
            errors.push('Please add at least one denomination.');

        if (!depositFormState.depositDate)
            errors.push('Please select a deposit date.');
    }

    return errors;
}

// ?????????????????????????????????????????????
// Show/Clear errors
// ?????????????????????????????????????????????

function showDepositErrors(errors) {
    let container = document.getElementById('validationErrors');

    if (!container) {
        container = document.createElement('div');
        container.id = 'validationErrors';
        container.className = 'alert alert-danger mb-4';
        document.getElementById('depositForm').prepend(container);
    }

    container.innerHTML = '<ul class="mb-0">'
        + errors.map(e => `<li>${e}</li>`).join('')
        + '</ul>';

    container.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function clearDepositErrors() {
    const container = document.getElementById('validationErrors');
    if (container) container.remove();
}

// ?????????????????????????????????????????????
// Get anti-forgery token
// ?????????????????????????????????????????????

function getDepositAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

// ?????????????????????????????????????????????
// Submit form
// ?????????????????????????????????????????????

async function submitDepositForm(action) {
    clearDepositErrors();

    const isDraft = action.includes('SaveDraft');
    const errors = validateDepositForm(isDraft);

    if (errors.length > 0) {
        showDepositErrors(errors);
        return;
    }

    const payload = {
        id: depositFormState.id,
        bouBranchId: parseInt(depositFormState.bouBranchId) || 0,
        cbCode: depositFormState.cbCode,
        bouBranchCode: depositFormState.bouBranchCode,
        depositDate: depositFormState.depositDate,
        rtgsDate: depositFormState.rtgsDate,
        rtgsCode: depositFormState.rtgsCode,
        requiresSpecialApproval: depositFormState.requiresSpecialApproval,
        specialApprovalReason: depositFormState.specialApprovalReason,
        agentIds: depositFormState.agentIds.filter(Boolean),
        denominations: buildDepositDenomLines(),
        remarks: depositFormState.remarks,
        cbId: parseInt(depositFormState.cbId) || 0,
    };

    setDepositButtonsDisabled(true);

    try {
        const res = await fetch(action, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getDepositAntiForgeryToken(),
            },
            body: JSON.stringify(payload),
        });

        if (res.ok) {
            const redirectUrl = await res.text();
            window.location.href = redirectUrl;
        } else {
            const data = await res.json();
            showDepositErrors(data.errors ?? ['An unexpected error occurred. Please try again.']);
            setDepositButtonsDisabled(false);
        }
    } catch {
        showDepositErrors(['A network error occurred. Please check your connection and try again.']);
        setDepositButtonsDisabled(false);
    }
}

function setDepositButtonsDisabled(disabled) {
    document.querySelectorAll('button[onclick^="submitDepositForm"]')
        .forEach(btn => btn.disabled = disabled);
}

// Special Approval

function isSameDayApprovalRequired() {
    // Always use Uganda time (EAT = UTC+3) regardless of browser locale
    const nowUganda = new Date(new Date().toLocaleString('en-US', { timeZone: 'Africa/Kampala' }));
    const isMonday = nowUganda.getDay() === 1;
    const before11 = nowUganda.getHours() < 11;

    // Monday before 11am EAT — grace period, no special approval
    if (isMonday && before11) return false;

    let today = new Date();
    const formatter = new Intl.DateTimeFormat('en-CA', {
        timeZone: 'Etc/GMT-3',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
    });

    depositFormState.createdDate = formatter.format(today);

    return depositFormState.createdDate !== null
        && depositFormState.depositDate !== null
        && depositFormState.createdDate === depositFormState.depositDate;
}

function checkSpecialApproval() {
    const warning = document.getElementById('specialApprovalWarning');
    console.log("Checking Exceptional Approval");
    if (isSameDayApprovalRequired()) {
        depositFormState.specialApprovalReason = null;
        new bootstrap.Modal(document.getElementById('specialApprovalModal')).show();
        //bootstrap.Modal.getOrCreateInstance(document.getElementById('specialApprovalModal')).show();
        console.log("After Model showing");
    } else {
        depositFormState.requiresSpecialApproval = false;
        depositFormState.specialApprovalReason = null;
        if (warning) warning.style.setProperty('display', 'none', 'important');
    }
}

function onSpecialApprovalConfirmed() {
    const reasonInput = document.getElementById('specialApprovalReasonInput');
    const reasonError = document.getElementById('specialApprovalReasonError');
    const warning = document.getElementById('specialApprovalWarning');
    const reason = reasonInput.value.trim();

    if (!reason) {
        reasonError.style.display = 'block';
        return;
    }

    reasonError.style.display = 'none';
    depositFormState.specialApprovalReason = reason;
    depositFormState.requiresSpecialApproval = true;

    document.getElementById('specialApprovalReasonDisplay').textContent = reason;

    bootstrap.Modal.getInstance(document.getElementById('specialApprovalModal')).hide();

    if (warning) warning.style.setProperty('display', 'flex', 'important');
}

function editSpecialApprovalReason() {
    const input = document.getElementById('specialApprovalReasonInput');
    input.value = depositFormState.specialApprovalReason || '';
    new bootstrap.Modal(document.getElementById('specialApprovalModal')).show();
}

function onSpecialApprovalCancelled() {
    depositFormState.depositDate = null;
    depositFormState.specialApprovalReason = null;
    depositFormState.requiresSpecialApproval = false;

    document.getElementById('depositDate').value = '';
    document.getElementById('specialApprovalReasonInput').value = '';

    const warning = document.getElementById('specialApprovalWarning');
    if (warning) warning.style.setProperty('display', 'none', 'important');

    bootstrap.Modal.getInstance(document.getElementById('specialApprovalModal')).hide();
}

// Add this function to build form data (used by both create and edit)
function buildDepositFormData() {
    return {
        id: depositFormState.id || null,
        cbId: depositFormState.cbId,
        cbCode: depositFormState.cbCode,
        bouBranchCode: depositFormState.bouBranchCode,
        bouBranchId: depositFormState.bouBranchId,
        depositDate: depositFormState.depositDate,
        rtgsDate: depositFormState.rtgsDate,
        rtgsCode: depositFormState.rtgsCode,
        requiresSpecialApproval: depositFormState.requiresSpecialApproval,
        specialApprovalReason: depositFormState.specialApprovalReason,
        agentIds: [
            parseInt(document.getElementById('agentId-1')?.value || 0),
            parseInt(document.getElementById('agentId-2')?.value || 0)
        ],
        denominations: Object.values(depDtGetRows())
            .filter(r => r.valid && r.amount > 0 && r.denomValue > 0)
            .map(r => ({
                denominationId: r.denomId,
                denominationValue: r.denomValue,
                denominationTypeId: r.typeId,
                bundles: r.type === 'notes' ? r.qty : null,
                boxes: r.type === 'coins' ? r.qty : null,
                amount: r.amount,
                subCategoryId: r.subCategoryId
            }))
    };

    //const payload = {
    //    id: depositFormState.id,
    //    bouBranchId: parseInt(depositFormState.bouBranchId) || 0,
    //    cbCode: depositFormState.cbCode,
    //    bouBranchCode: depositFormState.bouBranchCode,
    //    depositDate: depositFormState.depositDate,
    //    rtgsDate: depositFormState.rtgsDate,
    //    rtgsCode: depositFormState.rtgsCode,
    //    requiresSpecialApproval: depositFormState.requiresSpecialApproval,
    //    specialApprovalReason: depositFormState.specialApprovalReason,
    //    agentIds: depositFormState.agentIds.filter(Boolean),
    //    denominations: buildDepositDenomLines(),
    //    remarks: depositFormState.remarks,
    //    cbId: parseInt(depositFormState.cbId) || 0,
    //};
}
