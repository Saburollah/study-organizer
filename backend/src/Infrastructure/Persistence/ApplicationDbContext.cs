using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.Identity;

namespace StudyOrganizer.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<StudyModule> Modules => Set<StudyModule>();

    public DbSet<StudyTask> Tasks => Set<StudyTask>();

    public DbSet<ExternalCourse> ExternalCourses => Set<ExternalCourse>();

    public DbSet<CourseSubscription> CourseSubscriptions => Set<CourseSubscription>();

    public DbSet<ExternalContent> ExternalContents => Set<ExternalContent>();

    public DbSet<ExternalTaskLink> ExternalTaskLinks => Set<ExternalTaskLink>();

    public DbSet<ScanRun> ScanRuns => Set<ScanRun>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
