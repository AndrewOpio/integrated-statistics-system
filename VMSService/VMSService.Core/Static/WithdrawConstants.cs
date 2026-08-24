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
            public const string PendingRTGSVerification = "Pending RTGS Code";
            public const string VerificationFailed = "RTGS Confirmation Failed";
            public const string PendingWithdrawalConfirmation = "Pending RTGS Confirmation";
            public const string PendingExceptionalApproval = "Pending Exceptional Approval";
            public const string RejectExceptionalApproval = "Exceptional Approval Rejected";
            public const string PendingVerification = "Pending Verification";
            public const string PendingExceptionalVerification = "Pending Exceptional Verification";
            public const string PendingApproval = "Pending Approval";
            public const string RecallRequested = "Recall Requested";
            public const string RecallVerified = "Recall Verified";
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

            /// <summary>Genuinely final statuses — nobody owes this request any further action.
            /// Deliberately excludes Rejected/RejectExceptionalApproval/RecallApproved, which still
            /// owe a ReturnRtgsPending/RecallRtgsPending completion step (Complete Reject / Complete
            /// Reject Exceptional / Complete Recall) — those stay visible on the main list until
            /// actually completed.</summary>
            public static readonly string[] TerminalStatuses =
            [
                Approved,
                RejectComplete,
                RejectExceptionalApprovalComplete,
                RecallComplete,
                Expired,
            ];

            /// <summary>Statuses that exist before the CB Approver has submitted onward to BOU —
            /// the set Section 10's stale-request rule (collection date already passed) applies to.</summary>
            public static readonly string[] PreSubmissionStatuses =
            [
                Draft,
                PendingInternalApproval,
                ReturnedForRevision,
            ];
        }
        public static class WithdrawalStatusActionNames
        {
            // CB Maker / CB Approver
            public const string SubmitForInternalApproval = "SubmitForInternalApproval";
            public const string ReturnForRevision = "ReturnForRevision";
            public const string ResubmitForApproval = "ResubmitForApproval";
            public const string ApproveToRTGSVerification = "ApproveToRTGSCode";
            public const string ExceptionalWithdrawalRequest = "ExceptionalWithdrawalRequest";
            public const string RequestRecall = "RequestRecall";

            // Back Office
            public const string Submit_Verification_Exceptional = "Submit_Verification_Exceptional";
            public const string Submit_Verification = "Submit_Verification";
            public const string RtgsConfirmationFailed = "RTGS Confirmation Failed";
            public const string VerifyExceptional = "VerifyExceptional";
            public const string VerifyRecall = "VerifyRecall";
            public const string CompleteRejectExceptional = "Complete Reject Exceptional";
            public const string CompleteReject = "Complete Reject";
            public const string CompleteCancel = "Complete Cancel";
            public const string CompleteRecall = "Complete Recall";

            // Panel B
            public const string SubmitForApproval = "Submit for Approval";

            // Panel A
            public const string Approve = "Approve";
            public const string Reject = "Reject";

            // Currency Approver
            public const string ApproveExceptional = "ApproveExceptional";
            public const string RejectExceptional = "RejectExceptional";
            public const string ApproveRecall = "ApproveRecall";
            public const string RejectRecall = "RejectRecall";
        }

        public static class AmountRules
        {
            public const decimal MinimumWithdrawalAmount = 10_000_000m;
            public const decimal BundleMultiple = 1000m;
        }

        public static class ReasonRules
        {
            public const int MinLength = 15;
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
