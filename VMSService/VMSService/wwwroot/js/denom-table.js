(function () {
    const BUNDLE_SIZE = 1000;
    const MAX_BUNDLES = 100000;
    const bundles = {};  // keyed by d.id to avoid value clashes (e.g. 1000 note vs 1000 coin)

    const $ = id => document.getElementById(id);
    const fmtN = n => Math.round(n).toLocaleString();
    const fmt = n => 'UGX ' + fmtN(n);

    const amount = (id, value) => (bundles[id] || 0) * BUNDLE_SIZE * value;
    const noteTotal = () => window.denomData.notes.reduce((s, d) => s + amount(d.id, d.value), 0);
    const coinTotal = () => window.denomData.coins.reduce((s, d) => s + amount(d.id, d.value), 0);
    const grandTotal = () => noteTotal() + coinTotal();

    function buildRows(denoms, listId, type) {
        denoms.forEach(d => bundles[d.id] = 0);
        $(listId).innerHTML = denoms.map(d => `
            <tr>
                <td class="ps-3">
                    <span class="fw-bold">${d.label}</span>
                </td>
                <td style="min-width:200px;">
                    <div class="d-flex align-items-center gap-2">
                        <input type="range"
                               class="form-range flex-grow-1"
                               id="dt-slider-${d.id}"
                               min="0" max="${MAX_BUNDLES}" value="0" step="1"
                               oninput="dtSlide('${d.id}', ${d.value}, '${type}', this.value)" />
                        <input type="number"
                               class="form-control form-control-sm text-center"
                               id="dt-qty-${d.id}"
                               min="0" max="${MAX_BUNDLES}" value="0"
                               style="width:70px;"
                               oninput="dtInput('${d.id}', ${d.value}, '${type}', this.value)" />
                        <span class="text-muted small text-nowrap">${type === 'note' ? 'bdl' : 'box'}</span>
                    </div>
                </td>
                <td class="text-end pe-3">
                    <span class="fw-bold ${type === 'note' ? 'text-primary' : 'text-warning'}"
                          id="dt-sub-${d.id}">—</span>
                </td>
            </tr>`).join('');
    }

    window.dtSlide = function (id, value, type, val) {
        bundles[id] = parseInt(val) || 0;
        $('dt-qty-' + id).value = bundles[id];
        refreshTotals();
        notifyParent();
    };

    window.dtInput = function (id, value, type, val) {
        bundles[id] = Math.min(MAX_BUNDLES, Math.max(0, parseInt(val) || 0));
        $('dt-qty-' + id).value = bundles[id];
        $('dt-slider-' + id).value = bundles[id];
        refreshTotals();
        notifyParent();
    };

    function refreshTotals() {
        // Row subtotals
        [...window.denomData.notes, ...window.denomData.coins].forEach(d => {
            const sub = $('dt-sub-' + d.id);
            if (!sub) return;
            const a = amount(d.id, d.value);
            sub.textContent = a > 0 ? fmt(a) : '—';
        });

        const nt = noteTotal(), ct = coinTotal(), gt = grandTotal();

        // Badges
        $('dt-notes-badge').textContent = fmt(nt);
        $('dt-coins-badge').textContent = fmt(ct);

        // Totals
        $('dt-notes-total').textContent = fmt(nt);
        $('dt-coins-total').textContent = fmt(ct);
        $('dt-grand-total').textContent = fmt(gt);
        $('dt-grand-words').textContent = toWords(gt);

        // Breakdowns — hidden, just clear them
        $('dt-notes-breakdown').textContent = '';
        $('dt-coins-breakdown').textContent = '';
    }

    // ── Amount in words ───────────────────────────────────────
    const ONES = ['', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine',
        'Ten', 'Eleven', 'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen',
        'Seventeen', 'Eighteen', 'Nineteen'];
    const TENS = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety'];

    function wordsUnder1000(n) {
        if (n === 0) return '';
        if (n < 20) return ONES[n];
        if (n < 100) return TENS[Math.floor(n / 10)] + (n % 10 ? ' ' + ONES[n % 10] : '');
        return ONES[Math.floor(n / 100)] + ' Hundred' + (n % 100 ? ' ' + wordsUnder1000(n % 100) : '');
    }

    function toWords(n) {
        if (n === 0) return 'Zero shillings only';
        const parts = [];
        const trillions = Math.floor(n / 1_000_000_000_000); n %= 1_000_000_000_000;
        const billions = Math.floor(n / 1_000_000_000); n %= 1_000_000_000;
        const millions = Math.floor(n / 1_000_000); n %= 1_000_000;
        const thousands = Math.floor(n / 1_000); n %= 1_000;
        if (trillions) parts.push(wordsUnder1000(trillions) + ' Trillion');
        if (billions) parts.push(wordsUnder1000(billions) + ' Billion');
        if (millions) parts.push(wordsUnder1000(millions) + ' Million');
        if (thousands) parts.push(wordsUnder1000(thousands) + ' Thousand');
        if (n) parts.push(wordsUnder1000(n));
        return parts.join(' ') + ' shillings only';
    }

    window.dtGetBundles = () => ({ ...bundles });
    window.dtGetGrandTotal = () => grandTotal();
    window.dtHasValue = () => grandTotal() > 0;
    window.dtShowError = () => $('dt-err-notes').style.removeProperty('display');
    window.dtHideError = () => $('dt-err-notes').style.display = 'none';

    function notifyParent() {
        document.dispatchEvent(new CustomEvent('denomChanged', {
            detail: { grandTotal: grandTotal(), bundles: { ...bundles } }
        }));
    }

    // ── Init ──────────────────────────────────
    buildRows(window.denomData.notes, 'dt-list-notes', 'note');
    buildRows(window.denomData.coins, 'dt-list-coins', 'coin');
    refreshTotals();
})();