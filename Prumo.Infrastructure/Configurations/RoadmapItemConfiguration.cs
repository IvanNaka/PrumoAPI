using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class RoadmapItemConfiguration : ConfiguracaoBase<RoadmapItem>
    {
        public override void Configure(EntityTypeBuilder<RoadmapItem> builder)
        {
            base.Configure(builder);

            builder.ToTable("RoadmapItems");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(r => r.PortfolioId).HasColumnType("uuid").IsRequired();
            builder.Property(r => r.ProjectId).HasColumnType("uuid").IsRequired(false);

            builder.Property(r => r.Title).HasMaxLength(300).IsRequired();
            builder.Property(r => r.Description).HasMaxLength(2000).IsRequired(false);

            builder.Property(r => r.StartDate).IsRequired();
            builder.Property(r => r.EndDate).IsRequired();
            builder.Property(r => r.Status).HasConversion<string>().IsRequired();
            builder.Property(r => r.Order).IsRequired();

            builder.HasIndex(r => r.PortfolioId);
            builder.HasIndex(r => r.ProjectId);

            builder.HasOne(r => r.Portfolio)
                .WithMany(p => p.RoadmapItems)
                .HasForeignKey(r => r.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
