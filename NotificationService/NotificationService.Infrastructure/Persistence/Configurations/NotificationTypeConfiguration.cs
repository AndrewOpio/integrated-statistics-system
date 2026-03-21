using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public class NotificationTypeConfiguration : IEntityTypeConfiguration<NotificationType>
{
    public void Configure(EntityTypeBuilder<NotificationType> b)
    {
        b.ToTable("NotificationTypes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);

        // Seed the two initial types.
        b.HasData(
            new { Id = 1, Name = "Transactional", Description = "Triggered by a user action e.g. approval request, password reset" },
            new { Id = 2, Name = "System", Description = "Internal alerts e.g. job failures, health warnings" }
        );
    }
}
