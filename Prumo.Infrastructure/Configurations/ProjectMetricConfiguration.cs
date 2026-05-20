using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ProjectMetricConfiguration : ConfiguracaoBase<ProjectMetric>
    {
        public override void Configure(EntityTypeBuilder<ProjectMetric> builder)
        {
            base.Configure(builder);

            builder.ToTable("ProjectMetrics");

            builder.HasKey(pm => pm.Id);
            builder.Property(pm => pm.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(pm => pm.ProjectId).HasColumnType("uuid").IsRequired();
            builder.Property(pm => pm.Value).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(pm => pm.Effort).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(pm => pm.Risk).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(pm => pm.CollectedAt).IsRequired();

            builder.HasIndex(pm => pm.ProjectId);
        }
    }
}
