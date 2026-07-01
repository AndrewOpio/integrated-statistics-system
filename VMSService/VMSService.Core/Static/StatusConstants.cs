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
            public const string PendingDiscrepancy = "Pending Mismatch";
            public const string DiscrepancyPendingVerification = "Mismatch Pending Verification";
            public const string DiscrepancyPendingResolution = "Mismatch Pending Resolution";
            public const string DiscrepancyResolved = "Mismatch Resolved";
        }
        public static class WithdrawalRequest
        {
            public const string Draft = "Draft";
            public const string SpecialApproval = "Special Approval";
            public const string PendingInternalApproval = "Pending Internal Approval";
            public const string PendingDisbursement = "Request Withdraw";
            public const string Recommended = "Recommended";
            public const string Verified = "Verified";
            public const string Returned = "Returned";
            public const string Authorized = "Approved by BOU";
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

        public static class Logistics
        {
            public const string Draft = "Draft";
            public const string PendingApproval = "Pending Approval";
            public const string Approved = "Approved";
        }

        public static class DestructionRequest
        {
            public const string Draft = "Draft";
            public const string PendingVerification = "Pending Verification";
            public const string PendingApproval = "Pending Approval";
            public const string Approved = "Approved";
            public const string PendingCompletion = "Pending Completion";
            public const string Completed = "Completed";
            public const string ReturnedToStorage = "Returned To Storage";
            public const string ReturnedForDestruction = "Returned For Destruction";
        }

        public static class DestructionAuthorization
        {
            public const string Draft             = "Draft";
            public const string PendingApproval   = "Pending Approval";
            public const string Approved          = "Approved";
            public const string Rejected          = "Rejected";
            public const string AmendmentDraft    = "Amendment Draft";
            public const string AmendmentApproved = "Amendment Approved";
        }

        public static class CfbReport
        {
            public const string Draft = "Draft";
            public const string PendingVerification = "Pending Verification";
            public const string PendingApproval = "Pending Approval";
            public const string Approved = "Approved";
            public const string Returned = "Returned";
        }

        public static class MonthlySurplus
        {
            public const string Draft                    = "Draft";
            public const string PendingAcknowledgement   = "Pending Acknowledgement";
        }

        public static class MonthlyShortage
        {
            public const string Draft                = "Draft";
            public const string PendingApproval      = "Pending Approval";
            public const string PendingConfirmation  = "Pending Confirmation";
            public const string Confirmed            = "Confirmed";
        }

        public static class MonthlyCommercialBankSettlement
        {
            public const string Draft                = "Draft";
            public const string PendingApproval      = "Pending Approval";
            public const string PendingConfirmation  = "Pending Confirmation";
            public const string Confirmed            = "Confirmed";
        }

        public static class Entity
        {
            public const string ApprovedCurrencyProcurement   = "ApprovedCurrencyProcurement";
            public const string ExPrintMint                   = "ExPrintMint";
            public const string ReceivedStock                 = "ReceivedStock";
            public const string StockMovement                 = "StockMovement";
            public const string StockMovementDiscrepancy      = "StockMovementDiscrepancy";
            public const string Logistics                     = "Logistics";
            public const string WithdrawalRequest             = "Withdrawal Request";
            public const string AgentDetail                   = "AgentDetail";
            public const string DepositRequest                = "DepositRequest";
            public const string DepositRequestExceptional     = "DepositRequestExceptional";
            public const string DestructionRequest            = "Destruction";
            public const string DestructionAuthorization      = "DestructionAuthorization";
            public const string CfbReport                     = "EndOfDayDiscrepancies";
            public const string MonthlySurpluses              = "MonthlySurpluses";
            public const string MonthlyShortages              = "MonthlyShortages";
            public const string MonthlyCommercialBankSettlements      = "MonthlyCommercialBankSettlements";
        }
    }
}

