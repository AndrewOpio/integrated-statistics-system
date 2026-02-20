namespace IdentityService.Core.Static
{
    public class SystemPermissions
    {
        public static class USM_ApplicationManagement
        {
            public const string ViewApplications = "USM ApplicationsController Index";
            public const string ViewDetails = "USM ApplicationsController Details";
            public const string CreateApplication = "USM ApplicationsController Create";
            public const string EditApplication = "USM ApplicationsController Edit";
            public const string DeleteApplication = "USM ApplicationsController Delete";
        }

        public static class USM_RoleManagement
        {
            public const string ViewRoles = "USM RolesController Index";
            public const string CreateRole = "USM RolesController Create";
        }

        public static class USM_UserManagement
        {
            public const string ViewUsers = "USM UsersController Index";
            public const string CreateUser = "USM UsersController Create";
            public const string UpdateUser = "USM UsersController Update";
            public const string ManageUserRoles = "USM UsersController ManageRoles";
        }
    }
}
