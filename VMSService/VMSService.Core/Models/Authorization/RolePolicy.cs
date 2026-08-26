namespace VMSService.Core.Authorization
{
    /// <summary>
    /// Single source of truth for role names and groupings.
    /// Role name strings must match exactly what the user management microservice puts in the JWT role claim.
    /// </summary>
    public static class RolePolicy
    {
        // ── Role names ────────────────────────────────────────────
        public const string CbMaker = "CB Maker";
        public const string CbApprover = "CB Approver";
        public const string BouChecker = "BOU Checker";
        public const string PanelA = "Panel A";
        public const string PanelB = "Panel B";
        public const string RemittanceOfficer = "Remittance Officer";
        public const string BackOffice = "Back Office";
        public const string CurrencyApprover = "Currency Approver";

        // ── Groupings ─────────────────────────────────────────────
        public static readonly IReadOnlySet<string> CbRoles =
            new HashSet<string> { CbMaker, CbApprover };

        public static readonly IReadOnlySet<string> BouRoles =
            new HashSet<string> { BouChecker, PanelA, PanelB, RemittanceOfficer, BackOffice, CurrencyApprover };

        // Withdrawal-workflow-specific — narrower than BouRoles (shared with deposits).
        // BouChecker and Head of Back Office are deliberately excluded: confirmed by the user
        // they don't do withdrawal work. See ticket 34.
        public static readonly IReadOnlySet<string> WithdrawalBouRoles =
            new HashSet<string> { BackOffice, RemittanceOfficer, CurrencyApprover, PanelB, PanelA };

        // ── Claims ────────────────────────────────────────────────
        public const string CbIdClaim = "cbId";  // present on company users only
    }
}

