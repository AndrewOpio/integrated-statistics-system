using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Static
{
    public static class WithdrawStatuses
    {
        public static class WithdrawalStatusNames
        {
            public const string Draft = "Internal Draft";
            public const string PendingInternalApproval = "Pending Internal Approval";
            public const string ReturnedForRevision = "Returned for Internal Revision";
            public const string PendingRecommendation = "Pending Recommendation";
            public const string PendingExceptionalApproval = "Pending Exceptional Approval";
            public const string PendingConfirmation = "Pending Confirmation";
            public const string WithdrawalConfirmed = "Withdrawal Confirmed";
            public const string WithdrawalReturned = "Withdrawal Returned";
            public const string RejectExceptionalApproval = "Reject Exceptional Approval";
            public const string BouDraft = "BOU Draft";
            public const string PendingWithdrawalConfirmation = "Pending Withdrawal Confirmation";
            public const string RecallRequested = "Recall Requested";
            public const string Recalled = "Recalled";
            public const string RecallComplete = "Recall Complete";
            public const string VerificationFailed = "Verification Failed";
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
