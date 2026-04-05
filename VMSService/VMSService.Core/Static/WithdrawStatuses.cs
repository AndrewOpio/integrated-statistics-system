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
            public const string Draft = "Draft";
            public const string SubmittedToSupervisor = "Pending Internal Approval";
            public const string ReturnedForRevision = "Returned for Internal Revision";
            public const string SubmittedToBank = "Pending Disbursement Confirmation";
            public const string PendingSpecialApproval = "Pending Special Approval";
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
            public const int SubmittedToBank = 27;
            public const int PendingSpecialApproval = 28;
            public const int Recommended = 29;
            public const int Approved = 30;
            public const int Rejected = 31;
            public const int Cancelled = 32;
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

        public static class WithdrawalRemarks
        {
            public const string SavedAsDraft = "Saved as draft";
            public const string SubmittedToSupervisor = "Submitted to supervisor";
            public const string SubmittedToBank = "Submitted to bank";
            public const string ReturnedForRevision = "Returned for revision";
        }
    }
}
