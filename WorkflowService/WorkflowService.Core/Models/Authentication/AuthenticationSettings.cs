namespace WorkflowService.Core.Models.Authentication
{
    public class AuthenticationSettings
    {
        public string LoginPath { get; set; }
        public string AccessDeniedPath { get; set; }

        /// <summary>
        /// Domain the shared AuthToken cookie is scoped to (e.g. ".bou.or.ug").
        /// Must match the value used when the cookie is issued, otherwise the
        /// cookie can never be deleted. Leave empty for host-only cookies (localhost).
        /// </summary>
        public string? CookieDomain { get; set; }
    }
}
