using System.ComponentModel.DataAnnotations;
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

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterUserRequest request,
        IUserHandler userHandler,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);

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

    private static Dictionary<string, string[]> Validate(
        RegisterUserRequest request)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(request);

        Validator.TryValidateObject(
            request,
            context,
            results,
            validateAllProperties: true);

        return results
            .SelectMany(
                result => result.MemberNames
                    .DefaultIfEmpty("request"),
                (result, memberName) => new
                {
                    MemberName = memberName,
                    Message = result.ErrorMessage
                        ?? "Invalid value."
                })
            .GroupBy(item => item.MemberName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => item.Message)
                    .ToArray());
    }
}
