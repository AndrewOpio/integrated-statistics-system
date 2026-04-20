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
            public const string PendingSpecialApproval = "Pending Exceptional Approval";
            public const string Recommended = "Recommended";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
            public const string Cancelled = "Cancelled";
            public const string Completed = "Completed";
        }

        //public static class DepositStatusIds
        //{
        //    public const string Draft = "Draft";
        //    public const string SubmittedToSupervisor = "Submitted To Supervisor";
        //    public const string ReturnedForRevision = "Returned For Revision";
        //    public const string SubmittedToBank = "Submitted To Bank";
        //    public const string PendingSpecialApproval = "Pending Special Approval";
        //    public const string Recommended = "Recommended";
        //    public const string Approved = "Approved";
        //    public const string Rejected = "Rejected";
        //    public const string Cancelled = "Cancelled";
        //}

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