using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class TeamUserConfiguration : ConfiguracaoBase<TeamUser>
    {
        public override void Configure(EntityTypeBuilder<TeamUser> builder)
        {
            base.Configure(builder);

            builder.ToTable("TeamUsers");

            // Use the typed composite key (no shadow properties)
            builder.HasKey(tu => new { tu.TeamId, tu.UserId });

            builder.Property(tu => tu.TeamId)
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(tu => tu.UserId)
                .HasColumnType("uuid")
                .IsRequired();

            // Relationships
            builder.HasOne(tu => tu.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(tu => tu.TeamId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(tu => tu.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tu => tu.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
