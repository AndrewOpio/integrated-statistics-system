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

            public const string BranchManager = "Panel B";
            public const string RemittancesOfficer = "Remittance Officer";

            public const string CbMaker = "CB Maker";
            public const string CbApprover = "CB Approver";
            public const string BouChecker = "Bou Checker";
            public const string PanelA = "Panel A";
            public const string PanelB = "Panel B";

            public const string Admin = "Admin";
            public const string HeadBackOffice = "Head of Back Office";
            public const string AssistantBranchManager = "Assistant Branch Manager";
            public const string HeadOfBackOffice = "Head of Back Office";
        }

        public static class ClaimType
        {
            public const string AppRole = "appRole";
            public const string AppPermission = "Permission";
        }

        public static class SystemSettings
        {
            public const string AppAcronym = "CAS";
            public const string AppRoleClaimType = "appRoleId";
        }

        public static class Claims
        {
            public const string ContractManager = $"{SystemSettings.AppAcronym}:Contract Manager";
            public const string VerificationOfficer = $"{SystemSettings.AppAcronym}:Verification Officer";
            public const string StockVerifier = $"{SystemSettings.AppAcronym}:Stock Verifier";
            public const string StockApprover = $"{SystemSettings.AppAcronym}:Stock Approver";
            public const string CurrencyApprover = $"{SystemSettings.AppAcronym}:Currency Approver";
            public const string Director = $"{SystemSettings.AppAcronym}:Director";
            public const string CbId = "cbId";
        }

        public static class FeeTypes {
            public const string ProcessingFees = "Processing Fees";
            public const string MinDepositFees = "Minimum Deposit Penalty Fees";
        }
    }
}