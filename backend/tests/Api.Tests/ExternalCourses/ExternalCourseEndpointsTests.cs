using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.ExternalCourses;
using StudyOrganizer.Application.ExternalCourses;

namespace StudyOrganizer.Api.Tests.ExternalCourses;

public sealed class ExternalCourseEndpointsTests
{
    private static readonly string SigningKey = new('a', 64);
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private static readonly Guid ModuleId = Guid.NewGuid();

    [Theory]
    [InlineData("POST", "/api/course-subscriptions")]
    [InlineData("GET", "/api/course-subscriptions")]
    [InlineData("GET", "/api/course-subscriptions/11111111-1111-1111-1111-111111111111/contents")]
    [InlineData("POST", "/api/course-subscriptions/11111111-1111-1111-1111-111111111111/scan")]
    public async Task Routes_WithoutToken_ReturnUnauthorized(string method, string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST" && path == "/api/course-subscriptions")
        {
            request.Content = JsonContent.Create(new { courseUrl = FixtureUrl });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_NewSubscription_ReturnsCreated()
    {
        var ownerId = Guid.NewGuid();
        var handler = new StubRegistrationHandler(new CourseRegistrationResult(
            CourseRegistrationOutcome.Created,
            Subscription));
        using var factory = CreateFactory(registrationHandler: handler);
        using var client = CreateAuthorizedClient(factory, ownerId);

        var response = await client.PostAsJsonAsync(
            "/api/course-subscriptions",
            new { courseUrl = FixtureUrl });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Equal(SubscriptionId, body!.Id);
        Assert.Equal(ModuleId, body.ModuleId);
        Assert.Equal("Software Engineering", body.CourseName);
        Assert.Equal(ownerId, handler.ReceivedOwnerId);
        Assert.Equal(FixtureUrl, handler.ReceivedCourseUrl);
    }

    [Fact]
    public async Task Register_ExistingSubscription_ReturnsOk()
    {
        var handler = new StubRegistrationHandler(new CourseRegistrationResult(
            CourseRegistrationOutcome.Existing,
            Subscription));
        using var factory = CreateFactory(registrationHandler: handler);
        using var client = CreateAuthorizedClient(factory, Guid.NewGuid());

        var response = await client.PostAsJsonAsync(
            "/api/course-subscriptions",
            new { courseUrl = FixtureUrl });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            SubscriptionId,
            (await response.Content.ReadFromJsonAsync<CourseSubscriptionResponse>())!.Id);
    }

    [Theory]
    [InlineData(CourseRegistrationOutcome.InvalidUrl, "invalid_course_url")]
    [InlineData(CourseRegistrationOutcome.UnsupportedUrl, "unsupported_course_url")]
    public async Task Register_RejectedUrl_ReturnsBadRequestWithSafeDetail(
        CourseRegistrationOutcome outcome,
        string detail)
    {
        var handler = new StubRegistrationHandler(
            new CourseRegistrationResult(outcome, null));
        using var factory = CreateFactory(registrationHandler: handler);
        using var client = CreateAuthorizedClient(factory, Guid.NewGuid());

        var response = await client.PostAsJsonAsync(
            "/api/course-subscriptions",
            new { courseUrl = FixtureUrl });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(detail, problem!.Detail);
    }

    [Fact]
    public async Task Register_MalformedUrl_ReturnsValidationProblemWithoutCallingHandler()
    {
        var handler = new StubRegistrationHandler(new CourseRegistrationResult(
            CourseRegistrationOutcome.Created,
            Subscription));
        using var factory = CreateFactory(registrationHandler: handler);
        using var client = CreateAuthorizedClient(factory, Guid.NewGuid());

        var response = await client.PostAsJsonAsync(
            "/api/course-subscriptions",
            new { courseUrl = "not a URL" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(handler.ReceivedOwnerId);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyOwnersSubscriptions()
    {
        var ownerId = Guid.NewGuid();
        var handler = new StubQueryHandler([Subscription], []);
        using var factory = CreateFactory(queryHandler: handler);
        using var client = CreateAuthorizedClient(factory, ownerId);

        var response = await client.GetAsync("/api/course-subscriptions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            SubscriptionId,
            Assert.Single(
                (await response.Content.ReadFromJsonAsync<List<CourseSubscriptionResponse>>())!).Id);
        Assert.Equal(ownerId, handler.ReceivedListOwnerId);
    }

    [Fact]
    public async Task GetContents_OwnedSubscription_ReturnsNormalizedContents()
    {
        var ownerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var content = new ExternalContentResult(
            Guid.NewGuid(),
            "exercise-1",
            "Exercise 1",
            "Read chapter one",
            "https://mock-moodle.local/content/exercise-1",
            new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero),
            ExternalContentDisplayStatus.TaskCreated,
            null,
            taskId);
        var handler = new StubQueryHandler([Subscription], [content]);
        using var factory = CreateFactory(queryHandler: handler);
        using var client = CreateAuthorizedClient(factory, ownerId);

        var response = await client.GetAsync(
            $"/api/course-subscriptions/{SubscriptionId}/contents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = Assert.Single(
            (await response.Content.ReadFromJsonAsync<List<ExternalCourseContentResponse>>())!);
        Assert.Equal("exercise-1", body.ProviderContentId);
        Assert.Equal("TaskCreated", body.Status);
        Assert.Equal(taskId, body.TaskId);
        Assert.Equal((ownerId, SubscriptionId), handler.ReceivedContentsRequest);
    }

    [Fact]
    public async Task GetContents_ForeignSubscription_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var handler = new StubQueryHandler([Subscription], null);
        using var factory = CreateFactory(queryHandler: handler);
        using var client = CreateAuthorizedClient(factory, ownerId);

        var response = await client.GetAsync(
            $"/api/course-subscriptions/{SubscriptionId}/contents");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal((ownerId, SubscriptionId), handler.ReceivedContentsRequest);
    }

    [Fact]
    public async Task Scan_WhenSucceeded_ReturnsSummary()
    {
        var handler = new StubScanHandler(new CourseScanResult(
            CourseScanOutcome.Succeeded,
            new CourseScanSummary(2, 1, 3, 4, 1),
            null));
        using var factory = CreateFactory(scanHandler: handler);
        using var client = CreateAuthorizedClient(factory, Guid.NewGuid());

        var response = await client.PostAsync(
            $"/api/course-subscriptions/{SubscriptionId}/scan",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CourseScanResponse>();
        Assert.Equal("Succeeded", body!.Status);
        Assert.Equal(2, body.NewContentCount);
        Assert.Equal(1, body.ChangedContentCount);
        Assert.Equal(3, body.ReviewRequiredCount);
        Assert.Equal(4, body.NotVisibleCount);
        Assert.Equal(1, body.NewTaskEligibleCount);
    }

    [Fact]
    public async Task Scan_WhenAlreadyRunning_ReturnsConflict()
    {
        var handler = new StubScanHandler(new CourseScanResult(
            CourseScanOutcome.AlreadyRunning,
            null,
            "scan_in_progress"));
        using var factory = CreateFactory(scanHandler: handler);
        using var client = CreateAuthorizedClient(factory, Guid.NewGuid());

        var response = await client.PostAsync(
            $"/api/course-subscriptions/{Guid.NewGuid()}/scan",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("scan_in_progress", problem!.Detail);
    }

    [Theory]
    [InlineData(CourseScanOutcome.ExternalFailure, "external_timeout")]
    [InlineData(CourseScanOutcome.InvalidSnapshot, "invalid_external_response")]
    public async Task Scan_WhenExternalStateIsUnsafe_ReturnsBadGatewayWithSafeDetail(
        CourseScanOutcome outcome,
        string errorCode)
    {
        var handler = new StubScanHandler(
            new CourseScanResult(outcome, null, errorCode));
        using var factory = CreateFactory(scanHandler: handler);
        using var client = CreateAuthorizedClient(factory, Guid.NewGuid());

        var response = await client.PostAsync(
            $"/api/course-subscriptions/{SubscriptionId}/scan",
            null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(errorCode, problem!.Detail);
    }

    [Fact]
    public async Task Scan_ForeignSubscription_ReturnsNotFound()
    {
        var handler = new StubScanHandler(new CourseScanResult(
            CourseScanOutcome.NotFound,
            null,
            null));
        using var factory = CreateFactory(scanHandler: handler);
        using var client = CreateAuthorizedClient(factory, Guid.NewGuid());

        var response = await client.PostAsync(
            $"/api/course-subscriptions/{SubscriptionId}/scan",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private const string FixtureUrl =
        "https://mock-moodle.local/courses/software-engineering-2026";

    private static CourseSubscriptionResult Subscription => new(
        SubscriptionId,
        ModuleId,
        "Software Engineering",
        "mock-moodle",
        "software-engineering-2026",
        "NeverScanned",
        null);

    private static WebApplicationFactory<Program> CreateFactory(
        IExternalCourseRegistrationHandler? registrationHandler = null,
        IExternalCourseQueryHandler? queryHandler = null,
        IExternalCourseScanHandler? scanHandler = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "StudyOrganizer.Api",
            ["Jwt:Audience"] = "StudyOrganizer.Clients",
            ["Jwt:SigningKey"] = SigningKey,
            ["Jwt:ExpiresInMinutes"] = "15",
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Database=test;Username=test;Password=test"
        };

        // Program validates these two settings before the test host applies its
        // ConfigureAppConfiguration callbacks. Match the existing API-test
        // bootstrap while retaining the complete isolated in-memory settings.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            settings["ConnectionStrings:DefaultConnection"]);
        Environment.SetEnvironmentVariable("Jwt__Issuer", settings["Jwt:Issuer"]);
        Environment.SetEnvironmentVariable("Jwt__Audience", settings["Jwt:Audience"]);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
        Environment.SetEnvironmentVariable(
            "Jwt__ExpiresInMinutes",
            settings["Jwt:ExpiresInMinutes"]);

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(settings));
                builder.ConfigureServices(services =>
                {
                    if (registrationHandler is not null)
                    {
                        services.RemoveAll<IExternalCourseRegistrationHandler>();
                        services.AddSingleton(registrationHandler);
                    }

                    if (queryHandler is not null)
                    {
                        services.RemoveAll<IExternalCourseQueryHandler>();
                        services.AddSingleton(queryHandler);
                    }

                    if (scanHandler is not null)
                    {
                        services.RemoveAll<IExternalCourseScanHandler>();
                        services.AddSingleton(scanHandler);
                    }
                });
            });
    }

    private static HttpClient CreateAuthorizedClient(
        WebApplicationFactory<Program> factory,
        Guid ownerId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        var token = new JwtAccessTokenService(
            new JwtOptions
            {
                Issuer = "StudyOrganizer.Api",
                Audience = "StudyOrganizer.Clients",
                SigningKey = SigningKey,
                ExpiresInMinutes = 15
            },
            TimeProvider.System).Create(ownerId, "owner@example.com");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private sealed class StubRegistrationHandler(CourseRegistrationResult result)
        : IExternalCourseRegistrationHandler
    {
        public Guid? ReceivedOwnerId { get; private set; }
        public string? ReceivedCourseUrl { get; private set; }

        public Task<CourseRegistrationResult> RegisterAsync(
            Guid ownerId,
            string courseUrl,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReceivedOwnerId = ownerId;
            ReceivedCourseUrl = courseUrl;
            return Task.FromResult(result);
        }
    }

    private sealed class StubQueryHandler(
        IReadOnlyList<CourseSubscriptionResult> subscriptions,
        IReadOnlyList<ExternalContentResult>? contents)
        : IExternalCourseQueryHandler
    {
        public Guid? ReceivedListOwnerId { get; private set; }
        public (Guid OwnerId, Guid SubscriptionId)? ReceivedContentsRequest { get; private set; }

        public Task<IReadOnlyList<CourseSubscriptionResult>> GetByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReceivedListOwnerId = ownerId;
            return Task.FromResult(subscriptions);
        }

        public Task<IReadOnlyList<ExternalContentResult>?> GetContentsAsync(
            Guid ownerId,
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReceivedContentsRequest = (ownerId, subscriptionId);
            return Task.FromResult(contents);
        }
    }

    private sealed class StubScanHandler(CourseScanResult result)
        : IExternalCourseScanHandler
    {
        public Task<CourseScanResult> ScanAsync(
            Guid ownerId,
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }
}
