using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Client
{
    public static class NotificationServiceClientExtensions
    {
        /// <summary>
        /// Registers the HTTP notifications.
        /// Use when your service needs a response back.
        /// </summary>
        public static IServiceCollection AddNotificationServiceClient(
        this IServiceCollection services, string baseUrl)
        {
            services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            });
            return services;
        }

        /// <summary>
        /// Registers the RabbitMQ publisher for fire and forget notifications.
        /// Use when your service does not need a response back.
        /// </summary>
        public static IServiceCollection AddNotificationEventPublisher(
            this IServiceCollection services,
            string host,
            string username,
            string password)
        {
            services.AddMassTransit(mt =>
            {
                mt.UsingRabbitMq((ctx, rmq) =>
                {
                    rmq.Host(host, "/", h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    rmq.ConfigureEndpoints(ctx);
                });
            });

            services.AddScoped<INotificationEventPublisher, NotificationEventPublisher>();

            return services;
        }
    }
}
