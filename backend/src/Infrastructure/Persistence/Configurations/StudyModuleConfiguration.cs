using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.Modules;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class StudyModuleConfiguration
    : IEntityTypeConfiguration<StudyModule>
{
    public void Configure(
        EntityTypeBuilder<StudyModule> builder)
    {
        builder.ToTable("modules");

        builder.HasKey(module => module.Id);

        builder.Property(module => module.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(module => module.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(module => module.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(module => module.Code)
            .HasColumnName("code")
            .HasMaxLength(30);

        builder.Property(module => module.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(module => module.Color)
            .HasColumnName("color")
            .HasMaxLength(7);

        builder.Property(module => module.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(module => module.OwnerId)
            .HasDatabaseName("ix_modules_owner_id");
    }
}