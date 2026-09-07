namespace VMSService.Core.Static
{
    public static class StockConstants
    {
        public static class StockCounterPartyType
        {
            public const string Vendor = "Vendor";
            public const string CommercialBank = "Commercial Bank";
            public const string Branch = "Branch";
            public const string Category = "Currency Category";
            public const string Cfb = "CFB";
        }

        public static class StoreTypes
        {
            public const string New = "New";
            public const string Paids = "Paids";
            public const string Cancelled = "Cancelled";
            public const string Reissue = "Re-Issue";
            public const string MWB = "MWB"; //Money With Boards
        }

        public static class DenominationTypes
        {
            public const string Note = "Note";
            public const string Coin = "Coin";
        }

        public static class DestructionTypes
        {
            public const string Online  = "Online";
            public const string Offsite = "Offsite";

            /// <summary>Fixed capacity of a single bag of cancelled notes destined for destruction, regardless of denomination.</summary>
            public const int NotesPerDestructionBag = 500000;
        }

        public static class StockMovementTypes
        {
            public const string BranchToBranch = "BranchToBranch";
            public const string BufferToOperational = "BufferToOperational";
            public const string OperationalToBuffer = "OperationalToBuffer";
            public const string CommBankToBranch = "Commercial Bank To Branch";
            public const string BranchToCommBank = "BranchToCommercialBank";
            public const string PaidsToMWB = "Paids To Money With Boards";
            // Reversal of PaidsToMWB, posted when a plan's results are cancelled.
            public const string MWBToPaids = "Money With Boards To Paids";
            // MI & Fit issued to boards alongside the Paids -> MWB move at IssueToBoards.
            public const string MIToMWB = "MI To Money With Boards";
            public const string FitToMWB = "Fit To Money With Boards";
            // Reversals of the two above, posted at ConfirmResults from what the board supervisors
            // declared they handed back unused.
            public const string MWBToMI = "Money With Boards To MI";
            public const string MWBToFit = "Money With Boards To Fit";
            public const string MWBToReIssue = "Money With Boards To Re-Issue";
            public const string MWBToCancelled = "Money With Boards To Cancelled";
            public const string MWBToShredded = "Money With Boards To Shredded";
            public const string BranchToCfb = "BranchToCfb";
            public const string VendorToBranch = "VendorToBranch";
        }

        public static readonly (string Key, string Value)[] MovementTypes = new[]
        {
            (Key: StockMovementTypes.BranchToBranch, Value: "Branch to Branch"),
            (Key: StockMovementTypes.BufferToOperational, Value: "Buffer to Operational"),
            (Key: StockMovementTypes.OperationalToBuffer, Value: "Operational to Buffer")
        };

        public static class ContainerTypes
        {
            public const string Bag = "Bag";
            public const string Box = "Box";
        }

        public static class SubCategories
        {
            public const string Fit = "Fit";
            public const string Unfit = "Unfit";
            public const string NA = "N/A";
            public const string Mixed = "Mixed";

            /// <summary>The only subcategories a commercial bank may declare against deposited
            /// banknotes. The Paids category carries more than these — "Mixed", plus whatever an
            /// admin adds through the SubCategories screen — and deposit-denom-table.js resolves
            /// its Fit/Unfit/N-A constants by name, silently ignoring anything else: an
            /// unrecognised option would be selectable but excluded from the Fit/Unfit subtotals
            /// and from the legend above the table. So the narrowing happens server-side, before
            /// the list is ever serialised into the page.</summary>
            /// Case-insensitive on purpose: deposit-denom-table.js matches these names with
            /// label.toLowerCase(), so a "FIT" row in the database is one it can still classify.
            /// Filtering more strictly than the script that consumes the list would drop an
            /// option the UI would otherwise have handled correctly.
            public static readonly IReadOnlySet<string> DepositSelectable =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Fit, Unfit, NA };
        }

        public static class StockCategories
        {
            public const string New     = "New";
            public const string ReIssue = "Re-Issue";
            public const string Paids   = "Paids";
            public const string Cfb     = "CFB";
            public const string Mi      = "MI";
            public const string Fit     = "Fit";
            public const string Mwb     = "MWB";
        }
    }
}
