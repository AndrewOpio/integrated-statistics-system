namespace IdentityService.Core.Models
{
    // Initialize Request
    public class RsaInitializeRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public List<RsaSubjectCredential> SubjectCredentials { get; set; } = new();
        public RsaContext Context { get; set; } = new();
        public int? AuthnAttemptTimeout { get; set; }
        public string? Lang { get; set; }
    }

    // Verify Request
    public class RsaVerifyRequest
    {
        public List<RsaSubjectCredential> SubjectCredentials { get; set; } = new();
        public RsaContext Context { get; set; } = new();
    }

    // Subject Credential
    public class RsaSubjectCredential
    {
        public string MethodId { get; set; } = "SECURID";
        public List<RsaCollectedInput> CollectedInputs { get; set; } = new();
    }

    // Collected Input
    public class RsaCollectedInput
    {
        public string Name { get; set; } = "SECURID";
        public string Value { get; set; } = string.Empty;
    }

    // Context
    public class RsaContext
    {
        public string AuthnAttemptId { get; set; } = string.Empty;
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string InResponseTo { get; set; } = string.Empty;
    }

    // Authentication Response
    public class RsaAuthenticationResponse
    {
        public RsaContext Context { get; set; } = new();
        public List<RsaCredentialValidationResult> CredentialValidationResults { get; set; } = new();
        public string AttemptResponseCode { get; set; } = string.Empty;
        public string AttemptReasonCode { get; set; } = string.Empty;
        public RsaChallengeMethods? ChallengeMethods { get; set; }
    }

    // Credential Validation Result
    public class RsaCredentialValidationResult
    {
        public string MethodId { get; set; } = string.Empty;
        public string MethodResponseCode { get; set; } = string.Empty;
        public string? MethodReasonCode { get; set; }
        public List<RsaAuthnAttribute> AuthnAttributes { get; set; } = new();
    }

    // Authentication Attribute
    public class RsaAuthnAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
    }

    // Challenge Methods
    public class RsaChallengeMethods
    {
        public List<RsaChallenge> Challenges { get; set; } = new();
    }

    // Challenge
    public class RsaChallenge
    {
        public string? MethodSetId { get; set; }
        public List<RsaRequiredMethod> RequiredMethods { get; set; } = new();
    }

    // Required Method
    public class RsaRequiredMethod
    {
        public string MethodId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int? Priority { get; set; }
        public List<RsaMethodVersion> Versions { get; set; } = new();
    }

    // Method Version
    public class RsaMethodVersion
    {
        public string VersionId { get; set; } = string.Empty;
        public bool ValueRequired { get; set; }
        public RsaPrompt? Prompt { get; set; }
    }

    // Prompt
    public class RsaPrompt
    {
        public string PromptResourceId { get; set; } = string.Empty;
        public string DefaultText { get; set; } = string.Empty;
        public bool Sensitive { get; set; }
        public bool SubjectNameRequired { get; set; }
    }

    // Status Request
    public class RsaStatusRequest
    {
        public string AuthnAttemptId { get; set; } = string.Empty;
        public bool RemoveAttemptId { get; set; } = true;
    }

    // Status Response
    public class RsaStatusResponse
    {
        public string AttemptResponseCode { get; set; } = string.Empty;
        public string AttemptReasonCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string AuthnPolicyId { get; set; } = string.Empty;
        public List<string> SuccessfulMethods { get; set; } = new();
        public string AttemptExpires { get; set; } = string.Empty;
    }

    // Cancel Request
    public class RsaCancelRequest
    {
        public string AuthnAttemptId { get; set; } = string.Empty;
        public bool RemoveAttemptId { get; set; } = true;
        public string Reason { get; set; } = "USER_ACTION";
    }

    // Response Codes
    public static class RsaResponseCodes
    {
        public const string Success = "SUCCESS";
        public const string Challenge = "CHALLENGE";
        public const string Fail = "FAIL";
        public const string Error = "ERROR";
        public const string InProcess = "IN_PROCESS";
    }

    // Reason Codes
    public static class RsaReasonCodes
    {
        public const string CredentialVerified = "CREDENTIAL_VERIFIED";
        public const string NoChallengesRemaining = "NO_CHALLENGES_REMAINING";
        public const string ChallengesInitialized = "CHALLENGES_INITIALIZED";
        public const string SecurIdAuthFailure = "SECURID_AUTH_FAILURE";
        public const string SecurIdNextCodeRequired = "SECURID_NEXT_CODE_REQUIRED";
        public const string SecurIdNewPinRequired = "SECURID_NEW_PIN_REQUIRED";
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string LockedOut = "LOCKED_OUT";
    }
}