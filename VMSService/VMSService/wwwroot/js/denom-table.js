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

        return id; // return row id for pre-fill use
    };

    // ── On Denomination Selected ──────────────────────────────
    window.denomOnSel = function (id, type, sel) {
        const opt = sel.options[sel.selectedIndex];
        const newVal = parseInt(sel.value) || 0;
        const prevVal = rows[id]?.denomValue || 0;

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

    // ── Pre-fill rows from saved denominations (edit page) ────
    window.preFillDenoms = function () {
        if (!window.savedDenoms?.length) return;

        window.savedDenoms.forEach(saved => {
            // Determine type from denomData
            const isNote = window.denomData.notes.some(d => d.id === saved.id);
            const type = isNote ? 'notes' : 'coins';
            const qty = saved.bundles ?? saved.boxes ?? 0;

            if (!qty) return;

            // Find the denom in denomData to get value
            const allDenoms = [...window.denomData.notes, ...window.denomData.coins];
            const denom = allDenoms.find(d => d.id === saved.id);
            if (!denom) return;

            // Add a new row and get its id
            const rowId = window.denomAddRow(type);

            // Pre-select the denomination
            const sel = $(`denom-sel-${rowId}`);
            if (sel) {
                sel.value = denom.value;
                denomOnSel(rowId, type, sel);
            }

            // Pre-fill the quantity
            const inp = $(`denom-inp-${rowId}`);
            if (inp) {
                inp.value = qty;
                rows[rowId].qty = qty;
                recalcRow(rowId);
            }
        });

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
        $('denom-g-words').textContent = toWords(grand);

        document.dispatchEvent(new CustomEvent('denomChanged', {
            detail: { grandTotal: grand }
        }));
    }

    // ── Public API ────────────────────────────────────────────
    window.dtHasValue = () =>
        Object.values(rows).some(r => r.qty > 0 && r.denomValue > 0);

    window.dtGetRows = () => ({ ...rows });

    // ── Auto pre-fill on load if savedDenoms exists ───────────
    document.addEventListener('DOMContentLoaded', () => {
        if (window.savedDenoms?.length) preFillDenoms();
    });

})();

//(function () {
//    const BUNDLE_SIZE = 1000;
//    const fmt = n => 'UGX ' + Math.round(n).toLocaleString();
//    const $ = id => document.getElementById(id);

//    // Track state per row: { denomId, typeId, denomValue, qty, type }
//    const rows = {};
//    const usedValues = { notes: new Set(), coins: new Set() };
//    let rid = 0;

//    // ── Add Row ───────────────────────────────────────────────
//    window.denomAddRow = function (type) {
//        const denoms = type === 'notes'
//            ? window.denomData.notes
//            : window.denomData.coins;

//        const available = denoms.filter(d => !usedValues[type].has(d.value));
//        if (!available.length) {
//            alert('All denominations have been added.'); return;
//        }

//        const id = `${type}-${++rid}`;
//        rows[id] = { denomId: null, typeId: null, denomValue: 0, qty: 0, type };

//        const tbody = $(`denom-${type}-body`);
//        const empty = tbody.querySelector('.denom-empty-row');
//        if (empty) empty.remove();

//        const optHtml = '<option value="">— Select denomination —</option>'
//            + denoms.map(d =>
//                `<option value="${d.value}"
//                         data-denom-id="${d.id}"
//                         data-type-id="${d.typeId}"
//                         ${usedValues[type].has(d.value) ? 'disabled' : ''}>
//                    ${d.label}
//                </option>`).join('');

//        const tr = document.createElement('tr');
//        tr.id = `denom-row-${id}`;
//        tr.className = 'border-bottom';
//        tr.innerHTML = `
//            <td class="px-4 py-2">
//                <select class="form-select form-select-sm"
//                        id="denom-sel-${id}"
//                        onchange="denomOnSel('${id}','${type}',this)">
//                    ${optHtml}
//                </select>
//            </td>
//            <td class="px-4 py-2">
//                <input class="form-control form-control-sm text-end"
//                       id="denom-inp-${id}"
//                       type="number" min="0" placeholder="0"
//                       oninput="denomOnInput('${id}','${type}',this)" />
//            </td>
//            <td class="px-4 py-2 text-end fw-bold text-muted" id="denom-amt-${id}">—</td>
//            <td class="px-2 py-2 text-center">
//                <button type="button"
//                        class="btn btn-link text-danger p-0"
//                        onclick="denomRemoveRow('${id}','${type}')">
//                    <i class="ti ti-trash fs-5"></i>
//                </button>
//            </td>`;
//        tbody.appendChild(tr);
//    };

