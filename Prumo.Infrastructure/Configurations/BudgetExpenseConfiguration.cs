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

            builder.Property(e => e.ProjectId).HasColumnType("uuid").IsRequired();
            builder.Property(e => e.Description).HasMaxLength(300).IsRequired();
            builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(e => e.Date).HasColumnType("date").IsRequired();

            builder.HasIndex(e => e.ProjectId);

            builder.HasOne(e => e.Project)
                .WithMany(p => p.Expenses)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
