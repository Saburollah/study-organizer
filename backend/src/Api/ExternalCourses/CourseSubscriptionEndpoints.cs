using System.Security.Claims;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Validation;
using StudyOrganizer.Application.ExternalCourses;
using StudyOrganizer.Domain.ExternalCourses;

namespace StudyOrganizer.Api.ExternalCourses;

public static class CourseSubscriptionEndpoints
{
    public static RouteGroupBuilder MapCourseSubscriptionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/modules/{moduleId:guid}/course-subscription")
            .WithTags("Course subscriptions")
            .RequireAuthorization();

        group.MapPut("/", RegisterAsync)
            .WithName("RegisterCourseSubscription")
            .Produces<CourseSubscriptionResponse>(
                StatusCodes.Status200OK)
            .Produces<CourseSubscriptionResponse>(
                StatusCodes.Status202Accepted)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(
                StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", GetAsync)
            .WithName("GetCourseSubscription")
            .Produces<CourseSubscriptionResponse>(
                StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/", EndAsync)
            .WithName("EndCourseSubscription")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/scans", StartScanAsync)
            .WithName("StartCourseScan")
            .Produces<ScanRunResponse>(StatusCodes.Status200OK)
            .Produces<ScanRunResponse>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/scans/{scanRunId:guid}", GetScanAsync)
            .WithName("GetCourseScan")
            .Produces<ScanRunResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        Guid moduleId,
        RegisterCourseSubscriptionRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ICourseSubscriptionHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var validationErrors = RequestValidator.Validate(request);
        if (!string.IsNullOrWhiteSpace(request.CourseUrl)
            && request.CourseUrl.Length <= 2048
            && (!Uri.TryCreate(
                    request.CourseUrl,
                    UriKind.Absolute,
                    out var absoluteCourseUri)
                || string.IsNullOrWhiteSpace(
                    absoluteCourseUri.Host)))
        {
            validationErrors[nameof(request.CourseUrl)] =
                ["CourseUrl must be an absolute URL."];
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "validation-error"
                });
        }

        var result = await handler.RegisterAsync(
            ownerId,
            moduleId,
            request.CourseUrl,
            cancellationToken);

        switch (result.Outcome)
        {
            case CourseSubscriptionRegistrationOutcome.NotFound:
                return Results.NotFound();
            case CourseSubscriptionRegistrationOutcome
                .UnsupportedCourseUrl:
                return Problem(
                    StatusCodes.Status422UnprocessableEntity,
                    "unsupported-course-url",
                    "The course URL is not supported.");
            case CourseSubscriptionRegistrationOutcome
                .ModuleAlreadySubscribed:
                return Problem(
                    StatusCodes.Status409Conflict,
                    "module-already-subscribed",
                    "The Study Module already has a Course Subscription.");
            case CourseSubscriptionRegistrationOutcome
                .CourseAlreadySubscribed:
                return Problem(
                    StatusCodes.Status409Conflict,
                    "course-already-subscribed",
                    "The External Course is already subscribed.");
        }

        var subscription = result.Subscription
            ?? throw new InvalidOperationException(
                "A successful registration must return a subscription.");
        SetNoStore(httpContext.Response);

        if (result.Outcome ==
            CourseSubscriptionRegistrationOutcome.Running)
        {
            var scanRunId = subscription.LatestScan?.ScanRunId
                ?? throw new InvalidOperationException(
                    "A running registration must expose its Scan Run.");
            SetAcceptedScanHeaders(
                httpContext.Response,
                moduleId,
                scanRunId);
            return Results.Json(
                ToResponse(subscription),
                statusCode: StatusCodes.Status202Accepted);
        }

