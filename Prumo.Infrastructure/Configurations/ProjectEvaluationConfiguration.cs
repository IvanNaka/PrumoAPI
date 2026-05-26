using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ProjectEvaluationConfiguration : ConfiguracaoBase<ProjectEvaluation>
    {
        public override void Configure(EntityTypeBuilder<ProjectEvaluation> builder)
        {
            base.Configure(builder);

            builder.ToTable("ProjectEvaluation");

            builder.HasKey(pe => pe.Id);
            builder.Property(pe => pe.Id).HasColumnType("uuid").ValueGeneratedOnAdd();

            builder.Property(pe => pe.PriorityCriteriaId).HasColumnType("uuid").IsRequired();
            builder.Property(pe => pe.Value).HasColumnType("numeric").IsRequired();

            builder.HasIndex(pe => pe.PriorityCriteriaId);
            builder.HasIndex(pe => pe.ProjectId);

            builder.HasOne(pe => pe.PriorityCriteria)
                .WithMany()
                .HasForeignKey(pe => pe.PriorityCriteriaId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(pe => pe.User)
                .WithMany()
                .HasForeignKey(pe => pe.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}