//    // ── On Denomination Selected ──────────────────────────────
//    window.denomOnSel = function (id, type, sel) {
//        const opt = sel.options[sel.selectedIndex];
//        const newVal = parseInt(sel.value) || 0;
//        const prevVal = rows[id]?.denomValue || 0;

//        // Free old, claim new
//        if (prevVal) usedValues[type].delete(prevVal);
//        if (newVal) usedValues[type].add(newVal);

//        rows[id].denomValue = newVal;
//        rows[id].denomId = parseInt(opt.dataset.denomId) || 0;
//        rows[id].typeId = parseInt(opt.dataset.typeId) || 0;

//        refreshSelects(type);
//        recalcRow(id);
//        refreshTotals();
//    };

//    // ── On Quantity Input ─────────────────────────────────────
//    window.denomOnInput = function (id, type, el) {
//        rows[id].qty = Math.max(0, parseInt(el.value) || 0);
//        recalcRow(id);
//        refreshTotals();
//    };

//    // ── Remove Row ────────────────────────────────────────────
//    window.denomRemoveRow = function (id, type) {
//        const val = rows[id]?.denomValue || 0;
//        if (val) usedValues[type].delete(val);
//        delete rows[id];

//        $(`denom-row-${id}`)?.remove();
//        refreshSelects(type);

//        const tbody = $(`denom-${type}-body`);
//        if (!tbody.querySelector('tr[id]')) {
//            const tr = document.createElement('tr');
//            tr.className = 'denom-empty-row';
//            tr.innerHTML = `<td colspan="4" class="text-center py-3 text-muted small">
//                No ${type === 'notes' ? 'banknotes' : 'coins'} added</td>`;
//            tbody.appendChild(tr);
//        }

//        refreshTotals();
//    };

//    // ── Recalc single row amount ──────────────────────────────
//    function recalcRow(id) {
//        const r = rows[id];
//        const amt = r.qty * BUNDLE_SIZE * r.denomValue;
//        const el = $(`denom-amt-${id}`);
//        if (!el) return;
//        if (amt > 0) {
//            el.textContent = fmt(amt);
//            el.className = `px-4 py-2 text-end fw-bold ${r.type === 'notes' ? 'text-primary' : 'text-warning'}`;
//        } else {
//            el.textContent = '—';
//            el.className = 'px-4 py-2 text-end fw-bold text-muted';
//        }
//    }

//    // ── Refresh disabled options across all selects ───────────
//    function refreshSelects(type) {
//        document.querySelectorAll(`#denom-${type}-body .form-select`).forEach(sel => {
//            const curVal = rows[sel.id.replace('denom-sel-', '')]?.denomValue || 0;
//            Array.from(sel.options).forEach(o => {
//                if (!o.value) return;
//                o.disabled = usedValues[type].has(parseInt(o.value))
//                    && parseInt(o.value) !== curVal;
//            });
//        });
//    }

//    // ── Refresh totals ────────────────────────────────────────
//    function refreshTotals() {
//        const noteAmt = Object.values(rows)
//            .filter(r => r.type === 'notes')
//            .reduce((s, r) => s + r.qty * BUNDLE_SIZE * r.denomValue, 0);

//        const coinAmt = Object.values(rows)
//            .filter(r => r.type === 'coins')
//            .reduce((s, r) => s + r.qty * BUNDLE_SIZE * r.denomValue, 0);

//        const grand = noteAmt + coinAmt;

//        $('denom-note-sub').textContent = fmt(noteAmt);
//        $('denom-coin-sub').textContent = fmt(coinAmt);
//        $('denom-t-notes').textContent = fmt(noteAmt);
//        $('denom-t-coins').textContent = fmt(coinAmt);
//        $('denom-g-val').textContent = fmt(grand);
//        $('denom-g-words').textContent = toWords(grand) + ' only';

//        document.dispatchEvent(new CustomEvent('denomChanged', {
//            detail: { grandTotal: grand }
//        }));
//    }

//    // ── Public API (used by withdrawal-form.js) ───────────────
//    window.dtHasValue = () =>
//        Object.values(rows).some(r => r.qty > 0 && r.denomValue > 0);

//    window.dtGetRows = () => ({ ...rows });

//})();

