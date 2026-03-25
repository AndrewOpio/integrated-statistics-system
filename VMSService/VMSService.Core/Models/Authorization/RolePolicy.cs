namespace VMSService.Core.Authorization
{
    /// <summary>
    /// Single source of truth for role names and groupings.
    /// Role name strings must match exactly what the user management microservice puts in the JWT role claim.
    /// </summary>
    public static class RolePolicy
    {
        // ── Role names ────────────────────────────────────────────
        public const string CbMaker = "maker";
        public const string CbApprover = "CB Approver";
        public const string BouChecker = "Bou Checker";
        public const string PanelA = "panel a";
        public const string PanelB = "pabel b";

        // ── Groupings ─────────────────────────────────────────────
        public static readonly IReadOnlySet<string> CbRoles =
            new HashSet<string> { CbMaker, CbApprover };

        public static readonly IReadOnlySet<string> BouRoles =
            new HashSet<string> { BouChecker, PanelA, PanelB };

        // ── Policy names (used in [Authorize] attributes) ─────────
        public const string CbOnly = "CbOnly";
        public const string BouOnly = "BouOnly";

        // ── Claims ────────────────────────────────────────────────
        public const string CbIdClaim = "cbId";  // present on company users only
    }
}

