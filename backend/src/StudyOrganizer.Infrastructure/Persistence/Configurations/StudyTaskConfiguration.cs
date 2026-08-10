using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Domain.Tasks;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class StudyTaskConfiguration
    : IEntityTypeConfiguration<StudyTask>
{
    public void Configure(EntityTypeBuilder<StudyTask> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(task => task.ModuleId)
            .HasColumnName("module_id")
            .IsRequired();

        builder.Property(task => task.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(task => task.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(task => task.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(task => task.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(task => task.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<StudyModule>()
            .WithMany()
            .HasForeignKey(task => task.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(task => task.ModuleId)
            .HasDatabaseName("ix_tasks_module_id");

        builder.HasIndex(task => new { task.ModuleId, task.Status })
            .HasDatabaseName("ix_tasks_module_id_status");
    }
}