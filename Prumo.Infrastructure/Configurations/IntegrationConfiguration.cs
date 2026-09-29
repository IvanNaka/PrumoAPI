using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class IntegrationConfiguration : ConfiguracaoBase<Integration>
    {
        public override void Configure(EntityTypeBuilder<Integration> builder)
        {
            base.Configure(builder);

            builder.ToTable("Integrations");

            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(i => i.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(i => i.ApiUrl).HasMaxLength(1000).IsRequired();
            builder.Property(i => i.Email).HasMaxLength(200).IsRequired();
            builder.Property(i => i.Token).HasMaxLength(4000).IsRequired();
            builder.Property(i => i.IsActive).IsRequired();
            builder.Property(i => i.SyncIntervalMinutes).IsRequired().HasDefaultValue(60);
            builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(i => i.LastSyncedAt).IsRequired(false);
            builder.Property(i => i.FailedAttempts).IsRequired();
            builder.Property(i => i.NextAttemptAt).IsRequired(false);

            builder.HasIndex(i => i.Type).IsUnique();

            builder.HasMany(i => i.Logs)
                .WithOne(l => l.Integration)
                .HasForeignKey(l => l.IntegrationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class IntegrationSyncLogConfiguration : IEntityTypeConfiguration<IntegrationSyncLog>
    {
        public void Configure(EntityTypeBuilder<IntegrationSyncLog> builder)
        {
            builder.ToTable("IntegrationSyncLogs");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.ErrorMessage).HasMaxLength(2000);
            builder.HasIndex(l => new { l.IntegrationId, l.StartedAt });
        }
    }

    public class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
    {
        public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
        {
            builder.ToTable("DataProtectionKeys");
            builder.HasKey(k => k.Id);
            builder.Property(k => k.FriendlyName).HasMaxLength(200);
            builder.Property(k => k.Xml).IsRequired();
        }
    }
}
