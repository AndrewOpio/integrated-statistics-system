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
    //isLumpSum: false,
    //lumpSumAmount: null,
};

// ──────────────────────────────────────────────────────
//To and From Selection — called from _BranchSelection
// ──────────────────────────────────────────────────────

function onBouBranchSelected(id) {
    formState.bouBranchId = id ? parseInt(id) : null;
}

function onCollectionDateChanged(value) {
    formState.collectionDate = value || null;

    const isSameDay = value && value === today;

    const sameDayWarning = document.getElementById('sameDayMakerWarning');
    if (sameDayWarning) {
        sameDayWarning.classList.toggle('d-none', !isSameDay);
        sameDayWarning.classList.toggle('d-flex', isSameDay);
    }

    if (isSameDay) {
        openSameDayReasonModal();
        const rtgsCodeInput = document.getElementById('rtgsCode');
        if (rtgsCodeInput) rtgsCodeInput.required = true;
        const rtgsCodeHint = document.getElementById('rtgsCodeHint');
        if (rtgsCodeHint) rtgsCodeHint.classList.remove('d-none');
    } else {
        formState.requiresSpecialApproval = false;
        formState.specialApprovalReason = null;

        const rtgsCodeInput = document.getElementById('rtgsCode');
        if (rtgsCodeInput) rtgsCodeInput.required = false;
        const rtgsCodeHint = document.getElementById('rtgsCodeHint');
        if (rtgsCodeHint) rtgsCodeHint.classList.add('d-none');

        const sameDayReasonDisplay = document.getElementById('sameDayReasonDisplay');
        const sameDayReasonText    = document.getElementById('sameDayReasonText');
        if (sameDayReasonDisplay) sameDayReasonDisplay.classList.add('d-none');
        if (sameDayReasonText)    sameDayReasonText.textContent = '';

        const specialApprovalWarning = document.getElementById('specialApprovalWarning');
        if (specialApprovalWarning) {
            specialApprovalWarning.classList.add('d-none');
            specialApprovalWarning.classList.remove('d-flex');
        }
        const specialApprovalReasonDisplay = document.getElementById('specialApprovalReasonDisplay');
        if (specialApprovalReasonDisplay) specialApprovalReasonDisplay.textContent = '';
    }

    // Fund settlement value date always mirrors the collection date.
    // Update formState first so it stays in sync even when the settlement date
    // field isn't present on the page (e.g. the Edit page renders it read-only).
    formState.rtgsDate = value || null;

    const rtgsDisplay = document.getElementById('rtgsDateDisplay');
    if (rtgsDisplay) {
        rtgsDisplay.textContent = value
            ? new Date(value + 'T00:00:00').toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
            : '—';
    }

    const rtgsInput = document.getElementById('rtgsDate');
    if (rtgsInput) rtgsInput.value = value || '';
}

// ─────────────────────────────────────────────
// Same-Day Reason Modal
// ─────────────────────────────────────────────

function openSameDayReasonModal() {
    const modalEl = document.getElementById('sameDayReasonModal');
    if (!modalEl) return;

    // Reset modal state each time it opens.
    const textarea = document.getElementById('sameDayReasonInput');
    const errorEl  = document.getElementById('sameDayReasonError');
    const countEl  = document.getElementById('sameDayReasonCount');
    if (textarea) textarea.value = formState.specialApprovalReason || '';
    if (errorEl)  errorEl.style.display = 'none';
    if (countEl)  countEl.textContent = textarea ? textarea.value.length : 0;

    bootstrap.Modal.getOrCreateInstance(modalEl).show();
}

function clearCollectionDate() {
    const dateInput = document.getElementById('collectionDate');
    if (dateInput) dateInput.value = '';

    formState.collectionDate = null;
    formState.rtgsDate = null;
    formState.requiresSpecialApproval = false;
    formState.specialApprovalReason = null;

    const sameDayWarning = document.getElementById('sameDayMakerWarning');
    if (sameDayWarning) {
        sameDayWarning.classList.add('d-none');
        sameDayWarning.classList.remove('d-flex');
    }

    const reasonDisplay = document.getElementById('sameDayReasonDisplay');
    const reasonText    = document.getElementById('sameDayReasonText');
    if (reasonDisplay) reasonDisplay.classList.add('d-none');
    if (reasonText)    reasonText.textContent = '';

    const rtgsInput = document.getElementById('rtgsDate');
    if (rtgsInput) rtgsInput.value = '';

    const rtgsDisplay = document.getElementById('rtgsDateDisplay');
    if (rtgsDisplay) rtgsDisplay.textContent = '—';

    const rtgsCodeInput = document.getElementById('rtgsCode');
    if (rtgsCodeInput) rtgsCodeInput.required = false;
    const rtgsCodeHint = document.getElementById('rtgsCodeHint');
    if (rtgsCodeHint) rtgsCodeHint.classList.add('d-none');
}

