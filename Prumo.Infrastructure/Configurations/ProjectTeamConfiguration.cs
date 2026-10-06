using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ProjectTeamConfiguration : ConfiguracaoBase<ProjectTeam>
    {
        public override void Configure(EntityTypeBuilder<ProjectTeam> builder)
        {
            base.Configure(builder);

            builder.ToTable("ProjectTeams");

            builder.HasKey(pt => new { pt.ProjectId, pt.TeamId });

            builder.Property(pt => pt.ProjectId).HasColumnType("uuid").IsRequired();
            builder.Property(pt => pt.TeamId).HasColumnType("uuid").IsRequired();

            builder.HasIndex(pt => pt.TeamId);

            builder.HasOne(pt => pt.Project)
                .WithMany(p => p.ProjectTeams)
                .HasForeignKey(pt => pt.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pt => pt.Team)
                .WithMany(t => t.Projects)
                .HasForeignKey(pt => pt.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
