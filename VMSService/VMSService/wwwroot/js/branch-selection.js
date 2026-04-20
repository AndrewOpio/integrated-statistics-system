// ── Branch Selection ─────────────────────────────────────────────────────────

async function loadBranchDetails(type, id) {

    if (type === 'cb')  return;

    const card = document.getElementById('bouBranchCard');
    if (!card) return;

    if (!id) {
        card.style.display = 'none';
        formState.bouBranchCode = null;
        return;
    }


    try {
        const res = await fetch(`/CbWithdrawRequests/BranchDetails?type=${type}&id=${id}`);
        if (!res.ok) throw new Error();
        const d = await res.json();

        setText('bouBranchName', d.name);
        setText('bouBranchCode', d.code);

        //bouBranchCodeValue = d.code;

        //formState.bouBranchCode = document.getElementById('bouBranchCode')?.textContent?.trim() || null;
        formState.bouBranchCode = d.code || null;

        card.style.display = 'block';
    } catch {
        card.style.display = 'none';
    }
}
function initBranchSelection() {
    const sel = document.getElementById('BouBranchSelect');
    if (sel?.value) {
        onBouBranchSelected(sel.value);
        loadBranchDetails('bou', sel.value); // show card with AJAX on edit
    }
}

document.addEventListener('DOMContentLoaded', initBranchSelection);

// ── Helpers ───────────────────────────────────────────────────────────────────
//function setText(id, value) {
//    const el = document.getElementById(id);
//    if (el) el.textContent = value ?? '';
//}

