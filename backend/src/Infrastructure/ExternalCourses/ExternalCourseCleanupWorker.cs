using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StudyOrganizer.Infrastructure.ExternalCourses;

public sealed class ExternalCourseCleanupWorker(
    ExternalCourseCleanup cleanup,
    ExternalCourseCleanupOptions options,
    TimeProvider timeProvider,
    ILogger<ExternalCourseCleanupWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cleanedCourseCount =
                    await cleanup.CleanupExpiredAsync(stoppingToken);
                if (cleanedCourseCount > 0)
                {
                    logger.LogInformation(
                        "Cleaned {ExternalCourseCount} inactive External Courses.",
                        cleanedCourseCount);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "External Course cleanup failed.");
            }

            try
            {
                await Task.Delay(
                    options.Interval,
                    timeProvider,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
