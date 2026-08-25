using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.ExternalCourses;
using StudyOrganizer.Api.Tasks;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Api.Tests.ExternalCourses;

[Collection(CourseImportApiCollection.Name)]
public sealed class CourseImportApiTests(
    CourseImportApiFixture fixture) : IAsyncLifetime
{
    private const string CourseUrl =
        "https://example.test/mock-moodle/course/software-engineering";

    public Task InitializeAsync()
    {
        return fixture.ResetAsync();
    }

    public Task DisposeAsync()
    {
        fixture.CourseSource.ReleaseFetch();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Register_ValidatesCourseUrl()
    {
        var actor = await fixture.CreateUserAndModuleAsync(
            "owner@example.test");
        using var client = CreateClient(actor.UserId);

        var invalidRequests = new object[]
        {
            new { },
            new { courseUrl = "/mock-moodle/course/software-engineering" },
            new { courseUrl = "https://example.test/" + new string('a', 2049) }
        };

        foreach (var request in invalidRequests)
        {
            using var response = await client.PutAsJsonAsync(
                $"/api/modules/{actor.ModuleId}/course-subscription",
                request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(
                "validation-error",
                await ReadProblemCodeAsync(response));
        }

        using var unsupportedResponse = await client.PutAsJsonAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription",
            new
            {
                courseUrl = "https://moodle.example.test/course/17"
            });

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            unsupportedResponse.StatusCode);
        Assert.Equal(
            "unsupported-course-url",
            await ReadProblemCodeAsync(unsupportedResponse));
    }

    [Fact]
    public async Task CourseResources_HideUnknownAndForeignModules()
    {
        var owner = await fixture.CreateUserAndModuleAsync(
            "owner@example.test");
        var stranger = await fixture.CreateUserAndModuleAsync(
            "stranger@example.test",
            "Foreign module");
        using var client = CreateClient(stranger.UserId);

        var resourcePaths = new[]
        {
            $"/api/modules/{owner.ModuleId}/course-subscription",
            $"/api/modules/{Guid.NewGuid()}/course-subscription"
        };

        foreach (var path in resourcePaths)
        {
            using var getResponse = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
            Assert.Equal(
                string.Empty,
                await getResponse.Content.ReadAsStringAsync());

            using var putResponse = await client.PutAsJsonAsync(
                path,
                new { courseUrl = CourseUrl });
            Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
            Assert.Equal(
                string.Empty,
                await putResponse.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task Register_CreatesSafeIdempotentSubscription()
    {
        var actor = await fixture.CreateUserAndModuleAsync(
            "owner@example.test");
        using var client = CreateClient(actor.UserId);

        using var firstResponse = await client.PutAsJsonAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription",
            new
            {
                courseUrl = CourseUrl + "?token=private-registration-value"
            });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.True(firstResponse.Headers.CacheControl?.NoStore);
        var firstBody = await firstResponse.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.NotNull(firstBody);
        Assert.Equal("Active", firstBody.Status);
        Assert.Equal("Software Engineering", firstBody.Course.DisplayName);
        Assert.Equal("mock-moodle", firstBody.Course.SourceType);
        Assert.Equal(CourseUrl, firstBody.Course.SourceUrl);
        Assert.Equal(3, firstBody.LatestSnapshot?.KnownContentCount);
        Assert.Equal("Succeeded", firstBody.LatestScan?.Status);
        Assert.Equal(3, firstBody.LatestScan?.ContentCounts.New);
        Assert.Equal(3, firstBody.LatestScan?.PersonalImpact.TasksCreated);
        Assert.Equal(1, firstBody.LatestScan?.PersonalImpact.PdfTasksCreated);
        Assert.Equal(2, firstBody.LatestScan?.PersonalImpact.NonPdfTasksCreated);

        var rawFirstBody =
            await firstResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(
            "private-registration-value",
            rawFirstBody,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "externalCourseId",
            rawFirstBody,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "activationSubscriptionId",
            rawFirstBody,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "courseWasReused",
            rawFirstBody,
            StringComparison.OrdinalIgnoreCase);

        using var tasksResponse = await client.GetAsync(
            $"/api/modules/{actor.ModuleId}/tasks/");
        var tasks = await tasksResponse.Content
            .ReadFromJsonAsync<List<StudyTaskResponse>>();
        Assert.NotNull(tasks);
        Assert.Equal(3, tasks.Count);
        var pdfTask = Assert.Single(tasks.Where(task =>
            task.ImportSource?.ContentType == "File"));
        Assert.Null(pdfTask.DueDateUtc);
        Assert.Equal("Available", pdfTask.ImportSource?.Status);
        Assert.Equal("application/pdf", pdfTask.ImportSource?.MediaType);
        Assert.Equal(
            "https://example.test/mock-moodle/content/reading.pdf",
            pdfTask.ImportSource?.SourceUrl);
        Assert.DoesNotContain(
            "private",
            await tasksResponse.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);

        using var secondResponse = await client.PutAsJsonAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription",
            new { courseUrl = CourseUrl });
        var secondBody = await secondResponse.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(1, fixture.CourseSource.FetchCount);
        Assert.Equal(
            firstBody.LatestScan?.ScanRunId,
            secondBody?.LatestScan?.ScanRunId);
    }

    [Fact]
    public async Task Register_ReusesActiveCourseWithoutRevealingReuse()
    {
        var first = await fixture.CreateUserAndModuleAsync(
            "first@example.test",
            "Module A");
        var second = await fixture.CreateUserAndModuleAsync(
            "second@example.test",
            "Module B");
        using var firstClient = CreateClient(first.UserId);
        using var secondClient = CreateClient(second.UserId);

        using var firstResponse = await RegisterAsync(
            firstClient,
            first.ModuleId);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using var secondResponse = await secondClient.PutAsJsonAsync(
            $"/api/modules/{second.ModuleId}/course-subscription",
            new
            {
                courseUrl = CourseUrl + "?session=another-private-value"
            });
        var secondBody = await secondResponse.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal("Active", secondBody?.Status);
        Assert.Equal(3, secondBody?.LatestSnapshot?.KnownContentCount);
        Assert.Null(secondBody?.LatestScan);
        Assert.Empty(secondBody?.RecentScans ?? []);
        Assert.Equal(1, fixture.CourseSource.FetchCount);
        Assert.DoesNotContain(
            "reused",
            await secondResponse.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);

        using var tasksResponse = await secondClient.GetAsync(
            $"/api/modules/{second.ModuleId}/tasks/");
        var tasks = await tasksResponse.Content
            .ReadFromJsonAsync<List<StudyTaskResponse>>();
        Assert.Equal(3, tasks?.Count);
    }

    [Fact]
    public async Task Register_ReturnsStableSubscriptionConflictCodes()
    {
        var actor = await fixture.CreateUserAndModuleAsync(
            "owner@example.test",
            "Module A");
        var secondModuleId = await fixture.CreateModuleAsync(
            actor.UserId,
            "Module B");
        using var client = CreateClient(actor.UserId);
        using var registration = await RegisterAsync(
            client,
            actor.ModuleId);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        using var moduleConflict = await client.PutAsJsonAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription",
            new
            {
                courseUrl =
                    "https://example.test/mock-moodle/course/databases"
            });
        Assert.Equal(HttpStatusCode.Conflict, moduleConflict.StatusCode);
        Assert.Equal(
            "module-already-subscribed",
            await ReadProblemCodeAsync(moduleConflict));

        using var courseConflict = await client.PutAsJsonAsync(
            $"/api/modules/{secondModuleId}/course-subscription",
            new { courseUrl = CourseUrl });
        Assert.Equal(HttpStatusCode.Conflict, courseConflict.StatusCode);
        Assert.Equal(
            "course-already-subscribed",
            await ReadProblemCodeAsync(courseConflict));
    }

    [Fact]
    public async Task FailedSetup_RequiresExplicitSafeRetry()
    {
        var actor = await fixture.CreateUserAndModuleAsync(
            "owner@example.test");
        using var client = CreateClient(actor.UserId);
        fixture.CourseSource.FailUnexpectedly();

        using var firstResponse = await RegisterAsync(
            client,
            actor.ModuleId);
        var firstBody = await firstResponse.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal("Pending", firstBody?.Status);
        Assert.Equal("Failed", firstBody?.LatestScan?.Status);
        Assert.Equal("unexpected", firstBody?.LatestScan?.ErrorCode);
        Assert.True(firstBody?.LatestScan?.CanRetry);
        Assert.DoesNotContain(
            "private-value",
            await firstResponse.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);

        using var repeatedRegistration = await RegisterAsync(
            client,
            actor.ModuleId);
        var repeatedBody = await repeatedRegistration.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Equal(HttpStatusCode.OK, repeatedRegistration.StatusCode);
        Assert.Equal(
            firstBody?.LatestScan?.ScanRunId,
            repeatedBody?.LatestScan?.ScanRunId);
        Assert.Equal(1, fixture.CourseSource.FetchCount);

        fixture.CourseSource.ClearFailure();
        using var retryResponse = await client.PostAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription/scans",
            content: null);
        var retryBody = await retryResponse.Content
            .ReadFromJsonAsync<CourseScanResponse>();

        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        Assert.Equal("Succeeded", retryBody?.Status);
        Assert.Equal(3, retryBody?.PersonalImpact.TasksCreated);
        Assert.Equal(2, fixture.CourseSource.FetchCount);

        using var overviewResponse = await client.GetAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription");
        var overview = await overviewResponse.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Equal("Active", overview?.Status);
    }

    [Fact]
    public async Task RunningScan_IsSharedAndSurvivesRequestAbort()
    {
        var actor = await fixture.CreateUserAndModuleAsync(
            "owner@example.test");
        using var client = CreateClient(actor.UserId);
        using var registration = await RegisterAsync(
            client,
            actor.ModuleId);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        fixture.CourseSource.BlockNextFetch();
        using var requestCancellation = new CancellationTokenSource();
        var abandonedRequest = client.PostAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription/scans",
            content: null,
            requestCancellation.Token);
        await fixture.CourseSource.FetchStarted.WaitAsync(
            TimeSpan.FromSeconds(10));

        using var sharedResponse = await client.PostAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription/scans",
            content: null);
        var sharedScan = await sharedResponse.Content
            .ReadFromJsonAsync<CourseScanResponse>();

        Assert.Equal(HttpStatusCode.Accepted, sharedResponse.StatusCode);
        Assert.Equal("Running", sharedScan?.Status);
        Assert.Equal("1", sharedResponse.Headers.RetryAfter?.ToString());
        Assert.Equal(
            $"/api/modules/{actor.ModuleId}/course-subscription/scans/{sharedScan?.ScanRunId}",
            sharedResponse.Headers.Location?.ToString());
        Assert.True(sharedResponse.Headers.CacheControl?.NoStore);
        Assert.Equal(2, fixture.CourseSource.FetchCount);

        requestCancellation.Cancel();
        fixture.CourseSource.ReleaseFetch();
        try
        {
            using var abandonedResponse = await abandonedRequest;
        }
        catch (OperationCanceledException)
        {
            // The client no longer observes the response. The persisted Scan Run
            // remains server-owned and is asserted through its public status URL.
        }

        CourseScanResponse? completedScan = null;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            using var statusResponse = await client.GetAsync(
                $"/api/modules/{actor.ModuleId}/course-subscription/scans/{sharedScan?.ScanRunId}");
            completedScan = await statusResponse.Content
                .ReadFromJsonAsync<CourseScanResponse>();
            if (completedScan?.Status != "Running")
            {
                break;
            }

            await Task.Yield();
        }

        Assert.Equal("Succeeded", completedScan?.Status);
        Assert.Null(completedScan?.ErrorCode);
        Assert.Equal(2, fixture.CourseSource.FetchCount);
    }

    [Fact]
    public async Task ConcurrentRegistration_ReusesSetupScanAndActivatesEachSubscriber()
    {
        var first = await fixture.CreateUserAndModuleAsync(
            "first@example.test",
            "Module A");
        var second = await fixture.CreateUserAndModuleAsync(
            "second@example.test",
            "Module B");
        using var firstClient = CreateClient(first.UserId);
        using var secondClient = CreateClient(second.UserId);
        fixture.CourseSource.BlockNextFetch();

        var firstRegistration = RegisterAsync(
            firstClient,
            first.ModuleId);
        await fixture.CourseSource.FetchStarted.WaitAsync(
            TimeSpan.FromSeconds(10));

        using var sharedRegistration = await RegisterAsync(
            secondClient,
            second.ModuleId);
        var sharedBody = await sharedRegistration.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Equal(
            HttpStatusCode.Accepted,
            sharedRegistration.StatusCode);
        Assert.Equal("Pending", sharedBody?.Status);
        Assert.Equal("Running", sharedBody?.LatestScan?.Status);
        Assert.Equal("1", sharedRegistration.Headers.RetryAfter?.ToString());
        Assert.Equal(1, fixture.CourseSource.FetchCount);

        fixture.CourseSource.ReleaseFetch();
        using var completedFirstRegistration = await firstRegistration;
        var firstBody = await completedFirstRegistration.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Equal(
            sharedBody?.LatestScan?.ScanRunId,
            firstBody?.LatestScan?.ScanRunId);

        using var secondOverviewResponse = await secondClient.GetAsync(
            $"/api/modules/{second.ModuleId}/course-subscription");
        var secondOverview = await secondOverviewResponse.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Equal(HttpStatusCode.OK, secondOverviewResponse.StatusCode);
        Assert.Equal("Active", secondOverview?.Status);
        Assert.Equal(3, secondOverview?.LatestSnapshot?.KnownContentCount);
        Assert.Equal(1, fixture.CourseSource.FetchCount);

        using var secondTasksResponse = await secondClient.GetAsync(
            $"/api/modules/{second.ModuleId}/tasks/");
        var secondTasks = await secondTasksResponse.Content
            .ReadFromJsonAsync<List<StudyTaskResponse>>();
        Assert.Equal(3, secondTasks?.Count);
    }

    [Fact]
    public async Task SourceUpdate_AcknowledgementIsSafeAndIdempotent()
    {
        var actor = await fixture.CreateUserAndModuleAsync(
            "owner@example.test");
        using var client = CreateClient(actor.UserId);
        using var registration = await RegisterAsync(
            client,
            actor.ModuleId);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        using var initialTasksResponse = await client.GetAsync(
            $"/api/modules/{actor.ModuleId}/tasks/");
        var initialTasks = await initialTasksResponse.Content
            .ReadFromJsonAsync<List<StudyTaskResponse>>();
        var importedPdfTask = Assert.Single(
            initialTasks!.Where(task =>
                task.ImportSource?.MediaType == "application/pdf"));

        using var personalUpdate = await client.PutAsJsonAsync(
            $"/api/modules/{actor.ModuleId}/tasks/{importedPdfTask.Id}",
            new
            {
                title = "My personal reading plan",
                description = "Keep these notes",
                dueDateUtc = (DateTimeOffset?)null
            });
        Assert.Equal(HttpStatusCode.OK, personalUpdate.StatusCode);
        using var completion = await client.PatchAsJsonAsync(
            $"/api/modules/{actor.ModuleId}/tasks/{importedPdfTask.Id}/status",
            new { status = "Completed" });
        Assert.Equal(HttpStatusCode.OK, completion.StatusCode);

        fixture.CourseSource.UseSnapshot(
            new CourseSourceSnapshot(
            [
                new CourseSourceItem(
                    new ExternalContentKey("reading-pdf"),
                    ExternalLearningContentType.File,
                    "Renamed architecture reading",
                    DateTimeOffset.Parse("2026-10-01T18:00:00Z"),
                    "application/pdf",
                    "/mock-moodle/content/renamed.pdf?token=secret"),
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
            ]));

        using var scanResponse = await client.PostAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription/scans",
            content: null);
        var scan = await scanResponse.Content
            .ReadFromJsonAsync<CourseScanResponse>();
        Assert.Equal(HttpStatusCode.OK, scanResponse.StatusCode);
        Assert.Equal(1, scan?.ContentCounts.Updated);
        Assert.Equal(1, scan?.PersonalImpact.SourceUpdatesCreated);

        using var changedTasksResponse = await client.GetAsync(
            $"/api/modules/{actor.ModuleId}/tasks/");
        var changedTasks = await changedTasksResponse.Content
            .ReadFromJsonAsync<List<StudyTaskResponse>>();
        var changedTask = Assert.Single(changedTasks!.Where(task =>
            task.Id == importedPdfTask.Id));
        Assert.Equal("My personal reading plan", changedTask.Title);
        Assert.Equal("Keep these notes", changedTask.Description);
        Assert.Null(changedTask.DueDateUtc);
        Assert.Equal("Completed", changedTask.Status);
        Assert.True(changedTask.ImportSource?.HasSourceUpdate);
        Assert.Equal(
            "https://example.test/mock-moodle/content/renamed.pdf",
            changedTask.ImportSource?.SourceUrl);
        Assert.DoesNotContain(
            "secret",
            await changedTasksResponse.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var acknowledgeResponse = await client.PostAsync(
                $"/api/modules/{actor.ModuleId}/tasks/{importedPdfTask.Id}/source-update/acknowledge",
                content: null);
            var acknowledgedTask = await acknowledgeResponse.Content
                .ReadFromJsonAsync<StudyTaskResponse>();
            Assert.Equal(HttpStatusCode.OK, acknowledgeResponse.StatusCode);
            Assert.False(acknowledgedTask?.ImportSource?.HasSourceUpdate);
            Assert.Equal(
                "My personal reading plan",
                acknowledgedTask?.Title);
        }

        using var createTaskResponse = await client.PostAsJsonAsync(
            $"/api/modules/{actor.ModuleId}/tasks/",
            new { title = "Personal task without a due date" });
        var personalTask = await createTaskResponse.Content
            .ReadFromJsonAsync<StudyTaskResponse>();
        Assert.Equal(HttpStatusCode.Created, createTaskResponse.StatusCode);
        Assert.Null(personalTask?.DueDateUtc);
        Assert.Null(personalTask?.ImportSource);

        using var conflictResponse = await client.PostAsync(
            $"/api/modules/{actor.ModuleId}/tasks/{personalTask?.Id}/source-update/acknowledge",
            content: null);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal(
            "task-not-imported",
            await ReadProblemCodeAsync(conflictResponse));
    }

    [Fact]
    public async Task Overview_ReturnsOnlyTenScansSinceOwnActivation()
    {
        var first = await fixture.CreateUserAndModuleAsync(
            "first@example.test",
            "Module A");
        var second = await fixture.CreateUserAndModuleAsync(
            "second@example.test",
            "Module B");
        using var firstClient = CreateClient(first.UserId);
        using var secondClient = CreateClient(second.UserId);
        using var registration = await RegisterAsync(
            firstClient,
            first.ModuleId);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        for (var scanNumber = 0; scanNumber < 11; scanNumber++)
        {
            using var scanResponse = await firstClient.PostAsync(
                $"/api/modules/{first.ModuleId}/course-subscription/scans",
                content: null);
            Assert.Equal(HttpStatusCode.OK, scanResponse.StatusCode);
        }

        using var firstOverviewResponse = await firstClient.GetAsync(
            $"/api/modules/{first.ModuleId}/course-subscription");
        var firstOverview = await firstOverviewResponse.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Equal(10, firstOverview?.RecentScans.Count);
        Assert.Equal(
            firstOverview?.LatestScan?.ScanRunId,
            firstOverview?.RecentScans[0].ScanRunId);
        Assert.True(firstOverview?.RecentScans
            .Zip(firstOverview.RecentScans.Skip(1))
            .All(pair =>
                pair.First.StartedAtUtc >= pair.Second.StartedAtUtc));

        using var secondRegistration = await RegisterAsync(
            secondClient,
            second.ModuleId);
        var secondRegistrationBody = await secondRegistration.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Empty(secondRegistrationBody?.RecentScans ?? []);

        using var sharedScanResponse = await firstClient.PostAsync(
            $"/api/modules/{first.ModuleId}/course-subscription/scans",
            content: null);
        var sharedScan = await sharedScanResponse.Content
            .ReadFromJsonAsync<CourseScanResponse>();
        Assert.DoesNotContain(
            second.ModuleId.ToString(),
            await sharedScanResponse.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);

        using var secondOverviewResponse = await secondClient.GetAsync(
            $"/api/modules/{second.ModuleId}/course-subscription");
        var secondOverview = await secondOverviewResponse.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        var visibleScan = Assert.Single(
            secondOverview?.RecentScans ?? []);
        Assert.Equal(sharedScan?.ScanRunId, visibleScan.ScanRunId);
        Assert.Equal(0, visibleScan.PersonalImpact.TasksCreated);
    }

    [Fact]
    public async Task EndSubscription_IsIdempotentAndPreservesPersonalTasks()
    {
        var first = await fixture.CreateUserAndModuleAsync(
            "first@example.test",
            "Module A");
        var second = await fixture.CreateUserAndModuleAsync(
            "second@example.test",
            "Module B");
        using var firstClient = CreateClient(first.UserId);
        using var secondClient = CreateClient(second.UserId);
        using var firstRegistration = await RegisterAsync(
            firstClient,
            first.ModuleId);
        var firstRegistrationBody = await firstRegistration.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        using var secondRegistration = await RegisterAsync(
            secondClient,
            second.ModuleId);
        Assert.Equal(HttpStatusCode.OK, secondRegistration.StatusCode);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var endResponse = await firstClient.DeleteAsync(
                $"/api/modules/{first.ModuleId}/course-subscription");
            Assert.Equal(HttpStatusCode.NoContent, endResponse.StatusCode);
        }

        using var hiddenOverview = await firstClient.GetAsync(
            $"/api/modules/{first.ModuleId}/course-subscription");
        Assert.Equal(HttpStatusCode.NotFound, hiddenOverview.StatusCode);
        Assert.Equal(
            string.Empty,
            await hiddenOverview.Content.ReadAsStringAsync());

        using var hiddenScan = await firstClient.GetAsync(
            $"/api/modules/{first.ModuleId}/course-subscription/scans/{firstRegistrationBody?.LatestScan?.ScanRunId}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenScan.StatusCode);

        using var firstTasksResponse = await firstClient.GetAsync(
            $"/api/modules/{first.ModuleId}/tasks/");
        var firstTasks = await firstTasksResponse.Content
            .ReadFromJsonAsync<List<StudyTaskResponse>>();
        Assert.Equal(3, firstTasks?.Count);
        Assert.All(firstTasks!, task =>
        {
            Assert.Equal(
                "SubscriptionEnded",
                task.ImportSource?.Status);
            Assert.Null(task.ImportSource?.ContentType);
            Assert.Null(task.ImportSource?.MediaType);
            Assert.Null(task.ImportSource?.SourceUrl);
        });

        using var secondOverview = await secondClient.GetAsync(
            $"/api/modules/{second.ModuleId}/course-subscription");
        Assert.Equal(HttpStatusCode.OK, secondOverview.StatusCode);
        using var secondScan = await secondClient.PostAsync(
            $"/api/modules/{second.ModuleId}/course-subscription/scans",
            content: null);
        Assert.Equal(HttpStatusCode.OK, secondScan.StatusCode);
    }

    [Fact]
    public async Task ReactivateInactiveCourse_UsesFreshScanWithoutTaskDuplicates()
    {
        var actor = await fixture.CreateUserAndModuleAsync(
            "owner@example.test");
        var emptyModuleId = await fixture.CreateModuleAsync(
            actor.UserId,
            "Module without subscription");
        using var client = CreateClient(actor.UserId);
        using var registration = await RegisterAsync(
            client,
            actor.ModuleId);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        Assert.Equal(1, fixture.CourseSource.FetchCount);

        using var emptyEnd = await client.DeleteAsync(
            $"/api/modules/{emptyModuleId}/course-subscription");
        Assert.Equal(HttpStatusCode.NoContent, emptyEnd.StatusCode);

        using var endResponse = await client.DeleteAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription");
        Assert.Equal(HttpStatusCode.NoContent, endResponse.StatusCode);

        using var reactivation = await RegisterAsync(
            client,
            actor.ModuleId);
        var reactivationBody = await reactivation.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Equal(HttpStatusCode.OK, reactivation.StatusCode);
        Assert.Equal("Active", reactivationBody?.Status);
        Assert.Equal("Succeeded", reactivationBody?.LatestScan?.Status);
        Assert.Equal(2, fixture.CourseSource.FetchCount);

        using var tasksResponse = await client.GetAsync(
            $"/api/modules/{actor.ModuleId}/tasks/");
        var tasks = await tasksResponse.Content
            .ReadFromJsonAsync<List<StudyTaskResponse>>();
        Assert.Equal(3, tasks?.Count);

        using var repeatedReactivation = await RegisterAsync(
            client,
            actor.ModuleId);
        Assert.Equal(
            HttpStatusCode.OK,
            repeatedReactivation.StatusCode);
        Assert.Equal(2, fixture.CourseSource.FetchCount);
    }

    [Fact]
    public async Task PendingReactivation_ExposesOnlyItsSetupScan()
    {
        var actor = await fixture.CreateUserAndModuleAsync(
            "owner@example.test");
        using var client = CreateClient(actor.UserId);
        using var registration = await RegisterAsync(
            client,
            actor.ModuleId);
        var initialSubscription = await registration.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        var previousScanRunId =
            initialSubscription!.LatestScan!.ScanRunId;

        using var endResponse = await client.DeleteAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription");
        Assert.Equal(HttpStatusCode.NoContent, endResponse.StatusCode);
        fixture.CourseSource.FailWith(ScanRunErrorCode.AccessDenied);

        using var failedReactivation = await RegisterAsync(
            client,
            actor.ModuleId);
        var pendingSubscription = await failedReactivation.Content
            .ReadFromJsonAsync<CourseSubscriptionResponse>();
        Assert.Equal("Pending", pendingSubscription?.Status);
        var setupScan = Assert.Single(
            pendingSubscription?.RecentScans ?? []);
        Assert.Equal("Failed", setupScan.Status);
        Assert.NotEqual(previousScanRunId, setupScan.ScanRunId);

        using var previousScanResponse = await client.GetAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription/scans/{previousScanRunId}");
        Assert.Equal(
            HttpStatusCode.NotFound,
            previousScanResponse.StatusCode);

        using var setupScanResponse = await client.GetAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription/scans/{setupScan.ScanRunId}");
        Assert.Equal(HttpStatusCode.OK, setupScanResponse.StatusCode);
    }

    [Fact]
    public async Task ScanStatus_ContainsOnlyRequestingSubscriptionsImpact()
    {
        var first = await fixture.CreateUserAndModuleAsync(
            "first@example.test",
            "Module A");
        var second = await fixture.CreateUserAndModuleAsync(
            "second@example.test",
            "Module B");
        var stranger = await fixture.CreateUserAndModuleAsync(
            "stranger@example.test",
            "Module C");
        using var firstClient = CreateClient(first.UserId);
        using var secondClient = CreateClient(second.UserId);
        using var strangerClient = CreateClient(stranger.UserId);
        using var firstRegistration = await RegisterAsync(
            firstClient,
            first.ModuleId);
        using var secondRegistration = await RegisterAsync(
            secondClient,
            second.ModuleId);
        Assert.Equal(HttpStatusCode.OK, firstRegistration.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondRegistration.StatusCode);

        fixture.CourseSource.UseSnapshot(CreateSnapshotWithNewContent());
        using var scanResponse = await firstClient.PostAsync(
            $"/api/modules/{first.ModuleId}/course-subscription/scans",
            content: null);
        var firstScan = await scanResponse.Content
            .ReadFromJsonAsync<CourseScanResponse>();

        Assert.Equal(HttpStatusCode.OK, scanResponse.StatusCode);
        Assert.Equal(1, firstScan?.ContentCounts.New);
        Assert.Equal(1, firstScan?.PersonalImpact.TasksCreated);
        Assert.Equal(1, firstScan?.PersonalImpact.NonPdfTasksCreated);
        var rawScan = await scanResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(
            second.ModuleId.ToString(),
            rawScan,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            second.UserId.ToString(),
            rawScan,
            StringComparison.OrdinalIgnoreCase);

        using var secondStatusResponse = await secondClient.GetAsync(
            $"/api/modules/{second.ModuleId}/course-subscription/scans/{firstScan?.ScanRunId}");
        var secondScan = await secondStatusResponse.Content
            .ReadFromJsonAsync<CourseScanResponse>();
        Assert.Equal(HttpStatusCode.OK, secondStatusResponse.StatusCode);
        Assert.Equal(1, secondScan?.PersonalImpact.TasksCreated);

        using var hiddenStatusResponse = await strangerClient.GetAsync(
            $"/api/modules/{stranger.ModuleId}/course-subscription/scans/{firstScan?.ScanRunId}");
        Assert.Equal(
            HttpStatusCode.NotFound,
            hiddenStatusResponse.StatusCode);
        Assert.Equal(
            string.Empty,
            await hiddenStatusResponse.Content.ReadAsStringAsync());

        using var unknownStatusResponse = await firstClient.GetAsync(
            $"/api/modules/{first.ModuleId}/course-subscription/scans/{Guid.NewGuid()}");
        Assert.Equal(
            HttpStatusCode.NotFound,
            unknownStatusResponse.StatusCode);
        Assert.Equal(
            string.Empty,
            await unknownStatusResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteModule_RemovesOnlyItsCourseSubscription()
    {
        var first = await fixture.CreateUserAndModuleAsync(
            "first@example.test",
            "Module A");
        var second = await fixture.CreateUserAndModuleAsync(
            "second@example.test",
            "Module B");
        using var firstClient = CreateClient(first.UserId);
        using var secondClient = CreateClient(second.UserId);
        using var firstRegistration = await RegisterAsync(
            firstClient,
            first.ModuleId);
        using var secondRegistration = await RegisterAsync(
            secondClient,
            second.ModuleId);
        Assert.Equal(HttpStatusCode.OK, firstRegistration.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondRegistration.StatusCode);

        using var deleteModuleResponse = await firstClient.DeleteAsync(
            $"/api/modules/{first.ModuleId}");
        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteModuleResponse.StatusCode);

        using var removedTasksResponse = await firstClient.GetAsync(
            $"/api/modules/{first.ModuleId}/tasks/");
        Assert.Equal(HttpStatusCode.NotFound, removedTasksResponse.StatusCode);

        using var secondOverviewResponse = await secondClient.GetAsync(
            $"/api/modules/{second.ModuleId}/course-subscription");
        Assert.Equal(HttpStatusCode.OK, secondOverviewResponse.StatusCode);
        using var secondScanResponse = await secondClient.PostAsync(
            $"/api/modules/{second.ModuleId}/course-subscription/scans",
            content: null);
        Assert.Equal(HttpStatusCode.OK, secondScanResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteImportedTask_PreservesDismissedImportDecision()
    {
        var actor = await fixture.CreateUserAndModuleAsync(
            "owner@example.test");
        using var client = CreateClient(actor.UserId);
        using var registration = await RegisterAsync(
            client,
            actor.ModuleId);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        using var initialTasksResponse = await client.GetAsync(
            $"/api/modules/{actor.ModuleId}/tasks/");
        var initialTasks = await initialTasksResponse.Content
            .ReadFromJsonAsync<List<StudyTaskResponse>>();
        var dismissedTask = initialTasks![0];

        using var deleteTaskResponse = await client.DeleteAsync(
            $"/api/modules/{actor.ModuleId}/tasks/{dismissedTask.Id}");
        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteTaskResponse.StatusCode);

        using var repeatedScanResponse = await client.PostAsync(
            $"/api/modules/{actor.ModuleId}/course-subscription/scans",
            content: null);
        Assert.Equal(HttpStatusCode.OK, repeatedScanResponse.StatusCode);

        using var remainingTasksResponse = await client.GetAsync(
            $"/api/modules/{actor.ModuleId}/tasks/");
        var remainingTasks = await remainingTasksResponse.Content
            .ReadFromJsonAsync<List<StudyTaskResponse>>();
        Assert.Equal(2, remainingTasks?.Count);
        Assert.DoesNotContain(
            remainingTasks!,
            task => task.Id == dismissedTask.Id);
    }

    private HttpClient CreateClient(Guid userId)
    {
        var client = fixture.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
        var tokenService = new JwtAccessTokenService(
            new JwtOptions
            {
                Issuer = "StudyOrganizer.Api",
                Audience = "StudyOrganizer.Clients",
                SigningKey = CourseImportApiFixture.SigningKey,
                ExpiresInMinutes = 15
            },
            TimeProvider.System);
        var token = tokenService.Create(
            userId,
            "owner@example.test");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        Guid moduleId)
    {
        return client.PutAsJsonAsync(
            $"/api/modules/{moduleId}/course-subscription",
            new { courseUrl = CourseUrl });
    }

    private static async Task<string?> ReadProblemCodeAsync(
        HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        return body.RootElement.TryGetProperty("code", out var code)
            ? code.GetString()
            : null;
    }

    private static CourseSourceSnapshot CreateSnapshotWithNewContent()
    {
        return new CourseSourceSnapshot(
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
                null),
            new CourseSourceItem(
                new ExternalContentKey("new-link"),
                ExternalLearningContentType.Link,
                "New reference",
                null,
                null,
                "/mock-moodle/content/new-reference?token=private")
        ]);
    }
}
