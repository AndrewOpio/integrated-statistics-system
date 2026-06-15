using IdentityService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace IdentityService.Core.Services.RsaAuthentication
{
    public class RsaSecurIdService : IRsaSecurIdService
    {
        private readonly HttpClient _httpClient;
        private readonly RsaSecurIdSettings _settings;
        private readonly ILogger<RsaSecurIdService> _logger;

        public RsaSecurIdService(
            HttpClient httpClient,
            IOptions<RsaSecurIdSettings> settings,
            ILogger<RsaSecurIdService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("client-key", _settings.ClientKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<RsaAuthenticationResponse> InitializeAuthenticationAsync(string username, string? passcode = null)
        {
            try
            {
                var request = new RsaInitializeRequest
                {
                    ClientId = _settings.ClientId,
                    SubjectName = username,
                    AuthnAttemptTimeout = _settings.AuthnAttemptTimeout,
                    Lang = _settings.Language,
                    Context = new RsaContext
                    {
                        AuthnAttemptId = string.Empty,
                        MessageId = Guid.NewGuid().ToString(),
                        InResponseTo = string.Empty
                    }
                };

                // If passcode provided, include it (single-step authentication)
                if (!string.IsNullOrEmpty(passcode))
                {
                    request.SubjectCredentials = new List<RsaSubjectCredential>
                    {
                        new RsaSubjectCredential
                        {
                            MethodId = "SECURID",
                            CollectedInputs = new List<RsaCollectedInput>
                            {
                                new RsaCollectedInput
                                {
                                    Name = "SECURID",
                                    Value = passcode
                                }
                            }
                        }
                    };
                }

                _logger.LogInformation("Initializing RSA authentication for user: {Username}", username);

                var response = await _httpClient.PostAsJsonAsync("/mfa/v1_1/authn/initialize", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("RSA initialize failed with status {StatusCode}: {Content}",
                        response.StatusCode, responseContent);
                    
                    return new RsaAuthenticationResponse
                    {
                        AttemptResponseCode = RsaResponseCodes.Error,
                        AttemptReasonCode = $"HTTP_{response.StatusCode}"
                    };
                }

                var result = JsonSerializer.Deserialize<RsaAuthenticationResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("RSA initialize response for {Username}: {ResponseCode}",
                    username, result?.AttemptResponseCode);

                return result ?? new RsaAuthenticationResponse
                {
                    AttemptResponseCode = RsaResponseCodes.Error,
                    AttemptReasonCode = "DESERIALIZATION_FAILED"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during RSA initialize for user: {Username}", username);
                return new RsaAuthenticationResponse
                {
                    AttemptResponseCode = RsaResponseCodes.Error,
                    AttemptReasonCode = "EXCEPTION_OCCURRED"
                };
            }
        }

        public async Task<RsaAuthenticationResponse> VerifyAuthenticationAsync(
            string authnAttemptId,
            string passcode,
            string inResponseTo)
        {
            try
            {
                var request = new RsaVerifyRequest
                {
                    SubjectCredentials = new List<RsaSubjectCredential>
                    {
                        new RsaSubjectCredential
                        {
                            MethodId = "SECURID",
                            CollectedInputs = new List<RsaCollectedInput>
                            {
                                new RsaCollectedInput
                                {
                                    Name = "SECURID",
                                    Value = passcode
                                }
                            }
                        }
                    },
                    Context = new RsaContext
                    {
                        AuthnAttemptId = authnAttemptId,
                        MessageId = Guid.NewGuid().ToString(),
                        InResponseTo = inResponseTo
                    }
                };

                _logger.LogInformation("Verifying RSA authentication for attempt: {AttemptId}", authnAttemptId);

                var response = await _httpClient.PostAsJsonAsync("/mfa/v1_1/authn/verify", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("RSA verify failed with status {StatusCode}: {Content}",
                        response.StatusCode, responseContent);
                    
                    return new RsaAuthenticationResponse
                    {
                        AttemptResponseCode = RsaResponseCodes.Error,
                        AttemptReasonCode = $"HTTP_{response.StatusCode}"
                    };
                }

                var result = JsonSerializer.Deserialize<RsaAuthenticationResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("RSA verify response for attempt {AttemptId}: {ResponseCode}",
                    authnAttemptId, result?.AttemptResponseCode);

                return result ?? new RsaAuthenticationResponse
                {
                    AttemptResponseCode = RsaResponseCodes.Error,
                    AttemptReasonCode = "DESERIALIZATION_FAILED"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during RSA verify for attempt: {AttemptId}", authnAttemptId);
                return new RsaAuthenticationResponse
                {
                    AttemptResponseCode = RsaResponseCodes.Error,
                    AttemptReasonCode = "EXCEPTION_OCCURRED"
                };
            }
        }

        public async Task<RsaStatusResponse> CheckAuthenticationStatusAsync(string authnAttemptId)
        {
            try
            {
                var request = new RsaStatusRequest
                {
                    AuthnAttemptId = authnAttemptId,
                    RemoveAttemptId = false // Don't remove, just check
                };

                _logger.LogInformation("Checking RSA authentication status for attempt: {AttemptId}", authnAttemptId);

                var response = await _httpClient.PostAsJsonAsync("/mfa/v1_1/authn/status", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("RSA status check failed with status {StatusCode}: {Content}",
                        response.StatusCode, responseContent);
                    
                    return new RsaStatusResponse
                    {
                        AttemptResponseCode = RsaResponseCodes.Error,
                        AttemptReasonCode = $"HTTP_{response.StatusCode}"
                    };
                }

                var result = JsonSerializer.Deserialize<RsaStatusResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result ?? new RsaStatusResponse
                {
                    AttemptResponseCode = RsaResponseCodes.Error,
                    AttemptReasonCode = "DESERIALIZATION_FAILED"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during RSA status check for attempt: {AttemptId}", authnAttemptId);
                return new RsaStatusResponse
                {
                    AttemptResponseCode = RsaResponseCodes.Error,
                    AttemptReasonCode = "EXCEPTION_OCCURRED"
                };
            }
        }

        public async Task<bool> CancelAuthenticationAsync(string authnAttemptId, string reason = "USER_ACTION")
        {
            try
            {
                var request = new RsaCancelRequest
                {
                    AuthnAttemptId = authnAttemptId,
                    RemoveAttemptId = true,
                    Reason = reason
                };

                _logger.LogInformation("Cancelling RSA authentication for attempt: {AttemptId}", authnAttemptId);

                var response = await _httpClient.PostAsJsonAsync("/mfa/v1_1/authn/cancel", request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully cancelled RSA authentication attempt: {AttemptId}", authnAttemptId);
                    return true;
                }

                _logger.LogWarning("Failed to cancel RSA authentication attempt {AttemptId}: {StatusCode}",
                    authnAttemptId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during RSA cancel for attempt: {AttemptId}", authnAttemptId);
                return false;
            }
        }
    }
}