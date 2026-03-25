using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Email;


/// <summary>
/// Sends emails via Microsoft Graph API (Exchange Online). Requires an Entra ID app registration with Mail.
/// </summary>
public class ExchangeEmailSender(
    IOptions<ExchangeEmailOptions> opts,
    ILogger<ExchangeEmailSender> logger) : IEmailSender
{
    private readonly ExchangeEmailOptions _opts = opts.Value;

    public async Task SendAsync(EmailMessageDto message, CancellationToken ct = default)
    {
        var credential = new ClientSecretCredential(
            _opts.TenantId,
            _opts.ClientId,
            _opts.ClientSecret);

        var graphClient = new GraphServiceClient(
            credential, ["https://graph.microsoft.com/.default"]);

        var mail = new SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = message.Subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = message.HtmlBody
                },
                ToRecipients =
                [
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = message.ToEmail,
                            Name    = message.ToName
                        }
                    }
                ]
            },
            SaveToSentItems = false
        };

        await graphClient
            .Users[_opts.SenderEmail]
            .SendMail
            .PostAsync(mail, cancellationToken: ct);

        logger.LogInformation(
            "Email sent to {Email} via Exchange Online", message.ToEmail);
    }
}
