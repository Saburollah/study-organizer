using System.Security.Claims;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Validation;
using StudyOrganizer.Application.Modules;

namespace StudyOrganizer.Api.Modules;

public static class ModuleEndpoints
{
    public static RouteGroupBuilder MapModuleEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/modules")
            .WithTags("Modules")
            .RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .WithName("CreateModule")
            .Produces<ModuleResponse>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized);

        group.MapGet("/", GetByOwnerAsync)
            .WithName("GetModules")
            .Produces<IReadOnlyList<ModuleResponse>>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status401Unauthorized);

        group.MapPut("/{moduleId:guid}", UpdateAsync)
            .WithName("UpdateModule")
            .Produces<ModuleResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        group.MapDelete("/{moduleId:guid}", DeleteAsync)
            .WithName("DeleteModule")
            .Produces(
                StatusCodes.Status204NoContent)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);
        return group;
    }

    private static async Task<IResult> CreateAsync(
        CreateModuleRequest request,
        ClaimsPrincipal user,
        IModuleHandler moduleHandler,
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

        var module = await moduleHandler.CreateAsync(
            ownerId,
            request.Name,
            request.Code,
            request.Description,
            request.Color,
            cancellationToken);

        return Results.Json(
            ToResponse(module),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetByOwnerAsync(
        ClaimsPrincipal user,
        IModuleHandler moduleHandler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var modules =
            await moduleHandler.GetByOwnerAsync(
                ownerId,
                cancellationToken);

        return Results.Ok(
            modules.Select(ToResponse));
    }

    private static async Task<IResult> UpdateAsync(
        Guid moduleId,
        UpdateModuleRequest request,
        ClaimsPrincipal user,
        IModuleHandler moduleHandler,
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

        var module = await moduleHandler.UpdateAsync(
            ownerId,
            moduleId,
            request.Name,
            request.Code,
            request.Description,
            request.Color,
            cancellationToken);

        if (module is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(module));
    }

    private static async Task<IResult> DeleteAsync(
        Guid moduleId,
        ClaimsPrincipal user,
        IModuleHandler moduleHandler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var ownerId))
        {
            return Results.Unauthorized();
        }

        var result =
            await moduleHandler.DeleteAsync(
                ownerId,
                moduleId,
                cancellationToken);

        return result switch
        {
            ModuleDeleteOutcome.Deleted => Results.NoContent(),
            ModuleDeleteOutcome.LinkedToExternalCourse => Results.Problem(
                detail: "linked_external_course_module",
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.NotFound()
        };
    }

    private static ModuleResponse ToResponse(
        ModuleResult module)
    {
        return new ModuleResponse(
            module.Id,
            module.Name,
            module.Code,
            module.Description,
            module.Color,
            module.CreatedAt,
            module.IsExternalCourseLinked);
    }
}
