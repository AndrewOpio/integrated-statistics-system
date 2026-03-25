using System.Net;
using System.Text.Json;
using FluentValidation;
using NotificationService.Api.Models;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Api.Middleware;


/// <summary>
/// Catches all unhandled exceptions
/// Exception → HTTP Status mapping:
///   ValidationException (FluentValidation) → 400 Bad Request
///   DomainValidationException              → 400 Bad Request
///   NotSupportedException                  → 400 Bad Request
///   Everything else                        → 500 Internal Server Error
/// </summary>
public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (ValidationException ex)
        {
            // FluentValidation failure — triggered by SendNotificationRequestValidator
            logger.LogWarning("Validation failed: {Errors}",
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));

            await WriteErrorResponseAsync(
                ctx,
                HttpStatusCode.BadRequest,
                "One or more validation errors occurred.",
                ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (DomainValidationException ex)
        {
            // Domain rule violation — thrown by Notification, NotificationRecipient etc.
            logger.LogWarning("Domain validation failed: {Property} — {Message}",
                ex.PropertyName, ex.Message);

            await WriteErrorResponseAsync(
                ctx,
                HttpStatusCode.BadRequest,
                "A domain rule was violated.",
                [$"{ex.PropertyName}: {ex.Message}"]);
        }
        catch (NotSupportedException ex)
        {
            // for unsupported notification channel in dispatcher
            logger.LogWarning("Not supported: {Message}", ex.Message);

            await WriteErrorResponseAsync(
                ctx,
                HttpStatusCode.BadRequest,
                ex.Message,
                []);
        }
        catch (Exception ex)
        {
            // Unexpected error — log full details but never expose them to the caller
            logger.LogError(ex, "Unhandled exception occurred");

            await WriteErrorResponseAsync(
                ctx,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later.",
                []);
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext ctx,
        HttpStatusCode statusCode,
        string message,
        IEnumerable<string> errors)
    {
        ctx.Response.StatusCode = (int)statusCode;
        ctx.Response.ContentType = "application/json";

        var response = new ValidationErrorResponseDto(message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await ctx.Response.WriteAsync(json);
    }
}

