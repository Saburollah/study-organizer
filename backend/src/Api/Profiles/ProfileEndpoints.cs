using System.Security.Claims;
using StudyOrganizer.Api.Authentication;
using StudyOrganizer.Api.Validation;
using StudyOrganizer.Application.Profiles;
using StudyOrganizer.Domain.Users;

namespace StudyOrganizer.Api.Profiles;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", GetAsync)
            .WithName("GetProfile")
            .Produces<ProfileResponse>(
                StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/", UpdateAsync)
            .WithName("UpdateProfile")
            .Produces<ProfileResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> GetAsync(
        ClaimsPrincipal user,
        IProfileHandler profileHandler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return Results.Unauthorized();
        }

        var profile = await profileHandler.GetAsync(
            userId,
            cancellationToken);

        return profile is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(profile));
    }

    private static async Task<IResult> UpdateAsync(
        UpdateProfileRequest request,
        ClaimsPrincipal user,
        IProfileHandler profileHandler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return Results.Unauthorized();
        }

        var validationErrors =
            RequestValidator.Validate(request);

        if (request.DateOfBirth is DateOnly dateOfBirth
            && dateOfBirth > DateOnly.FromDateTime(
                DateTime.UtcNow))
        {
            validationErrors[nameof(request.DateOfBirth)] =
                ["Date of birth cannot be in the future."];
        }

        ProfileGender? gender = null;

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            var isValidGender =
                Enum.TryParse<ProfileGender>(
                    request.Gender,
                    ignoreCase: true,
                    out var parsedGender)
                && Enum.IsDefined(parsedGender)
                && Enum.GetNames<ProfileGender>().Any(name =>
                    string.Equals(
                        name,
                        request.Gender,
                        StringComparison.OrdinalIgnoreCase));

            if (!isValidGender)
            {
                validationErrors[nameof(request.Gender)] =
                [
                    "Gender must be Female, Male "
                    + "or PreferNotToSay."
                ];
            }
            else
            {
                gender = parsedGender;
            }
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(
                validationErrors);
        }

        var profile = await profileHandler.UpdateAsync(
            userId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            gender,
            cancellationToken);

        return profile is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(profile));
    }

    private static ProfileResponse ToResponse(
        ProfileResult profile)
    {
        return new ProfileResponse(
            profile.UserId,
            profile.Email,
            profile.FirstName,
            profile.LastName,
            profile.DateOfBirth,
            profile.Gender?.ToString());
    }
}
