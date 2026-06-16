using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class PriorityCriteriaConfiguration : ConfiguracaoBase<PriorityCriteria>
    {
        public override void Configure(EntityTypeBuilder<PriorityCriteria> builder)
        {
            base.Configure(builder);

            builder.ToTable("PriorityCriteria");

            builder.HasKey(pc => pc.Id);
            builder.Property(pc => pc.Id)
                .HasColumnType("uuid")
                .ValueGeneratedOnAdd();

            builder.Property(pc => pc.Name)
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(pc => pc.ValueWeight)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(pc => pc.PortfolioId)
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(pc => pc.UserId)
                .HasColumnType("uuid")
                .IsRequired();

            builder.HasIndex(pc => pc.PortfolioId);
            builder.HasIndex(pc => pc.UserId);

            // EXPLICIT: single relationship mapped to PortfolioId
            builder.HasOne(pc => pc.Portfolio)
                   .WithMany(p => p.PriorityCriterias)
                   .HasForeignKey(pc => pc.PortfolioId)
                   .OnDelete(DeleteBehavior.NoAction)
                   .IsRequired();

            // User relationship
            builder.HasOne(pc => pc.User)
                   .WithMany()
                   .HasForeignKey(pc => pc.UserId)
                   .OnDelete(DeleteBehavior.Cascade)
                   .IsRequired();
        }
    }
}