using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class CourseSubscriptionConfiguration
    : IEntityTypeConfiguration<CourseSubscription>
{
    public void Configure(EntityTypeBuilder<CourseSubscription> builder)
    {
        builder.ToTable("course_subscriptions");

        builder.HasKey(subscription => subscription.Id);

        builder.Property(subscription => subscription.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(subscription => subscription.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(subscription => subscription.ExternalCourseId)
            .HasColumnName("external_course_id")
            .IsRequired();

        builder.Property(subscription => subscription.ModuleId)
            .HasColumnName("module_id")
            .IsRequired();

        builder.Property(subscription => subscription.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<ExternalCourse>()
            .WithMany()
            .HasForeignKey(subscription => subscription.ExternalCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StudyModule>()
            .WithMany()
            .HasForeignKey(subscription => subscription.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(subscription => new
            {
                subscription.OwnerId,
                subscription.ExternalCourseId
            })
            .HasDatabaseName(
                "ix_course_subscriptions_owner_id_external_course_id")
            .IsUnique();
    }
}
