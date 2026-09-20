using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class ExternalContentConfiguration
    : IEntityTypeConfiguration<ExternalContent>
{
    public void Configure(EntityTypeBuilder<ExternalContent> builder)
    {
        builder.ToTable("external_contents");

        builder.HasKey(content => content.Id);

        builder.Property(content => content.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(content => content.ExternalCourseId)
            .HasColumnName("external_course_id")
            .IsRequired();

        builder.Property(content => content.ProviderContentId)
            .HasColumnName("provider_content_id")
            .IsRequired();

        builder.Property(content => content.Kind)
            .HasColumnName("kind")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(content => content.Title)
            .HasColumnName("title")
            .IsRequired();

        builder.Property(content => content.Description)
            .HasColumnName("description");

        builder.Property(content => content.SourceUrl)
            .HasColumnName("source_url")
            .IsRequired();

        builder.Property(content => content.StructuredDueDateUtc)
            .HasColumnName("structured_due_date_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(content => content.ProcessingState)
            .HasColumnName("processing_state")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(content => content.ReviewReason)
            .HasColumnName("review_reason")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(content => content.Visibility)
            .HasColumnName("visibility")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(content => content.LastSeenAtUtc)
            .HasColumnName("last_seen_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<ExternalCourse>()
            .WithMany()
            .HasForeignKey(content => content.ExternalCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(content => new
            {
                content.ExternalCourseId,
                content.ProviderContentId
            })
            .HasDatabaseName(
                "ix_external_contents_external_course_id_provider_content_id")
            .IsUnique();
    }
}
