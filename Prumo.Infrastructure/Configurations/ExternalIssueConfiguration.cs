using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prumo.Domain.Entities;

namespace Prumo.Infrastructure.Configurations
{
    public class ExternalIssueConfiguration : IEntityTypeConfiguration<ExternalIssue>
    {
        public void Configure(EntityTypeBuilder<ExternalIssue> builder)
        {
            builder.ToTable("Issues");

            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).HasColumnType("uuid").ValueGeneratedNever();

            builder.Property(i => i.ExternalId).HasMaxLength(50).IsRequired();
            builder.HasIndex(i => i.ExternalId).IsUnique();
            builder.Property(i => i.Source).HasMaxLength(20).IsRequired().HasDefaultValue("Jira");
            builder.Property(i => i.Title).HasMaxLength(500).IsRequired();
            builder.Property(i => i.Type).HasMaxLength(20).IsRequired();
            builder.Property(i => i.Status).HasMaxLength(100).IsRequired();
            builder.Property(i => i.EstimateHours).HasPrecision(10, 2);
            builder.Property(i => i.SpentHours).HasPrecision(10, 2);
            builder.Property(i => i.AssigneeEmail).HasMaxLength(200);
            builder.HasIndex(i => i.AssigneeEmail);

            builder.HasOne(i => i.Project)
                .WithMany()
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(i => i.Worklogs)
                .WithOne(w => w.Issue)
                .HasForeignKey(w => w.IssueId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ExternalWorklogConfiguration : IEntityTypeConfiguration<ExternalWorklog>
    {
        public void Configure(EntityTypeBuilder<ExternalWorklog> builder)
        {
            builder.ToTable("Worklogs");

            builder.HasKey(w => w.Id);
            builder.Property(w => w.Id).HasColumnType("uuid").ValueGeneratedNever();

            builder.Property(w => w.ExternalId).HasMaxLength(50).IsRequired();
            builder.HasIndex(w => w.ExternalId).IsUnique();
            builder.Property(w => w.AuthorEmail).HasMaxLength(200);
            builder.HasIndex(w => w.AuthorEmail);
            builder.Property(w => w.Hours).HasPrecision(10, 2);
        }
    }
}
