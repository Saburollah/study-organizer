using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Tasks;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class ExternalTaskLinkConfiguration
    : IEntityTypeConfiguration<ExternalTaskLink>
{
    public void Configure(EntityTypeBuilder<ExternalTaskLink> builder)
    {
        builder.ToTable("external_task_links");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(link => link.CourseSubscriptionId)
            .HasColumnName("course_subscription_id")
            .IsRequired();

        builder.Property(link => link.ExternalContentId)
            .HasColumnName("external_content_id")
            .IsRequired();

        builder.Property(link => link.TaskId)
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(link => link.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<CourseSubscription>()
            .WithMany()
            .HasForeignKey(link => link.CourseSubscriptionId);

        builder.HasOne<ExternalContent>()
            .WithMany()
            .HasForeignKey(link => link.ExternalContentId);

        builder.HasOne<StudyTask>()
            .WithMany()
            .HasForeignKey(link => link.TaskId);

        builder.HasIndex(link => new
            {
                link.CourseSubscriptionId,
                link.ExternalContentId
            })
            .HasDatabaseName(
                "ix_external_task_links_course_subscription_id_external_content_id")
            .IsUnique();

        builder.HasIndex(link => link.TaskId)
            .HasDatabaseName("ix_external_task_links_task_id")
            .IsUnique();
    }
}
