using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class BudgetConfiguration : ConfiguracaoBase<Budget>
    {
        public override void Configure(EntityTypeBuilder<Budget> builder)
        {
            base.Configure(builder);

            builder.ToTable("Budgets");

            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(b => b.ProjectId).HasColumnType("uuid").IsRequired();
            builder.Property(b => b.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(b => b.Currency).HasMaxLength(10).IsRequired();

            // 1:1 com Project
            builder.HasIndex(b => b.ProjectId).IsUnique();

            builder.HasOne(b => b.Project)
                .WithOne(p => p.Budget)
                .HasForeignKey<Budget>(b => b.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
