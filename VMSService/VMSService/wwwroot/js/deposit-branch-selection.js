// ?? Deposit Branch Selection ?????????????????????????????????????????????????

async function loadDepositBranchDetails(type, id) {
    if (type === 'cb') return;

    const card = document.getElementById('bouBranchCard');
    if (!card) return;

    if (!id) {
        card.style.display = 'none';
        return;
    }

    try {
        const res = await fetch(`/CbDepositRequests/BranchDetails?type=${type}&id=${id}`);
        if (!res.ok) throw new Error();
        const d = await res.json();

        setDepositText('bouBranchName', d.name);
        setDepositText('bouBranchCode', d.code);

        depositFormState.bouBranchCode = document.getElementById('bouBranchCode')?.textContent?.trim() || null;

        card.style.display = 'block';
    } catch {
        card.style.display = 'none';
    }
}

function setDepositText(id, value) {
    const el = document.getElementById(id);
    if (el) el.textContent = value ?? '';
}