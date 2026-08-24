using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class CourseSnapshotConfiguration
    : IEntityTypeConfiguration<CourseSnapshot>
{
    public void Configure(EntityTypeBuilder<CourseSnapshot> builder)
    {
        builder.ToTable("course_snapshots");

        builder.HasKey(snapshot => snapshot.Id);
        builder.HasAlternateKey(snapshot => new
        {
            snapshot.Id,
            snapshot.ExternalCourseId
        });

        builder.Property(snapshot => snapshot.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(snapshot => snapshot.ExternalCourseId)
            .HasColumnName("external_course_id")
            .IsRequired();
        builder.Property(snapshot => snapshot.ScanRunId)
            .HasColumnName("scan_run_id")
            .IsRequired();
        builder.Property(snapshot => snapshot.ObservedAt)
            .HasColumnName("observed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(snapshot => snapshot.IsCurrent)
            .HasColumnName("is_current")
            .IsRequired();

        builder.HasOne<ExternalCourse>()
            .WithMany()
            .HasForeignKey(snapshot => snapshot.ExternalCourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ScanRun>()
            .WithMany()
            .HasForeignKey(snapshot => new
            {
                snapshot.ScanRunId,
                snapshot.ExternalCourseId
            })
            .HasPrincipalKey(scan => new
            {
                scan.Id,
                scan.ExternalCourseId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(snapshot => snapshot.ScanRunId)
            .HasDatabaseName("ux_course_snapshots_scan_run_id")
            .IsUnique();
        builder.HasIndex(snapshot => snapshot.ExternalCourseId)
            .HasDatabaseName("ux_course_snapshots_current_course")
            .HasFilter("\"is_current\"")
            .IsUnique();
    }
}
