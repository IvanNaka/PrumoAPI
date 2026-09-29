using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class BusinessCaseConfiguration : ConfiguracaoBase<BusinessCase>
    {
        public override void Configure(EntityTypeBuilder<BusinessCase> builder)
        {
            base.Configure(builder);
            builder.ToTable("BusinessCases");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.InitialInvestment).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(b => b.AnnualDiscountRate).HasColumnType("decimal(5,2)").IsRequired();

            // 1 business case por projeto.
            builder.HasIndex(b => b.ProjectId).IsUnique();
            builder.HasOne(b => b.Project)
                .WithOne(p => p.BusinessCase)
                .HasForeignKey<BusinessCase>(b => b.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(b => b.Flows)
                .WithOne(f => f.BusinessCase)
                .HasForeignKey(f => f.BusinessCaseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class CashFlowForecastConfiguration : IEntityTypeConfiguration<CashFlowForecast>
    {
        public void Configure(EntityTypeBuilder<CashFlowForecast> builder)
        {
            builder.ToTable("CashFlowForecasts");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Value).HasColumnType("decimal(18,2)").IsRequired();
            builder.HasIndex(f => new { f.BusinessCaseId, f.Month }).IsUnique();
        }
    }

    public class RealizedReturnConfiguration : ConfiguracaoBase<RealizedReturn>
    {
        public override void Configure(EntityTypeBuilder<RealizedReturn> builder)
        {
            base.Configure(builder);
            builder.ToTable("RealizedReturns");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Date).HasColumnType("date").IsRequired();
            builder.Property(r => r.Value).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(r => r.Description).HasMaxLength(300);
            builder.HasIndex(r => r.ProjectId);
            builder.HasOne(r => r.Project)
                .WithMany(p => p.RealizedReturns)
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
