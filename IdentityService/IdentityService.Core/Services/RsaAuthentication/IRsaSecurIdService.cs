using IdentityService.Core.Models;

namespace IdentityService.Core.Services.RsaAuthentication
{
    public interface IRsaSecurIdService
    {
        /// <summary>
        /// Initialize authentication - Start a new authentication attempt
        /// </summary>
        Task<RsaAuthenticationResponse> InitializeAuthenticationAsync(string username, string? passcode = null);

        /// <summary>
        /// Verify authentication - Submit credentials for a challenge
        /// </summary>
        Task<RsaAuthenticationResponse> VerifyAuthenticationAsync(string authnAttemptId, string passcode, string inResponseTo);

        /// <summary>
        /// Check authentication status
        /// </summary>
        Task<RsaStatusResponse> CheckAuthenticationStatusAsync(string authnAttemptId);

        /// <summary>
        /// Cancel authentication attempt
        /// </summary>
        Task<bool> CancelAuthenticationAsync(string authnAttemptId, string reason = "USER_ACTION");
    }
}