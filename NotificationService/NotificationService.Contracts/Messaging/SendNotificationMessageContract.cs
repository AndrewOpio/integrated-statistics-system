namespace NotificationService.Contracts.Messaging;

/// <summary>
/// The RabbitMQ message contract other microservices publish to request a notification be sent.
///
/// Public API of this microservice over the message bus. 
/// </summary>
public class SendNotificationMessageContract
{
    public SendNotificationMessageContract() { }
    public string Title { get; init; } = default!;
    public string Body { get; init; } = default!;
    public int NotificationTypeId { get; init; }
    public List<RecipientMessageContract> Recipients { get; init; } = [];
    public string? TemplateKey { get; init; }
    public Dictionary<string, string>? TemplateData { get; init; }
    public DateTimeOffset? ScheduledAt { get; init; }
}
