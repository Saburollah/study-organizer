using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class SourceUpdateConfiguration
    : IEntityTypeConfiguration<SourceUpdate>
{
    public void Configure(EntityTypeBuilder<SourceUpdate> builder)
    {
        builder.ToTable("source_updates");

        builder.HasKey(update => update.Id);

        builder.Property(update => update.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(update => update.SubscriptionContentStateId)
            .HasColumnName("subscription_content_state_id")
            .IsRequired();
        builder.Property(update => update.DetectedAt)
            .HasColumnName("detected_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(update => update.DetectedByScanRunId)
            .HasColumnName("detected_by_scan_run_id");

        builder.OwnsOne(update => update.DetectedSignature, signature =>
        {
            signature.Property(value => value.Version)
                .HasColumnName("detected_signature_version")
                .IsRequired();
            signature.Property(value => value.Hash)
                .HasColumnName("detected_signature_hash")
                .HasMaxLength(64)
                .IsFixedLength()
                .IsRequired();
        });
        builder.Navigation(update => update.DetectedSignature).IsRequired();

        builder.HasOne<SubscriptionContentState>()
            .WithMany()
            .HasForeignKey(update => update.SubscriptionContentStateId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ScanRun>()
            .WithMany()
            .HasForeignKey(update => update.DetectedByScanRunId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(update => update.SubscriptionContentStateId)
            .HasDatabaseName("ux_source_updates_subscription_content_state")
            .IsUnique();
    }
}
