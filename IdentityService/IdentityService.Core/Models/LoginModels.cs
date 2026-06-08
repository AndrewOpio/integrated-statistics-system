using System.ComponentModel.DataAnnotations;

namespace IdentityService.Core.Models
{
    public class FirstFactorRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class FirstFactorResponse
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

    public class SecondFactorRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string RsaPasscode { get; set; } = string.Empty;

        [Required]
        public string AuthnAttemptId { get; set; } = string.Empty;

        public string? InResponseTo { get; set; }
    }

    public class SecondFactorResponse
    {
        public bool Success { get; set; }
        public bool RequiresRetry { get; set; }
        public string? RsaAuthnAttemptId { get; set; }
        public string? RsaMessageId { get; set; }
        public string? RsaPromptText { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class LoginRequest
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
}