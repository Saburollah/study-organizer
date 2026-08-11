using StudyOrganizer.Api.Validation;
using StudyOrganizer.Application.Authentication;
using StudyOrganizer.Application.Users;

namespace StudyOrganizer.Api.Users;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterUser")
            .Produces<RegisterUserResponse>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest);

        group.MapPost("/login", LoginAsync)
            .WithName("LoginUser")
            .Produces<LoginUserResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterUserRequest request,
        IUserHandler userHandler,
        CancellationToken cancellationToken)
    {
        var validationErrors =
            RequestValidator.Validate(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors);
        }

        var result = await userHandler.RegisterAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["registration"] =
                        result.Errors.ToArray()
                });
        }

        if (result.UserId is not Guid userId)
        {
            throw new InvalidOperationException(
                "A successful registration must return a user ID.");
        }

        return Results.Json(
            new RegisterUserResponse(
                userId,
                request.Email),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> LoginAsync(
        LoginUserRequest request,
        IUserHandler userHandler,
        IAccessTokenService accessTokenService,
        CancellationToken cancellationToken)
    {
        var validationErrors =
            RequestValidator.Validate(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors);
        }

        var result = await userHandler.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Results.Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title: "Authentication failed.",
                detail: "Invalid email or password.");
        }

        if (result.UserId is not Guid userId
            || string.IsNullOrWhiteSpace(result.Email))
        {
            throw new InvalidOperationException(
                "A successful login must return user data.");
        }

        var accessToken = accessTokenService.Create(
            userId,
            result.Email);

        return Results.Ok(
            new LoginUserResponse(
                accessToken.Value,
                accessToken.ExpiresAtUtc));
    }
}
