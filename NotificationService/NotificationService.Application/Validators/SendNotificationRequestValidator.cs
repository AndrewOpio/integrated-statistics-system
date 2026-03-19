using FluentValidation;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Validators;


/// <summary>
/// Validates SendNotificationRequestDto at the Application boundary.
/// This same validator is reused by:
///   1. The API controller (via middleware) for HTTP requests
///   2. The RabbitMQ consumer for incoming messages — preventing poison messages
///
/// Having one validator in one place means business rules are never duplicated between the HTTP and messaging boundaries.
/// </summary>
public class SendNotificationRequestValidator : AbstractValidator<SendNotificationRequestDto>
{
    public SendNotificationRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MaximumLength(4000).WithMessage("Body cannot exceed 4000 characters.");

        RuleFor(x => x.NotificationTypeId)
            .GreaterThan(0).WithMessage("A valid notification type must be provided.");

        RuleFor(x => x.Channel)
            .Equal(NotificationChannel.Email)
            .WithMessage("Only the Email channel is currently supported.");

        RuleFor(x => x.Recipients)
            .NotEmpty().WithMessage("At least one recipient is required.");

        RuleForEach(x => x.Recipients).ChildRules(recipient =>
        {
            recipient.RuleFor(r => r.UserId)
                .NotEmpty().WithMessage("Recipient user ID is required.");

            recipient.RuleFor(r => r.Email)
                .NotEmpty().WithMessage("Recipient email is required.")
                .EmailAddress().WithMessage("Recipient email is not a valid email address.");

            recipient.RuleFor(r => r.DisplayName)
                .NotEmpty().WithMessage("Recipient display name is required.");
        });

        // If TemplateKey is provided, TemplateData must also be provided and not empty.
        // You cannot reference a template without supplying its variables.
        When(x => !string.IsNullOrWhiteSpace(x.TemplateKey), () =>
        {
            RuleFor(x => x.TemplateData)
                .NotNull().WithMessage("TemplateData is required when a TemplateKey is provided.")
                .Must(d => d != null && d.Count > 0)
                .WithMessage("TemplateData cannot be empty when a TemplateKey is provided.");
        });


        // After
        When(x => x.ScheduledAt.HasValue && x.ScheduledAt.Value > DateTimeOffset.MinValue, () =>
        {
            RuleFor(x => x.ScheduledAt!.Value)
        .GreaterThan(_ => DateTimeOffset.UtcNow.AddMinutes(1))
        .WithMessage("ScheduledAt must be at least 1 minute in the future.");
        });
    }
}
