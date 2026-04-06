namespace VMSService.Core.Static
{
    public static class DepositStatuses
    {
        public static class DepositStatusNames
        {
            public const string Draft = "Draft";
            public const string SubmittedToSupervisor = "Pending Internal Approval";
            public const string ReturnedForRevision = "Returned for Internal Revision";
            public const string SubmittedToBank = "Pending Deposit Confirmation";
            public const string PendingSpecialApproval = "Pending Special Approval";
            public const string Recommended = "Recommended";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
            public const string Cancelled = "Cancelled";
        }

        public static class DepositStatusIds
        {
            public const int Draft = 33;
            public const int SubmittedToSupervisor = 34;
            public const int ReturnedForRevision = 35;
            public const int SubmittedToBank = 36;
            public const int PendingSpecialApproval = 37;
            public const int Recommended = 38;
            public const int Approved = 39;
            public const int Rejected = 40;
            public const int Cancelled = 41;
        }

        public static class DepositOrigin
        {
            public const string Cb = "Cb";
            public const string Bou = "BOU";
        }

        public static class DepositEnteredBy
        {
            public const string Cb = "Cb";
            public const string Bou = "BOU";
        }

        public static class DepositRemarks
        {
            public const string SavedAsDraft = "Saved as draft";
            public const string SubmittedToSupervisor = "Submitted to supervisor";
            public const string SubmittedToBank = "Submitted to bank";
            public const string ReturnedForRevision = "Returned for revision";
        }
    }
}