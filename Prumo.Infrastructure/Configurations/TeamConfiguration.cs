using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class TeamConfiguration : ConfiguracaoBase<Team>
    {
        public override void Configure(EntityTypeBuilder<Team> builder)
        {
            base.Configure(builder);

            builder.ToTable("Teams");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
            builder.HasIndex(t => t.Name).IsUnique();

            builder.Property(t => t.PortfolioId).HasColumnType("uuid").IsRequired(false);
            builder.HasOne(t => t.Portfolio)
                .WithMany(p => p.Teams)
                .HasForeignKey(t => t.PortfolioId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(t => t.OwnerUserId).HasColumnType("uuid").IsRequired(false);
            builder.HasOne(t => t.OwnerUser)
                .WithMany()
                .HasForeignKey(t => t.OwnerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
