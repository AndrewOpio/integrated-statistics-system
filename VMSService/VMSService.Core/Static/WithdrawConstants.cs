using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Static
{
    public static class WithdrawConstants
    {
        public static class WithdrawalStatusNames
        {
            public const string Draft = "Internal Draft";
            public const string PendingInternalApproval = "Pending Internal Approval";
            public const string ReturnedForRevision = "Returned for Internal Revision";
            public const string PendingRTGSVerification = "Pending RTGS Verification";
            public const string VerificationFailed = "Verification Failed";
            public const string PendingWithdrawalConfirmation = "Pending Withdrawal Confirmation";
            public const string PendingExceptionalApproval = "Pending Exceptional Approval";
            public const string RejectExceptionalApproval = "Reject Exceptional Approval";
            public const string PendingVerification = "Pending Verification";
            public const string PendingApproval = "Pending Approval";
            public const string RecallRequested = "Recall Requested";
            public const string RecallApproved = "Recall Approved";
            public const string RecallComplete = "Recall Complete";
            //public const string BouDraft = "BOU Draft";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
            public const string Completed = "Completed";
            public const string Expired = "Expired";
            public const string RejectComplete = "Reject Complete";
            public const string Cancelled = "Cancelled";
            public const string CancelComplete = "Cancel Complete";
            public const string RejectExceptionalApprovalComplete = "Reject Exceptional Approval Complete";
        }
        public static class WithdrawalStatusActionNames
        {
            public const string ApproveRecall = "ApproveRecall";
            public const string Submit_Verification_Exceptional = "Submit_Verification_Exceptional";
            public const string Submit_Verification = "Submit_Verification";
        }

        public static class AmountRules
        {
            public const decimal MinimumWithdrawalAmount = 10_000_000m;
            public const decimal BundleMultiple = 1000m;
        }

        public static class RtgsVerification
        {
            public const string Pending = "Pending";
            public const string Confirmed = "Confirmed";
            public const string AmountMismatch = "AmountMismatch";
            public const string DateMismatch = "DateMismatch";
            public const string NotFound = "NotFound";
        }
        public static string ResolveDisplayOrigin(string statusName)
        {
            return statusName switch
            {
                WithdrawalStatusNames.Draft
                or WithdrawalStatusNames.PendingInternalApproval
                //or WithdrawalStatusNames.PendingExceptionalApproval
                //or WithdrawalStatusNames.PendingRecommendation
                //or WithdrawalStatusNames.PendingDisbursement
                => WithdrawalOrigin.Cb,

                _ => WithdrawalOrigin.Bou,
            };
        }
        public static class BundleTypes
        {
            public const string New = "New";
            public const string Reissue = "Reissue";
            public const string Coin = "Coin";
            public const string NotApplicable = "NA";
        }

        public static class WithdrawalOrigin
        {
            public const string Cb = "Cb";
            public const string Bou = "BOU";
        }

        public static class WithdrawalEnteredBy
        {
            public const string Cb = "Cb";
            public const string Bou = "BOU";
        }

    }
}
