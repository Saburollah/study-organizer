using System.Security.Claims;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Validation;
using StudyOrganizer.Application.Tasks;

namespace StudyOrganizer.Api.Tasks;

public static class StudyTaskEndpoints
{
    public static RouteGroupBuilder MapStudyTaskEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/modules/{moduleId:guid}/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .WithName("CreateStudyTask")
            .Produces<StudyTaskResponse>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        group.MapGet("/", GetByModuleAsync)
            .WithName("GetStudyTasks")
            .Produces<IReadOnlyList<StudyTaskResponse>>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> CreateAsync(
        Guid moduleId,
        CreateStudyTaskRequest request,
        ClaimsPrincipal user,
        IStudyTaskHandler taskHandler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var validationErrors =
            RequestValidator.Validate(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors);
        }

        var task = await taskHandler.CreateAsync(
            ownerId,
            moduleId,
            request.Title,
            request.DueDateUtc!.Value,
            request.Description,
            cancellationToken);

        if (task is null)
        {
            return Results.NotFound();
        }

        return Results.Json(
            ToResponse(task),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetByModuleAsync(
        Guid moduleId,
        ClaimsPrincipal user,
        IStudyTaskHandler taskHandler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var tasks =
            await taskHandler.GetByModuleAsync(
                ownerId,
                moduleId,
                cancellationToken);

        if (tasks is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(
            tasks.Select(ToResponse));
    }

    private static StudyTaskResponse ToResponse(
        StudyTaskResult task)
    {
        return new StudyTaskResponse(
            task.Id,
            task.ModuleId,
            task.Title,
            task.Description,
            task.DueDateUtc,
            task.Status.ToString(),
            task.CreatedAtUtc,
            task.UpdatedAtUtc);
    }
}
