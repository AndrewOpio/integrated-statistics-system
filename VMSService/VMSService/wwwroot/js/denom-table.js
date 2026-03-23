(function () {
    const BUNDLE_SIZE = 1000;
    const MAX_BUNDLES = 500;
    const bundles = {};

    const $ = id => document.getElementById(id);
    const fmtN = n => Math.round(n).toLocaleString();
    const fmt = n => 'UGX ' + fmtN(n);
    const amount = v => (bundles[v] || 0) * BUNDLE_SIZE * v;
    const allValues = () => Object.keys(bundles).map(Number);
    const grandTotal = () => allValues().reduce((s, v) => s + amount(v), 0);

    function buildRows(denoms, listId, type) {
        denoms.forEach(d => bundles[d.value] = 0);
        $(listId).innerHTML = denoms.map(d => `
            <tr>
                <td class="ps-3">
                    <span class="fw-bold">${d.displayLabel}</span>
                </td>
                <td style="min-width:200px;">
                    <div class="d-flex align-items-center gap-2">
                        <input type="range"
                               class="form-range flex-grow-1"
                               id="dt-slider-${d.value}"
                               min="0" max="${MAX_BUNDLES}" value="0" step="1"
                               oninput="dtSlide(${d.value}, '${type}', this.value)" />
                        <input type="number"
                               class="form-control form-control-sm text-center"
                               id="dt-qty-${d.value}"
                               min="0" max="${MAX_BUNDLES}" value="0"
                               style="width:70px;"
                               oninput="dtInput(${d.value}, '${type}', this.value)" />
                        <span class="text-muted small text-nowrap">bdl</span>
                    </div>
                </td>
                <td class="text-end pe-3">
                    <span class="fw-bold ${type === 'note' ? 'text-primary' : 'text-warning'}"
                          id="dt-sub-${d.value}">—</span>
                </td>
            </tr>`).join('');
    }

    window.dtSlide = function (d, type, val) {
        bundles[d] = parseInt(val) || 0;
        $('dt-qty-' + d).value = bundles[d];
        refreshTotals();
        notifyParent();
    };

    window.dtInput = function (d, type, val) {
        bundles[d] = Math.min(MAX_BUNDLES, Math.max(0, parseInt(val) || 0));
        $('dt-qty-' + d).value = bundles[d];
        $('dt-slider-' + d).value = bundles[d];
        refreshTotals();
        notifyParent();
    };

    function refreshTotals() {
        // Row subtotals
        allValues().forEach(v => {
            const sub = $('dt-sub-' + v);
            if (!sub) return;
            const a = amount(v);
            sub.textContent = a > 0 ? fmt(a) : '—';
        });

        const notes = window.denomData.notes.map(d => d.value);
        const coins = window.denomData.coins.map(d => d.value);
        const nt = notes.reduce((s, v) => s + amount(v), 0);
        const ct = coins.reduce((s, v) => s + amount(v), 0);
        const gt = grandTotal();

        // Badges
        $('dt-notes-badge').textContent = fmt(nt);
        $('dt-coins-badge').textContent = fmt(ct);

        // Totals
        $('dt-notes-total').textContent = fmt(nt);
        $('dt-coins-total').textContent = fmt(ct);
        $('dt-grand-total').textContent = fmt(gt);
        $('dt-grand-words').textContent = toWords(gt);

        // Breakdowns
        const noteParts = notes.filter(v => bundles[v] > 0).map(v => `${fmtN(v)} × ${bundles[v]} bdl`);
        const coinParts = coins.filter(v => bundles[v] > 0).map(v => `${fmtN(v)} × ${bundles[v]} bdl`);

        $('dt-notes-breakdown').textContent = noteParts.length ? noteParts.join(' · ') : 'No notes selected';
        $('dt-coins-breakdown').textContent = coinParts.length ? coinParts.join(' · ') : 'No coins selected';
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

    // ── Init — data comes from window.denomData set in the view ──
    buildRows(window.denomData.notes, 'dt-list-notes', 'note');
    buildRows(window.denomData.coins, 'dt-list-coins', 'coin');
    refreshTotals();
})();