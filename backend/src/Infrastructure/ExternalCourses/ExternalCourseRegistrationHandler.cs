using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class ExternalCourseRegistrationHandler(
    ApplicationDbContext dbContext,
    IEnumerable<IExternalCourseProvider> providers,
    TimeProvider timeProvider)
    : IExternalCourseRegistrationHandler
{
    private readonly IReadOnlyList<IExternalCourseProvider> _providers =
        providers.ToList();

    public async Task<CourseRegistrationResult> RegisterAsync(
        Guid ownerId,
        string courseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(courseUrl, UriKind.Absolute, out var courseUri)
            || !string.Equals(
                courseUri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            return new CourseRegistrationResult(
                CourseRegistrationOutcome.InvalidUrl,
                null);
        }

        var matchingProviders = _providers
            .Where(provider => provider.CanHandle(courseUri))
            .Take(2)
            .ToArray();

        if (matchingProviders.Length != 1)
        {
            return new CourseRegistrationResult(
                CourseRegistrationOutcome.UnsupportedUrl,
                null);
        }

        var discovery = await matchingProviders[0].DiscoverAsync(
            courseUri,
            cancellationToken);

        return await PersistRegistrationAsync(
            ownerId,
            discovery,
            retryOnUniqueConflict: true,
            cancellationToken);
    }

    private async Task<CourseRegistrationResult> PersistRegistrationAsync(
        Guid ownerId,
        CourseDiscovery discovery,
        bool retryOnUniqueConflict,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            var course = await dbContext.ExternalCourses
                .SingleOrDefaultAsync(
                    item => item.ProviderKey == discovery.ProviderKey
                        && item.ExternalCourseId == discovery.ExternalCourseId,
                    cancellationToken);

            if (course is not null)
            {
                var existing = await dbContext.CourseSubscriptions
                    .SingleOrDefaultAsync(
                        item => item.OwnerId == ownerId
                            && item.ExternalCourseId == course.Id,
                        cancellationToken);

                if (existing is not null)
                {
                    var existingResult = await CreateSubscriptionResultAsync(
                        existing,
                        course,
                        cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return new CourseRegistrationResult(
                        CourseRegistrationOutcome.Existing,
                        existingResult);
                }
            }
            else
            {
                course = new ExternalCourse(
                    discovery.ProviderKey,
                    discovery.ExternalCourseId,
                    discovery.Name,
                    timeProvider.GetUtcNow());
                dbContext.ExternalCourses.Add(course);
            }

            var module = new StudyModule(ownerId, discovery.Name);
            var subscription = new CourseSubscription(
                ownerId,
                course.Id,
                module.Id,
                timeProvider.GetUtcNow());
            dbContext.Modules.Add(module);
            dbContext.CourseSubscriptions.Add(subscription);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CourseRegistrationResult(
                CourseRegistrationOutcome.Created,
                ExternalCourseQueryHandler.ToSubscriptionResult(
                    subscription,
                    course,
                    null));
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();

            if (!IsUniqueConstraintViolation(exception))
            {
                throw;
            }

            var existingResult = await FindExistingResultAsync(
                ownerId,
                discovery,
                cancellationToken);
            if (existingResult is not null)
            {
                return new CourseRegistrationResult(
                    CourseRegistrationOutcome.Existing,
                    existingResult);
            }

            if (!retryOnUniqueConflict)
            {
                throw;
            }

            return await PersistRegistrationAsync(
                ownerId,
                discovery,
                retryOnUniqueConflict: false,
                cancellationToken);
        }
    }

    private async Task<CourseSubscriptionResult?> FindExistingResultAsync(
        Guid ownerId,
        CourseDiscovery discovery,
        CancellationToken cancellationToken)
    {
        var registration = await (
                from subscription in dbContext.CourseSubscriptions.AsNoTracking()
                join course in dbContext.ExternalCourses.AsNoTracking()
                    on subscription.ExternalCourseId equals course.Id
                where subscription.OwnerId == ownerId
                    && course.ProviderKey == discovery.ProviderKey
                    && course.ExternalCourseId == discovery.ExternalCourseId
                select new
                {
                    Subscription = subscription,
                    Course = course
                })
            .SingleOrDefaultAsync(cancellationToken);

        return registration is null
            ? null
            : await CreateSubscriptionResultAsync(
                registration.Subscription,
                registration.Course,
                cancellationToken);
    }

    private async Task<CourseSubscriptionResult> CreateSubscriptionResultAsync(
        CourseSubscription subscription,
        ExternalCourse course,
        CancellationToken cancellationToken)
    {
        var scanRuns = await dbContext.ScanRuns
            .AsNoTracking()
            .Where(scanRun => scanRun.ExternalCourseId == course.Id)
            .ToListAsync(cancellationToken);
        var latestScanRun = scanRuns
            .OrderByDescending(scanRun => scanRun.StartedAtUtc)
            .FirstOrDefault();

        return ExternalCourseQueryHandler.ToSubscriptionResult(
            subscription,
            course,
            latestScanRun);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException databaseException
                && string.Equals(
                    databaseException.SqlState,
                    "23505",
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (current.Message.Contains(
                "UNIQUE constraint failed",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
