using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Tasks;
using StudyOrganizer.Application.Tasks;
using StudyOrganizer.Domain.Tasks;

namespace StudyOrganizer.Api.Tests.Tasks;

public sealed class StudyTaskEndpointsTests
{
    private static readonly string SigningKey =
        new('a', 64);

    [Fact]
    public async Task GetTasks_WithoutToken_ReturnsUnauthorized()
    {
        var moduleId = Guid.NewGuid();

        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync(
            $"/api/modules/{moduleId}/tasks/");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithValidData_ReturnsCreated()
    {
        var ownerId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var dueDate =
            DateTimeOffset.Parse(
                "2026-09-01T18:00:00Z");

        var task = new StudyTaskResult(
            Guid.NewGuid(),
            moduleId,
            "Kapitel 1 wiederholen",
            "Notizen lesen",
            dueDate,
            StudyTaskStatus.Open,
            DateTimeOffset.UtcNow,
            null);

        var handler =
            new StubStudyTaskHandler(task);

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, ownerId);

        var response = await client.PostAsJsonAsync(
            $"/api/modules/{moduleId}/tasks/",
            new
            {
                title = "Kapitel 1 wiederholen",
                description = "Notizen lesen",
                dueDateUtc = dueDate
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                StudyTaskResponse>();

        Assert.NotNull(body);
        Assert.Equal(task.Id, body.Id);
        Assert.Equal(moduleId, body.ModuleId);
        Assert.Equal("Open", body.Status);
        Assert.Equal(
            ownerId,
            handler.ReceivedCreateOwnerId);
        Assert.Equal(
            moduleId,
            handler.ReceivedCreateModuleId);
    }
    [Fact]
    public async Task CreateTask_WithInvalidData_ReturnsBadRequest()
    {
        var ownerId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var handler = new StubStudyTaskHandler();

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, ownerId);

        var response = await client.PostAsJsonAsync(
            $"/api/modules/{moduleId}/tasks/",
            new
            {
                title = "   ",
                description = "Ungültige Aufgabe"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.False(handler.CreateWasCalled);
    }

    [Fact]
    public async Task CreateTask_ForUnavailableModule_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var handler = new StubStudyTaskHandler();

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, ownerId);

        var response = await client.PostAsJsonAsync(
            $"/api/modules/{moduleId}/tasks/",
            new
            {
                title = "Kapitel wiederholen",
                dueDateUtc = "2026-09-01T18:00:00Z"
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.True(handler.CreateWasCalled);
        Assert.Equal(
            ownerId,
            handler.ReceivedCreateOwnerId);
        Assert.Equal(
            moduleId,
            handler.ReceivedCreateModuleId);
    }
    [Fact]
    public async Task GetTasks_SeparatesUsers()
    {
        var firstOwnerId = Guid.NewGuid();
        var secondOwnerId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();

        var task = new StudyTaskResult(
            Guid.NewGuid(),
            moduleId,
            "Kapitel 1 wiederholen",
            null,
            DateTimeOffset.Parse(
                "2026-09-01T18:00:00Z"),
            StudyTaskStatus.Open,
            DateTimeOffset.UtcNow,
            null);

        var tasksByOwnerAndModule =
            new Dictionary<
                (Guid OwnerId, Guid ModuleId),
                IReadOnlyList<StudyTaskResult>>
            {
                [(firstOwnerId, moduleId)] =
                    new[] { task }
            };

        var handler = new StubStudyTaskHandler(
            tasksByOwnerAndModule:
                tasksByOwnerAndModule);

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        AddAuthorization(client, firstOwnerId);

        var firstResponse = await client.GetAsync(
            $"/api/modules/{moduleId}/tasks/");

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var firstBody =
            await firstResponse.Content.ReadFromJsonAsync<
                List<StudyTaskResponse>>();

        Assert.NotNull(firstBody);
        Assert.Single(firstBody);
        Assert.Equal(task.Id, firstBody[0].Id);

        AddAuthorization(client, secondOwnerId);

        var secondResponse = await client.GetAsync(
            $"/api/modules/{moduleId}/tasks/");

        Assert.Equal(
            HttpStatusCode.NotFound,
            secondResponse.StatusCode);

        Assert.Equal(2, handler.ReceivedGetRequests.Count);
        Assert.Equal(
            firstOwnerId,
            handler.ReceivedGetRequests[0].OwnerId);
        Assert.Equal(
            secondOwnerId,
            handler.ReceivedGetRequests[1].OwnerId);
    }

    private static void AddAuthorization(
        HttpClient client,
        Guid userId)
    {
        var tokenService =
            new JwtAccessTokenService(
                new JwtOptions
                {
                    Issuer = "StudyOrganizer.Api",
                    Audience =
                        "StudyOrganizer.Clients",
                    SigningKey = SigningKey,
                    ExpiresInMinutes = 15
                },
                TimeProvider.System);

        var token = tokenService.Create(
            userId,
            "owner@example.com");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.Value);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IStudyTaskHandler? handler = null)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Database=test;"
            + "Username=test;Password=test");

        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            SigningKey);

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                if (handler is not null)
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<
                            IStudyTaskHandler>();

                        services.AddSingleton(handler);
                    });
                }
            });
    }

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress =
                    new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    private sealed class StubStudyTaskHandler(
        StudyTaskResult? createResult = null,
        IReadOnlyDictionary<
            (Guid OwnerId, Guid ModuleId),
            IReadOnlyList<StudyTaskResult>>?
            tasksByOwnerAndModule = null)
        : IStudyTaskHandler
    {
        public Guid? ReceivedCreateOwnerId
        {
            get;
            private set;
        }

        public Guid? ReceivedCreateModuleId
        {
            get;
            private set;
        }

        public bool CreateWasCalled { get; private set; }

        public List<(Guid OwnerId, Guid ModuleId)>
        ReceivedGetRequests { get; } = [];

        public Task<StudyTaskResult?> CreateAsync(
            Guid ownerId,
            Guid moduleId,
            string title,
            DateTimeOffset dueDateUtc,
            string? description,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CreateWasCalled = true;
            ReceivedCreateOwnerId = ownerId;
            ReceivedCreateModuleId = moduleId;

            return Task.FromResult(createResult);
        }

        public Task<IReadOnlyList<StudyTaskResult>?>
            GetByModuleAsync(
                Guid ownerId,
                Guid moduleId,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReceivedGetRequests.Add(
                (ownerId, moduleId));

            if (tasksByOwnerAndModule is not null
                && tasksByOwnerAndModule.TryGetValue(
                    (ownerId, moduleId),
                    out var tasks))
            {
                return Task.FromResult<
                    IReadOnlyList<StudyTaskResult>?>(tasks);
            }

            return Task.FromResult<
                IReadOnlyList<StudyTaskResult>?>(null);
        }
    }
}
