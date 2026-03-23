async function loadAgentDetails(person, id) {
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
        setText(`agentCb${person}`, d.agentCb);
        setText(`agentCbBranch${person}`, d.commercialBankBranchId);
        setText(`agentSignatureScan${person}`, d.agentSignatureScan);


        //agentDetailVm.AgentNames,
        //agentDetailVm.NIN,
        //    agentDetailVm.WorkIdScan,
        //    agentDetailVm.CommercialBankBranchId,
        //    agentDetailVm.BranchId,
        //    agentDetailVm.PassportPhoto,
        //    agentDetailVm.SignatureScan,

        // Photo
        setImage(`agentPhoto${person}`, `agentPhotoPlaceholder${person}`, d.photoUrl);

        // Signature
        setImage(`agentSignature${person}`, null, d.signatureUrl);

        card.style.display = 'block';
    } catch {
        card.style.display = 'none';
    }
}

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