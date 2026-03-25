using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.TemplateKey).HasMaxLength(100);
        b.Property(x => x.FailureReason).HasMaxLength(1000);

        // See NotificationStatus and NotificationChannel enum comments.
        b.Property(x => x.Channel).IsRequired();
        b.Property(x => x.Status).IsRequired();

        // TemplateData stored as a JSON column 
        b.Property(x => x.TemplateData)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v,
                    (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v,
                    (System.Text.Json.JsonSerializerOptions?)null)
                    ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<Dictionary<string, string>>(
                    (c1, c2) => c1 != null && c2 != null &&
                                c1.Count == c2.Count &&
                                c1.All(kv => c2.ContainsKey(kv.Key) && c2[kv.Key] == kv.Value),
                    c => c.Aggregate(0, (a, v) =>
                        HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                    c => c.ToDictionary(kv => kv.Key, kv => kv.Value)));

        // Foreign key to NotificationTypes lookup table
        b.HasOne(x => x.Type)
         .WithMany()
         .HasForeignKey(x => x.NotificationTypeId)
         .OnDelete(DeleteBehavior.Restrict);

        // Recipients are owned by the Notification aggregate
        b.HasMany(x => x.Recipients)
         .WithOne()
         .HasForeignKey(r => r.NotificationId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Ignore(x => x.DomainEvents);

        // Indexes for the background job queries
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.Channel);
        b.HasIndex(x => x.ScheduledAt);
        b.HasIndex(x => new { x.Status, x.Channel });
    }
}
