using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class TeamConfiguration : ConfiguracaoBase<Team>
    {
        public override void Configure(EntityTypeBuilder<Team> builder)
        {
            base.Configure(builder);

            builder.ToTable("Teams");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
            builder.Property(t => t.OwnerUserId).HasColumnType("uuid").IsRequired(false);

            builder.HasOne(t => t.OwnerUser)
                .WithMany()
                .HasForeignKey(t => t.OwnerUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
