using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class PortfolioMemberConfiguration : IEntityTypeConfiguration<PortfolioMember>
    {
        public void Configure(EntityTypeBuilder<PortfolioMember> builder)
        {
            builder.ToTable("PortfolioMembers");
            builder.HasKey(m => new { m.PortfolioId, m.UserId });

            builder.HasOne(m => m.Portfolio)
                .WithMany(p => p.Members)
                .HasForeignKey(m => m.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => m.UserId);
        }
    }
}
