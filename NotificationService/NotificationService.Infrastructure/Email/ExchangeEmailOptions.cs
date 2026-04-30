using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Infrastructure.Email;


/// <summary>
/// Bound from appsettings.json "ExchangeEmail" section.
/// </summary>
public class ExchangeEmailOptions
{
    public const string Section = "ExchangeEmail";
    public string TenantId { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string SenderEmail { get; set; } = default!;
}
