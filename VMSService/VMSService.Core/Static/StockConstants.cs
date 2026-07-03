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
        }

        public static class StockMovementTypes
        {
            public const string BranchToBranch = "BranchToBranch";
            public const string BufferToOperational = "BufferToOperational";
            public const string OperationalToBuffer = "OperationalToBuffer";
            public const string CommBankToBranch = "Commercial Bank To Branch";
            public const string BranchToCommBank = "BranchToCommercialBank";
            public const string PaidsToMWB = "Paids To Money With Boards";
            public const string MWBToReIssue = "Money With Boards To Re-Issue";
            public const string MWBToCancelled = "Money With Boards To Cancelled";
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
        }
    }
}
