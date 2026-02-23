namespace IdentityService.Core.Static
{
    public class SystemPolicy
    {
        public static class Roles
        {
            public const string IsAdmin = "IsAdmin";
        }

        public static class ClaimType
        {
            public const string AppRole = "appRole";
            public const string AppPermission = "Permission";
        }

        public static class Claims
        {
            public const string USMAdmin = "USM:Admin";

            public const string USMViewApplications = "USM:ViewApplications";
            public const string USMViewApplicationDetails = "USM:ViewApplicationDetails";
            public const string USMEditApplication = "USM:EditApplication";
            public const string USMDeleteApplication = "USM:DeleteApplication";
            public const string USMViewRoles = "USM:ViewRoles";
            public const string USMCreateRole = "USM:CreateRole";
            public const string USMViewUsers = "USM:ViewUsers";
            public const string USMCreateUser = "USM:CreateUser";
            public const string USMUpdateUser = "USM:UpdateUser";
            public const string USMManageUserRoles = "USM:ManageUserRoles";

            public const string ContractManager = "VMS:Contract Manager";
            public const string VerificationOfficer = "VMS:Verification Officer";
            public const string StockVerifier = "VMS:Stock Verifier"; // Assitant Branch Manager
            public const string StockApprover = "VMS:Stock Approver"; // Branch Manager
            public const string Director = "VMS:Director";
        }
    }
}
