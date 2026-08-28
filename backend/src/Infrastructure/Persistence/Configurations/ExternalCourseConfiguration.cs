using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class ExternalCourseConfiguration
    : IEntityTypeConfiguration<ExternalCourse>
{
    public void Configure(EntityTypeBuilder<ExternalCourse> builder)
    {
        builder.ToTable("external_courses");

        builder.HasKey(course => course.Id);

        builder.Property(course => course.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(course => course.ProviderKey)
            .HasColumnName("provider_key")
            .IsRequired();

        builder.Property(course => course.ExternalCourseId)
            .HasColumnName("external_course_id")
            .IsRequired();

        builder.Property(course => course.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(course => course.ActiveScanRunId)
            .HasColumnName("active_scan_run_id");

        builder.Property(course => course.LastSuccessfulScanAtUtc)
            .HasColumnName("last_successful_scan_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(course => course.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(course => new
            {
                course.ProviderKey,
                course.ExternalCourseId
            })
            .HasDatabaseName(
                "ix_external_courses_provider_key_external_course_id")
            .IsUnique();
    }
}
