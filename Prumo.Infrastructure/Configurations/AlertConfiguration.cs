using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class AlertConfiguration : ConfiguracaoBase<Alert>
    {
        public override void Configure(EntityTypeBuilder<Alert> builder)
        {
            base.Configure(builder);

            builder.ToTable("Alerts");

            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasColumnType("uuid").ValueGeneratedNever();

            builder.Property(a => a.Message).HasMaxLength(500).IsRequired();
            builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(a => a.EntityType).HasMaxLength(50);

            builder.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => new { a.UserId, a.Status });
            builder.HasIndex(a => new { a.UserId, a.Type, a.EntityId });
        }
    }
}
