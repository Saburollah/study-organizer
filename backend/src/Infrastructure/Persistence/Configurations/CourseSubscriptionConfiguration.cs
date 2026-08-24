using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Infrastructure.Identity;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class CourseSubscriptionConfiguration
    : IEntityTypeConfiguration<CourseSubscription>
{
    public void Configure(EntityTypeBuilder<CourseSubscription> builder)
    {
        builder.ToTable(
            "course_subscriptions",
            table => table.HasCheckConstraint(
                "ck_course_subscriptions_state",
                "\"state\" IN ('Pending', 'Active', 'Ended')"));

        builder.HasKey(subscription => subscription.Id);
        builder.HasAlternateKey(subscription => new
        {
            subscription.Id,
            subscription.ExternalCourseId
        });

        builder.Property(subscription => subscription.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(subscription => subscription.StudyModuleId)
            .HasColumnName("study_module_id")
            .IsRequired();
        builder.Property(subscription => subscription.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();
        builder.Property(subscription => subscription.ExternalCourseId)
            .HasColumnName("external_course_id")
            .IsRequired();
        builder.Property(subscription => subscription.State)
            .HasColumnName("state")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(subscription => subscription.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(subscription => subscription.ActivatedAt)
            .HasColumnName("activated_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(subscription => subscription.EndedAt)
            .HasColumnName("ended_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<StudyModule>()
            .WithMany()
            .HasForeignKey(subscription => subscription.StudyModuleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(subscription => subscription.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalCourse>()
            .WithMany()
            .HasForeignKey(subscription => subscription.ExternalCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(subscription => subscription.StudyModuleId)
            .HasDatabaseName("ux_course_subscriptions_study_module_id")
            .IsUnique();
        builder.HasIndex(subscription => new
        {
            subscription.OwnerId,
            subscription.ExternalCourseId
        })
            .HasDatabaseName("ux_course_subscriptions_owner_course")
            .IsUnique();
    }
}
