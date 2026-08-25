using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Application.Modules;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Infrastructure.Persistence;

namespace StudyOrganizer.Infrastructure.Modules;

public sealed class ModuleHandler(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : IModuleHandler
{
    public async Task<ModuleResult> CreateAsync(
        Guid ownerId,
        string name,
        string? code,
        string? description,
        string? color,
        CancellationToken cancellationToken = default)
    {
        var module = new StudyModule(
            ownerId,
            name,
            code,
            description,
            color);

        dbContext.Modules.Add(module);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return ToResult(module);
    }

    public async Task<IReadOnlyList<ModuleResult>>
        GetByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Modules
            .AsNoTracking()
            .Where(module =>
                module.OwnerId == ownerId)
            .OrderByDescending(module =>
                module.CreatedAt)
            .Select(module =>
                new ModuleResult(
                    module.Id,
                    module.Name,
                    module.Code,
                    module.Description,
                    module.Color,
                    module.CreatedAt))
            .ToListAsync(cancellationToken);
    }
    public async Task<ModuleResult?> UpdateAsync(
        Guid ownerId,
        Guid moduleId,
        string name,
        string? code,
        string? description,
        string? color,
        CancellationToken cancellationToken = default)
    {
        var module = await dbContext.Modules
            .SingleOrDefaultAsync(
                item =>
                    item.Id == moduleId
                    && item.OwnerId == ownerId,
                cancellationToken);

        if (module is null)
        {
            return null;
        }

        module.Update(
            name,
            code,
            description,
            color);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return ToResult(module);
    }

    public async Task<bool> DeleteAsync(
        Guid ownerId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        var module = await dbContext.Modules
            .SingleOrDefaultAsync(
                item =>
                    item.Id == moduleId
                    && item.OwnerId == ownerId,
                cancellationToken);

        if (module is null)
        {
            return false;
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var subscription = await dbContext.CourseSubscriptions
            .SingleOrDefaultAsync(
                candidate => candidate.StudyModuleId == moduleId,
                cancellationToken);
        if (subscription is not null)
        {
            var now = timeProvider.GetUtcNow();
            var runningActivationScans = await dbContext.ScanRuns
                .Where(scan =>
                    scan.ActivationSubscriptionId == subscription.Id
                    && scan.Status == ScanRunStatus.Running)
                .ToListAsync(cancellationToken);
            foreach (var scan in runningActivationScans)
            {
                scan.Cancel(now);
            }

            var states = await dbContext.SubscriptionContentStates
                .Where(state =>
                    state.CourseSubscriptionId == subscription.Id)
                .ToListAsync(cancellationToken);
            var stateIds = states.Select(state => state.Id).ToList();
            var sourceUpdates = await dbContext.SourceUpdates
                .Where(update =>
                    stateIds.Contains(
                        update.SubscriptionContentStateId))
                .ToListAsync(cancellationToken);

            dbContext.SourceUpdates.RemoveRange(sourceUpdates);
            dbContext.SubscriptionContentStates.RemoveRange(states);
            dbContext.CourseSubscriptions.Remove(subscription);

            var hasOtherActiveSubscription =
                await dbContext.CourseSubscriptions.AnyAsync(
                    candidate =>
                        candidate.ExternalCourseId ==
                            subscription.ExternalCourseId
                        && candidate.Id != subscription.Id
                        && candidate.State ==
                            CourseSubscriptionState.Active,
                    cancellationToken);
            if (!hasOtherActiveSubscription
                && subscription.State == CourseSubscriptionState.Active)
            {
                var course = await dbContext.ExternalCourses
                    .SingleAsync(
                        candidate =>
                            candidate.Id == subscription.ExternalCourseId,
                        cancellationToken);
                course.Deactivate(now);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        dbContext.Modules.Remove(module);

        await dbContext.SaveChangesAsync(
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private static ModuleResult ToResult(
        StudyModule module)
    {
        return new ModuleResult(
            module.Id,
            module.Name,
            module.Code,
            module.Description,
            module.Color,
            module.CreatedAt);
    }
}
