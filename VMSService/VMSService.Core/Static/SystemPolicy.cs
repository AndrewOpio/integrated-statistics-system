namespace VMSService.Core.Static
{
    public static class SystemPolicy
    {
        public static class Roles
        {
            public const string ContractManager = "Contract Manager";
            public const string VerificationOfficer = "VerificationOfficer";
            public const string StockVerifier = "StockVerifier"; // Assitant Branch Manager
            public const string StockApprover = "StockApprover"; // Branch Manager
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
            public const string StockVerifier = "VMS:Stock Verifier"; // Assitant Branch Manager
            public const string StockApprover = "VMS:Stock Approver"; // Branch Manager
            public const string Director = "VMS:Director";
        }
    }
}
