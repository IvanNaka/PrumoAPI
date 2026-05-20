using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ExternalDataConfiguration : ConfiguracaoBase<ExternalData>
    {
        public override void Configure(EntityTypeBuilder<ExternalData> builder)
        {
            base.Configure(builder);

            builder.ToTable("ExternalData");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(e => e.IntegrationId).HasColumnType("uuid").IsRequired();
            builder.Property(e => e.ExternalId).HasMaxLength(200).IsRequired();
            builder.Property(e => e.RawDataJson).IsRequired();

            builder.Property(e => e.ImportedAt).IsRequired();

            builder.HasIndex(e => e.IntegrationId);
        }
    }
}
