using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ProjectObjectiveConfiguration : ConfiguracaoBase<ProjectObjective>
    {
        public override void Configure(EntityTypeBuilder<ProjectObjective> builder)
        {
            base.Configure(builder);

            builder.ToTable("ProjectObjectives");

            builder.HasKey(po => new { po.ProjectId, po.ObjectiveId });

            builder.Property(po => po.ProjectId).HasColumnType("uuid").IsRequired();
            builder.Property(po => po.ObjectiveId).HasColumnType("uuid").IsRequired();

            builder.HasIndex(po => po.ProjectId);
            builder.HasIndex(po => po.ObjectiveId);
        }
    }
}
