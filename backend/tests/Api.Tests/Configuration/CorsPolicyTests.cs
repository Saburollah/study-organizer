using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StudyOrganizer.Api.Tests.Configuration;

public sealed class CorsPolicyTests
{
    private const string AllowedOrigin =
        "http://localhost:5173";

    [Fact]
    public async Task Preflight_FromAllowedOrigin_ReturnsCorsHeaders()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        using var request = CreatePreflightRequest(AllowedOrigin);

        using var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues(
                "Access-Control-Allow-Origin",
                out var origins));

        Assert.Contains(AllowedOrigin, origins);
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_DoesNotReturnCorsHeader()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        using var request = CreatePreflightRequest(
            "https://unknown.example");

        using var response = await client.SendAsync(request);

        Assert.False(
            response.Headers.Contains(
                "Access-Control-Allow-Origin"));
    }

    private static HttpRequestMessage CreatePreflightRequest(
        string origin)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/auth/login");

        request.Headers.Add("Origin", origin);
        request.Headers.Add(
            "Access-Control-Request-Method",
            "POST");
        request.Headers.Add(
            "Access-Control-Request-Headers",
            "authorization,content-type");

        return request;
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Database=test;"
            + "Username=test;Password=test");

        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            new string('a', 64));

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration(
                    (_, configuration) =>
                    {
                        configuration.AddInMemoryCollection(
                            new Dictionary<string, string?>
                            {
                                [
                                    "Cors:AllowedOrigins:0"
                                ] = AllowedOrigin
                            });
                    });
            });
    }

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress =
                    new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }
}
