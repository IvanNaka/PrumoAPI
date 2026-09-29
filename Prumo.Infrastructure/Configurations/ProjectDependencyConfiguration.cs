using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ProjectDependencyConfiguration : IEntityTypeConfiguration<ProjectDependency>
    {
        public void Configure(EntityTypeBuilder<ProjectDependency> builder)
        {
            builder.HasKey(pd => pd.Id);
            builder.Property(pd => pd.Reason)
                .HasMaxLength(500)
                .IsRequired(false);

            // RN21: a mesma dependência não pode ser repetida.
            builder.HasIndex(pd => new { pd.ProjectId, pd.DependsOnProjectId }).IsUnique();

            builder.HasOne(pd => pd.Project)
                .WithMany(p => p.Dependencies)
                .HasForeignKey(pd => pd.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pd => pd.DependsOnProject)
                .WithMany(p => p.DependentProjects)
                .HasForeignKey(pd => pd.DependsOnProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pd => pd.User)
                .WithMany()
                .HasForeignKey(pd => pd.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pd => pd.Portfolio)
                .WithMany(p => p.ProjectDependencies)
                .HasForeignKey(pd => pd.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
