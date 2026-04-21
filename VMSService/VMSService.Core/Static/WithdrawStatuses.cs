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
            public const string PendingDisbursement = "Pending Disbursement";
            public const string PendingExceptionalApproval = "Pending Exceptional Approval";
            public const string Recommended = "Recommended";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
            public const string Cancelled = "Cancelled";
        }
        public static class WithdrawalStatusIds
        {
            public const int Draft = 24;
            public const int SubmittedToSupervisor = 25;
            public const int ReturnedForRevision = 26;
            public const int PendingRecommendation = 27;
            public const int PendingSpecialApproval = 28;
            public const int Recommended = 29;
            public const int Approved = 30;
            public const int Rejected = 31;
            public const int Cancelled = 32;
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
