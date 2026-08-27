using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class ExternalLearningContentConfiguration
    : IEntityTypeConfiguration<ExternalLearningContent>
{
    public void Configure(
        EntityTypeBuilder<ExternalLearningContent> builder)
    {
        builder.ToTable(
            "external_learning_contents",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_external_learning_contents_type",
                    "\"type\" IN ('File', 'Link', 'Activity')");
                table.HasCheckConstraint(
                    "ck_external_learning_contents_availability",
                    "\"availability\" IN ('Available', 'Unavailable')");
            });

        builder.HasKey(content => content.Id);
        builder.HasAlternateKey(content => new
        {
            content.Id,
            content.ExternalCourseId
        });

        builder.Property(content => content.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(content => content.ExternalCourseId)
            .HasColumnName("external_course_id")
            .IsRequired();
        builder.Property(content => content.ExternalContentKey)
            .HasColumnName("external_content_key")
            .HasConversion(
                key => key.Value,
                value => new ExternalContentKey(value))
            .HasMaxLength(512)
            .IsRequired();
        builder.Property(content => content.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(content => content.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(content => content.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("timestamp with time zone");
        builder.Property(content => content.MediaType)
            .HasColumnName("media_type")
            .HasMaxLength(255);
        builder.Property(content => content.SourceReference)
            .HasColumnName("source_reference")
            .HasMaxLength(2048);
        builder.Property(content => content.Availability)
            .HasColumnName("availability")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(content => content.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(content => content.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(content => content.MetadataPurgedAt)
            .HasColumnName("metadata_purged_at")
            .HasColumnType("timestamp with time zone");

        ConfigureSignature(builder);

        builder.HasOne<ExternalCourse>()
            .WithMany()
            .HasForeignKey(content => content.ExternalCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(content => new
        {
            content.ExternalCourseId,
            content.ExternalContentKey
        })
            .HasDatabaseName("ux_external_learning_contents_course_key")
            .IsUnique();
    }

    private static void ConfigureSignature(
        EntityTypeBuilder<ExternalLearningContent> builder)
    {
        builder.OwnsOne(content => content.Signature, signature =>
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

        builder.Navigation(content => content.Signature)
            .IsRequired();
    }
}
