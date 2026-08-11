using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StudyOrganizer.Api.Authentication;

public sealed class AuthorizationOperationFilter
    : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var metadata =
            context.ApiDescription
                .ActionDescriptor
                .EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any()
            || !metadata.OfType<IAuthorizeData>().Any())
        {
            return;
        }

        operation.Responses.TryAdd(
            StatusCodes.Status401Unauthorized.ToString(),
            new OpenApiResponse
            {
                Description = "Unauthorized"
            });

        operation.Security =
            new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    [
                        new OpenApiSecurityScheme
                        {
                            Reference =
                                new OpenApiReference
                                {
                                    Type =
                                        ReferenceType
                                            .SecurityScheme,
                                    Id = "Bearer"
                                }
                        }
                    ] = Array.Empty<string>()
                }
            };
    }
}
