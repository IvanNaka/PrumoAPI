using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class BudgetExpenseConfiguration : ConfiguracaoBase<BudgetExpense>
    {
        public override void Configure(EntityTypeBuilder<BudgetExpense> builder)
        {
            base.Configure(builder);

            builder.ToTable("BudgetExpenses");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(e => e.BudgetId).HasColumnType("uuid").IsRequired();

            builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Category).HasConversion<string>().IsRequired();
            builder.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(e => e.Date).IsRequired();

            builder.HasIndex(e => e.BudgetId);

            builder.HasOne(e => e.Budget)
                .WithMany(b => b.Expenses)
                .HasForeignKey(e => e.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
