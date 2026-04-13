namespace VMSService.Core.Static
{
    public static class StatusConstants
    {
        public static class Category
        {
            public const string ApprovedCurrencyProcurement = "Approved Currency Procurement";
            public const string ExPrintMint = "ExPrint and ExMint";
            public const string ReceivedStock = "Received Stock";
            public const string StockMovement = "Stock Movement";
            public const string StockMovementDiscrepancy = "Stock Movement Discrepancy";
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

        public static class StockMovement
        {
            public const string Draft = "Draft";
            public const string PendingVerification = "Pending Verification";
            public const string PendingReceipt = "Pending Receipt";
            public const string Returned = "Returned";
            public const string Approved = "Approved";
            public const string Dispatched = "Dispatched";
            public const string Received = "Received";
        }

        public static class StockMovementDiscrepancy
        {
            public const string PendingDiscrepancy = "Pending Discrepancy";
            public const string DiscrepancyPendingVerification = "Discrepancy Pending Verification";
            public const string DiscrepancyPendingResolution = "Discrepancy Pending Resolution";
            public const string DiscrepancyResolved = "Discrepancy Resolved";
        }
        public static class ApprovedWithdrawalRequest
        {
            public const string Draft = "Draft";
            public const string PendingInternalApproval = "Pending Internal Approval";
            public const string Verified = "Verified";
            public const string Returned = "Returned";
        }

        public static class AgentDetail
        {
            public const string Draft = "Draft";
            public const string Returned = "Returned";
            public const string PendingInternalApproval = "Pending Internal Approval";
            public const string PendingBouApproval = "Pending BOU Approval";
            public const string Rejected = "Rejected";
            public const string Approved = "Approved";
        }

        public static class Entity
        {
            public const string ApprovedCurrencyProcurement = "ApprovedCurrencyProcurement";
            public const string ExPrintMint = "ExPrintMint";
            public const string ReceivedStock = "ReceivedStock";
            public const string StockMovement = "StockMovement";
            public const string StockMovementDiscrepancy = "StockMovementDiscrepancy";
            public const string WithdrawalRequest = "WithdrawalRequest";
            public const string AgentDetail = "AgentDetail";
            public const string DepositRequest = "DepositRequest";
        }
    }
}
