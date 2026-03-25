using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services;
using FluentValidation;

namespace NotificationService.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scans this assembly and registers all AbstractValidator<T> implementations.
        // This picks up SendNotificationRequestValidator automatically.
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

        services.AddScoped<INotificationAppService, NotificationAppService>();

        return services;
    }
}