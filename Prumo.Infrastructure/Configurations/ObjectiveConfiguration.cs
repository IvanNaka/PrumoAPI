using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ObjectiveConfiguration : ConfiguracaoBase<Objective>
    {
        public override void Configure(EntityTypeBuilder<Objective> builder)
        {
            base.Configure(builder);

            builder.ToTable("Objectives");

            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(o => o.Title).HasMaxLength(200).IsRequired();
            builder.Property(o => o.Description).HasMaxLength(1000).IsRequired(false);
            builder.Property(o => o.StartDate).HasColumnType("date").IsRequired(false);
            builder.Property(o => o.EndDate).HasColumnType("date").IsRequired(false);

            builder.HasMany(o => o.KeyResults)
                .WithOne(k => k.Objective)
                .HasForeignKey(k => k.ObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
