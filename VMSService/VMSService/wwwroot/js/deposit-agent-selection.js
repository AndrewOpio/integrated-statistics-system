async function loadAgentDetails(person, id) {
    onDepositAgentSelected(person, id ? parseInt(id) : null);
    crossDisableDeposit(person, id ? parseInt(id) : null);

    const card = document.getElementById(`agentCard${person}`);

    if (!id) {
        card.style.display = 'none';
        return;
    }

    try {
        const res = await fetch(`/CbDepositRequests/AgentDetails?id=${id}`);
        if (!res.ok) throw new Error();
        const d = await res.json();

        // Text fields
        setDepositAgentText(`agentNames${person}`, d.agentNames);
        setDepositAgentText(`agentNin${person}`, d.nin);
        setDepositAgentText(`agentPhone${person}`, d.phoneNumber);

        // Photo
        setDepositAgentImage(`agentPhoto${person}`, `agentPhotoPlaceholder${person}`, d.passportPhoto);

        // Work ID scan
        setDepositAgentImage(`agentWorkIdScanImg${person}`, `agentScanPlaceholder${person}`, d.workIdScan);

        // Signature
        setDepositAgentImage(`agentSignatureScan${person}`, `agentSigPlaceholder${person}`, d.signatureScan);

        card.style.display = 'block';
    } catch {
        card.style.display = 'none';
    }
}

function crossDisableDeposit(person, selectedId) {
    const other = person === 1 ? 2 : 1;
    const otherSelect = document.getElementById(`agentSelect${other}`);
    if (!otherSelect) return;

    Array.from(otherSelect.options).forEach(o => {
        if (!o.value) return;
        o.disabled = selectedId && parseInt(o.value) === selectedId;
    });
}

function setDepositAgentText(id, value) {
    const el = document.getElementById(id);
    if (el) el.textContent = value ?? '';
}

function setDepositAgentImage(imgId, placeholderId, url) {
    const img = document.getElementById(imgId);
    const placeholder = placeholderId ? document.getElementById(placeholderId) : null;
    if (!img) return;

    if (url) {
        img.src = url;
        img.style.display = 'block';
        if (placeholder) placeholder.style.display = 'none';
    } else {
        img.style.display = 'none';
        if (placeholder) placeholder.style.display = 'block';
    }
}

// Initialize agent selection on page load (for edit mode)
function initializeDepositAgentSelection() {
    for (let i = 1; i <= 2; i++) {
        const agentSelect = document.getElementById(`agentSelect${i}`);
        if (agentSelect && agentSelect.value) {
            loadAgentDetails(i, agentSelect.value);
        }
    }
}

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeDepositAgentSelection);
} else {
    initializeDepositAgentSelection();
}