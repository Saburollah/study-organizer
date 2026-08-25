using System.Security.Claims;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Validation;
using StudyOrganizer.Application.Tasks;
using StudyOrganizer.Domain.Tasks;

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

        group.MapPut("/{taskId:guid}", UpdateAsync)
            .WithName("UpdateStudyTask")
            .Produces<StudyTaskResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        group.MapPatch(
                "/{taskId:guid}/status",
                UpdateStatusAsync)
            .WithName("UpdateStudyTaskStatus")
            .Produces<StudyTaskResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        group.MapDelete("/{taskId:guid}", DeleteAsync)
            .WithName("DeleteStudyTask")
            .Produces(
                StatusCodes.Status204NoContent)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        group.MapPost(
                "/{taskId:guid}/source-update/acknowledge",
                AcknowledgeSourceUpdateAsync)
            .WithName("AcknowledgeStudyTaskSourceUpdate")
            .Produces<StudyTaskResponse>(
                StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

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
            request.DueDateUtc,
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

    private static async Task<IResult> UpdateAsync(
        Guid moduleId,
        Guid taskId,
        UpdateStudyTaskRequest request,
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

        var task = await taskHandler.UpdateAsync(
            ownerId,
            moduleId,
            taskId,
            request.Title,
            request.DueDateUtc,
            request.Description,
            cancellationToken);

        return task is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> UpdateStatusAsync(
        Guid moduleId,
        Guid taskId,
        UpdateStudyTaskStatusRequest request,
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

        var isValidStatus =
            Enum.TryParse<StudyTaskStatus>(
                request.Status,
                ignoreCase: true,
                out var status)
            && Enum.IsDefined(status)
            && Enum.GetNames<StudyTaskStatus>().Any(name =>
                string.Equals(
                    name,
                    request.Status,
                    StringComparison.OrdinalIgnoreCase));

        if (!isValidStatus)
        {
            validationErrors[nameof(request.Status)] =
                ["Status must be Open or Completed."];
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors);
        }

        var task = await taskHandler.SetStatusAsync(
            ownerId,
            moduleId,
            taskId,
            status,
            cancellationToken);

        return task is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> DeleteAsync(
        Guid moduleId,
        Guid taskId,
        ClaimsPrincipal user,
        IStudyTaskHandler taskHandler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var wasDeleted = await taskHandler.DeleteAsync(
            ownerId,
            moduleId,
            taskId,
            cancellationToken);

        return wasDeleted
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> AcknowledgeSourceUpdateAsync(
        Guid moduleId,
        Guid taskId,
        ClaimsPrincipal user,
        IStudyTaskHandler taskHandler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var result = await taskHandler.AcknowledgeSourceUpdateAsync(
            ownerId,
            moduleId,
            taskId,
            cancellationToken);

        return result.Outcome switch
        {
            AcknowledgeSourceUpdateOutcome.NotFound =>
                Results.NotFound(),
            AcknowledgeSourceUpdateOutcome.TaskNotImported =>
                Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "The Study Task is not imported.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "task-not-imported"
                    }),
            AcknowledgeSourceUpdateOutcome.Succeeded =>
                Results.Ok(ToResponse(
                    result.Task
                    ?? throw new InvalidOperationException(
                        "A successful acknowledgement must return a Study Task."))),
            _ => throw new ArgumentOutOfRangeException()
        };
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
            task.UpdatedAtUtc,
            task.ImportSource is null
                ? null
                : new StudyTaskImportSourceResponse(
                    task.ImportSource.Status.ToString(),
                    task.ImportSource.ContentType?.ToString(),
                    task.ImportSource.MediaType,
                    task.ImportSource.SourceUrl,
                    task.ImportSource.HasSourceUpdate));
    }
}
