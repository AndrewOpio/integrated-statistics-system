(function () {
    const BUNDLE_SIZE = 1000;
    const fmt = n => 'UGX ' + Math.round(n).toLocaleString();
    const $ = id => document.getElementById(id);

    // Track state per row: { denomId, typeId, denomValue, qty, type }
    const rows = {};
    const usedValues = { notes: new Set(), coins: new Set() };
    let rid = 0;

    // ── Add Row ───────────────────────────────────────────────
    window.denomAddRow = function (type) {
        const denoms = type === 'notes'
            ? window.denomData.notes
            : window.denomData.coins;

        const available = denoms.filter(d => !usedValues[type].has(d.value));
        if (!available.length) {
            alert('All denominations have been added.'); return;
        }

        const id = `${type}-${++rid}`;
        rows[id] = { denomId: null, typeId: null, denomValue: 0, qty: 0, type };

        const tbody = $(`denom-${type}-body`);
        const empty = tbody.querySelector('.denom-empty-row');
        if (empty) empty.remove();

        const optHtml = '<option value="">— Select denomination —</option>'
            + denoms.map(d =>
                `<option value="${d.value}"
                         data-denom-id="${d.id}"
                         data-type-id="${d.typeId}"
                         ${usedValues[type].has(d.value) ? 'disabled' : ''}>
                    ${d.label}
                </option>`).join('');

        const tr = document.createElement('tr');
        tr.id = `denom-row-${id}`;
        tr.className = 'border-bottom';
        tr.innerHTML = `
            <td class="px-4 py-2">
                <select class="form-select form-select-sm"
                        id="denom-sel-${id}"
                        onchange="denomOnSel('${id}','${type}',this)">
                    ${optHtml}
                </select>
            </td>
            <td class="px-4 py-2">
                <input class="form-control form-control-sm text-end"
                       id="denom-inp-${id}"
                       type="number" min="0" placeholder="0"
                       oninput="denomOnInput('${id}','${type}',this)" />
            </td>
            <td class="px-4 py-2 text-end fw-bold text-muted" id="denom-amt-${id}">—</td>
            <td class="px-2 py-2 text-center">
                <button type="button"
                        class="btn btn-link text-danger p-0"
                        onclick="denomRemoveRow('${id}','${type}')">
                    <i class="ti ti-trash fs-5"></i>
                </button>
            </td>`;
        tbody.appendChild(tr);
    };

    // ── On Denomination Selected ──────────────────────────────
    window.denomOnSel = function (id, type, sel) {
        const opt = sel.options[sel.selectedIndex];
        const newVal = parseInt(sel.value) || 0;
        const prevVal = rows[id]?.denomValue || 0;

        // Free old, claim new
        if (prevVal) usedValues[type].delete(prevVal);
        if (newVal) usedValues[type].add(newVal);

        rows[id].denomValue = newVal;
        rows[id].denomId = parseInt(opt.dataset.denomId) || 0;
        rows[id].typeId = parseInt(opt.dataset.typeId) || 0;

        refreshSelects(type);
        recalcRow(id);
        refreshTotals();
    };

    // ── On Quantity Input ─────────────────────────────────────
    window.denomOnInput = function (id, type, el) {
        rows[id].qty = Math.max(0, parseInt(el.value) || 0);
        recalcRow(id);
        refreshTotals();
    };

    // ── Remove Row ────────────────────────────────────────────
    window.denomRemoveRow = function (id, type) {
        const val = rows[id]?.denomValue || 0;
        if (val) usedValues[type].delete(val);
        delete rows[id];

        $(`denom-row-${id}`)?.remove();
        refreshSelects(type);

        const tbody = $(`denom-${type}-body`);
        if (!tbody.querySelector('tr[id]')) {
            const tr = document.createElement('tr');
            tr.className = 'denom-empty-row';
            tr.innerHTML = `<td colspan="4" class="text-center py-3 text-muted small">
                No ${type === 'notes' ? 'banknotes' : 'coins'} added</td>`;
            tbody.appendChild(tr);
        }

        refreshTotals();
    };

    // ── Recalc single row amount ──────────────────────────────
    function recalcRow(id) {
        const r = rows[id];
        const amt = r.qty * BUNDLE_SIZE * r.denomValue;
        const el = $(`denom-amt-${id}`);
        if (!el) return;
        if (amt > 0) {
            el.textContent = fmt(amt);
            el.className = `px-4 py-2 text-end fw-bold ${r.type === 'notes' ? 'text-primary' : 'text-warning'}`;
        } else {
            el.textContent = '—';
            el.className = 'px-4 py-2 text-end fw-bold text-muted';
        }
    }

    // ── Refresh disabled options across all selects ───────────
    function refreshSelects(type) {
        document.querySelectorAll(`#denom-${type}-body .form-select`).forEach(sel => {
            const curVal = rows[sel.id.replace('denom-sel-', '')]?.denomValue || 0;
            Array.from(sel.options).forEach(o => {
                if (!o.value) return;
                o.disabled = usedValues[type].has(parseInt(o.value))
                    && parseInt(o.value) !== curVal;
            });
        });
    }

    // ── Refresh totals ────────────────────────────────────────
    function refreshTotals() {
        const noteAmt = Object.values(rows)
            .filter(r => r.type === 'notes')
            .reduce((s, r) => s + r.qty * BUNDLE_SIZE * r.denomValue, 0);

        const coinAmt = Object.values(rows)
            .filter(r => r.type === 'coins')
            .reduce((s, r) => s + r.qty * BUNDLE_SIZE * r.denomValue, 0);

        const grand = noteAmt + coinAmt;

        $('denom-note-sub').textContent = fmt(noteAmt);
        $('denom-coin-sub').textContent = fmt(coinAmt);
        $('denom-t-notes').textContent = fmt(noteAmt);
        $('denom-t-coins').textContent = fmt(coinAmt);
        $('denom-g-val').textContent = fmt(grand);
        $('denom-g-words').textContent = toWords(grand) + ' only';

        document.dispatchEvent(new CustomEvent('denomChanged', {
            detail: { grandTotal: grand }
        }));
    }

    // ── Public API (used by withdrawal-form.js) ───────────────
    window.dtHasValue = () =>
        Object.values(rows).some(r => r.qty > 0 && r.denomValue > 0);

    window.dtGetRows = () => ({ ...rows });

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
        if (n === 0) return 'Zero shillings';
        const scales = [[1e12, 'Trillion'], [1e9, 'Billion'], [1e6, 'Million'], [1e3, 'Thousand'], [1, '']];
        let res = '', rem = n;
        scales.forEach(([s, name]) => {
            if (rem >= s) {
                const chunk = Math.floor(rem / s);
                res += wordsUnder1000(chunk) + (name ? ' ' + name + ' ' : ' ');
                rem %= s;
            }
        });
        return res.trim() + ' Shillings';
    }
})();



