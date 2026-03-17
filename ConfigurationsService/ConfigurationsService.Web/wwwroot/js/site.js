// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(function () {

    // set common configuration for all datatables
    $('.datatable').each(function () {
        const tableId = $(this).attr('id');
        const defaultConfig = {
            responsive: true,
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





});