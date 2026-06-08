(function () {
    const BUNDLE_SIZE = 1000;
    const fmt = n => 'UGX ' + Math.round(n).toLocaleString();
    const $ = id => document.getElementById(id);

    // SubCategory constants - will be populated from server data
    let SUB_FIT;
    let SUB_UNFIT;
    let SUB_N;
    let SUB_OPTIONS = [];

    // Initialize subcategories from server data
    function initializeSubcategories() {
        if (window.denomData && window.denomData.subcategories) {
            SUB_OPTIONS = window.denomData.subcategories.map(s => ({
                id: s.id,
                label: s.label,
                cssClass: getSubcategoryCssClass(s.label)
            }));

            // Set constants based on subcategory names
            SUB_OPTIONS.forEach(opt => {
                if (opt.label.toLowerCase() === 'fit') SUB_FIT = opt.id;
                else if (opt.label.toLowerCase() === 'unfit') SUB_UNFIT = opt.id;
                else if (opt.label.toLowerCase() === 'n/a') SUB_NA = opt.id;
            });
        }
    }

    // Helper function to determine CSS class based on subcategory label
    function getSubcategoryCssClass(label) {
        const lower = label.toLowerCase();
        if (lower === 'fit') return 'text-bg-success';
        if (lower === 'unfit') return 'text-bg-danger';
        if (lower === 'n/a') return 'text-bg-secondary';
        return 'text-bg-secondary'; // default
    }

    // Track state per row: { denomId, typeId, denomValue, qty, amount, type, subCategoryId, valid }
    const rows = {};
    // For notes, key is `${denomValue}-${subCategoryId}` to allow same denom in different subcategories
    const usedCombos = { notes: new Set(), coins: new Set() };
    let rid = 0;

    // Strip non-digit characters and parse to integer
    function parseAmount(str) {
        return Math.max(0, parseInt(str.replace(/[^0-9]/g, ''), 10) || 0);
    }

    // Format a number with commas for display inside the input
    function fmtInput(n) {
        return n > 0 ? n.toLocaleString() : '';
    }

    // Add Row
    window.depositDenomAddRow = function (type) {
        // Initialize subcategories if not already done
        if (SUB_OPTIONS.length === 0) {
            initializeSubcategories();
        }

        const denoms = type === 'notes'
            ? window.denomData.notes
            : window.denomData.coins;

        if (!denoms.length) {
            alert('No denominations available.');
            return;
        }

        const id = `dep-${type}-${++rid}`;
        const defaultSub = type === 'coins' ? SUB_NA : SUB_FIT;
        rows[id] = { denomId: null, typeId: null, denomValue: 0, qty: 0, amount: 0, type, subCategoryId: defaultSub, valid: true };

        const tbody = $(`dep-denom-${type}-body`);
        const empty = tbody.querySelector('.denom-empty-row');
        if (empty) empty.remove();

        const denomOptHtml = '<option value="">-- Select denomination --</option>'
            + denoms.map(d =>
                `<option value="${d.value}"
                         data-denom-id="${d.id}"
                         data-type-id="${d.typeId}">
                    ${d.label}
                </option>`).join('');

        const tr = document.createElement('tr');
        tr.id = `dep-denom-row-${id}`;
        tr.className = 'border-bottom';

        if (type === 'notes') {
            // Banknotes: Denomination | Subcategory | Amount (input) | Bundles (computed) | Actions
            const subOptHtml = SUB_OPTIONS.map(s =>
                `<option value="${s.id}" ${s.id === defaultSub ? 'selected' : ''}>${s.label}</option>`
            ).join('');

            tr.innerHTML = `
                <td class="px-4 py-2">
                    <select class="form-select form-select-sm"
                            id="dep-denom-sel-${id}"
                            onchange="depDenomOnSel('${id}','${type}',this)">
                        ${denomOptHtml}
                    </select>
                </td>
                <td class="px-4 py-2">
                    <select class="form-select form-select-sm"
                            id="dep-sub-sel-${id}"
                            onchange="depSubOnSel('${id}',this)">
                        ${subOptHtml}
                    </select>
                </td>
                <td class="px-4 py-2">
                    <input class="form-control form-control-sm text-end"
                           id="dep-denom-inp-${id}"
                           type="text" inputmode="numeric" placeholder="0"
                           oninput="depDenomOnAmountInput('${id}','${type}',this)" />
                    <div class="text-danger small mt-1 d-none" id="dep-denom-warn-${id}">
                        <i class="ti ti-alert-triangle me-1"></i>Amount must equal a whole number of bundles
                    </div>
                </td>
                <td class="px-4 py-2 text-end fw-bold text-muted" id="dep-denom-bundles-${id}">-</td>
                <td class="px-2 py-2 text-center">
                    <button type="button"
                            class="btn text-danger p-0"
                            onclick="depDenomRemoveRow('${id}','${type}')">
                        <i class="ti ti-trash fs-5"></i>
                    </button>
                </td>`;
        } else {
            // Coins: Denomination | Amount (input) | Boxes (computed) | Actions
            tr.innerHTML = `
                <td class="px-4 py-2">
                    <select class="form-select form-select-sm"
                            id="dep-denom-sel-${id}"
                            onchange="depDenomOnSel('${id}','${type}',this)">
                        ${denomOptHtml}
                    </select>
                </td>
                <td class="px-4 py-2">
                    <input class="form-control form-control-sm text-end"
                           id="dep-denom-inp-${id}"
                           type="text" inputmode="numeric" placeholder="0"
                           oninput="depDenomOnAmountInput('${id}','${type}',this)" />
                    <div class="text-danger small mt-1 d-none" id="dep-denom-warn-${id}">
                        <i class="ti ti-alert-triangle me-1"></i>Amount must equal a whole number of boxes
                    </div>
                </td>
                <td class="px-4 py-2 text-end fw-bold text-muted" id="dep-denom-bundles-${id}">-</td>
                <td class="px-2 py-2 text-center">
                    <button type="button"
                            class="btn text-danger p-0"
                            onclick="depDenomRemoveRow('${id}','${type}')">
                        <i class="ti ti-trash fs-5"></i>
                    </button>
                </td>`;
        }
        tbody.appendChild(tr);
    };

    // On Denomination Selected 
    window.depDenomOnSel = function (id, type, sel) {
        const opt = sel.options[sel.selectedIndex];
        const newVal = parseInt(sel.value) || 0;
        const prevVal = rows[id]?.denomValue || 0;
        const sub = rows[id].subCategoryId;

        // Free old combo, claim new
        if (prevVal) usedCombos[type].delete(`${prevVal}-${sub}`);
        if (newVal) usedCombos[type].add(`${newVal}-${sub}`);

        rows[id].denomValue = newVal;
        rows[id].denomId = parseInt(opt.dataset.denomId) || 0;
        rows[id].typeId = parseInt(opt.dataset.typeId) || 0;

        refreshDepositSelects(type);
        recalcRow(id);
        refreshTotals();
    };

    // On Subcategory Selected 
    window.depSubOnSel = function (id, sel) {
        const row = rows[id];
        const prevSub = row.subCategoryId;
        const newSub = parseInt(sel.value);
        const type = row.type;

        // Free old combo, claim new
        if (row.denomValue) {
            usedCombos[type].delete(`${row.denomValue}-${prevSub}`);
            usedCombos[type].add(`${row.denomValue}-${newSub}`);
        }

        row.subCategoryId = newSub;
        refreshDepositSelects(type);
        recalcRow(id);
        refreshTotals();
    };

    // On Amount Input — user enters the amount, we format with commas and compute bundles/boxes
    window.depDenomOnAmountInput = function (id, type, el) {
        const raw = parseAmount(el.value);
        rows[id].amount = raw;

        // Preserve cursor position relative to digits
        const cursorPos = el.selectionStart;
        const beforeCursor = el.value.substring(0, cursorPos);
        const digitsBefore = beforeCursor.replace(/[^0-9]/g, '').length;

        el.value = fmtInput(raw);

        // Restore cursor position
        let digits = 0, newPos = 0;
        for (let i = 0; i < el.value.length; i++) {
            if (/[0-9]/.test(el.value[i])) digits++;
            if (digits === digitsBefore) { newPos = i + 1; break; }
        }
        if (digitsBefore === 0) newPos = 0;
        el.setSelectionRange(newPos, newPos);

        recalcRow(id);
        refreshTotals();
    };

    // Remove Row
    window.depDenomRemoveRow = function (id, type) {
        const row = rows[id];
        if (row && row.denomValue) {
            usedCombos[type].delete(`${row.denomValue}-${row.subCategoryId}`);
        }
        delete rows[id];

        $(`dep-denom-row-${id}`)?.remove();
        refreshDepositSelects(type);

        const tbody = $(`dep-denom-${type}-body`);
        if (!tbody.querySelector('tr[id]')) {
            const tr = document.createElement('tr');
            tr.className = 'denom-empty-row';
            tr.innerHTML = `<td colspan="${type === 'notes' ? 5 : 4}" class="text-center py-3 text-muted small">
                No ${type === 'notes' ? 'banknotes' : 'coins'} added</td>`;
            tbody.appendChild(tr);
        }

        refreshTotals();
    };

    // Recalc single row — compute bundles/boxes from amount and show warning if not whole
    function recalcRow(id) {
        const r = rows[id];
        const bundlesEl = $(`dep-denom-bundles-${id}`);
        const warnEl = $(`dep-denom-warn-${id}`);
        const inputEl = $(`dep-denom-inp-${id}`);
        if (!bundlesEl) return;

        const bundleValue = r.denomValue * BUNDLE_SIZE;

        if (r.amount > 0 && bundleValue > 0) {
            const bundles = r.amount / bundleValue;
            const isWhole = Number.isInteger(bundles);
            const label = r.type === 'notes' ? 'bundle' : 'box';
            const pluralLabel = r.type === 'notes' ? 'bundles' : 'boxes';

            r.qty = isWhole ? bundles : 0;
            r.valid = isWhole;

            bundlesEl.textContent = isWhole
                ? `${bundles} ${bundles === 1 ? label : pluralLabel}`
                : `${bundles.toFixed(2)} ${pluralLabel}`;
            bundlesEl.className = isWhole
                ? `px-4 py-2 text-end fw-bold`
                : 'px-4 py-2 text-end fw-bold text-danger';

            // Show/hide warning
            if (warnEl) warnEl.classList.toggle('d-none', isWhole);
            if (inputEl) {
                inputEl.classList.toggle('is-invalid', !isWhole);
            }
        } else {
            r.qty = 0;
            r.valid = true;
            bundlesEl.textContent = '-';
            bundlesEl.className = 'px-4 py-2 text-end fw-bold text-muted';
            if (warnEl) warnEl.classList.add('d-none');
            if (inputEl) inputEl.classList.remove('is-invalid');
        }
    }

    // Refresh disabled options
    // For coins: same denomination can only appear once (always N/A)
    // For notes: same denomination+subcategory combo can only appear once
    function refreshDepositSelects(type) {
        const selectors = document.querySelectorAll(`#dep-denom-${type}-body .form-select[id^="dep-denom-sel-"]`);
        selectors.forEach(sel => {
            const rowId = sel.id.replace('dep-denom-sel-', '');
            const row = rows[rowId];
            if (!row) return;

            Array.from(sel.options).forEach(o => {
                if (!o.value) return;
                const combo = `${parseInt(o.value)}-${row.subCategoryId}`;
                o.disabled = usedCombos[type].has(combo) && parseInt(o.value) !== row.denomValue;
            });
        });
    }

    // Refresh totals — only count valid (whole-bundle) rows
    function refreshTotals() {
        // Initialize subcategories if not already done
        if (SUB_OPTIONS.length === 0) {
            initializeSubcategories();
        }

        let fitAmt = 0, unfitAmt = 0, noteAmt = 0, coinAmt = 0;

        Object.values(rows).forEach(r => {
            if (!r.valid) return;
            const amt = r.amount;
            if (r.type === 'notes') {
                noteAmt += amt;
                if (r.subCategoryId === SUB_FIT) fitAmt += amt;
                if (r.subCategoryId === SUB_UNFIT) unfitAmt += amt;
            } else {
                coinAmt += amt;
            }
        });

        const grand = noteAmt + coinAmt;

        $('dep-denom-note-sub').textContent = fmt(noteAmt);
        $('dep-denom-coin-sub').textContent = fmt(coinAmt);
        $('dep-t-fit').textContent = fmt(fitAmt);
        $('dep-t-unfit').textContent = fmt(unfitAmt);
        $('dep-t-notes').textContent = fmt(noteAmt);
        $('dep-t-coins').textContent = fmt(coinAmt);
        $('dep-g-val').textContent = fmt(grand);
        $('dep-g-words').textContent = toWords(grand) + ' only';

        document.dispatchEvent(new CustomEvent('depositDenomChanged', {
            detail: { grandTotal: grand }
        }));
    }

    // Public API — only valid rows count
    window.depDtHasValue = () =>
        Object.values(rows).some(r => r.valid && r.amount > 0 && r.denomValue > 0);

    // Returns true if all rows with amounts have whole bundles/boxes
    window.depDtIsValid = () =>
        Object.values(rows).every(r => r.valid);

    window.depDtGetRows = () => ({ ...rows });

    // Amount in words
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

    // Initialize subcategories when the script loads
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeSubcategories);
    } else {
        initializeSubcategories();
    }
})();