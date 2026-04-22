(function () {
    const fmt = n => 'UGX ' + Math.round(n).toLocaleString();
    const $ = id => document.getElementById(id);

    let currentBouBranchId = 0;
    let notesTotal = 0;
    let coinsTotal = 0;
    let processingFee = 0;
    let penaltyFee = 0;
    let violations = [];

    // Initialize - listen to denom changes and branch selection
    function init() {
        // Listen to denomination changes
        document.addEventListener('depositDenomChanged', handleDenomChange);

        // Listen to branch selection changes
        const branchSelect = $('bouBranchSelect');
        if (branchSelect) {
            branchSelect.addEventListener('change', function() {
                currentBouBranchId = parseInt(this.value) || 0;
                recalculateFees();
            });
        }
    }

    // Handle denomination change event
    function handleDenomChange(event) {
        const rows = window.depDtGetRows();
        
        notesTotal = 0;
        coinsTotal = 0;

        Object.values(rows).forEach(r => {
            if (!r.valid || r.amount === 0) return;
            
            if (r.type === 'notes') {
                notesTotal += r.amount;
            } else if (r.type === 'coins') {
                coinsTotal += r.amount;
            }
        });

        recalculateFees();
    }

    // Recalculate all fees
    async function recalculateFees() {
        await calculateProcessingFees();
        await checkMinimumStandards();
        updateFeesDisplay();
    }

    // Calculate processing fees via API
    async function calculateProcessingFees() {
        try {
            const response = await fetch('/CbDepositRequests/CalculateProcessingFees', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    notesTotal: notesTotal,
                    coinsTotal: coinsTotal
                })
            });

            if (response.ok) {
                const data = await response.json();
                processingFee = data.totalProcessingFee || 0;
            } else {
                console.error('Failed to calculate processing fees');
                processingFee = 0;
            }
        } catch (error) {
            console.error('Error calculating processing fees:', error);
            processingFee = 0;
        }
    }

    // Check minimum deposit standards via API
    async function checkMinimumStandards() {
        if (currentBouBranchId === 0) {
            penaltyFee = 0;
            violations = [];
            return;
        }

        try {
            const rows = window.depDtGetRows();
            const denominations = [];

            Object.values(rows).forEach(r => {
                if (!r.valid || r.amount === 0 || !r.denomId) return;
                
                denominations.push({
                    denominationId: r.denomId,
                    denominationValue: r.denomValue,
                    bundles: r.qty
                });
            });

            if (denominations.length === 0) {
                penaltyFee = 0;
                violations = [];
                return;
            }

            const response = await fetch('/CbDepositRequests/CheckMinimumDepositStandards', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    bouBranchId: currentBouBranchId,
                    denominations: denominations
                })
            });

            if (response.ok) {
                const data = await response.json();
                penaltyFee = data.penaltyFee || 0;
                violations = data.violations || [];
            } else {
                console.error('Failed to check minimum standards');
                penaltyFee = 0;
                violations = [];
            }
        } catch (error) {
            console.error('Error checking minimum standards:', error);
            penaltyFee = 0;
            violations = [];
        }
    }

    // Update fees display
    function updateFeesDisplay() {
        const totalFees = processingFee + penaltyFee;

        $('fees-processing').textContent = fmt(processingFee);
        $('fees-penalty').textContent = fmt(penaltyFee);
        $('fees-total').textContent = fmt(totalFees);

        // Show/hide penalty details
        const penaltyDetails = $('penalty-details');
        const violationsList = $('penalty-violations-list');

        if (violations.length > 0) {
            penaltyDetails.style.display = 'block';
            
            let html = '<ul class="mb-0 small">';
            violations.forEach(v => {
                html += `<li>
                    <strong>UGX ${v.denominationValue.toLocaleString()}</strong>: 
                    Required ${v.required} bundles, provided ${v.actual} 
                    (shortage: ${v.shortage}) 
                    - Penalty: <strong class="text-danger">${fmt(v.penalty)}</strong>
                </li>`;
            });
            html += '</ul>';
            
            violationsList.innerHTML = html;
        } else {
            penaltyDetails.style.display = 'none';
            violationsList.innerHTML = '';
        }
    }

    // Public API
    window.depositFeesCalculator = {
        recalculate: recalculateFees,
        getProcessingFee: () => processingFee,
        getPenaltyFee: () => penaltyFee,
        getTotalFees: () => processingFee + penaltyFee,
        getViolations: () => violations
    };

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();