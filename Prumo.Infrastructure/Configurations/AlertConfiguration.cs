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
            builder.Property(a => a.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(a => a.ProjectId).HasColumnType("uuid").IsRequired();
            builder.Property(a => a.Message).HasMaxLength(1000).IsRequired();
            builder.Property(a => a.Type).IsRequired();

            builder.HasIndex(a => a.ProjectId);
        }
    }
}
