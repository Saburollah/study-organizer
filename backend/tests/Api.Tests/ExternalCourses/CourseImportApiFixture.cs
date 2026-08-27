using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;
using StudyOrganizer.Domain.Modules;
using StudyOrganizer.Infrastructure.Identity;
using StudyOrganizer.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace StudyOrganizer.Api.Tests.ExternalCourses;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CourseImportApiCollection
    : ICollectionFixture<CourseImportApiFixture>
{
    public const string Name = "Course import API";
}

public sealed class CourseImportApiFixture
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly string SigningKey = new('b', 64);

    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithCleanUp(true)
            .Build();

    public ControllableExternalCourseSource CourseSource { get; } =
        new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            _container.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExternalCourseSource>();
            services.AddSingleton<IExternalCourseSource>(CourseSource);
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<ApplicationDbContext>>();
        await using var context =
            await contextFactory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }

    public async Task ResetAsync()
    {
        CourseSource.Reset();

        await using var scope = Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<ApplicationDbContext>>();
        await using var context =
            await contextFactory.CreateDbContextAsync();
        await context.Database.ExecuteSqlRawAsync(
            """
            DROP SCHEMA public CASCADE;
            CREATE SCHEMA public;
            """);
        await context.Database.MigrateAsync();
    }

    public async Task<(Guid UserId, Guid ModuleId)> CreateUserAndModuleAsync(
        string email,
        string moduleName = "Software Engineering")
    {
        await using var scope = Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<ApplicationDbContext>>();
        await using var context =
            await contextFactory.CreateDbContextAsync();

        var user = new ApplicationUser
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant()
        };
        var module = new StudyModule(user.Id, moduleName);
        context.Users.Add(user);
        context.Modules.Add(module);
        await context.SaveChangesAsync();
        return (user.Id, module.Id);
    }

    public async Task<Guid> CreateModuleAsync(
        Guid ownerId,
        string moduleName)
    {
        await using var scope = Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<ApplicationDbContext>>();
        await using var context =
            await contextFactory.CreateDbContextAsync();
        var module = new StudyModule(ownerId, moduleName);
        context.Modules.Add(module);
        await context.SaveChangesAsync();
        return module.Id;
    }

    public new async Task DisposeAsync()
    {
        Dispose();
        await _container.DisposeAsync();
    }
}

public sealed class ControllableExternalCourseSource
    : IExternalCourseSource
{
    private readonly object _gate = new();
    private ExternalCourseSourcePayload _sourcePayload = CreateDefaultPayload();
    private ScanRunErrorCode? _failure;
    private bool _throwUnexpected;
    private bool _blockNextFetch;
    private int _fetchCount;
    private TaskCompletionSource _fetchStarted = NewSignal();
    private TaskCompletionSource _releaseFetch = NewSignal();

    public int FetchCount
    {
        get
        {
            lock (_gate)
            {
                return _fetchCount;
            }
        }
    }

    public Task FetchStarted
    {
        get
        {
            lock (_gate)
            {
                return _fetchStarted.Task;
            }
        }
    }

    public void UsePayload(ExternalCourseSourcePayload sourcePayload)
    {
        ArgumentNullException.ThrowIfNull(sourcePayload);
        lock (_gate)
        {
            _sourcePayload = sourcePayload;
        }
    }

    public void FailWith(ScanRunErrorCode errorCode)
    {
        lock (_gate)
        {
            _failure = errorCode;
            _throwUnexpected = false;
        }
    }

    public void FailUnexpectedly()
    {
        lock (_gate)
        {
            _failure = null;
            _throwUnexpected = true;
        }
    }

    public void ClearFailure()
    {
        lock (_gate)
        {
            _failure = null;
            _throwUnexpected = false;
        }
    }

    public void BlockNextFetch()
    {
        lock (_gate)
        {
            _blockNextFetch = true;
            _fetchStarted = NewSignal();
            _releaseFetch = NewSignal();
        }
    }

    public void ReleaseFetch()
    {
        lock (_gate)
        {
            _releaseFetch.TrySetResult();
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _sourcePayload = CreateDefaultPayload();
            _failure = null;
            _throwUnexpected = false;
            _blockNextFetch = false;
            _fetchCount = 0;
            _fetchStarted.TrySetCanceled();
            _releaseFetch.TrySetResult();
            _fetchStarted = NewSignal();
            _releaseFetch = NewSignal();
        }
    }

    public async Task<ExternalCourseSourcePayload> FetchCourseDataAsync(
        ExternalCourseIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ExternalCourseSourcePayload sourcePayload;
        ScanRunErrorCode? failure;
        bool throwUnexpected;
        Task? releaseTask = null;

        lock (_gate)
        {
            _fetchCount++;
            sourcePayload = _sourcePayload;
            failure = _failure;
            throwUnexpected = _throwUnexpected;

            if (_blockNextFetch)
            {
                _blockNextFetch = false;
                releaseTask = _releaseFetch.Task;
                _fetchStarted.TrySetResult();
            }
        }

        if (releaseTask is not null)
        {
            await releaseTask.WaitAsync(cancellationToken);
        }

        if (failure.HasValue)
        {
            throw new ExternalCourseSourceException(failure.Value);
        }

        if (throwUnexpected)
        {
            throw new InvalidOperationException(
                "credential=private-value; internal database failure");
        }

        return sourcePayload;
    }

    private static TaskCompletionSource NewSignal()
    {
        return new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static ExternalCourseSourcePayload CreateDefaultPayload()
    {
        return new ExternalCourseSourcePayload(
        [
            new CourseSourceItem(
                new ExternalContentKey("reading-pdf"),
                ExternalLearningContentType.File,
                "Architecture reading",
                null,
                "application/pdf",
                "/mock-moodle/content/reading.pdf?token=private"),
            new CourseSourceItem(
                new ExternalContentKey("reference-link"),
                ExternalLearningContentType.Link,
                "Reference link",
                null,
                null,
                "/mock-moodle/content/reference?session=private"),
            new CourseSourceItem(
                new ExternalContentKey("practice-activity"),
                ExternalLearningContentType.Activity,
                "Practice activity",
                DateTimeOffset.Parse("2026-09-15T18:00:00Z"),
                null,
                null)
        ]);
    }
}
