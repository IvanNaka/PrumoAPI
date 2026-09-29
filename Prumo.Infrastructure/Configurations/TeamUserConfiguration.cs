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

            builder.HasKey(tu => tu.Id);
            builder.Property(tu => tu.Id).HasColumnType("uuid");

            builder.Property(tu => tu.Name).HasMaxLength(150).IsRequired();
            builder.Property(tu => tu.Email).HasMaxLength(200).IsRequired();
            builder.Property(tu => tu.HourlyCost).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(tu => tu.MonthlyCapacityHours).IsRequired();

            builder.HasIndex(tu => tu.TeamId);
            builder.HasIndex(tu => tu.Email);

            builder.HasOne(tu => tu.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(tu => tu.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tu => tu.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tu => tu.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
