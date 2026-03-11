namespace GatewayService.Static
{
    public static class SystemPolicy
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
        }

    }
}
