namespace NotificationService.Api.Models;

/// <summary>
/// Standardised error response for validation failures (HTTP 400).
/// Returned by the global exception middleware for both FluentValidation.ValidationException and DomainValidationException.
/// </summary>
public record ValidationErrorResponseDto(
    string Message,
    IEnumerable<string> Errors);