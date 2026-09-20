using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class ScanRunConfiguration
    : IEntityTypeConfiguration<ScanRun>
{
    public void Configure(EntityTypeBuilder<ScanRun> builder)
    {
        builder.ToTable("scan_runs");

        builder.HasKey(scanRun => scanRun.Id);

        builder.Property(scanRun => scanRun.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(scanRun => scanRun.ExternalCourseId)
            .HasColumnName("external_course_id")
            .IsRequired();

        builder.Property(scanRun => scanRun.RequestedByOwnerId)
            .HasColumnName("requested_by_owner_id")
            .IsRequired();

        builder.Property(scanRun => scanRun.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(scanRun => scanRun.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(scanRun => scanRun.FinishedAtUtc)
            .HasColumnName("finished_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(scanRun => scanRun.ErrorCode)
            .HasColumnName("error_code");

        builder.HasOne<ExternalCourse>()
            .WithMany()
            .HasForeignKey(scanRun => scanRun.ExternalCourseId);
    }
}
