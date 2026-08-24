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

    public DbSet<CourseSubscription> CourseSubscriptions =>
        Set<CourseSubscription>();

    public DbSet<ExternalLearningContent> ExternalLearningContents =>
        Set<ExternalLearningContent>();

    public DbSet<ScanRun> ScanRuns => Set<ScanRun>();

    public DbSet<CourseSnapshot> CourseSnapshots => Set<CourseSnapshot>();

    public DbSet<CourseSnapshotItem> CourseSnapshotItems =>
        Set<CourseSnapshotItem>();

    public DbSet<SubscriptionContentState> SubscriptionContentStates =>
        Set<SubscriptionContentState>();

    public DbSet<SourceUpdate> SourceUpdates => Set<SourceUpdate>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
