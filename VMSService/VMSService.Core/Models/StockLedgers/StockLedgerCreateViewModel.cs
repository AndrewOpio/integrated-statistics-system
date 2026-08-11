using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.StockLedgers
{
    /// <summary>
    /// Backs the manual "Add To Ledger" form. One ledger header plus the detail lines that are
    /// posted with it — the StockService create endpoint persists both in a single call.
    /// </summary>
    public class StockLedgerCreateViewModel
    {
        [Required(ErrorMessage = "Reference code is required.")]
        [StringLength(100, ErrorMessage = "Reference code cannot exceed 100 characters.")]
        [Display(Name = "Reference Code")]
        public string ReferenceCode { get; set; } = string.Empty;

        [Display(Name = "Direction")]
        public bool IsIn { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "Select a branch (store).")]
        [Display(Name = "Branch (Store)")]
        public int StoreId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a counter party type.")]
        [Display(Name = "Counter Party Type")]
        public int CounterPartyTypeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a counter party.")]
        [Display(Name = "Counter Party")]
        public int CounterPartyId { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        // Detail lines are posted as JSON from the dynamic rows, so they bypass model binding
        // of a nested collection (which does not survive rows being added/removed client-side).
        public string? DetailsJson { get; set; }

        // Populated for redisplay only.
        public List<StockLedgerDetailEntry> Details { get; set; } = new();
    }

    public class StockLedgerDetailEntry
    {
        public int DenominationId { get; set; }
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public int NumberOfPieces { get; set; }
        public int? NumberOfBundles { get; set; }
        public int NumberOfBoxes { get; set; }
        public decimal Value { get; set; }
    }

    /// <summary>Lookup row for the Category → Sub-Category cascade in the form.</summary>
    public class SubCategoryOption
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Counter party options for one counter party type. Which list applies is decided by the
    /// type's name, since CounterPartyId points at a different table per type.
    /// </summary>
    public class CounterPartyOption
    {
        public int CounterPartyTypeId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
