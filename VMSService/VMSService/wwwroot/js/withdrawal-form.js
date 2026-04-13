// ─────────────────────────────────────────────
// Form State
// Single source of truth for all form selections
// ─────────────────────────────────────────────

// Always derive "today" from Uganda time (EAT = UTC+3)
const today = new Date(new Date().toLocaleString('en-US', { timeZone: 'Africa/Kampala' }))
    .toISOString().split('T')[0];

const formState = {
    id: null,       // set if editing existing request
    cbCode: null, 
    bouBranchId: null,
    bouBranchCode: null,
    collectionDate: null,
    rtgsDate: null,
    rtgsCode: null,
    requiresSpecialApproval: false,
    specialApprovalReason: null,
    agentIds: [],
    createdDate: null,
};

// ──────────────────────────────────────────────────────
//To and From Selection — called from _BranchSelection
// ──────────────────────────────────────────────────────

function onBouBranchSelected(id) {
    formState.bouBranchId = id ? parseInt(id) : null;
}

// ─────────────────────────────────────────────
// Agent selection — called from _AgentSelection
// ─────────────────────────────────────────────

function onAgentSelected(slot, id) {
    formState.agentIds[slot - 1] = id || null;
}

// ─────────────────────────────────────────────
// Build denomination lines from denom-table.js
// ─────────────────────────────────────────────

function buildDenomLines() {
    const rows = window.dtGetRows();
    return Object.values(rows)
        .filter(r => r.qty > 0 && r.denomValue > 0)
        .map(r => ({
            denominationId: r.denomId,
            denominationTypeId: r.typeId,
            denominationValue: r.denomValue,
            bundles: r.type === 'notes' ? r.qty : null,
            boxes: r.type === 'coins' ? r.qty : null,
            amount: r.qty * 1000 * r.denomValue,
        }));
}

// ─────────────────────────────────────────────
// Validation — client side pre-check
// ─────────────────────────────────────────────

function validateForm(isDraft) {
    const errors = [];

    if (!isDraft) {

        if (!formState.bouBranchId)
            errors.push('Please select a BOU branch.');

        if (formState.agentIds.filter(Boolean).length !== 2)
            errors.push('Exactly 2 agents are required.');

        if (!window.dtHasValue())
            errors.push('Please add at least one denomination.');
             
        if (!formState.collectionDate)
            errors.push('Please select a collection date.');

        if (formState.rtgsCode && !formState.rtgsDate)
            errors.push('RTGS date is required when an RTGS code is provided.');

        if (formState.rtgsDate && !formState.rtgsCode)
            errors.push('RTGS code is required when an RTGS date is provided.');

        if (formState.rtgsDate && formState.rtgsDate <= today)
            errors.push('RTGS date cannot be in the past.');
    }

    return errors;
}

// ─────────────────────────────────────────────
// Show errors
// ─────────────────────────────────────────────

