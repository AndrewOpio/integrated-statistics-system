namespace VMSService.Core.Static
{
    public static class PenaltyConstants
    {
        public static class AppliesTo
        {
            public const string PreTransaction = "Pre-Transaction";
            public const string PointOfTransaction = "At the Point of Transaction";
            public const string Processing = "Processing";
        }

        public static class Source
        {
            public const string Withdrawal = "WITHDRAWAL";
            public const string Deposit = "DEPOSIT";
            public const string Processing = "PROCESSING";
        }
    }
}
