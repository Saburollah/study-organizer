using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class ExternalCourseConfiguration
    : IEntityTypeConfiguration<ExternalCourse>
{
    public void Configure(EntityTypeBuilder<ExternalCourse> builder)
    {
        builder.ToTable(
            "external_courses",
            table => table.HasCheckConstraint(
                "ck_external_courses_state",
                "\"state\" IN ('Inactive', 'Active')"));

        builder.HasKey(course => course.Id);

        builder.Property(course => course.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(course => course.Name)
            .HasColumnName("name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(course => course.State)
            .HasColumnName("state")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(course => course.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(course => course.InactiveSince)
            .HasColumnName("inactive_since")
            .HasColumnType("timestamp with time zone");

        builder.OwnsOne(course => course.Identity, identity =>
        {
            identity.Property(value => value.SourceType)
                .HasColumnName("source_type")
                .HasMaxLength(100)
                .IsRequired();

            identity.Property(value => value.SourceInstance)
                .HasColumnName("source_instance")
                .HasMaxLength(2048)
                .IsRequired();

            identity.Property(value => value.ExternalCourseKey)
                .HasColumnName("external_course_key")
                .HasMaxLength(512)
                .IsRequired();

            identity.HasIndex(value => new
            {
                value.SourceType,
                value.SourceInstance,
                value.ExternalCourseKey
            })
                .HasDatabaseName("ux_external_courses_identity")
                .IsUnique();
        });

        builder.Navigation(course => course.Identity)
            .IsRequired();
    }
}
