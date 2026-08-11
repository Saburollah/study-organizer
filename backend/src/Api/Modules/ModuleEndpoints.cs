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

    private static ModuleResponse ToResponse(
        ModuleResult module)
    {
        return new ModuleResponse(
            module.Id,
            module.Name,
            module.Code,
            module.Description,
            module.Color,
            module.CreatedAt);
    }
}
