(function () {
    const fmt = n => 'UGX ' + Math.round(n).toLocaleString();
    const $ = id => document.getElementById(id);

    // Track state per row: { denomId, typeId, denomValue, qty, amount, type }
    const rows = {};
    const usedValues = { notes: new Set(), coins: new Set() };
    let rid = 0;

    // ── Current mode ──────────────────────────────────────────
    let currentMode = 'lumpsum'; // 'breakdown' | 'lumpsum'
    let lumpSumValue = 0;

    // ── Mode Switching ────────────────────────────────────────
    window.setDenomMode = function (mode) {
        if (mode === currentMode) return;
        if ($(`mode-radio-${mode}`)?.disabled) return;
        currentMode = mode;
        const isBreakdown = mode === 'breakdown';
        $('denom-breakdown-section').style.display = isBreakdown ? '' : 'none';
        $('denom-lumpsum-section').style.display = isBreakdown ? 'none' : '';
        $('denom-add-buttons').classList.toggle('d-none', !isBreakdown); 
        if (isBreakdown) clearLumpSum();
        else clearBreakdown();
        refreshTotals();
    };

    // ── Clear helpers ─────────────────────────────────────────
    function clearLumpSum() {
        lumpSumValue = 0;
        const inp = $('lumpSumInput');
        if (inp) inp.value = '';
        showLumpSumError(false);
    }

    function clearBreakdown() {
        ['notes', 'coins'].forEach(type => {
            Object.keys(rows)
                .filter(id => rows[id].type === type)
                .forEach(id => denomRemoveRow(id, type));
        });
    }

    // ── Lump Sum Input ────────────────────────────────────────
    window.onLumpSumInput = function (val) {
        const n = parseInt(val) || 0;
        const bundleMultiple = window.withdrawalBundleMultiple;
        const isBundleValid = n > 0 && n % bundleMultiple === 0;
        const isBelowMinimum = isBundleValid && n < window.minWithdrawalAmount;

        if (n > 0 && !isBundleValid) {
            showLumpSumError(true, 'Amount must be a whole bundle — no partial amounts allowed.');
        } else if (isBelowMinimum) {
            showLumpSumError(true, `Minimum withdrawal amount is UGX ${window.minWithdrawalAmount.toLocaleString()}.`);
        } else {
            showLumpSumError(false);
        }

        lumpSumValue = isBundleValid ? n : 0;
        refreshTotals();
    };

    function showLumpSumError(show, message) {
        const err = $('lumpSumError');
        const errText = $('lumpSumErrorText');
        const hint = $('lumpSumHint');
        if (!err) return;
        if (errText && message) errText.textContent = message;
        err.style.setProperty('display', show ? 'block' : 'none', 'important');
        if (hint) hint.style.display = show ? 'none' : '';
    }

    // ── Add Row ───────────────────────────────────────────────
    window.denomAddRow = function (type) {
        const denoms = type === 'notes'
            ? window.denomData.notes
            : window.denomData.coins;

        const available = denoms.filter(d => !usedValues[type].has(d.value));
        if (!available.length) { alert('All denominations have been added.'); return; }

        const id = `${type}-${++rid}`;
        rows[id] = { denomId: null, typeId: null, denomValue: 0, qty: 0, amount: 0, type };

        const tbody = $(`denom-${type}-body`);
        const empty = tbody.querySelector('.denom-empty-row');
        if (empty) empty.remove();

        const unitLabel = type === 'notes' ? 'bundle' : 'box';

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
                       id="denom-amt-inp-${id}"
                       type="text" inputmode="numeric" pattern="[0-9]*" placeholder="0"
                       oninput="denomOnAmountInput('${id}','${type}',this)" />
                <small id="denom-amt-err-${id}" class="text-danger mt-1 d-none">
                    <i class="ti ti-alert-circle me-1"></i>
                    Must be a whole ${unitLabel} — no partial amounts.
                </small>
            </td>
            <td class="px-4 py-2 text-end fw-bold text-muted" id="denom-qty-${id}">—</td>
            <td class="px-2 py-2 text-center">
                <button type="button" class="btn btn-link text-danger p-0"
                        onclick="denomRemoveRow('${id}','${type}')">
                    <i class="ti ti-trash fs-5"></i>
                </button>
            </td>`;
        tbody.appendChild(tr);
        return id;
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

        // Re-validate already-entered amount against new denomination
        const inp = $(`denom-amt-inp-${id}`);
        if (inp && inp.value) denomOnAmountInput(id, type, inp);

        refreshSelects(type);
        refreshTotals();
    };

    // ── On Amount Input ───────────────────────────────────────
    window.denomOnAmountInput = function (id, type, el) {
        el.value = el.value.replace(/\D/g, '');
        const r = rows[id];
        const enteredAmt = parseInt(el.value) || 0;
        const err = $(`denom-amt-err-${id}`);
        const qtyEl = $(`denom-qty-${id}`);
        const unitLabel = type === 'notes' ? 'bundle' : 'box';

        // Need a denomination selected first
        if (!r.denomValue) {
            err.classList.remove('d-none');
            err.innerHTML = '<i class="ti ti-alert-circle me-1"></i>Select a denomination first.';
            r.amount = 0; r.qty = 0;
            qtyEl.textContent = '—';
            qtyEl.className = 'px-4 py-2 text-end fw-bold text-muted';
            refreshTotals();
            return;
        }

        const bundleValue = r.denomValue * window.withdrawalBundleMultiple;
        const valid = enteredAmt > 0 && enteredAmt % bundleValue === 0;

        if (enteredAmt > 0 && !valid) {
            err.classList.remove('d-none');
            err.innerHTML = `<i class="ti ti-alert-circle me-1"></i>
                Amount must be a multiple of UGX ${bundleValue.toLocaleString()} (1 ${unitLabel}).`;
            r.amount = 0; r.qty = 0;
            qtyEl.textContent = '—';
            qtyEl.className = 'px-4 py-2 text-end fw-bold text-muted';
        } else {
            err.classList.add('d-none');
            r.amount = enteredAmt;
            r.qty = valid ? enteredAmt / bundleValue : 0;
            if (r.qty > 0) {
                qtyEl.textContent = r.qty.toLocaleString();
                qtyEl.className = `px-4 py-2 text-end fw-bold ${type === 'notes' ? 'text-primary' : 'text-warning'}`;
            } else {
                qtyEl.textContent = '—';
                qtyEl.className = 'px-4 py-2 text-end fw-bold text-muted';
            }
        }

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
            const isNote = window.denomData.notes.some(d => d.id === saved.id);
            const type = isNote ? 'notes' : 'coins';
            const qty = saved.bundles ?? saved.boxes ?? 0;
            if (!qty) return;

            const allDenoms = [...window.denomData.notes, ...window.denomData.coins];
            const denom = allDenoms.find(d => d.id === saved.id);
            if (!denom) return;

            const rowId = window.denomAddRow(type);
            const sel = $(`denom-sel-${rowId}`);
            if (sel) { sel.value = denom.value; denomOnSel(rowId, type, sel); }

            const bundleValue = denom.value * window.withdrawalBundleMultiple;
            const amt = qty * bundleValue;
            const inp = $(`denom-amt-inp-${rowId}`);
            if (inp) { inp.value = amt; denomOnAmountInput(rowId, type, inp); }
        });

        refreshTotals();
    };

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
        if (currentMode === 'lumpsum') {
            $('denom-note-sub').textContent = fmt(0);
            $('denom-coin-sub').textContent = fmt(0);
            $('denom-t-notes').textContent = fmt(0);
            $('denom-t-coins').textContent = fmt(0);
            $('denom-g-val').textContent = fmt(lumpSumValue);
            $('denom-g-words').textContent = toWords(lumpSumValue);
            document.dispatchEvent(new CustomEvent('denomChanged', {
                detail: { grandTotal: lumpSumValue, mode: 'lumpsum' }
            }));
            return;
        }

        const noteAmt = Object.values(rows)
            .filter(r => r.type === 'notes')
            .reduce((s, r) => s + (r.amount || 0), 0);

        const coinAmt = Object.values(rows)
            .filter(r => r.type === 'coins')
            .reduce((s, r) => s + (r.amount || 0), 0);

        const grand = noteAmt + coinAmt;

        $('denom-note-sub').textContent = fmt(noteAmt);
        $('denom-coin-sub').textContent = fmt(coinAmt);
        $('denom-t-notes').textContent = fmt(noteAmt);
        $('denom-t-coins').textContent = fmt(coinAmt);
        $('denom-g-val').textContent = fmt(grand);
        $('denom-g-words').textContent = toWords(grand);
        document.dispatchEvent(new CustomEvent('denomChanged', {
            detail: { grandTotal: grand, mode: 'breakdown' }
        }));
    }

    // ── Public API ────────────────────────────────────────────
    window.dtGetMode = () => currentMode;
    window.dtGetLumpSum = () => lumpSumValue;
    window.dtHasValue = () => currentMode === 'lumpsum'
        ? lumpSumValue > 0
        : Object.values(rows).some(r => r.amount > 0 && r.denomValue > 0);
    window.dtGetRows = () => ({ ...rows });
    window.dtGetGrandTotal = () => currentMode === 'lumpsum'
        ? lumpSumValue
        : Object.values(rows).reduce((s, r) => s + (r.amount || 0), 0);

    // ── Init on load ──────────────────────────────────────────
    function initDenomSection() {
        $('denom-breakdown-section').style.display = 'none';
        $('denom-lumpsum-section').style.display = '';
        $('denom-add-buttons').classList.add('d-none'); // ← changed
        currentMode = 'lumpsum';
        if (window.savedDenoms?.length) preFillDenoms();
    }

    document.addEventListener('DOMContentLoaded', initDenomSection);

})();