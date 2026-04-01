// ── Branch Selection ─────────────────────────────────────────────────────────
// Cb (sender) is server-side rendered — only bank branch needs AJAX

async function loadBranchDetails(type, id) {
    //let bouBranchCodeValue = null;
    //let cbCodeValue = null;

    if (type === 'cb') {
        //formState.cbCode = document.getElementById('cbCode')?.textContent?.trim() || null;
        //console.log('CB Code set to:', formState.cbCode);
        return;
    }

    const card = document.getElementById('bouBranchCard');
    if (!card) return;

    if (!id) {
        card.style.display = 'none';
        return;
    }


    try {
        const res = await fetch(`/CbWithdrawRequests/BranchDetails?type=${type}&id=${id}`);
        if (!res.ok) throw new Error();
        const d = await res.json();

        setText('bouBranchName', d.name);
        setText('bouBranchCode', d.code);

        //bouBranchCodeValue = d.code;

        formState.bouBranchCode = document.getElementById('bouBranchCode')?.textContent?.trim() || null;

        card.style.display = 'block';
    } catch {
        card.style.display = 'none';
    }
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function setText(id, value) {
    const el = document.getElementById(id);
    if (el) el.textContent = value ?? '';
}

//async function loadBranchDetails(type, id) {
//    const prefix = type === 'cb' ? 'cb' : 'bou';
//    const card = document.getElementById(`${prefix}BranchCard`);  // Ensure correct ID cbBranchDistrict

//    if (!card) return;

//    if (!id) {
//        card.style.display = 'none';
//        return;
//    }
//    if (!card) {
//        console.error("Card element not found");
//        return;
//    }

//    try {
//        const res = await fetch(`/CbWithdrawRequests/BranchDetails?type=${type}&id=${id}`);
//        if (!res.ok) throw new Error('Failed to load branch details');

//        const d = await res.json();

//        if (type === 'cb') {
//            //setText('cbhName', d.cbName);
//            setText('cbName', `${d.cbName}`);
//            //formState.cbCode = d.code;
//            setText('code', d.code);
//            setText('cbcode', d.cbCode);
//        } else {
//            setText('bouBranchName', d.name);
//            setText('bouBranchCode', d.code);
//        }

//        card.style.display = 'block';
//    } catch {
//        card.style.display = 'none';
//    }
//}