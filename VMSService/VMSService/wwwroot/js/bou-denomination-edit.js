(function () {
    'use strict';

    // ── Constants ─────────────────────────────────────────────
    const BUNDLE_SIZE = 1000;     // notes per bundle
    const BOX_BUNDLES = 50;       // bundles per box
    const FULL_BORDER = '#378ADD';
    const PARTIAL_BORDER = '#EF9F27';

    const fmt = n => Math.round(n).toLocaleString();
    const fmtUgx = n => 'UGX ' + fmt(n);

    // ── Denomination helpers (data from window.denomData) ─────
    const denomNotes = () => window.denomData?.notes ?? [];
    const denomCoins = () => window.denomData?.coins ?? [];

    function denomById(id, src) {
        return src.find(d => d.id === parseInt(id, 10));
    }

    function denomOptionsHtml(src, includeBlank = true) {
        const blank = includeBlank ? '<option value="">— Select —</option>' : '';
        return blank + src.map(d =>
            `<option value="${d.id}" data-value="${d.value}" data-type-id="${d.typeId}">${d.label}</option>`
        ).join('');
    }

    // ── Available-box cache (per denomination) ────────────────
    const availableBoxCache = {};

    async function fetchAvailableBoxes(denominationId) {
        if (availableBoxCache[denominationId]) return availableBoxCache[denominationId];

        const requestId = document.getElementById('new-banknotes-cards').dataset.requestId;
        const url = `/WithdrawalRequests/AvailableBoxes?denominationId=${denominationId}&requestId=${requestId}`;
        const resp = await fetch(url, { headers: { 'Accept': 'application/json' } });
        if (!resp.ok) {
            console.error('Failed to load boxes', resp.status);
            return [];
        }
        const boxes = await resp.json();
        availableBoxCache[denominationId] = boxes;
        return boxes;
    }

    function invalidateBoxCache(denominationId) {
        delete availableBoxCache[denominationId];
    }

    // ── Range parser ──────────────────────────────────────────
    function parseRange(input) {
        const t = (input || '').trim();
        if (!t) return null;
        const m = t.match(/^(\d+)(?:\s*[-–]\s*(\d+))?$/);
        if (!m) return null;
        const start = parseInt(m[1], 10);
        const end = m[2] ? parseInt(m[2], 10) : start;
        if (end < start) return null;
        return { start, end };
    }

    // ── Boxes already used in the New Banknotes section, per denomination ──
    function usedBoxesForDenom(denominationId) {
        const used = new Set();
        document.querySelectorAll(`#new-banknotes-cards .dispatch-card[data-denom-id="${denominationId}"]`)
            .forEach(card => {
                const s = parseInt(card.dataset.startBox, 10);
                const e = parseInt(card.dataset.endBox, 10);
                for (let i = s; i <= e; i++) used.add(i);
            });
        return used;
    }
    // ── Compute a summary of available boxes for the hint line ──
    //function summarizeAvailability(boxes) {
    //    if (!boxes || boxes.length === 0) {
    //        return { count: 0, first: null, last: null, firstFew: [] };
    //    }
    //    const numbers = boxes
    //        .map(b => parseInt(b.caseNumber, 10))
    //        .filter(n => !isNaN(n))
    //        .sort((a, b) => a - b);

    //    return {
    //        count: numbers.length,
    //        first: numbers[0],
    //        last: numbers[numbers.length - 1],
    //        firstFew: numbers.slice(0, 3),
    //    };
    //}
    function summarizeAvailability(boxes) {
        if (!boxes || boxes.length === 0) {
            return { count: 0, full: 0, partial: 0, firstFull: null, lastFull: null, partialNumbers: [] };
        }

        const fullBoxes = boxes.filter(b => b.remainingBundles === 50);
        const partialBoxes = boxes.filter(b => b.remainingBundles > 0 && b.remainingBundles < 50);

        const fullNums = fullBoxes
            .map(b => parseInt(b.caseNumber, 10))
            .filter(n => !isNaN(n))
            .sort((a, b) => a - b);

        const partialNums = partialBoxes
            .map(b => parseInt(b.caseNumber, 10))
            .filter(n => !isNaN(n))
            .sort((a, b) => a - b);

        return {
            count: boxes.length,
            full: fullBoxes.length,
            partial: partialBoxes.length,
            firstFull: fullNums[0] ?? null,
            lastFull: fullNums[fullNums.length - 1] ?? null,
            partialNumbers: partialNums,
        };
    }

    // ── Update the hint line + placeholder when denomination changes ──
    //async function refreshDenomHint() {
    //    const denomSel = document.getElementById('new-denom-input') || document.querySelector('.denom-select');
    //    const rangeInput = document.getElementById('new-range-input');
    //    const info = document.getElementById('new-range-info');
    //    if (!denomSel || !rangeInput || !info) return;

    //    const denomId = parseInt(denomSel.value, 10);
    //    if (!denomId) {
    //        info.textContent = '';
    //        info.className = 'd-block mt-1 small';
    //        rangeInput.placeholder = 'Box or range e.g. 30424 or 30424–30430';
    //        return;
    //    }

    //    // Loading state — boxes might not be cached yet
    //    info.textContent = 'Checking availability…';
    //    info.className = 'd-block mt-1 small text-muted';

    //    const boxes = await fetchAvailableBoxes(denomId);
    //    const summary = summarizeAvailability(boxes);

    //    if (summary.count === 0) {
    //        info.textContent = 'No boxes available for this denomination at this branch.';
    //        info.className = 'd-block mt-1 small text-warning';
    //        rangeInput.placeholder = 'Box or range e.g. 30424 or 30424–30430';
    //        return;
    //    }

    //    // Hint line: "12 boxes available · 31200 to 45100"
    //    info.textContent = `${summary.count} box${summary.count === 1 ? '' : 'es'} available · ${summary.first} to ${summary.last}`;
    //    info.className = 'd-block mt-1 small text-muted';

    //    // Dynamic placeholder using a real example
    //    const example = summary.firstFew[0];
    //    const exampleRange = summary.firstFew.length >= 3
    //        ? `${summary.firstFew[0]}–${summary.firstFew[2]}`
    //        : `${summary.firstFew[0]}`;
    //    rangeInput.placeholder = `e.g. ${example} or ${exampleRange}`;
    //}
    async function refreshDenomHint() {
        const denomSel = document.getElementById('new-denom-input') || document.querySelector('.denom-select');
        const rangeInput = document.getElementById('new-range-input');
        const info = document.getElementById('new-range-info');
        if (!denomSel || !rangeInput || !info) return;

        const denomId = parseInt(denomSel.value, 10);
        if (!denomId) {
            info.textContent = '';
            info.className = 'd-block mt-1 small';
            rangeInput.placeholder = 'Box or range e.g. 30424 or 30424–30430';
            return;
        }

        info.textContent = 'Checking availability…';
        info.className = 'd-block mt-1 small text-muted';

        const boxes = await fetchAvailableBoxes(denomId);
        const s = summarizeAvailability(boxes);

        if (s.count === 0) {
            info.textContent = 'No boxes available for this denomination at this branch.';
            info.className = 'd-block mt-1 small text-warning';
            rangeInput.placeholder = 'Box or range e.g. 30424 or 30424–30430';
            return;
        }

        const lines = [`${s.count} box${s.count === 1 ? '' : 'es'} available`];

        if (s.full > 0) {
            const fullRange = s.firstFull === s.lastFull ? `${s.firstFull}` : `${s.firstFull}–${s.lastFull}`;
            lines.push(`${s.full} full (${fullRange})`);
        }

        if (s.partial > 0) {
            const partialList = s.partialNumbers.length <= 3
                ? s.partialNumbers.join(', ')
                : s.partialNumbers.slice(0, 3).join(', ') + '…';
            lines.push(`${s.partial} partial (${partialList})`);
        }

        info.textContent = lines.join(' · ');
        info.className = 'd-block mt-1 small text-muted';

        // Placeholder uses first full box for the example
        const example = s.firstFull ?? s.partialNumbers[0];
        rangeInput.placeholder = `e.g. ${example} or ${example}–${example + 2}`;
    }

    // ── Wire the denomination select to refresh the hint on change ──
    document.addEventListener('change', e => {
        if (e.target.id === 'new-denom-input' || e.target.classList.contains('denom-select')) {
            refreshDenomHint();
        }
    });

    // ── Add new banknote range from the Add strip ─────────────
    window.addNewBanknoteRange = async function () {
        const denomSel = document.getElementById('new-denom-input') || document.querySelector('.denom-select');
        const rangeInput = document.getElementById('new-range-input');
        const info = document.getElementById('new-range-info');

        info.textContent = '';
        info.className = 'd-block mt-1 small';
        rangeInput.classList.remove('is-invalid');
        denomSel.classList.remove('is-invalid');

        if (!denomSel.value) {
            denomSel.classList.add('is-invalid');
            info.classList.add('text-danger');
            info.textContent = 'Select a denomination.';
            return;
        }

        const parsed = parseRange(rangeInput.value);
        if (!parsed) {
            rangeInput.classList.add('is-invalid');
            info.classList.add('text-danger');
            info.textContent = 'Enter a box number or range (e.g. 30424 or 30424–30430).';
            return;
        }

        const denominationId = parseInt(denomSel.value, 10);
        const denom = denomById(denominationId, denomNotes());
        if (!denom) return;

        const availableBoxes = await fetchAvailableBoxes(denominationId);
        const availableSet = new Set(availableBoxes.map(b => parseInt(b.caseNumber, 10)));
        const availableByNum = {};
        availableBoxes.forEach(b => { availableByNum[parseInt(b.caseNumber, 10)] = b; });

        const used = usedBoxesForDenom(denominationId);

        const addable = [];
        const unavailable = [];
        const duplicates = [];
        for (let b = parsed.start; b <= parsed.end; b++) {
            if (used.has(b)) duplicates.push(b);
            else if (!availableSet.has(b)) unavailable.push(b);
            else addable.push(b);
        }
        addable.sort((a, b) => a - b);

        if (!addable.length) {
            info.classList.add('text-danger');

            let msg = '';
            if (duplicates.length) msg += `Already added: ${duplicates.join(', ')}. `;
            if (unavailable.length) msg += `Unavailable: ${unavailable.join(', ')}. `;

            // Suggest the closest available alternative
            const usedSet = used;  // already in scope
            const stillAddable = availableBoxes
                .map(b => parseInt(b.caseNumber, 10))
                .filter(n => !isNaN(n) && !usedSet.has(n))
                .sort((a, b) => a - b);

            if (stillAddable.length > 0) {
                const next = stillAddable[0];
                msg += `Try boxes from ${next} onwards.`;
            } else {
                msg += 'No available boxes remain for this denomination.';
            }

            info.textContent = msg;
            return;
        }
        console.log('DEBUG addable:', addable, 'availableByNum keys:', Object.keys(availableByNum));

        if (addable.length === 1) {
            const b = addable[0];
            const boxData = availableByNum[b];

            console.log('DEBUG boxData for', b, ':', boxData, 'remainingBundles:', boxData?.remainingBundles);

            if (boxData && boxData.remainingBundles < 50) {
                appendPartialCard({ denom, boxNumber: b, boxData });
                info.classList.add('text-success');
                info.textContent =
                    `Added box ${b} as partial (${boxData.remainingBundles} bundles available).`;
                rangeInput.value = '';
                recomputeTotals();
                return;
            }
        }

        // Collapse contiguous addable boxes into ranges
        const segments = [];
        let segStart = addable[0], segEnd = addable[0];
        for (let i = 1; i < addable.length; i++) {
            if (addable[i] === segEnd + 1) {
                segEnd = addable[i];
            } else {
                segments.push({ start: segStart, end: segEnd });
                segStart = addable[i];
                segEnd = addable[i];
            }
        }
        segments.push({ start: segStart, end: segEnd });

        segments.forEach(s => appendFullCard({
            denom,
            startBox: s.start,
            endBox: s.end,
            availableByNum
        }));

        // Feedback
        const parts = [`Added ${addable.length} box${addable.length === 1 ? '' : 'es'}.`];
        if (duplicates.length) parts.push(`Already added: ${duplicates.join(', ')}.`);
        if (unavailable.length) parts.push(`Unavailable: ${unavailable.join(', ')}.`);
        info.classList.add(duplicates.length || unavailable.length ? 'text-warning' : 'text-success');
        info.textContent = parts.join(' ');

        rangeInput.value = '';
        recomputeTotals();
    };

    // ── Append a Full card to the DOM ─────────────────────────
    function appendFullCard({ denom, startBox, endBox, availableByNum }) {
        const boxCount = endBox - startBox + 1;
        const bundles = boxCount * BOX_BUNDLES;
        const firstBox = availableByNum[startBox];
        const lastBox = availableByNum[endBox];
        const serialStart = firstBox?.serialStart ?? '';
        const serialEnd = lastBox?.serialEnd ?? '';
        const remainingBundles = firstBox?.remainingBundles ?? BOX_BUNDLES;
        const amount = bundles * BUNDLE_SIZE * denom.value;
        const boxLabel = startBox === endBox ? String(startBox) : `${startBox}–${endBox}`;
        const isSingleBox = startBox === endBox;

        const card = document.createElement('div');
        card.className = 'border rounded-3 p-2 dispatch-card';
        card.dataset.rowId = `new-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
        card.dataset.kind = 'full';
        card.dataset.denomId = denom.id;
        card.dataset.denom = denom.value;
        card.dataset.startBox = startBox;
        card.dataset.endBox = endBox;
        card.dataset.bundles = bundles;
        card.dataset.serialStart = serialStart;
        card.dataset.serialEnd = serialEnd;
        card.dataset.remainingBundles = remainingBundles;
        card.style.borderLeft = `3px solid ${FULL_BORDER}`;

        const makePartialLink = isSingleBox
            ? `<button type="button" class="btn btn-link btn-sm p-0 text-warning"
                       onclick="makeCardPartial(this)"
                       style="font-size:10px;text-decoration:none;">
                 <i class="ti ti-arrows-split me-1"></i>Make partial
               </button>`
            : '';

        card.innerHTML = `
            <div class="row g-2 align-items-start">
                <div class="col-auto" style="width:120px;">
                    <div class="text-primary text-uppercase fw-bold" style="font-size:10px;letter-spacing:.04em;">Full</div>
                    <div class="fw-bold">${denom.label}</div>
                    ${makePartialLink ? `<div class="mt-1">${makePartialLink}</div>` : ''}
                </div>
                <div class="col">
                    <div class="d-flex align-items-center gap-2 small">
                        <span style="font-family:var(--bs-font-monospace);font-weight:600;">${boxLabel}</span>
                        <span class="text-muted">· ${boxCount} box${boxCount === 1 ? '' : 'es'}</span>
                    </div>
                    <div class="text-muted small mt-1">
                        ${bundles.toLocaleString()} bundles · serials ${serialStart}–${serialEnd}
                    </div>
                </div>
                <div class="col-auto text-end" style="width:160px;">
                    <div class="fw-bold row-amount">${fmt(amount)}</div>
                </div>
                <div class="col-auto" style="width:24px;">
                    <button type="button" class="btn btn-sm btn-link text-muted p-0"
                            onclick="removeDispatchCard(this)" aria-label="Remove entry">
                        <i class="ti ti-x"></i>
                    </button>
                </div>
            </div>`;

        insertCardSorted(card);
    }
    function appendPartialCard({ denom, boxNumber, boxData }) {
        const remainingBundles = boxData.remainingBundles ?? 1;
        const cursorStart = boxData.serialStart ?? '';
        const defaultBundles = 1;
        const range = serialRangeForBundles(cursorStart, defaultBundles);
        const amount = defaultBundles * BUNDLE_SIZE * denom.value;

        const card = document.createElement('div');
        card.className = 'border rounded-3 p-2 dispatch-card';
        card.dataset.rowId = `new-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
        card.dataset.kind = 'partial';
        card.dataset.denomId = denom.id;
        card.dataset.denom = denom.value;
        card.dataset.startBox = boxNumber;
        card.dataset.endBox = boxNumber;
        card.dataset.bundles = defaultBundles;
        card.dataset.serialStart = cursorStart;
        card.dataset.serialEnd = range.end;
        card.dataset.remainingBundles = remainingBundles;
        card.style.borderLeft = `3px solid ${PARTIAL_BORDER}`;

        card.innerHTML = `
        <div class="row g-2 align-items-start">
            <div class="col-auto" style="width:120px;">
                <div class="text-warning text-uppercase fw-bold" style="font-size:10px;letter-spacing:.04em;">Partial</div>
                <div class="fw-bold">${denom.label}</div>
            </div>
            <div class="col">
                <div class="small">
                    <span style="font-family:var(--bs-font-monospace);font-weight:600;">${boxNumber}</span>
                    <span class="text-muted ms-2">· ${remainingBundles} bundle${remainingBundles === 1 ? '' : 's'} available</span>
                </div>
                <input type="number"
                       class="form-control form-control-sm text-center mt-2 partial-bundles"
                       min="1" max="${Math.min(remainingBundles, 49)}"
                       value="${defaultBundles}"
                       aria-label="Bundles taken from box ${boxNumber}" />
                <div class="text-muted small mt-1">
                    bundles · serials <span class="partial-serial">${range.start}–${range.end}</span>
                </div>
            </div>
            <div class="col-auto text-end" style="width:160px;">
                <div class="fw-bold row-amount">${fmt(amount)}</div>
            </div>
            <div class="col-auto" style="width:24px;">
                <button type="button" class="btn btn-sm btn-link text-muted p-0"
                        onclick="removeDispatchCard(this)" aria-label="Remove entry">
                    <i class="ti ti-x"></i>
                </button>
            </div>
        </div>`;

        insertCardSorted(card);
    }


    // ── Insert a card into the cards list in sorted order ─────
    function insertCardSorted(newCard) {
        const list = document.getElementById('new-banknotes-cards');
        const newKind = newCard.dataset.kind;
        const newStart = parseInt(newCard.dataset.startBox, 10);
        const newDenom = parseFloat(newCard.dataset.denom);

        const cards = Array.from(list.querySelectorAll('.dispatch-card'));
        const kindRank = k => (k === 'full' ? 0 : 1);

        const insertBefore = cards.find(c => {
            const cKind = c.dataset.kind;
            const cStart = parseInt(c.dataset.startBox, 10);
            const cDenom = parseFloat(c.dataset.denom);

            // Sort: kind (Full < Partial), then denom desc, then startBox asc
            if (kindRank(newKind) !== kindRank(cKind)) return kindRank(newKind) < kindRank(cKind);
            if (newDenom !== cDenom) return newDenom > cDenom;
            return newStart < cStart;
        });

        if (insertBefore) list.insertBefore(newCard, insertBefore);
        else list.appendChild(newCard);
    }

    // ── Flip a single-box Full card to Partial ────────────────
    window.makeCardPartial = function (btn) {
        const card = btn.closest('.dispatch-card');
        if (!card || card.dataset.kind !== 'full') return;
        if (parseInt(card.dataset.startBox, 10) !== parseInt(card.dataset.endBox, 10)) return;

        const denom = denomById(parseInt(card.dataset.denomId, 10), denomNotes());
        if (!denom) return;

        const startBox = parseInt(card.dataset.startBox, 10);
        const cursorStart = card.dataset.serialStart;
        const remainingBundles = parseInt(card.dataset.remainingBundles, 10) || BOX_BUNDLES;
        //const defaultBundles = Math.min(BOX_BUNDLES, remainingBundles);
        const defaultBundles = 1;
        const range = serialRangeForBundles(cursorStart, defaultBundles);
        const amount = defaultBundles * BUNDLE_SIZE * denom.value;

        card.dataset.kind = 'partial';
        card.dataset.bundles = defaultBundles;
        card.dataset.serialEnd = range.end;
        card.style.borderLeft = `3px solid ${PARTIAL_BORDER}`;

        card.innerHTML = `
            <div class="row g-2 align-items-start">
                <div class="col-auto" style="width:120px;">
                    <div class="text-warning text-uppercase fw-bold" style="font-size:10px;letter-spacing:.04em;">Partial</div>
                    <div class="fw-bold">${denom.label}</div>
                </div>
                <div class="col">
                    <div class="small">
                        <span style="font-family:var(--bs-font-monospace);font-weight:600;">${startBox}</span>
                        <span class="text-muted ms-2">· ${remainingBundles} bundle${remainingBundles === 1 ? '' : 's'} available</span>
                    </div>
                    <input type="number"
                           class="form-control form-control-sm text-center mt-2 partial-bundles"
                           min="1" max="${Math.min(remainingBundles, 49)}"
                           value="${defaultBundles}"
                           aria-label="Bundles taken from box ${startBox}" />
                    <div class="text-muted small mt-1">
                        bundles · serials <span class="partial-serial">${range.start}–${range.end}</span>
                    </div>
                </div>
                <div class="col-auto text-end" style="width:160px;">
                    <div class="fw-bold row-amount">${fmt(amount)}</div>
                </div>
                <div class="col-auto" style="width:24px;">
                    <button type="button" class="btn btn-sm btn-link text-muted p-0"
                            onclick="removeDispatchCard(this)" aria-label="Remove entry">
                        <i class="ti ti-x"></i>
                    </button>
                </div>
            </div>`;

        const input = card.querySelector('.partial-bundles');
        input?.focus();
        input?.select();
        recomputeTotals();
    };

    // ── Serial arithmetic helpers ─────────────────────────────
    function splitSerial(serial) {
        if (!serial) return null;
        const m = String(serial).match(/^([A-Za-z]*)(\d+)$/);
        if (!m) return null;
        return { prefix: m[1], number: parseInt(m[2], 10), width: m[2].length };
    }

    function serialRangeForBundles(startSerial, bundles) {
        const parts = splitSerial(startSerial);
        if (!parts) return { start: '', end: '' };
        const endNumber = parts.number + (bundles * BUNDLE_SIZE) - 1;
        const end = parts.prefix + String(endNumber).padStart(parts.width, '0');
        return { start: startSerial, end };
    }

    // ── Partial bundles input changed ─────────────────────────
    function onPartialBundlesChange(input) {
        const card = input.closest('.dispatch-card');
        if (!card) return;

        let bundles = parseInt(input.value, 10);
        const remaining = parseInt(card.dataset.remainingBundles, 10) || BOX_BUNDLES;
        const cap = Math.min(remaining, 49);

        if (isNaN(bundles) || bundles < 1) bundles = 1;
        if (bundles > cap) bundles = cap;
        input.value = bundles;

        const denomValue = parseFloat(card.dataset.denom);
        const range = serialRangeForBundles(card.dataset.serialStart, bundles);

        card.dataset.bundles = bundles;
        card.dataset.serialEnd = range.end;

        const serialEl = card.querySelector('.partial-serial');
        if (serialEl) serialEl.textContent = `${range.start}–${range.end}`;

        const amt = bundles * BUNDLE_SIZE * denomValue;
        card.querySelector('.row-amount').textContent = fmt(amt);

        recomputeTotals();
    }

    // ── Remove card or simple row ─────────────────────────────
    window.removeDispatchCard = function (btn) {
        const card = btn.closest('.dispatch-card');
        const row = btn.closest('.dispatch-simple-row');
        if (card) {
            invalidateBoxCache(card.dataset.denomId);
            card.remove();
        } else if (row) {
            row.remove();
        }
        recomputeTotals();
    };

    // ── Reissue / Coin simple rows ────────────────────────────
    window.addDispatchRow = function (kind) {
        const tbody = document.getElementById(kind === 'coin' ? 'coinsBody' : 'reissueBanknotesBody');
        if (!tbody) return;

        const src = kind === 'coin' ? denomCoins() : denomNotes();

        const tr = document.createElement('tr');
        tr.className = 'dispatch-simple-row';
        tr.dataset.kind = kind;
        tr.innerHTML = `
            <td class="ps-3">
                <select class="form-select form-select-sm simple-denom">${denomOptionsHtml(src)}</select>
            </td>
            <td class="text-end">
                <input type="number" min="1" class="form-control form-control-sm text-end simple-qty" style="width:80px;display:inline-block;" />
            </td>
            <td class="text-end fw-bold row-amount">0</td>
            <td class="text-center">
                <button type="button" class="btn btn-sm btn-link text-muted p-0"
                        onclick="removeDispatchCard(this)" aria-label="Remove">
                    <i class="ti ti-x"></i>
                </button>
            </td>`;
        tbody.appendChild(tr);
    };

    function onSimpleDenomChange(sel) {
        const row = sel.closest('.dispatch-simple-row');
        if (!row) return;
        const opt = sel.options[sel.selectedIndex];
        row.dataset.denomId = sel.value;
        row.dataset.denom = opt?.dataset.value || 0;
        row.dataset.denomTypeId = opt?.dataset.typeId || 0;
        recomputeSimpleRow(row);
    }

    function onSimpleQtyChange(input) {
        recomputeSimpleRow(input.closest('.dispatch-simple-row'));
    }

    function recomputeSimpleRow(row) {
        if (!row) return;
        const qty = parseInt(row.querySelector('.simple-qty')?.value, 10) || 0;
        const denomValue = parseFloat(row.dataset.denom) || 0;
        const kind = row.dataset.kind;
        const amount = qty * BUNDLE_SIZE * denomValue;
        row.dataset.qty = qty;
        const amtEl = row.querySelector('.row-amount');
        if (amtEl) amtEl.textContent = fmt(amount);
        recomputeTotals();
    }

    // ── Delegated input/change handlers ───────────────────────
    document.addEventListener('input', e => {
        if (e.target.classList.contains('partial-bundles')) onPartialBundlesChange(e.target);
        else if (e.target.classList.contains('simple-qty')) onSimpleQtyChange(e.target);
    });

    document.addEventListener('change', e => {
        if (e.target.classList.contains('simple-denom')) onSimpleDenomChange(e.target);
    });

    // ── Recompute totals (cards + simple rows) ────────────────
    function parseAmt(el) {
        return parseFloat((el?.textContent || '0').replace(/,/g, '')) || 0;
    }

    function recomputeTotals() {
        let newTotal = 0, reissueTotal = 0, coinTotal = 0;

        document.querySelectorAll('#new-banknotes-cards .dispatch-card').forEach(card => {
            newTotal += parseAmt(card.querySelector('.row-amount'));
        });
        document.querySelectorAll('#reissueBanknotesBody .dispatch-simple-row').forEach(row => {
            reissueTotal += parseAmt(row.querySelector('.row-amount'));
        });
        document.querySelectorAll('#coinsBody .dispatch-simple-row').forEach(row => {
            coinTotal += parseAmt(row.querySelector('.row-amount'));
        });

        // Section subtotals
        setText('new-subtotal', fmt(newTotal));
        setText('reissueSubtotal', fmt(reissueTotal));
        setText('coinSubtotal', fmt(coinTotal));

        // Totals block
        setText('sum-new', fmt(newTotal));
        setText('sum-reissue', fmt(reissueTotal));
        setText('sum-coins', fmt(coinTotal));

        const bankTotal = newTotal + reissueTotal + coinTotal;
        setText('sum-bank', fmt(bankTotal));
        setText('grand-total', fmtUgx(bankTotal));
        setText('bankGrandTotal', fmtUgx(bankTotal));

        // Variance
        const wishlistEl = document.getElementById('varianceIndicator');
        const wishlist = parseFloat(wishlistEl?.dataset.wishlistTotal) || 0;
        const variance = bankTotal - wishlist;

        const varEl = document.getElementById('varianceAmount');
        if (varEl) {
            varEl.textContent = (variance >= 0 ? '+' : '') + fmt(variance);
            varEl.className = 'fw-bold ' + (variance === 0 ? 'text-success' : variance < 0 ? 'text-warning' : 'text-danger');
        }

        //const sumVarEl = document.getElementById('sum-variance');
        //if (sumVarEl) {
        //    let text, cls;
        //    if (variance === 0) { text = `0 · matches ${fmt(wishlist)}`; cls = 'text-success'; }
        //    else if (variance < 0) { text = `${fmt(variance)} · short`; cls = 'text-warning'; }
        //    else { text = `+${fmt(variance)} · over`; cls = 'text-danger'; }
        //    sumVarEl.textContent = text;
        //    sumVarEl.className = 'fw-bold ' + cls;
        //}
        const sumVarEl = document.getElementById('sum-variance');
        if (sumVarEl) {
            let text, cls, bg;
            if (variance === 0) {
                text = `0 · matches`;
                cls = 'text-success';
                bg = 'alert-success';
            } else if (variance < 0) {
                text = `${fmt(variance)} · short`;
                cls = 'text-danger';
                bg = 'alert-danger';
            } else {
                text = `+${fmt(variance)} · over`;
                cls = 'text-danger';
                bg = 'alert-danger';
            }

            sumVarEl.textContent = text;
            sumVarEl.className = 'fw-bold ' + cls;

            // Color the parent card
            const card = sumVarEl.closest('.card');
            if (card) {
                card.classList.remove('alert-success', 'alert-danger', 'bg-body-tertiary', 'border-0');
                card.classList.add(bg, 'border-0');
            }
        }
        // Update amount-in-words if helper available
        if (typeof toWords === 'function') {
            const wordsEl = document.getElementById('bank-denom-words');
            if (wordsEl) wordsEl.textContent = toWords(bankTotal);
        }
    }

    function setText(id, text) {
        const el = document.getElementById(id);
        if (el) el.textContent = text;
    }

    // ── Submit: serialize cards + simple rows into payload ────
    document.getElementById('bankDispatchForm')?.addEventListener('submit', function (e) {
        const payload = [];

        // Coalesce Full cards by denomination — multiple Full cards for the same denom
        // become ONE payload entry with combined boxNumbers.
        const newFullByDenom = new Map();

        document.querySelectorAll('#new-banknotes-cards .dispatch-card').forEach(card => {
            const kind = card.dataset.kind;
            const denomId = parseInt(card.dataset.denomId, 10);
            const denomValue = parseFloat(card.dataset.denom);
            const startBox = parseInt(card.dataset.startBox, 10);
            const endBox = parseInt(card.dataset.endBox, 10);
            const bundles = parseInt(card.dataset.bundles, 10);
            const denom = denomById(denomId, denomNotes());

            if (kind === 'full') {
                if (!newFullByDenom.has(denomId)) {
                    newFullByDenom.set(denomId, {
                        denominationId: denomId,
                        denominationValue: denomValue,
                        denominationTypeId: denom?.typeId ?? 0,
                        bundleType: 'New',
                        bundles: 0,
                        boxes: null,
                        amount: 0,
                        boxNumbers: [],
                    });
                }
                const entry = newFullByDenom.get(denomId);
                for (let b = startBox; b <= endBox; b++) entry.boxNumbers.push(String(b));
                const boxCount = endBox - startBox + 1;
                entry.bundles += boxCount * BOX_BUNDLES;
                entry.amount += boxCount * BOX_BUNDLES * BUNDLE_SIZE * denomValue;
            } else {
                // Partial — its own entry
                payload.push({
                    denominationId: denomId,
                    denominationValue: denomValue,
                    denominationTypeId: denom?.typeId ?? 0,
                    bundleType: 'New',
                    bundles: bundles,
                    boxes: null,
                    amount: bundles * BUNDLE_SIZE * denomValue,
                    boxNumbers: [String(startBox)],
                });
            }
        });

        newFullByDenom.forEach(entry => {
            entry.boxNumbers.sort((a, b) => parseInt(a, 10) - parseInt(b, 10));
            payload.push(entry);
        });

        // Reissue rows
        document.querySelectorAll('#reissueBanknotesBody .dispatch-simple-row').forEach(row => {
            const denomId = parseInt(row.dataset.denomId, 10);
            const denomValue = parseFloat(row.dataset.denom) || 0;
            const qty = parseInt(row.dataset.qty, 10) || 0;
            const denom = denomById(denomId, denomNotes());
            if (denomId > 0 && qty > 0) {
                payload.push({
                    denominationId: denomId,
                    denominationValue: denomValue,
                    denominationTypeId: denom?.typeId ?? 0,
                    bundleType: 'Reissue',
                    bundles: qty,
                    boxes: null,
                    amount: qty * BUNDLE_SIZE * denomValue,
                    boxNumbers: [],
                });
            }
        });

        // Coin rows
        document.querySelectorAll('#coinsBody .dispatch-simple-row').forEach(row => {
            const denomId = parseInt(row.dataset.denomId, 10);
            const denomValue = parseFloat(row.dataset.denom) || 0;
            const qty = parseInt(row.dataset.qty, 10) || 0;
            const denom = denomById(denomId, denomCoins());
            if (denomId > 0 && qty > 0) {
                payload.push({
                    denominationId: denomId,
                    denominationValue: denomValue,
                    denominationTypeId: denom?.typeId ?? 0,
                    bundleType: 'Coin',
                    bundles: null,
                    boxes: qty,
                    amount: qty * BUNDLE_SIZE * denomValue,
                    boxNumbers: [],
                });
            }
        });

        document.getElementById('dispatchPayload').value = JSON.stringify(payload);
    });

    // ── Init: populate the Add strip's denomination select + initial recompute ──
    //document.addEventListener('DOMContentLoaded', () => {
    //    document.querySelectorAll('.denom-select').forEach(sel => {
    //        if (sel.options.length > 1) return;
    //        sel.innerHTML = '<option value="">Denomination</option>' +
    //            denomNotes().map(d =>
    //                `<option value="${d.id}" data-value="${d.value}" data-type-id="${d.typeId}">${d.label}</option>`
    //            ).join('');
    //        sel.id = sel.id || 'new-denom-input';
    //    });

    //    recomputeTotals();
    //});
    document.addEventListener('DOMContentLoaded', () => {
        // Existing: populate the Add strip's denomination select
        document.querySelectorAll('.denom-select').forEach(sel => {
            if (sel.options.length > 1) return;
            sel.innerHTML = '<option value="">Denomination</option>' +
                denomNotes().map(d =>
                    `<option value="${d.id}" data-value="${d.value}" data-type-id="${d.typeId}">${d.label}</option>`
                ).join('');
            sel.id = sel.id || 'new-denom-input';
        });

        // NEW: hydrate server-rendered Reissue and Coin rows
        document.querySelectorAll('#reissueBanknotesBody .dispatch-simple-row, #coinsBody .dispatch-simple-row')
            .forEach(row => {
                const kind = row.dataset.kind;
                const src = kind === 'coin' ? denomCoins() : denomNotes();
                const sel = row.querySelector('.simple-denom');
                if (!sel) return;

                sel.innerHTML = denomOptionsHtml(src);
                sel.value = row.dataset.denomId;
            });

        recomputeTotals();
    });

})();