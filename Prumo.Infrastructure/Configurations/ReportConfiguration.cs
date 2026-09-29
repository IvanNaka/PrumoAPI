using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.ToTable("Reports");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasColumnType("uuid").ValueGeneratedNever();
            builder.Property(r => r.Name).HasMaxLength(300).IsRequired();
            builder.Property(r => r.Type).HasMaxLength(20).IsRequired();
            builder.Property(r => r.Format).HasMaxLength(10).IsRequired();
            builder.Property(r => r.GeneratedAt).IsRequired();

            builder.HasOne(r => r.Portfolio)
                .WithMany()
                .HasForeignKey(r => r.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.GeneratedBy)
                .WithMany()
                .HasForeignKey(r => r.GeneratedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new { r.PortfolioId, r.GeneratedAt });
        }
    }
}
