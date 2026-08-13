using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyOrganizer.Infrastructure.Identity;

namespace StudyOrganizer.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration
    : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(
        EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.Email)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();

        builder.Property(user => user.FirstName)
            .HasMaxLength(100);

        builder.Property(user => user.LastName)
            .HasMaxLength(100);

        builder.Property(user => user.DateOfBirth)
            .HasColumnType("date");

        builder.Property(user => user.Gender)
            .HasConversion<string>()
            .HasMaxLength(30);
    }
}