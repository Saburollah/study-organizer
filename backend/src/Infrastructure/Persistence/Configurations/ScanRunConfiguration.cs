using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class ScanRunConfiguration
    : IEntityTypeConfiguration<ScanRun>
{
    public void Configure(EntityTypeBuilder<ScanRun> builder)
    {
        builder.ToTable(
            "scan_runs",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_scan_runs_status",
                    "\"status\" IN ('Running', 'Succeeded', 'Failed', 'Cancelled', 'Expired')");
                table.HasCheckConstraint(
                    "ck_scan_runs_counts_non_negative",
                    "\"new_count\" >= 0 AND \"updated_count\" >= 0 AND \"unchanged_count\" >= 0 AND \"unavailable_count\" >= 0");
            });

        builder.HasKey(scan => scan.Id);
        builder.HasAlternateKey(scan => new
        {
            scan.Id,
            scan.ExternalCourseId
        });

        builder.Property(scan => scan.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(scan => scan.ExternalCourseId)
            .HasColumnName("external_course_id")
            .IsRequired();
        builder.Property(scan => scan.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(scan => scan.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(scan => scan.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(scan => scan.LeaseExpiresAt)
            .HasColumnName("lease_expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(scan => scan.ActivationSubscriptionId)
            .HasColumnName("activation_subscription_id");
        builder.Property(scan => scan.ErrorCode)
            .HasColumnName("error_code")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.OwnsOne(scan => scan.Counts, counts =>
        {
            counts.Property(value => value.New)
                .HasColumnName("new_count")
                .IsRequired();
            counts.Property(value => value.Updated)
                .HasColumnName("updated_count")
                .IsRequired();
            counts.Property(value => value.Unchanged)
                .HasColumnName("unchanged_count")
                .IsRequired();
            counts.Property(value => value.Unavailable)
                .HasColumnName("unavailable_count")
                .IsRequired();
        });
        builder.Navigation(scan => scan.Counts).IsRequired();

        builder.HasOne<ExternalCourse>()
            .WithMany()
            .HasForeignKey(scan => scan.ExternalCourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CourseSubscription>()
            .WithMany()
            .HasForeignKey(scan => new
            {
                scan.ActivationSubscriptionId,
                scan.ExternalCourseId
            })
            .HasPrincipalKey(subscription => new
            {
                subscription.Id,
                subscription.ExternalCourseId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(scan => scan.ExternalCourseId)
            .HasDatabaseName("ux_scan_runs_running_course")
            .HasFilter("\"status\" = 'Running'")
            .IsUnique();
    }
}
