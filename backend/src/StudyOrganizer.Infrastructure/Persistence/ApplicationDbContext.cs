using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Domain.Tasks;

namespace StudyOrganizer.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<StudyModule> Modules => Set<StudyModule>();

    public DbSet<StudyTask> Tasks => Set<StudyTask>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}