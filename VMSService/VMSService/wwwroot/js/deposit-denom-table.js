(function () {
    const BUNDLE_SIZE = 1000;
    const fmt = n => 'UGX ' + Math.round(n).toLocaleString();
    const $ = id => document.getElementById(id);

    // SubCategory constants
    const SUB_FIT = 1;
    const SUB_UNFIT = 2;
    const SUB_NA = 3;

    const SUB_OPTIONS = [
        { id: SUB_FIT,   label: 'Fit',   cssClass: 'text-bg-success' },
        { id: SUB_UNFIT, label: 'Unfit', cssClass: 'text-bg-danger'  },
        { id: SUB_NA,    label: 'N/A',   cssClass: 'text-bg-secondary' },
    ];

    // Track state per row: { denomId, typeId, denomValue, qty, type, subCategoryId }
    const rows = {};
    // For notes, key is `${denomValue}-${subCategoryId}` to allow same denom in different subcategories
    const usedCombos = { notes: new Set(), coins: new Set() };
    let rid = 0;

    // ?? Add Row ???????????????????????????????????????????????
    window.depositDenomAddRow = function (type) {
        const denoms = type === 'notes'
            ? window.denomData.notes
            : window.denomData.coins;

        if (!denoms.length) {
            alert('No denominations available.');
            return;
        }

        const id = `dep-${type}-${++rid}`;
        const defaultSub = type === 'coins' ? SUB_NA : SUB_FIT;
        rows[id] = { denomId: null, typeId: null, denomValue: 0, qty: 0, type, subCategoryId: defaultSub };

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
            // Banknotes: Denomination | Subcategory | Bundles | Amount | Actions
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
                           type="number" min="0" placeholder="0"
                           oninput="depDenomOnInput('${id}','${type}',this)" />
                </td>
                <td class="px-4 py-2 text-end fw-bold text-muted" id="dep-denom-amt-${id}">—</td>
                <td class="px-2 py-2 text-center">
                    <button type="button"
                            class="btn text-danger p-0"
                            onclick="depDenomRemoveRow('${id}','${type}')">
                        <i class="ti ti-trash fs-5"></i>
                    </button>
                </td>`;
        } else {
            // Coins: Denomination | Boxes | Amount | Actions (subcategory fixed to N/A)
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
                           type="number" min="0" placeholder="0"
                           oninput="depDenomOnInput('${id}','${type}',this)" />
                </td>
                <td class="px-4 py-2 text-end fw-bold text-muted" id="dep-denom-amt-${id}">—</td>
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

    // ?? On Denomination Selected ??????????????????????????????
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

    // ?? On Subcategory Selected ???????????????????????????????
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
        refreshTotals();
    };

    // ?? On Quantity Input ?????????????????????????????????????
    window.depDenomOnInput = function (id, type, el) {
        rows[id].qty = Math.max(0, parseInt(el.value) || 0);
        recalcRow(id);
        refreshTotals();
    };

    // ?? Remove Row ????????????????????????????????????????????
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

    // ?? Recalc single row amount ??????????????????????????????
    function recalcRow(id) {
        const r = rows[id];
        const amt = r.qty * BUNDLE_SIZE * r.denomValue;
        const el = $(`dep-denom-amt-${id}`);
        if (!el) return;
        if (amt > 0) {
            el.textContent = fmt(amt);
            el.className = `px-4 py-2 text-end fw-bold ${r.type === 'notes' ? 'text-primary' : 'text-warning'}`;
        } else {
            el.textContent = '—';
            el.className = 'px-4 py-2 text-end fw-bold text-muted';
        }
    }

    // ?? Refresh disabled options ??????????????????????????????
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

    // ?? Refresh totals ????????????????????????????????????????
    function refreshTotals() {
        let fitAmt = 0, unfitAmt = 0, noteAmt = 0, coinAmt = 0;

        Object.values(rows).forEach(r => {
            const amt = r.qty * BUNDLE_SIZE * r.denomValue;
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

    // ?? Public API ????????????????????????????????????????????
    window.depDtHasValue = () =>
        Object.values(rows).some(r => r.qty > 0 && r.denomValue > 0);

    window.depDtGetRows = () => ({ ...rows });

    // ?? Amount in words ???????????????????????????????????????
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