namespace VMSService.Core.Static
{
    public static class SystemPolicy
    {
        public static class Roles
        {
            public const string ContractManager = "Contract Manager";
            public const string VerificationOfficer = "Verification Officer";
            public const string StockVerifier = "Stock Verifier"; 
            public const string StockApprover = "Stock Approver"; 
            public const string CurrencyApprover = "Currency Approver";
            public const string Director = "Director";
            public const string FinanceAndBudgetingOfficer = "Finance and Budgeting Officer";
            public const string BranchManager = "Branch Manager";
            public const string AssistantBranchManager = "Assistant Branch Manager";
            public const string RemittancesOfficer = "Remittances and Control Officer";
            public const string HeadOfBackOffice = "Head of Back Office";
        }

        public static class ClaimType
        {
            public const string AppRole = "appRole";
            public const string AppPermission = "Permission";
        }

        public static class Claims
        {
            public const string ContractManager = "VMS:Contract Manager";
            public const string VerificationOfficer = "VMS:Verification Officer";
            public const string StockVerifier = "VMS:Stock Verifier";
            public const string StockApprover = "VMS:Stock Approver";
            public const string CurrencyApprover = "VMS:Currency Approver";
            public const string Director = "VMS:Director";
        }
    }
}
