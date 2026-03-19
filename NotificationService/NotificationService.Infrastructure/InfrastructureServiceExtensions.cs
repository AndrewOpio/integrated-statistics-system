using Hangfire;
using Hangfire.SqlServer;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Email;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Messaging.Consumers;
using NotificationService.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Infrastructure;


public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ── EF Core ──────────────────────────────────────────────────
        services.AddDbContext<NotificationDbContext>(opts =>
            opts.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(
                    typeof(InfrastructureServiceExtensions).Assembly.FullName)));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Email ─────────────────────────────────────────────────────
        services.Configure<ExchangeEmailOptions>(
            config.GetSection(ExchangeEmailOptions.Section));
        services.AddScoped<IEmailSender, ExchangeEmailSender>();

        // ── Dispatcher ────────────────────────────────────────────────
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        // ── Hangfire ──────────────────────────────────────────────────
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                config.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

        services.AddHangfireServer(opts =>
        {
            opts.WorkerCount = 5;

            opts.Queues = ["default"];
        });

        // ── MassTransit / RabbitMQ ────────────────────────────────────
        services.AddMassTransit(mt =>
        {
            mt.AddConsumer<SendNotificationConsumer>();

            mt.UsingRabbitMq((ctx, configurator) =>
            {
                configurator.Host(
                    config["RabbitMQ:Host"],
                    config["RabbitMQ:VirtualHost"],
                    h =>
                    {
                        h.Username(config["RabbitMQ:Username"]!);
                        h.Password(config["RabbitMQ:Password"]!);
                    });

                configurator.ReceiveEndpoint("notification-service-queue", ep =>
                {
                    // Retry — 3 attempts with 5 second intervals.
                    ep.UseMessageRetry(r =>
                        r.Interval(3, TimeSpan.FromSeconds(5)));

                    ep.ConfigureConsumer<SendNotificationConsumer>(ctx);
                });
            });
        });

        return services;
    }
}