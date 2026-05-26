using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ProjectConfiguration : ConfiguracaoBase<Project>
    {
        public override void Configure(EntityTypeBuilder<Project> builder)
        {
            base.Configure(builder);

            builder.ToTable("Projects");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            // Portfolio FK
            builder.Property(p => p.PortfolioId)
                .HasColumnType("uuid")
                .IsRequired();

            // Owner FK
            builder.Property(p => p.OwnerId).HasColumnType("uuid").IsRequired();

            builder.Property(p => p.Name).HasMaxLength(300).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(2000).IsRequired(false);
            builder.Property(p => p.Status).HasConversion<string>().IsRequired();

            builder.HasIndex(p => p.PortfolioId);
            builder.HasIndex(p => p.OwnerId);

            // Relationships
            builder.HasOne(p => p.Portfolio)
                .WithMany(port => port.Projects)
                .HasForeignKey(p => p.PortfolioId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(p => p.Owner)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}