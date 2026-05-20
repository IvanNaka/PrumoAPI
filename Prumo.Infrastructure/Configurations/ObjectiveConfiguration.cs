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

            builder.Property(o => o.Title).HasMaxLength(500).IsRequired();
            builder.Property(o => o.Description).HasMaxLength(2000).IsRequired(false);
            builder.Property(o => o.StartDate).IsRequired();
            builder.Property(o => o.EndDate).IsRequired();
        }
    }
}