// Wire up modal buttons once the DOM is ready.
document.addEventListener('DOMContentLoaded', () => {
    const modalEl     = document.getElementById('sameDayReasonModal');
    const confirmBtn  = document.getElementById('sameDayReasonConfirmBtn');
    const cancelBtn   = document.getElementById('sameDayReasonCancelBtn');
    const textarea    = document.getElementById('sameDayReasonInput');
    const errorEl     = document.getElementById('sameDayReasonError');
    const countEl     = document.getElementById('sameDayReasonCount');

    if (!modalEl) return;

    if (textarea && countEl) {
        textarea.addEventListener('input', () => {
            countEl.textContent = textarea.value.length;
            if (textarea.value.trim()) errorEl.style.display = 'none';
        });
    }

    if (confirmBtn) {
        confirmBtn.addEventListener('click', () => {
            const reason = textarea ? textarea.value.trim() : '';
            if (!reason) {
                if (errorEl) errorEl.style.display = 'block';
                return;
            }
            formState.requiresSpecialApproval = true;
            formState.specialApprovalReason   = reason;
            bootstrap.Modal.getInstance(modalEl).hide();

            const reasonText    = document.getElementById('sameDayReasonText');
            const reasonDisplay = document.getElementById('sameDayReasonDisplay');
            if (reasonText)    reasonText.textContent = reason;
            if (reasonDisplay) reasonDisplay.classList.remove('d-none');

            // Sync the RTGS-card block on the Edit page.
            const specialApprovalReasonDisplay = document.getElementById('specialApprovalReasonDisplay');
            if (specialApprovalReasonDisplay) specialApprovalReasonDisplay.textContent = reason;
            const specialApprovalWarning = document.getElementById('specialApprovalWarning');
            if (specialApprovalWarning) {
                specialApprovalWarning.classList.remove('d-none');
                specialApprovalWarning.classList.add('d-flex');
            }
        });
    }

    if (cancelBtn) {
        cancelBtn.addEventListener('click', () => {
            bootstrap.Modal.getInstance(modalEl).hide();
            // On Create only — clear the date so the user cannot skip providing a reason.
            if (!formState.id) clearCollectionDate();
        });
    }
});

// ─────────────────────────────────────────────
// Agent selection — called from _AgentSelection
// ─────────────────────────────────────────────

function onAgentSelected(slot, id) {
    formState.agentIds[slot - 1] = id || null;
}

// ─────────────────────────────────────────────
// Build denomination lines from denom-table.js
// ─────────────────────────────────────────────

//function buildDenomLines() {
//    const rows = window.dtGetRows();
//    return Object.values(rows)
//        .filter(r => r.qty > 0 && r.denomValue > 0)
//        .map(r => ({
//            denominationId: r.denomId,
//            denominationTypeId: r.typeId,
//            denominationValue: r.denomValue,
//            bundles: r.type === 'notes' ? r.qty : null,
//            boxes: r.type === 'coins' ? r.qty : null,
//            amount: r.qty * 1000 * r.denomValue,
//        }));
//}
function buildDenomLines() {
    if (window.dtGetMode() === 'lumpsum') return [];
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

    if (!formState.collectionDate)
        errors.push('Please select a collection date.');
    else if (formState.collectionDate < today)
        errors.push('Collection date cannot be in the past.');

    if (formState.requiresSpecialApproval && !formState.specialApprovalReason)
        errors.push('A reason is required for same-day collection requests.');

    if (formState.collectionDate === today && !formState.rtgsCode)
        errors.push('RTGS code is required for same-day collection requests.');

    if (!isDraft) {

        if (!formState.bouBranchId)
            errors.push('Please select a BOU branch.');

        if (formState.agentIds.filter(Boolean).length !== 2)
            errors.push('Exactly 2 agents are required.');

        if (formState.rtgsCode && !formState.rtgsDate)
            errors.push('Fund settlement value date is required when an RTGS code is provided.');

        if (formState.rtgsDate && !formState.rtgsCode)
            errors.push('RTGS code is required when a fund settlement value date is provided.');

        if (formState.rtgsDate && formState.collectionDate && formState.rtgsDate > formState.collectionDate)
            errors.push('Fund settlement value date cannot be after the collection date.');

        if (!window.dtHasValue())
            errors.push('Please add at least one denomination or enter a lump sum amount.');

        if (!window.dtIsLumpSumValid())
            errors.push('Lump sum amount must be a whole bundle — no partial amounts allowed.');

        if (window.dtGetMode() === 'lumpsum' && window.dtGetLumpSum() % 1000 !== 0)
            errors.push('Lump sum amount must be a whole bundle — no partial amounts allowed.');
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
            collectionDate: formState.collectionDate,
            rtgsDate: formState.rtgsDate,
            rtgsCode: formState.rtgsCode,
            requiresSpecialApproval: formState.requiresSpecialApproval,
            specialApprovalReason: formState.specialApprovalReason,
            agentIds: formState.agentIds.filter(Boolean),
            //denominations: buildDenomLines(),
            isLumpSum: window.dtGetMode() === 'lumpsum',
            lumpSumAmount: window.dtGetMode() === 'lumpsum' ? window.dtGetLumpSum() : null,
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
            //denominations: buildDenomLines(),
            isLumpSum: window.dtGetMode() === 'lumpsum',
            lumpSumAmount: window.dtGetMode() === 'lumpsum' ? window.dtGetLumpSum() : null,
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



