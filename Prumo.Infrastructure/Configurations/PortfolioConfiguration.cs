using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class PortfolioConfiguration : ConfiguracaoBase<Portfolio>
    {
        public override void Configure(EntityTypeBuilder<Portfolio> builder)
        {
            base.Configure(builder);

            builder.ToTable("Portfolios");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .HasColumnType("uuid")
                .ValueGeneratedOnAdd();

            builder.Property(p => p.Name)
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(p => p.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(p => p.OwnerId)
                .HasColumnType("uuid")
                .IsRequired();

            builder.HasIndex(p => p.OwnerId);

            builder.HasOne(p => p.Owner)
                .WithMany(u => u.Portfolios)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}