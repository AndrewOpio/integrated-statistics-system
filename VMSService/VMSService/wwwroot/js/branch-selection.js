async function loadBranchDetails(type, id) {
    const nameEl = document.getElementById(`${type}BranchName`);
    const codeEl = document.getElementById(`${type}BranchCode`);
    const detailsEl = document.getElementById(`${type}BranchDetails`);

    if (!id) {
        detailsEl.style.display = 'none';
        return;
    }

    try {
        const res = await fetch(`/CbWithdrawRequests/BranchDetails?type=${type}&id=${id}`);
        if (!res.ok) throw new Error('Failed to load branch details');

        const data = await res.json();
        nameEl.textContent = data.name;
        codeEl.textContent = `Code: ${data.code}`;
        detailsEl.style.display = 'block';
    } catch {
        detailsEl.style.display = 'none';
    }
}