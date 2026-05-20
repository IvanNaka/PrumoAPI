using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class IntegrationConfiguration : ConfiguracaoBase<Integration>
    {
        public override void Configure(EntityTypeBuilder<Integration> builder)
        {
            base.Configure(builder);

            builder.ToTable("Integrations");

            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(i => i.Type).IsRequired();
            builder.Property(i => i.ApiUrl).HasMaxLength(1000).IsRequired();
            builder.Property(i => i.Token).HasMaxLength(1000).IsRequired(false);
            builder.Property(i => i.IsActive).IsRequired();
        }
    }
}
