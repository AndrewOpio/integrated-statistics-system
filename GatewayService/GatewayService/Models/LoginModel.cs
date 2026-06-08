using System.ComponentModel.DataAnnotations;

namespace GatewayService.Models
{
    public class LoginModel
    {
        public record LoginResponse
        {
            public string Token { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string DisplayName { get; set; }

            /// <summary>
            /// Indicates if the user must change their password before proceeding.
            /// </summary>
            public bool MustChangePassword { get; set; } = false;

            // For 2FA flow
            public bool RequiresRsaMfa { get; set; }
            public string? RsaAuthnAttemptId { get; set; }
            public string? RsaMessageId { get; set; }
            public string? RsaPromptText { get; set; }
        }
        public record LogoutResponse(string Message);
        public record FirstFactorRequest
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;
        }

        public record FirstFactorResponse
        {
            public bool Success { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public bool MustChangePassword { get; set; }
            public bool RequiresSecondFactor { get; set; }
            public string? RsaAuthnAttemptId { get; set; }
            public string? RsaMessageId { get; set; }
            public string? RsaPromptText { get; set; }
            public string Message { get; set; } = string.Empty;
        }



        public record SecondFactorResponse
        {
            public bool Success { get; set; }
            public bool RequiresRetry { get; set; }
            public string? RsaAuthnAttemptId { get; set; }
            public string? RsaMessageId { get; set; }
            public string? RsaPromptText { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public record LoginRequest
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;

            // For 2FA
            public string? RsaPasscode { get; set; }
            public string? AuthnAttemptId { get; set; }
            public string? InResponseTo { get; set; }
        }

        public record SecondFactorRequest
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required]
            public string RsaPasscode { get; set; } = string.Empty;

            [Required]
            public string AuthnAttemptId { get; set; } = string.Empty;

            public string? InResponseTo { get; set; }
        }

        public class RsaCancelRequest
        {
            public string AuthnAttemptId { get; set; } = string.Empty;
            public bool RemoveAttemptId { get; set; } = true;
            public string Reason { get; set; } = "USER_ACTION";
        }

        public record RetryRSA
        {
            public string RsaAuthnAttemptId { get; set; } = string.Empty;
            public bool CanRetry { get; set; } = true;
            public string Message { get; set; } = string.Empty;
        }
    }
}
