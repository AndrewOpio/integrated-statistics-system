namespace VMSService.Core.Static
{
    public static class SystemPolicy
    {
        public static class Roles
        {
            public const string ContractManager = "Contract Manager";
            public const string VerificationOfficer = "VerificationOfficer";
            public const string StockVerifier = "StockVerifier"; 
            public const string StockApprover = "StockApprover"; 
            public const string CurrencyApprover = "CurrencyApprover";
            public const string Director = "Director";
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
