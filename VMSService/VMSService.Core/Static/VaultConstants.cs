using System;

namespace VMSService.Core.Static
{
    public static class VaultConstants
    {
        public const string BagsWithBoardsVaultName = "BAGS";

        // Bags are attached to this section automatically/systematically — manual attachment is disallowed.
        public const string BagsWithBoardsSectionName = "BAGS WITH BOARDS";

        // Sits under the same "BAGS WITH BOARDS" vault; holds legacy bags freshly created from the
        // StockProcessingPlan Create modal, kept distinct from BagsWithBoardsSectionName so they still
        // surface in the Available Bags pool instead of being treated as already-issued-to-boards.
        public const string NewLegacyBagsSectionName = "NEW LEGACY BAGS";

        // Sections whose bag membership is managed entirely by the system — manual attachment is disallowed.
        public static bool IsSystemManagedSectionName(string? name)
            => string.Equals(name?.Trim(), BagsWithBoardsSectionName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name?.Trim(), NewLegacyBagsSectionName, StringComparison.OrdinalIgnoreCase);
    }
}
