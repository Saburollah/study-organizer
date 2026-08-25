using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StudyOrganizer.Api.Tests.ExternalCourses;

public sealed class CourseSubscriptionEndpointsTests
{
    private static readonly string SigningKey = new('a', 64);

    [Fact]
    public async Task CourseSubscriptionEndpoints_WithoutToken_ReturnUnauthorized()
    {
        var moduleId = Guid.NewGuid();
        var scanRunId = Guid.NewGuid();

        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        var requests = new[]
        {
            new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/modules/{moduleId}/course-subscription")
            {
                Content = JsonContent.Create(new
                {
                    courseUrl =
                        "https://example.test/mock-moodle/course/software-engineering"
                })
            },
            new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/modules/{moduleId}/course-subscription"),
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/modules/{moduleId}/course-subscription"),
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/modules/{moduleId}/course-subscription/scans"),
            new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/modules/{moduleId}/course-subscription/scans/{scanRunId}")
        };

        foreach (var request in requests)
        {
            using (request)
            using (var response = await client.SendAsync(request))
            {
                Assert.Equal(
                    HttpStatusCode.Unauthorized,
                    response.StatusCode);
            }
        }
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    "Host=localhost;Database=test;"
                    + "Username=test;Password=test");
                builder.UseSetting("Jwt:SigningKey", SigningKey);
            });
    }

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }
}
