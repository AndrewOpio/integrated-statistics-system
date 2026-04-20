async function loadAgentDetails(person, id) {
    onAgentSelected(person, id ? parseInt(id) : null);
    crossDisable(person, id ? parseInt(id) : null);

    const card = document.getElementById(`agentCard${person}`);

    if (!id) {
        card.style.display = 'none';
        return;
    }

    try {
        const res = await fetch(`/CbWithdrawRequests/AgentDetails?id=${id}`);
        if (!res.ok) throw new Error();
        const d = await res.json();

        // Text fields
        setText(`agentNames${person}`, d.agentNames);
        setText(`agentNin${person}`, d.nin);
        setText(`agentWorkId${person}`, d.workIdScan);
        setText(`agentPhone${person}`, d.phoneNumber);
        setText(`agentCb${person}`, d.agentCb);
        setText(`agentCbBranch${person}`, d.commercialBankBranchId);

        // Photo
        setImage(`agentPhoto${person}`, `agentPhotoPlaceholder${person}`, d.passportPhoto);

        // Work ID scan
        setImage(`agentWorkIdScanImg${person}`, `agentScanPlaceholder${person}`, d.workIdScan);

        // Signature
        setImage(`agentSignatureScan${person}`, `agentSigPlaceholder${person}`, d.signatureScan);

        card.style.display = 'block';
    } catch {
        card.style.display = 'none';
    }
}

// ── Cross-disable ─────────────────────────────────────────────────────────────
// When person 1 picks an agent, disable that option in person 2's dropdown
// and vice versa
function crossDisable(person, selectedId) {
    const other = person === 1 ? 2 : 1;
    const otherSelect = document.getElementById(`agentSelect${other}`);
    if (!otherSelect) return;

    Array.from(otherSelect.options).forEach(o => {
        if (!o.value) return; // keep placeholder
        o.disabled = selectedId && parseInt(o.value) === selectedId;
    });
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function setText(id, value) {
    const el = document.getElementById(id);
    if (el) el.textContent = value ?? '';
}

function setImage(imgId, placeholderId, url) {
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

function initAgentSelection() {
    for (let i = 1; i <= 2; i++) {
        const select = document.getElementById(`agentSelect${i}`);
        if (select?.value) {
            loadAgentDetails(i, select.value);
        }
    }
}

document.addEventListener('DOMContentLoaded', initAgentSelection);