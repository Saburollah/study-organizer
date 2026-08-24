using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Tasks;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionContentStateConfiguration
    : IEntityTypeConfiguration<SubscriptionContentState>
{
    public void Configure(
        EntityTypeBuilder<SubscriptionContentState> builder)
    {
        builder.ToTable(
            "subscription_content_states",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_subscription_content_states_status",
                    "\"status\" IN ('Imported', 'Dismissed')");
                table.HasCheckConstraint(
                    "ck_subscription_content_states_task",
                    "(\"status\" = 'Imported' AND \"study_task_id\" IS NOT NULL) OR (\"status\" = 'Dismissed' AND \"study_task_id\" IS NULL)");
            });

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(state => state.CourseSubscriptionId)
            .HasColumnName("course_subscription_id")
            .IsRequired();
        builder.Property(state => state.ExternalCourseId)
            .HasColumnName("external_course_id")
            .IsRequired();
        builder.Property(state => state.ExternalLearningContentId)
            .HasColumnName("external_learning_content_id")
            .IsRequired();
        builder.Property(state => state.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(state => state.StudyTaskId)
            .HasColumnName("study_task_id");
        builder.Property(state => state.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(state => state.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.OwnsOne(state => state.ConfirmedSignature, signature =>
        {
            signature.Property(value => value.Version)
                .HasColumnName("confirmed_signature_version")
                .IsRequired();
            signature.Property(value => value.Hash)
                .HasColumnName("confirmed_signature_hash")
                .HasMaxLength(64)
                .IsFixedLength()
                .IsRequired();
        });
        builder.Navigation(state => state.ConfirmedSignature).IsRequired();

        builder.HasOne<CourseSubscription>()
            .WithMany()
            .HasForeignKey(state => new
            {
                state.CourseSubscriptionId,
                state.ExternalCourseId
            })
            .HasPrincipalKey(subscription => new
            {
                subscription.Id,
                subscription.ExternalCourseId
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalLearningContent>()
            .WithMany()
            .HasForeignKey(state => new
            {
                state.ExternalLearningContentId,
                state.ExternalCourseId
            })
            .HasPrincipalKey(content => new
            {
                content.Id,
                content.ExternalCourseId
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StudyTask>()
            .WithMany()
            .HasForeignKey(state => state.StudyTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(state => new
        {
            state.CourseSubscriptionId,
            state.ExternalLearningContentId
        })
            .HasDatabaseName("ux_subscription_content_states_subscription_content")
            .IsUnique();
        builder.HasIndex(state => state.StudyTaskId)
            .HasDatabaseName("ux_subscription_content_states_study_task")
            .HasFilter("\"study_task_id\" IS NOT NULL")
            .IsUnique();
    }
}
