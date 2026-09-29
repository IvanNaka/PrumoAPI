using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class UserConfiguration : ConfiguracaoBase<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);

            builder.ToTable("Users");

            builder.HasKey(prop => prop.Id);
            builder.Property(prop => prop.Id)
                .HasColumnType("uuid")
                .ValueGeneratedOnAdd();

            builder.Property(prop => prop.Email)
                .HasMaxLength(200)
                .IsRequired();
            builder.Property(prop => prop.Name)
                .HasMaxLength(150)
                .IsRequired();
            builder.Property(prop => prop.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // O e-mail é sempre gravado em minúsculas, então o índice único basta.
            builder.HasIndex(prop => prop.Email).IsUnique();

            builder.HasMany(u => u.Roles)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
