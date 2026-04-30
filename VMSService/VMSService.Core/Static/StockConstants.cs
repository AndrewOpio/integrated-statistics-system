namespace VMSService.Core.Static
{
    public static class StockConstants
    {
        public static class StockCounterPartyType
        {
            public const string Vendor = "Vendor";
            public const string CommercialBank = "Commercial Bank";
        }

        public static class StoreTypes
        {
            public const string New = "New";
            public const string Paids = "Paids";
        }

        public static class DenominationTypes
        {
            public const string Note = "Note";
            public const string Coin = "Coin";
        }

        public static class StockMovementTypes
        {
            public const string BranchToBranch = "BranchToBranch";
            public const string BufferToOperational = "BufferToOperational";
            public const string OperationalToBuffer = "OperationalToBuffer";
        }

        public static readonly (string Key, string Value)[] MovementTypes = new[]
        {
            (Key: StockMovementTypes.BranchToBranch, Value: "Branch to Branch"),
            (Key: StockMovementTypes.BufferToOperational, Value: "Buffer to Operational"),
            (Key: StockMovementTypes.OperationalToBuffer, Value: "Operational to Buffer")
        };
    }
}
