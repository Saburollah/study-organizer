using System.Security.Claims;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Validation;
using StudyOrganizer.Application.ExternalCourses;

namespace StudyOrganizer.Api.ExternalCourses;

public static class ExternalCourseEndpoints
{
    public static RouteGroupBuilder MapExternalCourseEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/course-subscriptions")
            .WithTags("Moodle Courses")
            .RequireAuthorization();

        group.MapPost("", RegisterAsync)
            .WithName("RegisterCourseSubscription")
            .Produces<CourseSubscriptionResponse>(StatusCodes.Status201Created)
            .Produces<CourseSubscriptionResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("", GetAllAsync)
            .WithName("GetCourseSubscriptions")
            .Produces<IReadOnlyList<CourseSubscriptionResponse>>(
                StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{subscriptionId:guid}/contents", GetContentsAsync)
            .WithName("GetExternalCourseContents")
            .Produces<IReadOnlyList<ExternalCourseContentResponse>>(
                StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{subscriptionId:guid}/scan", ScanAsync)
            .WithName("ScanExternalCourse")
            .Produces<CourseScanResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterCourseSubscriptionRequest request,
        ClaimsPrincipal user,
        IExternalCourseRegistrationHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var validationErrors = RequestValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var result = await handler.RegisterAsync(
            ownerId,
            request.CourseUrl,
            cancellationToken);

        return result.Outcome switch
        {
            CourseRegistrationOutcome.Created => Results.Json(
                ToResponse(result.Subscription!),
                statusCode: StatusCodes.Status201Created),
            CourseRegistrationOutcome.Existing =>
                Results.Ok(ToResponse(result.Subscription!)),
            CourseRegistrationOutcome.UnsupportedUrl => Results.Problem(
                detail: "unsupported_course_url",
                statusCode: StatusCodes.Status400BadRequest),
            _ => Results.Problem(
                detail: "invalid_course_url",
                statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private static async Task<IResult> GetAllAsync(
        ClaimsPrincipal user,
        IExternalCourseQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var subscriptions = await handler.GetByOwnerAsync(
            ownerId,
            cancellationToken);
        return Results.Ok(subscriptions.Select(ToResponse));
    }

    private static async Task<IResult> GetContentsAsync(
        Guid subscriptionId,
        ClaimsPrincipal user,
        IExternalCourseQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var contents = await handler.GetContentsAsync(
            ownerId,
            subscriptionId,
            cancellationToken);
        return contents is null
            ? Results.NotFound()
            : Results.Ok(contents.Select(ToResponse));
    }

    private static async Task<IResult> ScanAsync(
        Guid subscriptionId,
        ClaimsPrincipal user,
        IExternalCourseScanHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.ScanAsync(
            ownerId,
            subscriptionId,
            cancellationToken);

        return result.Outcome switch
        {
            CourseScanOutcome.Succeeded => Results.Ok(new CourseScanResponse(
                result.Outcome.ToString(),
                result.Summary!.NewContentCount,
                result.Summary.ChangedContentCount,
                result.Summary.ReviewRequiredCount,
                result.Summary.NotVisibleCount,
                result.Summary.NewTaskEligibleCount)),
            CourseScanOutcome.NotFound => Results.NotFound(),
            CourseScanOutcome.AlreadyRunning => Results.Problem(
                detail: result.ErrorCode ?? "scan_in_progress",
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(
                detail: result.ErrorCode ?? "external_scan_failed",
                statusCode: StatusCodes.Status502BadGateway)
        };
    }

    private static CourseSubscriptionResponse ToResponse(
        CourseSubscriptionResult subscription) => new(
        subscription.Id,
        subscription.ModuleId,
        subscription.CourseName,
        subscription.ProviderKey,
        subscription.ExternalCourseId,
        subscription.LastScanStatus,
        subscription.LastSuccessfulScanAtUtc);

    private static ExternalCourseContentResponse ToResponse(
        ExternalContentResult content) => new(
        content.Id,
        content.ProviderContentId,
        content.Title,
        content.Description,
        content.SourceUrl,
        content.DueDateUtc,
        content.Status.ToString(),
        content.ReviewReason,
        content.TaskId);
}
