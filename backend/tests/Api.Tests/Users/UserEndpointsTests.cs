using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyOrganizer.Api.Users;
using StudyOrganizer.Application.Users;

namespace StudyOrganizer.Api.Tests.Users;

public sealed class UserEndpointsTests
{
    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var userId = Guid.NewGuid();
        var handler = new StubUserHandler(
            new UserResult(
                true,
                userId,
                Array.Empty<string>()));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "new-user@example.com",
                password = "Registration-Test-2026"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                RegisterUserResponse>();

        Assert.NotNull(body);
        Assert.Equal(userId, body.UserId);
        Assert.Equal("new-user@example.com", body.Email);
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var handler = new StubUserHandler(
            new UserResult(
                true,
                Guid.NewGuid(),
                Array.Empty<string>()));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "invalid-email",
                password = "Registration-Test-2026"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task Register_WhenIdentityRejectsUser_ReturnsBadRequest()
    {
        var handler = new StubUserHandler(
            new UserResult(
                false,
                null,
                new[] { "Registration failed." }));

        using var factory = CreateFactory(handler);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "existing-user@example.com",
                password = "Registration-Test-2026"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.True(handler.WasCalled);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IUserHandler handler)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Database=test;"
            + "Username=test;Password=test");

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IUserHandler>();
                    services.AddSingleton(handler);
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

    private sealed class StubUserHandler(
        UserResult result)
        : IUserHandler
    {
        public bool WasCalled { get; private set; }

        public Task<UserResult> RegisterAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;

            return Task.FromResult(result);
        }
    }
}
