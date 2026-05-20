using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;
using Prumo.Infrastructure.Configurations;

namespace Prumo.Infrastructure.Configurations
{
    public class UserConfiguration : ConfiguracaoBase<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);

            builder.ToTable("Users");

            // Primary Key
            builder.HasKey(prop => prop.Id);
            builder.Property(prop => prop.Id)
                .HasColumnType("uuid")
                .ValueGeneratedOnAdd();

            // Foreign Key
            builder.Property(prop => prop.RoleId)
                .HasColumnType("uuid");

            // String Properties
            builder.Property(prop => prop.Email)
                .HasMaxLength(200)
                .IsRequired();
            builder.Property(prop => prop.Name)
                .HasMaxLength(200)
                .IsRequired();

            // Index for better query performance
            builder.HasIndex(prop => prop.Email);
            builder.HasIndex(prop => prop.RoleId);

            // Relationships
            builder.HasOne(a => a.Role)
                .WithMany(p => p.Users)
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
