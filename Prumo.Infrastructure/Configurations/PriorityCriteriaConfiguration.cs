using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class PriorityCriteriaConfiguration : ConfiguracaoBase<PriorityCriteria>
    {
        public override void Configure(EntityTypeBuilder<PriorityCriteria> builder)
        {
            base.Configure(builder);

            builder.ToTable("PriorityCriteria");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(p => p.ValueWeight).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(p => p.EffortWeight).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(p => p.RiskWeight).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(p => p.AlignmentWeight).HasColumnType("decimal(18,2)").IsRequired();

            builder.Property(p => p.UpdatedAt).IsRequired();
        }
    }
}