//(function () {
//    const BUNDLE_SIZE = 1000;
//    const MAX_BUNDLES = 100000;
//    const bundles = {};  // keyed by d.id to avoid value clashes (e.g. 1000 note vs 1000 coin)

//    const $ = id => document.getElementById(id);
//    const fmtN = n => Math.round(n).toLocaleString();
//    const fmt = n => 'UGX ' + fmtN(n);

//    const amount = (id, value) => (bundles[id] || 0) * BUNDLE_SIZE * value;
//    const noteTotal = () => window.denomData.notes.reduce((s, d) => s + amount(d.id, d.value), 0);
//    const coinTotal = () => window.denomData.coins.reduce((s, d) => s + amount(d.id, d.value), 0);
//    const grandTotal = () => noteTotal() + coinTotal();

//    function buildRows(denoms, listId, type) {
//        denoms.forEach(d => bundles[d.id] = 0);
//        $(listId).innerHTML = denoms.map(d => `
//            <tr>
//                <td class="ps-3">
//                    <span class="fw-bold">${d.label}</span>
//                </td>
//                <td style="min-width:200px;">
//                    <div class="d-flex align-items-center gap-2">
//                        <input type="range"
//                               class="form-range flex-grow-1"
//                               id="dt-slider-${d.id}"
//                               min="0" max="${MAX_BUNDLES}" value="0" step="1"
//                               oninput="dtSlide('${d.id}', ${d.value}, '${type}', this.value)" />
//                        <input type="number"
//                               class="form-control form-control-sm text-center"
//                               id="dt-qty-${d.id}"
//                               min="0" max="${MAX_BUNDLES}" value="0"
//                               style="width:70px;"
//                               oninput="dtInput('${d.id}', ${d.value}, '${type}', this.value)" />
//                        <span class="text-muted small text-nowrap">${type === 'note' ? 'bdl' : 'box'}</span>
//                    </div>
//                </td>
//                <td class="text-end pe-3">
//                    <span class="fw-bold ${type === 'note' ? 'text-primary' : 'text-warning'}"
//                          id="dt-sub-${d.id}">—</span>
//                </td>
//            </tr>`).join('');
//    }

