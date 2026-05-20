using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public abstract class ConfiguracaoBase<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.Property(prop => prop.CreatedDate)
                .IsRequired();

            builder.Property(prop => prop.UpdatedDate)
                .IsRequired(false);

            builder.Property(prop => prop.Active)
                .IsRequired();
        }
    }
}