        return Results.Ok(ToResponse(subscription));
    }

    private static async Task<IResult> GetAsync(
        Guid moduleId,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ICourseSubscriptionHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var subscription = await handler.GetAsync(
            ownerId,
            moduleId,
            cancellationToken);
        if (subscription is null)
        {
            return Results.NotFound();
        }

        SetNoStore(httpContext.Response);
        return Results.Ok(ToResponse(subscription));
    }

    private static async Task<IResult> EndAsync(
        Guid moduleId,
        ClaimsPrincipal user,
        ICourseSubscriptionHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.EndAsync(
            ownerId,
            moduleId,
            cancellationToken);
        return result == CourseSubscriptionEndResult.Ended
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> StartScanAsync(
        Guid moduleId,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ICourseSubscriptionHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.StartScanAsync(
            ownerId,
            moduleId,
            cancellationToken);
        if (result.Outcome == ScanRunRequestOutcome.NotFound)
        {
            return Results.NotFound();
        }

        var scan = result.Scan
            ?? throw new InvalidOperationException(
                "A successful scan request must return a Scan Run.");
        SetNoStore(httpContext.Response);

        if (result.Outcome == ScanRunRequestOutcome.Running)
        {
            SetAcceptedScanHeaders(
                httpContext.Response,
                moduleId,
                scan.ScanRunId);
            return Results.Json(
                ToResponse(scan),
                statusCode: StatusCodes.Status202Accepted);
        }

        return Results.Ok(ToResponse(scan));
    }

    private static async Task<IResult> GetScanAsync(
        Guid moduleId,
        Guid scanRunId,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ICourseSubscriptionHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var scan = await handler.GetScanAsync(
            ownerId,
            moduleId,
            scanRunId,
            cancellationToken);
        if (scan is null)
        {
            return Results.NotFound();
        }

        SetNoStore(httpContext.Response);
        return Results.Ok(ToResponse(scan));
    }

    private static CourseSubscriptionResponse ToResponse(
        CourseSubscriptionResult subscription)
    {
        return new CourseSubscriptionResponse(
            subscription.ModuleId,
            subscription.Status.ToString(),
            subscription.CreatedAtUtc,
            subscription.ActivatedAtUtc,
            new ExternalCourseSummaryResponse(
                subscription.Course.DisplayName,
                subscription.Course.SourceType,
                subscription.Course.SourceUrl),
            subscription.LatestSnapshot is null
                ? null
                : new CourseSnapshotSummaryResponse(
                    subscription.LatestSnapshot.ObservedAtUtc,
                    subscription.LatestSnapshot.KnownContentCount),
            subscription.LatestScan is null
                ? null
                : ToResponse(subscription.LatestScan),
            subscription.RecentScans
                .Select(ToResponse)
                .ToList());
    }

    private static ScanRunResponse ToResponse(
        ScanRunDetailsResult scan)
    {
        return new ScanRunResponse(
            scan.ScanRunId,
            scan.Status.ToString(),
            scan.StartedAtUtc,
            scan.CompletedAtUtc,
            new ScanRunContentCountsResponse(
                scan.ContentCounts.New,
                scan.ContentCounts.Updated,
                scan.ContentCounts.Unchanged,
                scan.ContentCounts.Unavailable),
            new ScanRunPersonalImpactResponse(
                scan.PersonalImpact.TasksCreated,
                scan.PersonalImpact.PdfTasksCreated,
                scan.PersonalImpact.NonPdfTasksCreated,
                scan.PersonalImpact.SourceUpdatesCreated),
            ToErrorCode(scan.ErrorCode),
            scan.CanRetry);
    }

    private static string? ToErrorCode(ScanRunErrorCode? errorCode)
    {
        return errorCode switch
        {
            null => null,
            ScanRunErrorCode.SourceUnreachable => "source-unreachable",
            ScanRunErrorCode.AccessDenied => "access-denied",
            ScanRunErrorCode.Timeout => "timeout",
            ScanRunErrorCode.InvalidSourceData => "invalid-source-data",
            ScanRunErrorCode.PersistenceConflict => "persistence-conflict",
            ScanRunErrorCode.Unexpected => "unexpected",
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode))
        };
    }

    private static IResult Problem(
        int statusCode,
        string code,
        string title)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: title,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code
            });
    }

    private static void SetNoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
    }

    private static void SetAcceptedScanHeaders(
        HttpResponse response,
        Guid moduleId,
        Guid scanRunId)
    {
        response.Headers.Location =
            $"/api/modules/{moduleId}/course-subscription/scans/{scanRunId}";
        response.Headers.RetryAfter = "1";
    }
}
