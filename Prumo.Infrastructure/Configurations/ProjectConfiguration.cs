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

            builder.Property(p => p.PortfolioId).HasColumnType("uuid").IsRequired();
            builder.Property(p => p.OwnerId).HasColumnType("uuid").IsRequired();

            builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(2000).IsRequired(false);
            builder.Property(p => p.StartDate).HasColumnType("date").IsRequired();
            builder.Property(p => p.EndDate).HasColumnType("date").IsRequired();
            builder.Property(p => p.ApprovedBudget).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(p => p.StrategicCategory).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(p => p.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(p => p.EvaluationStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(p => p.CurrentScore).HasColumnType("numeric(5,2)");
            builder.Property(p => p.JiraProjectKey).HasMaxLength(50).IsRequired(false);

            builder.HasIndex(p => p.PortfolioId);
            builder.HasIndex(p => p.OwnerId);

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
