using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ProjectMemberConfiguration : ConfiguracaoBase<ProjectMember>
    {
        public override void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
            base.Configure(builder);

            builder.ToTable("ProjectMembers");

            builder.HasKey(pm => new { pm.ProjectId, pm.UserId });

            builder.Property(pm => pm.ProjectId).HasColumnType("uuid").IsRequired();
            builder.Property(pm => pm.UserId).HasColumnType("uuid").IsRequired();

            builder.HasIndex(pm => pm.ProjectId);
            builder.HasIndex(pm => pm.UserId);
        }
    }
}
