using Microsoft.EntityFrameworkCore;
using StudyOrganizer.Domain.Tasks;
using StudyOrganizer.Infrastructure.ExternalCourses;
using StudyOrganizer.Infrastructure.Tasks;

namespace StudyOrganizer.Infrastructure.Tests.ExternalCourses;

public sealed class ExternalCourseScenario : IAsyncDisposable
{
    private const string PrimaryCourseUrl =
        "https://mock-moodle.local/courses/software-engineering-2026";

    private ExternalCourseScenario(
        ExternalCourseTestDatabase database,
        ControlledExternalCourseProvider provider,
        ExternalCourseRegistrationHandler registrationHandler,
        ExternalCourseScanHandler handler,
        StudyTaskHandler taskHandler,
        IReadOnlyList<Guid> ownerIds,
        IReadOnlyList<Guid> subscriptionIds,
        IReadOnlyList<Guid> moduleIds,
        IReadOnlyList<Guid> taskIds,
        DateTimeOffset dueDate)
    {
        Database = database;
        Provider = provider;
        RegistrationHandler = registrationHandler;
        Handler = handler;
        TaskHandler = taskHandler;
        OwnerIds = ownerIds;
        SubscriptionIds = subscriptionIds;
        ModuleIds = moduleIds;
        TaskIds = taskIds;
        DueDate = dueDate;
    }

    public ExternalCourseTestDatabase Database { get; }
    public ControlledExternalCourseProvider Provider { get; }
    public ExternalCourseRegistrationHandler RegistrationHandler { get; }
    public ExternalCourseScanHandler Handler { get; }
    public StudyTaskHandler TaskHandler { get; }
    public IReadOnlyList<Guid> OwnerIds { get; private set; }
    public IReadOnlyList<Guid> SubscriptionIds { get; private set; }
    public IReadOnlyList<Guid> ModuleIds { get; private set; }
    public IReadOnlyList<Guid> TaskIds { get; private set; }
    public DateTimeOffset DueDate { get; private set; }

    public static async Task<ExternalCourseScenario> CreateAsync(int subscriberCount)
    {
        if (subscriberCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(subscriberCount));
        }

        var database = await ExternalCourseTestDatabase.CreateAsync();

        try
        {
            var provider = ControlledExternalCourseProvider.ForSoftwareEngineering();
            var registrationHandler = new ExternalCourseRegistrationHandler(
                database.Context,
                [provider],
                database.TimeProvider);
            var scanHandler = new ExternalCourseScanHandler(
                database.Context,
                [provider],
                database.TimeProvider);
            var taskHandler = new StudyTaskHandler(database.Context);
            var ownerIds = new List<Guid>();
            var subscriptionIds = new List<Guid>();
            var moduleIds = new List<Guid>();

            for (var index = 0; index < subscriberCount; index++)
            {
                var ownerId = await database.CreateUserAsync(
                    $"subscriber-{index + 1}@example.com");
                var registration = await registrationHandler.RegisterAsync(
                    ownerId,
                    PrimaryCourseUrl);

                ownerIds.Add(ownerId);
                subscriptionIds.Add(registration.Subscription!.Id);
                moduleIds.Add(registration.Subscription.ModuleId);
            }

            return new ExternalCourseScenario(
                database,
                provider,
                registrationHandler,
                scanHandler,
                taskHandler,
                ownerIds,
                subscriptionIds,
                moduleIds,
                [],
                default);
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public static async Task<ExternalCourseScenario> CreateScannedAsync(
        int subscriberCount)
    {
        var scenario = await CreateAsync(subscriberCount);

        try
        {
            scenario.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);
            await scenario.Handler.ScanAsync(
                scenario.OwnerIds[0],
                scenario.SubscriptionIds[0]);

            var subscriptions = await scenario.Database.Context.CourseSubscriptions
                .AsNoTracking()
                .Where(subscription => scenario.SubscriptionIds.Contains(subscription.Id))
                .ToDictionaryAsync(subscription => subscription.Id);
            scenario.ModuleIds = scenario.SubscriptionIds
                .Select(subscriptionId => subscriptions[subscriptionId].ModuleId)
                .ToArray();
            var taskIdsBySubscription = await scenario.Database.Context.ExternalTaskLinks
                .AsNoTracking()
                .ToDictionaryAsync(
                    link => link.CourseSubscriptionId,
                    link => link.TaskId);
            scenario.TaskIds = scenario.SubscriptionIds
                .Select(subscriptionId => taskIdsBySubscription[subscriptionId])
                .ToArray();
            scenario.DueDate = await scenario.Database.Context.Tasks
                .AsNoTracking()
                .Select(task => task.DueDate)
                .FirstAsync();

            return scenario;
        }
        catch
        {
            await scenario.DisposeAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<StudyTask>> TasksForAsync(Guid ownerId)
    {
        var tasks = await Database.Context.Tasks
            .AsNoTracking()
            .Where(task => Database.Context.Modules.Any(
                module => module.Id == task.ModuleId && module.OwnerId == ownerId))
            .ToListAsync();

        return tasks
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Id)
            .ToList();
    }

    public async Task<StudyTask> ReloadTaskAsync(Guid taskId)
    {
        Database.Context.ChangeTracker.Clear();
        return await Database.Context.Tasks.SingleAsync(task => task.Id == taskId);
    }

    public ValueTask DisposeAsync() => Database.DisposeAsync();
}
