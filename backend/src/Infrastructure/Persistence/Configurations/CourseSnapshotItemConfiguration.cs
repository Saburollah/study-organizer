using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class CourseSnapshotItemConfiguration
    : IEntityTypeConfiguration<CourseSnapshotItem>
{
    public void Configure(EntityTypeBuilder<CourseSnapshotItem> builder)
    {
        builder.ToTable("course_snapshot_items");

        builder.HasKey(item => new
        {
            item.CourseSnapshotId,
            item.ExternalLearningContentId
        });

        builder.Property(item => item.CourseSnapshotId)
            .HasColumnName("course_snapshot_id");
        builder.Property(item => item.ExternalCourseId)
            .HasColumnName("external_course_id");
        builder.Property(item => item.ExternalLearningContentId)
            .HasColumnName("external_learning_content_id");
        builder.Property(item => item.ExternalContentKey)
            .HasColumnName("external_content_key")
            .HasConversion(
                key => key.Value,
                value => new ExternalContentKey(value))
            .HasMaxLength(512)
            .IsRequired();
        builder.Property(item => item.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(item => item.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(item => item.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("timestamp with time zone");
        builder.Property(item => item.MediaType)
            .HasColumnName("media_type")
            .HasMaxLength(255);
        builder.Property(item => item.SourceReference)
            .HasColumnName("source_reference")
            .HasMaxLength(2048);

        builder.OwnsOne(item => item.Signature, signature =>
        {
            signature.Property(value => value.Version)
                .HasColumnName("signature_version")
                .IsRequired();
            signature.Property(value => value.Hash)
                .HasColumnName("signature_hash")
                .HasMaxLength(64)
                .IsFixedLength()
                .IsRequired();
        });
        builder.Navigation(item => item.Signature).IsRequired();

        builder.HasOne<CourseSnapshot>()
            .WithMany()
            .HasForeignKey(item => new
            {
                item.CourseSnapshotId,
                item.ExternalCourseId
            })
            .HasPrincipalKey(snapshot => new
            {
                snapshot.Id,
                snapshot.ExternalCourseId
            })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ExternalLearningContent>()
            .WithMany()
            .HasForeignKey(item => new
            {
                item.ExternalLearningContentId,
                item.ExternalCourseId
            })
            .HasPrincipalKey(content => new
            {
                content.Id,
                content.ExternalCourseId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new
        {
            item.CourseSnapshotId,
            item.ExternalContentKey
        })
            .HasDatabaseName("ux_course_snapshot_items_snapshot_key")
            .IsUnique();
    }
}
