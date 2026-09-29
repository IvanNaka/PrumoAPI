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

            builder.Property(i => i.Type).HasConversion<string>().IsRequired();
            builder.Property(i => i.ApiUrl).HasMaxLength(1000).IsRequired();
            builder.Property(i => i.Token).HasMaxLength(1000).IsRequired(false);
            builder.Property(i => i.IsActive).IsRequired();
            builder.Property(i => i.SyncIntervalMinutes).IsRequired().HasDefaultValue(60);
            builder.Property(i => i.LastSyncedAt).IsRequired(false);
            builder.Property(i => i.LastSyncStatus).HasMaxLength(100).IsRequired(false);
        }
    }
}
