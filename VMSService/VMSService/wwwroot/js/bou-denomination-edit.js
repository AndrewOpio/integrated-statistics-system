(function () {
    const BUNDLE_SIZE = 1000;
    const BOX_BUNDLES = 50;
    const fmt = n => 'UGX ' + Math.round(n).toLocaleString();

    let seed = { notes: [], coins: [], existing: [] };
    try {
        seed.notes = window.denomData?.notes ?? [];
        seed.coins = window.denomData?.coins ?? [];
        const existingEl = document.getElementById('dispatchSeedData');
        if (existingEl) seed.existing = JSON.parse(existingEl.textContent);
    } catch (e) {
        console.error('Failed to load dispatch seed data', e);
    }
    const rows = {};        // rowId → state
    let rid = 0;

    // ── Range parser ──────────────────────────────────────────
    // Accepts input like "BX001-BX010, BX020, BX030-BX032".
    // Returns { boxes: ["BX001",...], error: null } or { boxes: null, error: "..." }.
    function parseRanges(input) {
        if (!input || !input.trim()) return { boxes: [], error: null };

        const tokens = input.split(',').map(t => t.trim()).filter(Boolean);
        const boxes = [];

        for (const token of tokens) {
            const dashSplit = token.split(/[-–]/).map(p => p.trim());
            if (dashSplit.length === 1) {
                if (!/^[A-Za-z]+\d+$/.test(dashSplit[0]))
                    return { boxes: null, error: `Invalid box: "${token}"` };
                boxes.push(dashSplit[0].toUpperCase());
            } else if (dashSplit.length === 2) {
                const [a, b] = dashSplit;
                const ma = a.match(/^([A-Za-z]+)(\d+)$/);
                const mb = b.match(/^([A-Za-z]+)(\d+)$/);
                if (!ma || !mb || ma[1].toUpperCase() !== mb[1].toUpperCase())
                    return { boxes: null, error: `Invalid range: "${token}"` };

                const prefix = ma[1].toUpperCase();
                const width = ma[2].length;
                const start = parseInt(ma[2], 10);
                const end = parseInt(mb[2], 10);
                if (end < start)
                    return { boxes: null, error: `Range end before start: "${token}"` };

                for (let i = start; i <= end; i++)
                    boxes.push(prefix + String(i).padStart(width, '0'));
            } else {
                return { boxes: null, error: `Invalid token: "${token}"` };
            }
        }

        // Dedupe within this input
        const unique = [...new Set(boxes)];
        if (unique.length !== boxes.length)
            return { boxes: null, error: 'Duplicate box numbers within ranges' };

        return { boxes: unique, error: null };
    }

    // ── Denomination options ──────────────────────────────────
    function denomOptions(bundleType) {
        const src = bundleType === 'coin' ? seed.coins : seed.notes;
        return '<option value="">— Select —</option>' +
            src.map(d => `<option value="${d.id}"
                                  data-value="${d.value}"
                                  data-type-id="${d.typeId}">${d.label}</option>`).join('');
    }

    // ── Add row ───────────────────────────────────────────────
    window.addDispatchRow = function (bundleType, prefill) {
        const id = `r${++rid}`;
        rows[id] = {
            bundleType,
            denomId: prefill?.denominationId ?? 0,
            denomValue: prefill?.denominationValue ?? 0,
            denomTypeId: prefill?.denominationTypeId ?? 0,
            qty: prefill?.bundles ?? prefill?.boxes ?? 0,
            amount: prefill?.amount ?? 0,
            boxes: prefill?.boxNumbers ?? []
        };

        const tbody = document.getElementById(
            bundleType === 'new' ? 'newBanknotesBody'
                : bundleType === 'reissue' ? 'reissueBanknotesBody'
                    : 'coinsBody');

        const tr = document.createElement('tr');
        tr.id = `row-${id}`;

        if (bundleType === 'new') {
            tr.innerHTML = `
                <td class="ps-3"><select class="form-select form-select-sm"
                    onchange="onDenomChange('${id}', this)">${denomOptions('new')}</select></td>
                <td>
                    <input type="text" class="form-control form-control-sm"
                           id="ranges-${id}" placeholder="e.g. BX001-BX010, BX020"
                           oninput="onRangesChange('${id}', this)" />
                    <small class="text-muted d-block mt-1" id="rangesInfo-${id}"></small>
                    <small class="text-danger d-none" id="rangesError-${id}"></small>
                </td>
                <td class="text-end fw-bold" id="qty-${id}">—</td>
                <td class="text-end fw-bold text-primary" id="amt-${id}">UGX 0</td>
                <td class="text-center">
                    <button type="button" class="btn btn-link text-danger p-0"
                            onclick="removeDispatchRow('${id}')"><i class="ti ti-trash"></i></button>
                </td>`;
        } else if (bundleType === 'reissue') {
            tr.innerHTML = `
                <td class="ps-3"><select class="form-select form-select-sm"
                    onchange="onDenomChange('${id}', this)">${denomOptions('reissue')}</select></td>
                <td><input type="number" min="0" class="form-control form-control-sm text-end"
                    oninput="onQtyChange('${id}', this.value)" value="${rows[id].qty || ''}" /></td>
                <td class="text-end fw-bold text-primary" id="amt-${id}">UGX 0</td>
                <td class="text-center">
                    <button type="button" class="btn btn-link text-danger p-0"
                            onclick="removeDispatchRow('${id}')"><i class="ti ti-trash"></i></button>
                </td>`;
        } else {
            tr.innerHTML = `
                <td class="ps-3"><select class="form-select form-select-sm"
                    onchange="onDenomChange('${id}', this)">${denomOptions('coin')}</select></td>
                <td><input type="number" min="0" class="form-control form-control-sm text-end"
                    oninput="onQtyChange('${id}', this.value)" value="${rows[id].qty || ''}" /></td>
                <td class="text-end fw-bold text-warning" id="amt-${id}">UGX 0</td>
                <td class="text-center">
                    <button type="button" class="btn btn-link text-danger p-0"
                            onclick="removeDispatchRow('${id}')"><i class="ti ti-trash"></i></button>
                </td>`;
        }

        tbody.appendChild(tr);

        // Prefill selects and inputs
        if (prefill?.denominationId) {
            const sel = tr.querySelector('select');
            sel.value = prefill.denominationId;
            onDenomChange(id, sel);
        }
        if (bundleType === 'new' && prefill?.boxNumbers?.length) {
            const inp = document.getElementById(`ranges-${id}`);
            inp.value = prefill.boxNumbers.join(', ');
            onRangesChange(id, inp);
        }
    };

    window.removeDispatchRow = function (id) {
        delete rows[id];
        document.getElementById(`row-${id}`)?.remove();
        refreshTotals();
    };

    window.onDenomChange = function (id, sel) {
        const opt = sel.options[sel.selectedIndex];
        rows[id].denomId = parseInt(sel.value) || 0;
        rows[id].denomValue = parseFloat(opt.dataset.value) || 0;
        rows[id].denomTypeId = parseInt(opt.dataset.typeId) || 0;
        recomputeRow(id);
    };

    window.onQtyChange = function (id, val) {
        rows[id].qty = parseInt(val) || 0;
        recomputeRow(id);
    };

    window.onRangesChange = function (id, inp) {
        const { boxes, error } = parseRanges(inp.value);
        const info = document.getElementById(`rangesInfo-${id}`);
        const err = document.getElementById(`rangesError-${id}`);

        if (error) {
            info.textContent = '';
            err.textContent = error;
            err.classList.remove('d-none');
            rows[id].boxes = [];
            rows[id].qty = 0;
        } else {
            err.classList.add('d-none');
            rows[id].boxes = boxes;
            rows[id].qty = boxes.length * BOX_BUNDLES;
            info.textContent = `${boxes.length} box(es), ${rows[id].qty} bundles`;
        }
        recomputeRow(id);
    };

    function recomputeRow(id) {
        const r = rows[id];
        r.amount = r.qty * BUNDLE_SIZE * r.denomValue;
        const qtyEl = document.getElementById(`qty-${id}`);
        if (qtyEl) qtyEl.textContent = r.qty ? r.qty.toLocaleString() : '—';
        document.getElementById(`amt-${id}`).textContent = fmt(r.amount);
        refreshTotals();
    }

    function refreshTotals() {
        const byType = { new: 0, reissue: 0, coin: 0 };
        for (const r of Object.values(rows)) byType[r.bundleType] += r.amount || 0;

        document.getElementById('newSubtotal').textContent = fmt(byType.new);
        document.getElementById('reissueSubtotal').textContent = fmt(byType.reissue);
        document.getElementById('coinSubtotal').textContent = fmt(byType.coin);

        const grand = byType.new + byType.reissue + byType.coin;
        const wishlist = parseFloat(document.getElementById('varianceIndicator').dataset.wishlistTotal) || 0;
        const variance = grand - wishlist;

        document.getElementById('bankGrandTotal').textContent = fmt(grand);
        const vEl = document.getElementById('varianceAmount');
        vEl.textContent = (variance >= 0 ? '+' : '') + fmt(variance);
        vEl.className = 'fw-bold ' + (variance === 0 ? 'text-success' : 'text-danger');
    }

    // ── Build payload on submit ───────────────────────────────
    document.getElementById('editDispatchForm')?.addEventListener('submit', function () {
        const bundleTypeMap = { new: 'New', reissue: 'Reissue', coin: 'Coin' };
        const payload = Object.values(rows).map(r => ({
            denominationId: r.denomId,
            denominationValue: r.denomValue,
            denominationTypeId: r.denomTypeId,
            bundleType: bundleTypeMap[r.bundleType],
            bundles: r.bundleType === 'coin' ? null : r.qty,
            boxes: r.bundleType === 'coin' ? r.qty : null,
            amount: r.amount,
            boxNumbers: r.bundleType === 'new' ? r.boxes : []
        })).filter(r => r.denominationId > 0 && r.amount > 0);

        document.getElementById('dispatchPayload').value = JSON.stringify(payload);
    });

    // ── Prefill from existing bank rows ───────────────────────
    document.addEventListener('DOMContentLoaded', () => {
        if (!seed.existing?.length) return;
        for (const row of seed.existing) {
            const type = row.bundleType === 'New' ? 'new'
                : row.bundleType === 'Reissue' ? 'reissue'
                    : row.bundleType === 'Coin' ? 'coin'
                        : null;
            if (!type) continue;        // skip NA / unknown
            addDispatchRow(type, row);
        }
    });
})();