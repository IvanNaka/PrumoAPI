using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class PortfolioObjectiveConfiguration : IEntityTypeConfiguration<PortfolioObjective>
    {
        public void Configure(EntityTypeBuilder<PortfolioObjective> builder)
        {
            builder.ToTable("PortfolioObjectives");
            builder.HasKey(po => new { po.PortfolioId, po.ObjectiveId });

            builder.HasOne(po => po.Portfolio)
                .WithMany(p => p.Objectives)
                .HasForeignKey(po => po.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(po => po.Objective)
                .WithMany()
                .HasForeignKey(po => po.ObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(po => po.ObjectiveId);
        }
    }
}