function showErrors(errors) {
    let container = document.getElementById('validationErrors');

    if (!container) {
        container = document.createElement('div');
        container.id = 'validationErrors';
        container.className = 'alert alert-danger mb-4';
        document.getElementById('withdrawalForm').prepend(container);
    }

    container.innerHTML = '<ul class="mb-0">'
        + errors.map(e => `<li>${e}</li>`).join('')
        + '</ul>';

    container.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function clearErrors() {
    const container = document.getElementById('validationErrors');
    if (container) container.remove();
}

// ─────────────────────────────────────────────
// Get anti-forgery token
// ─────────────────────────────────────────────

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

// ─────────────────────────────────────────────
// Submit form
// ─────────────────────────────────────────────

async function submitForm(action) {
    clearErrors();

    const isDraft = action.toLowerCase().includes('savedraft');
    const isEdit = !!formState.id;
    const errors = validateForm(isDraft);

    if (errors.length > 0) { showErrors(errors); return; }

    // On Edit only send fields the user is allowed to change.
    const payload = isEdit
        ? {
            id: formState.id,
            bouBranchId: parseInt(formState.bouBranchId) || 0,
            bouBranchCode: formState.bouBranchCode,
            rtgsDate: formState.rtgsDate,
            rtgsCode: formState.rtgsCode,
            requiresSpecialApproval: formState.requiresSpecialApproval,
            specialApprovalReason: formState.specialApprovalReason,
            agentIds: formState.agentIds.filter(Boolean),
            denominations: buildDenomLines(),
            
        }
        : {
            id: null,
            cbCode: formState.cbCode,
            bouBranchId: parseInt(formState.bouBranchId) || 0,
            bouBranchCode: formState.bouBranchCode,
            collectionDate: formState.collectionDate,
            rtgsDate: formState.rtgsDate,
            rtgsCode: formState.rtgsCode,
            requiresSpecialApproval: formState.requiresSpecialApproval,
            specialApprovalReason: formState.specialApprovalReason,
            agentIds: formState.agentIds.filter(Boolean),
            denominations: buildDenomLines(),
        };

    setButtonsDisabled(true);

    try {
        console.log('isEdit:', isEdit, 'payload:', JSON.stringify(payload));

        const res = await fetch(action, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken(),
            },
            body: JSON.stringify(payload),
        });

        if (res.ok) {
            window.location.href = await res.text();
        } else {
            const data = await res.json().catch(() => ({}));
            showErrors(data.errors ?? ['An unexpected error occurred. Please try again.']);
            setButtonsDisabled(false);
        }
    } catch {
        showErrors(['A network error occurred. Please check your connection and try again.']);
        setButtonsDisabled(false);
    }
}

// ─────────────────────────────────────────────
// Disable / enable submit buttons
// ─────────────────────────────────────────────

function setButtonsDisabled(disabled) {
    document.querySelectorAll('button[onclick^="submitForm"]')
        .forEach(btn => btn.disabled = disabled);
}

// ─────────────────────────────────────────────
// Listen for denomination changes
// ─────────────────────────────────────────────

document.addEventListener('denomChanged', e => {
    console.debug('Denom total:', e.detail.grandTotal);
});


// ── Special Approval ─────────────────────────────────────────────────────────

function isSameDayApprovalRequired() {
    // Always use Uganda time (EAT = UTC+3) regardless of browser locale
    const nowUganda = new Date(new Date().toLocaleString('en-US', { timeZone: 'Africa/Kampala' }));
    const isMonday = nowUganda.getDay() === 1;
    const before11 = nowUganda.getHours() < 11;

    // Monday before 11am EAT — grace period, no special approval
    if (isMonday && before11) return false;

    return formState.createdDate !== null
        && formState.collectionDate !== null
        && formState.createdDate === formState.collectionDate;
}

function checkSpecialApproval() {
    const warning = document.getElementById('specialApprovalWarning');

    if (isSameDayApprovalRequired()) {
        formState.specialApprovalReason = null;
        new bootstrap.Modal(document.getElementById('specialApprovalModal')).show();
    } else {
        formState.requiresSpecialApproval = false;
        formState.specialApprovalReason = null;
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
    formState.specialApprovalReason = reason;
    formState.requiresSpecialApproval = true;

    document.getElementById('specialApprovalReasonDisplay').textContent = reason;

    bootstrap.Modal.getInstance(document.getElementById('specialApprovalModal')).hide();

    if (warning) warning.style.setProperty('display', 'flex', 'important');
}

function editSpecialApprovalReason() {
    const input = document.getElementById('specialApprovalReasonInput');
    input.value = formState.specialApprovalReason || '';
    new bootstrap.Modal(document.getElementById('specialApprovalModal')).show();
}

function onSpecialApprovalCancelled() {
    formState.collectionDate = null;
    formState.specialApprovalReason = null;
    formState.requiresSpecialApproval = false;

    document.getElementById('collectionDate').value = '';
    document.getElementById('specialApprovalReasonInput').value = '';

    const warning = document.getElementById('specialApprovalWarning');
    if (warning) warning.style.setProperty('display', 'none', 'important');

    bootstrap.Modal.getInstance(document.getElementById('specialApprovalModal')).hide();
}


