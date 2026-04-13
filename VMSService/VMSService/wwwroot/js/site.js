// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
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
    if (n === 0) return 'Zero Shillings Only';
    const scales = [[1e12, 'Trillion'], [1e9, 'Billion'], [1e6, 'Million'], [1e3, 'Thousand'], [1, '']];
    let res = '', rem = n;
    scales.forEach(([s, name]) => {
        if (rem >= s) {
            const chunk = Math.floor(rem / s);
            res += wordsUnder1000(chunk) + (name ? ' ' + name + ' ' : ' ');
            rem %= s;
        }
    });
    return res.trim() + ' Shillings Only';
}


$(function () {

    // set common configuration for all datatables
    $('.datatable').each(function () {
        const tableId = $(this).attr('id');
        const defaultConfig = {
            responsive: true,
            autoWidth: false,
            language: {
                search: "_INPUT_",
                searchPlaceholder: "Search...",
                emptyTable: "No records found",
                info: "Showing _START_ to _END_ of _TOTAL_ entries",
            },
            columnDefs: [
                { orderable: false, targets: -1 } // Disables sorting on the last column
            ],
            pageLength: 10,

        };

        // Get custom configuration from data attributes if present
        const customConfig = $(this).data('config') || {};

        // Merge configurations, with custom overriding defaults
        const config = $.extend(true, {}, defaultConfig, customConfig);

        // merged all configurations
        $(this).DataTable(config);
    });

    

    //$('denom-g-words').textContent = toWords(grand);
});