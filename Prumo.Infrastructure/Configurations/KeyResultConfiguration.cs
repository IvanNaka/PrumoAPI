using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class KeyResultConfiguration : ConfiguracaoBase<KeyResult>
    {
        public override void Configure(EntityTypeBuilder<KeyResult> builder)
        {
            base.Configure(builder);

            builder.ToTable("KeyResults");

            builder.HasKey(k => k.Id);
            builder.Property(k => k.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(k => k.ObjectiveId).HasColumnType("uuid").IsRequired();
            builder.Property(k => k.Title).HasMaxLength(500).IsRequired();
            builder.Property(k => k.TargetValue).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(k => k.CurrentValue).HasColumnType("decimal(18,2)").IsRequired();

            builder.HasIndex(k => k.ObjectiveId);
        }
    }
}