//    window.dtSlide = function (id, value, type, val) {
//        bundles[id] = parseInt(val) || 0;
//        $('dt-qty-' + id).value = bundles[id];
//        refreshTotals();
//        notifyParent();
//    };

//    window.dtInput = function (id, value, type, val) {
//        bundles[id] = Math.min(MAX_BUNDLES, Math.max(0, parseInt(val) || 0));
//        $('dt-qty-' + id).value = bundles[id];
//        $('dt-slider-' + id).value = bundles[id];
//        refreshTotals();
//        notifyParent();
//    };

//    function refreshTotals() {
//        // Row subtotals
//        [...window.denomData.notes, ...window.denomData.coins].forEach(d => {
//            const sub = $('dt-sub-' + d.id);
//            if (!sub) return;
//            const a = amount(d.id, d.value);
//            sub.textContent = a > 0 ? fmt(a) : '—';
//        });

//        const nt = noteTotal(), ct = coinTotal(), gt = grandTotal();

//        // Badges
//        $('dt-notes-badge').textContent = fmt(nt);
//        $('dt-coins-badge').textContent = fmt(ct);

//        // Totals
//        $('dt-notes-total').textContent = fmt(nt);
//        $('dt-coins-total').textContent = fmt(ct);
//        $('dt-grand-total').textContent = fmt(gt);
//        $('dt-grand-words').textContent = toWords(gt);

//    }

//    // ── Amount in words ───────────────────────────────────────
//    const ONES = ['', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine',
//        'Ten', 'Eleven', 'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen',
//        'Seventeen', 'Eighteen', 'Nineteen'];
//    const TENS = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety'];

//    function wordsUnder1000(n) {
//        if (n === 0) return '';
//        if (n < 20) return ONES[n];
//        if (n < 100) return TENS[Math.floor(n / 10)] + (n % 10 ? ' ' + ONES[n % 10] : '');
//        return ONES[Math.floor(n / 100)] + ' Hundred' + (n % 100 ? ' ' + wordsUnder1000(n % 100) : '');
//    }

//    function toWords(n) {
//        if (n === 0) return 'Zero shillings only';
//        const parts = [];
//        const trillions = Math.floor(n / 1_000_000_000_000); n %= 1_000_000_000_000;
//        const billions = Math.floor(n / 1_000_000_000); n %= 1_000_000_000;
//        const millions = Math.floor(n / 1_000_000); n %= 1_000_000;
//        const thousands = Math.floor(n / 1_000); n %= 1_000;
//        if (trillions) parts.push(wordsUnder1000(trillions) + ' Trillion');
//        if (billions) parts.push(wordsUnder1000(billions) + ' Billion');
//        if (millions) parts.push(wordsUnder1000(millions) + ' Million');
//        if (thousands) parts.push(wordsUnder1000(thousands) + ' Thousand');
//        if (n) parts.push(wordsUnder1000(n));
//        return parts.join(' ') + ' shillings only';
//    }

//    window.dtGetBundles = () => ({ ...bundles });
//    window.dtGetGrandTotal = () => grandTotal();
//    window.dtHasValue = () => grandTotal() > 0;
//    window.dtShowError = () => $('dt-err-notes').style.removeProperty('display');
//    window.dtHideError = () => $('dt-err-notes').style.display = 'none';

//    function notifyParent() {
//        document.dispatchEvent(new CustomEvent('denomChanged', {
//            detail: { grandTotal: grandTotal(), bundles: { ...bundles } }
//        }));
//    }

//    // ── Init ──────────────────────────────────
//    buildRows(window.denomData.notes, 'dt-list-notes', 'note');
//    buildRows(window.denomData.coins, 'dt-list-coins', 'coin');
//    refreshTotals();
//})();