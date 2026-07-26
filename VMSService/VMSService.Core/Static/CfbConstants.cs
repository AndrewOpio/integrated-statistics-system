using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Static
{
    public static class CfbConstants
    {
        public static class BoxTypes
        {
            // Must match StatusConstants.DenominationName — same strings StockService returns in DenominationType.Name.
            public const string Notes = StatusConstants.DenominationName.Note;
            public const string Coins = StatusConstants.DenominationName.Coin;
        }

        public static class MovementTypes
        {
            public const string TopUp = "TopUp";
            public const string SessionLoss = "SessionLoss";
            public const string SessionSurplus = "SessionSurplus";
            public const string Reimbursement = "Reimbursement";
        }
        public static class CfbStatusNames
        {

            public const string Draft = "Draft";
            public const string PendingReview = "Pending Review";
            public const string ReturnedForRevision = "Returned for Revision";
            public const string PendingVerification = "Pending Verification";
            public const string PendingApproval = "Pending Approval";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
        }
        public static class CfbStatusActionNames
        {
            public const string SubmitForReview = "Submit for Review";
            public const string RequestForRevisions = "Request Revisions";
            public const string ResubmitForReview = "Resubmit for Review";
            public const string SendForVerification = "Send for Verification";
            public const string SubmitForApproval = "Submit for Approval";
            public const string Approve = "Approve";
            public const string Reject = "Reject";
        }
    }
}
