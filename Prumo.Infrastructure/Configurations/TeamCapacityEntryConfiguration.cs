using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class TeamCapacityEntryConfiguration : ConfiguracaoBase<TeamCapacityEntry>
    {
        public override void Configure(EntityTypeBuilder<TeamCapacityEntry> builder)
        {
            base.Configure(builder);

            builder.ToTable("TeamCapacityEntries");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(c => c.TeamId).HasColumnType("uuid").IsRequired();
            builder.Property(c => c.UserId).HasColumnType("uuid").IsRequired();

            builder.Property(c => c.Year).IsRequired();
            builder.Property(c => c.Month).IsRequired();

            builder.Property(c => c.AvailableHours).HasColumnType("numeric(9,2)").IsRequired();
            builder.Property(c => c.AllocatedHours).HasColumnType("numeric(9,2)").IsRequired();
            builder.Property(c => c.OccupancyPercent).IsRequired();

            builder.HasIndex(c => new { c.TeamId, c.UserId, c.Year, c.Month }).IsUnique();

            builder.HasOne(c => c.Team)
                .WithMany(t => t.CapacityEntries)
                .HasForeignKey(c => c.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
