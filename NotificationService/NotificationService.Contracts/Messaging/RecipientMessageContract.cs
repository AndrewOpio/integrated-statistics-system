namespace NotificationService.Contracts.Messaging;

public class RecipientMessageContract
{
    public RecipientMessageContract() { }
    public string UserId { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
}
