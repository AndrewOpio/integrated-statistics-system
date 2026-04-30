using Microsoft.OpenApi;

namespace NotificationService.Api.Extensions;


public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Notification Service API",
                Version = "v1",
                Description = "Handles email notification delivery for the platform. " +
                              "Other microservices can trigger notifications via the " +
                              "RabbitMQ SendNotificationMessageContract or this HTTP API."
            });

            // Include XML comments in Swagger UI (controller summaries show up)
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);
        });

        return services;
    }
}
