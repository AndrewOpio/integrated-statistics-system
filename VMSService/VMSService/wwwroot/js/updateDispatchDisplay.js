window.updateDispatchDisplay = function () {
    const pending = window.pendingDispatch;
    if (!pending) return;

    const notes = pending.filter(r => r.bundleType === 'New' || r.bundleType === 'Reissue');
    const coins = pending.filter(r => r.bundleType === 'Coin');

    const notesTotal = notes.reduce((s, r) => s + r.amount, 0);
    const coinsTotal = coins.reduce((s, r) => s + r.amount, 0);
    const grand = notesTotal + coinsTotal;

    // Replace the display section's contents
    const container = document.getElementById('bankDisplayContainer');
    if (!container) return;

    let html = '';

    if (notes.length > 0) {
        html += `<div class="border rounded-3 overflow-hidden mb-2">
            <div class="px-3 py-2 border-bottom bg-light fw-semibold small text-uppercase">Banknotes</div>
            <table class="table table-sm mb-0">
                <thead class="small text-muted"><tr>
                    <th class="ps-3">DENOMINATION</th>
                    <th>TYPE</th>
                    <th class="text-end">BUNDLES</th>
                    <th class="text-end pe-3">AMOUNT</th>
                </tr></thead>
                <tbody class="small">
                    ${notes.map(r => `<tr>
                        <td class="ps-3">UGX ${r.denominationValue.toLocaleString()}</td>
                        <td>${r.bundleType}</td>
                        <td class="text-end">${r.bundles?.toLocaleString() ?? '—'}</td>
                        <td class="text-end pe-3 text-primary">UGX ${r.amount.toLocaleString()}</td>
                    </tr>`).join('')}
                </tbody>
            </table>
        </div>`;
    }

    if (coins.length > 0) {
        html += `<div class="border rounded-3 overflow-hidden mb-2">
            <div class="px-3 py-2 border-bottom bg-light fw-semibold small text-uppercase">Coins</div>
            <table class="table table-sm mb-0">
                <thead class="small text-muted"><tr>
                    <th class="ps-3">DENOMINATION</th>
                    <th class="text-end">BOXES</th>
                    <th class="text-end pe-3">AMOUNT</th>
                </tr></thead>
                <tbody class="small">
                    ${coins.map(r => `<tr>
                        <td class="ps-3">UGX ${r.denominationValue.toLocaleString()}</td>
                        <td class="text-end">${r.boxes?.toLocaleString() ?? '—'}</td>
                        <td class="text-end pe-3 text-warning">UGX ${r.amount.toLocaleString()}</td>
                    </tr>`).join('')}
                </tbody>
            </table>
        </div>`;
    }

    container.innerHTML = html;

    // Update grand total
    const grandEl = document.getElementById('bankGrandTotalDisplay');
    if (grandEl) grandEl.textContent = 'UGX ' + grand.toLocaleString();

    const wordsEl = document.getElementById('bank-denom-words');
    if (wordsEl && typeof toWords === 'function') wordsEl.textContent = toWords(grand);
};