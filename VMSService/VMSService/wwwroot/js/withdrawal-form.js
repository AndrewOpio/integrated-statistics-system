// ─────────────────────────────────────────────
// Form State
// Single source of truth for all form selections
// ─────────────────────────────────────────────

const formState = {
    id: null,       // set if editing existing request
    cbCode: null, 
    bouBranchId: null,
    bouBranchCode: null,
    collectionDate: null,
    rtgsDate: null,
    rtgsCode: null,
    cbCode: null,
    agentIds: [],         // exactly 2 required
    remarks: null,
};

// ─────────────────────────────────────────────
// Branch selection — called from _BranchSelection
// ─────────────────────────────────────────────

//function onCbBranchSelected(id) {
//    formState.cbBranchId = id || null;
//}

function onBouBranchSelected(id) {
    formState.bouBranchId = id ? parseInt(id) : null;;
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

    // Draft only requires denominations or agents to be started
    // Full validation happens server side on submit
    if (!isDraft) {
        //if (!formState.cbBranchId)
        //    errors.push('Please select a company branch.');

        if (!formState.bouBranchId)
            errors.push('Please select a bank branch.');

        if (formState.agentIds.filter(Boolean).length !== 2)
            errors.push('Exactly 2 agents are required.');

        if (!window.dtHasValue())
            errors.push('Please add at least one denomination.');

        if (!formState.collectionDate)
            errors.push('Please select a collection date.');

        if (formState.rtgsCode && !formState.rtgsDate)
            errors.push('RTGS date is required when an RTGS code is provided.');
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


    const isDraft = action.includes('save-draft');
    const errors = validateForm(isDraft);

    if (errors.length > 0) {
        showErrors(errors);
        return;
    }

    const payload = {
        id: formState.id,
        bouBranchId: parseInt(formState.bouBranchId) || 0,
        cbCode: formState.cbCode,
        bouBranchCode: formState.bouBranchCode,
        collectionDate: formState.collectionDate,
        rtgsDate: formState.rtgsDate,
        rtgsCode: formState.rtgsCode,
        agentIds: formState.agentIds.filter(Boolean),
        denominations: buildDenomLines(),
        remarks: formState.remarks,
    };

    // Disable buttons to prevent double submit
    setButtonsDisabled(true);

    try {
        const res = await fetch(action, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken(),
            },
            body: JSON.stringify(payload),
        });

        if (res.ok) {
            const redirectUrl = await res.text();
            window.location.href = redirectUrl;
        } else {
            const data = await res.json();
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

function checkSpecialApproval() {
    const warning = document.getElementById('specialApprovalWarning');
    const show = formState.collectionDate
        && formState.rtgsDate
        && formState.collectionDate === formState.rtgsDate;
    warning.style.display = show ? 'flex' : 'none';
}
