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
        }
    }
}
