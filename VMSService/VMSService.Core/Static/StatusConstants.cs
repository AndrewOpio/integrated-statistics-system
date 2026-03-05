namespace VMSService.Core.Static
{
    public static class StatusConstants
    {
        public static class Category
        {
            public const string ApprovedCurrencyProcurement = "Approved Currency Procurement";
            public const string ExPrintMint = "ExPrint and ExMint";
            public const string ReceivedStock = "Received Stock";
        }

        public static class ApprovedCurrencyProcurement
        {
            public const string Draft = "Draft";
            public const string PendingVerification = "Pending Verification";
            public const string Verified = "Verified";
            public const string Returned = "Returned";
        }

        public static class ExPrintMint
        {
            public const string Draft = "Draft";
            public const string PendingVerification = "Pending Verification";
            public const string PendingReceipt = "Pending Receipt";
            public const string Returned = "Returned";
            public const string Verified = "Verified";
        }

        public static class ReceivedStock {
            public const string Draft = "Draft";
            public const string PendingConfirmation = "Pending Confirmation";
            public const string Confirmed = "Confirmed";
            public const string Returned = "Returned";
        }

        public static class Entity
        {
            public const string ApprovedCurrencyProcurement = "ApprovedCurrencyProcurement";
            public const string ExPrintMint = "ExPrintMint";
            public const string ReceivedStock = "ReceivedStock";
        }
    }
}
